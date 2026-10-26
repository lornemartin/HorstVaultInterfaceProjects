using System.Globalization;
using HorstMFG.Core.Entities;
using HorstMFG.Core.Enums;
using HorstMFG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Interactive;
using Syncfusion.Pdf.Parsing;

namespace HorstMFG.Web.Services;

/// <summary>Which import record an operation report prints from.</summary>
public enum ReportSource { Batch, Schedule }

public class ReportService
{
    /// <summary>A generated report plus the file name to serve it under.</summary>
    public sealed record ReportPdf(byte[] Bytes, string FileName);

    /// <summary>One thickness / struct-code group, for the print dialog.</summary>
    public sealed record OperationGroupCount(string Key, int PartCount);
    /// <summary>
    /// Groups is empty for operations that don't split (Purchased). SecondOps counts every part of
    /// the operation before the 2nd Op filter, so the dialog can always list them all.
    /// </summary>
    public sealed record OperationGroupSummary(int PartCount, IReadOnlyList<OperationGroupCount> Groups,
        IReadOnlyList<OperationGroupCount> SecondOps);

    /// <summary>Group for parts with a blank thickness / struct code.</summary>
    public const string NoneGroup = "(none)";

    private readonly ApplicationDbContext _db;
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ILogger<ReportService> _log;
    private readonly string _localPdfPath;

    public ReportService(ApplicationDbContext db, IDbContextFactory<ApplicationDbContext> dbFactory,
        ILogger<ReportService> log, IConfiguration config)
    {
        _db = db;
        _dbFactory = dbFactory;
        _log = log;
        _localPdfPath = config["FileSystemPaths:LocalPdfPath"] ?? @"C:\HorstMFG\PDFs\";
    }

    // ── Schedule (Schedule → ScheduleOrder → PartLineItem) ───────────────────

    /// <summary>
    /// Prints every part regardless of plant, since the traveller belongs to the plant doing the
    /// assembly. Parts made at another plant get a "MADE AT …" banner on their drawing pages and
    /// are listed on the product cover under "Arriving from …".
    /// </summary>
    public async Task<ReportPdf?> GenerateScheduleShopTravellerAsync(int scheduleId)
    {
        var schedule = await _db.Schedules
            .Include(s => s.ScheduleOrders)
                .ThenInclude(so => so.Parts)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);
        if (schedule == null) return null;
        var scheduleName = schedule.Name;

        var plantNames = await _db.Plants.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.Name);

        var pdfFolder = schedule.LocalPdfFolder
            ?? Path.Combine(_localPdfPath, "Schedules", scheduleName);

        // Latest handoff per order, for the status line under each cross-plant banner.
        var orderIds = schedule.ScheduleOrders.Select(so => so.Id).ToList();
        var handoffStatus = (await _db.ScheduleOrderHandoffs.AsNoTracking()
                .Where(h => orderIds.Contains(h.ScheduleOrderId))
                .ToListAsync())
            .GroupBy(h => h.ScheduleOrderId)
            .ToDictionary(g => g.Key, g => g.MaxBy(h => h.Id)!.Status);

        // Flatten schedule orders; group by the assembly PartNumber (= product key).
        // This grouping/lookup drives which physical drawing gets attached — left exactly
        // as-is (it's the proven-working path). Only the COVER PAGE's displayed number/title/
        // description is resolved separately below, from the true top-level item.
        var allOrders = schedule.ScheduleOrders
            .Select(so => new
            {
                so.OrderNumber,
                Qty   = so.Qty,
                ProductNumber = so.ProductNumber,
                ProductPart = so.Parts.FirstOrDefault(p => p.Category.Equals("Product", StringComparison.OrdinalIgnoreCase)),
                Parts = so.Parts.Where(p => !p.IsStock).ToList(),
            })
            .ToList();

        var products = new List<TravellerProduct>();

        foreach (var pg in allOrders.GroupBy(o =>
            o.Parts
                .FirstOrDefault(p => p.Category.Equals("Assembly", StringComparison.OrdinalIgnoreCase))
                ?.PartNumber ?? o.OrderNumber))
        {
            var assemblyPart = pg
                .SelectMany(o => o.Parts)
                .FirstOrDefault(p => p.Category.Equals("Assembly", StringComparison.OrdinalIgnoreCase));

            // Cover page identity: prefer the explicit "Product" category row (the true
            // top-level item), then ScheduleOrder.ProductNumber for at least the number,
            // and only fall back to the attached sub-assembly's own info as a last resort
            // when neither is available.
            var topLevelPart = pg.Select(o => o.ProductPart).FirstOrDefault(p => p != null);
            var topLevelNumber = pg.Select(o => o.ProductNumber).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));

            string coverNumber;
            string? coverTitle;
            string? coverDescription;
            if (topLevelPart != null)
            {
                coverNumber      = topLevelNumber ?? topLevelPart.PartNumber;
                coverTitle       = topLevelPart.Title;
                coverDescription = topLevelPart.Description;
            }
            else if (topLevelNumber != null)
            {
                coverNumber      = topLevelNumber;
                coverTitle       = null;
                coverDescription = null;
            }
            else
            {
                coverNumber      = pg.Key;
                coverTitle       = assemblyPart?.Title;
                coverDescription = assemblyPart?.Description;
            }

            var orders = pg.Select(o => (o.OrderNumber, o.Qty)).ToList();

            // Every Category="Assembly" row gets its own drawing page — an order commonly has
            // several (a top assembly plus nested sub-assemblies), and previously only the
            // first one found ever got printed, silently dropping the rest.
            var assemblies = pg
                .SelectMany(o => o.Parts
                    .Where(p => p.Category.Equals("Assembly", StringComparison.OrdinalIgnoreCase))
                    .Select(p => new PartOrderQty(p, o.OrderNumber, o.Qty)))
                .GroupBy(x => x.Part.PartNumber)
                // Preserve BOM order: PartLineItem.Id is assigned in the same sequence the
                // Vault BOM lines were ingested in, since no separate BOM-position column is
                // persisted. Using each group's earliest Id keeps drawings in BOM order rather
                // than alphabetical.
                .OrderBy(g => g.Min(x => x.Part.Id))
                .Select(g => BuildTravellerComponent(g, pdfFolder, plantNames, schedule.PlantId, handoffStatus))
                .ToList();

            var components = pg
                .SelectMany(o => o.Parts
                    .Where(p => !p.Category.Equals("Assembly", StringComparison.OrdinalIgnoreCase) &&
                                p.Operations != null &&
                                (p.Operations.Contains("bandsaw",     StringComparison.OrdinalIgnoreCase) ||
                                 p.Operations.Contains("iron worker", StringComparison.OrdinalIgnoreCase)))
                    .Select(p => new PartOrderQty(p, o.OrderNumber, o.Qty)))
                .GroupBy(x => x.Part.PartNumber)
                .Select(g => BuildTravellerComponent(g, pdfFolder, plantNames, schedule.PlantId, handoffStatus))
                .OrderBy(c => c.StructCode).ThenBy(c => c.Material)
                .ToList();

            if (!assemblies.Any() && !components.Any()) continue;

            // Every non-stock part made elsewhere, not just the ones that get a drawing page,
            // so the assembler sees everything to wait for.
            var arriving = pg
                .SelectMany(o => o.Parts
                    .Where(p => !p.Category.Equals("Product", StringComparison.OrdinalIgnoreCase))
                    .Select(p => new PartOrderQty(p, o.OrderNumber, o.Qty)))
                .Select(x => (x.Part, x.Qty, MadeAt: MadeAtPlantId(x.Part, schedule.PlantId)))
                .Where(x => x.MadeAt is int id && id != schedule.PlantId)
                .GroupBy(x => (x.Part.PartNumber, MadeAt: x.MadeAt!.Value))
                .Select(g => new ArrivingPart(g.Key.PartNumber, g.First().Part.Title,
                    PlantName(g.Key.MadeAt, plantNames), g.Sum(x => x.Part.Qty * x.Qty)))
                .OrderBy(a => a.MadeAtPlant).ThenBy(a => a.PartNumber)
                .ToList();

            products.Add(new TravellerProduct
            {
                ProductKey         = pg.Key,
                CoverNumber        = coverNumber,
                CoverTitle         = coverTitle,
                CoverDescription   = coverDescription,
                Orders             = orders,
                Assemblies         = assemblies,
                Components         = components,
                Arriving           = arriving,
            });
        }

        // Cross-references: partNumber → all (product, component) pairs that use it
        // (covers both assembly drawings and fabricated components).
        var partToComponents = new Dictionary<string, List<(TravellerProduct Product, TravellerComponent Component)>>(StringComparer.OrdinalIgnoreCase);
        foreach (var product in products)
            foreach (var comp in product.Assemblies.Concat(product.Components))
            {
                if (!partToComponents.TryGetValue(comp.PartNumber, out var list))
                    partToComponents[comp.PartNumber] = list = [];
                list.Add((product, comp));
            }

        foreach (var product in products)
            foreach (var comp in product.Assemblies.Concat(product.Components))
                if (partToComponents.TryGetValue(comp.PartNumber, out var entries))
                    comp.AlsoInProducts = entries
                        .Where(e => e.Product.ProductKey != product.ProductKey)
                        .SelectMany(e => e.Component.OrderBreakdown.Select(ob =>
                            new AlsoInEntry(BuildProductLabel(e.Product), ob.OrderNumber, ob.Qty)))
                        .ToList();

        if (!products.Any())
        {
            _log.LogWarning("No Shop Traveller content found for schedule '{Name}'", scheduleName);
            return null;
        }

        _log.LogInformation("Generating Shop Traveller for schedule '{Name}': {Count} products",
            scheduleName, products.Count);
        return new ReportPdf(BuildTravellerPdf(products, scheduleName, _log), $"ShopTraveller-{scheduleName}.pdf");
    }

    // ── Operation drawing sets (Laser / Iron Worker / Machine Shop / Bandsaw / Purchased) ──

    /// <summary>
    /// Prints the parts made at <paramref name="plantId"/> (null = every plant) for one operation.
    /// With <paramref name="secondOp"/>, only parts with that 2nd Operation print; with
    /// <paramref name="groupKeys"/>, only those thickness / struct-code groups.
    /// </summary>
    public async Task<ReportPdf?> GenerateOperationReportAsync(ReportSource source, int id, string operation,
        int? plantId, IReadOnlyCollection<string>? groupKeys, string? secondOp)
    {
        var loaded = await LoadOperationPartsAsync(_db, source, id, operation, plantId);
        if (loaded == null) return null;

        var parts = FilterSecondOp(loaded.Parts, secondOp);
        if (groupKeys is { Count: > 0 } && IsGroupedOperation(operation))
            parts = parts.Where(p => groupKeys.Contains(GroupKey(operation, p))).ToList();

        var kind = source == ReportSource.Batch ? "Batch" : "Schedule";
        if (parts.Count == 0)
        {
            _log.LogWarning("No '{Op}' parts found for {Kind} '{Name}', plant {Plant}", operation, kind, loaded.SourceName, plantId);
            return null;
        }

        _log.LogInformation("Generating '{Op}' report for {Kind} '{Name}', plant {Plant}: {Count} parts",
            operation, kind, loaded.SourceName, plantId, parts.Count);
        var secondOpSuffix = secondOp == null ? "" : "-" + SafeFileNamePart(secondOp);
        var groupSuffix = groupKeys is { Count: 1 } ? "-" + SafeFileNamePart(groupKeys.First()) : "";
        return new ReportPdf(
            BuildReport(parts, loaded.SourceName, isSchedule: source == ReportSource.Schedule, operation, _log, secondOp),
            $"{operation}-{kind}-{loaded.SourceName}{PlantFileSuffix(plantId, loaded.Plants)}{secondOpSuffix}{groupSuffix}.pdf");
    }

    /// <summary>Part counts per thickness / struct-code group, in print order, for the print dialog.</summary>
    public async Task<OperationGroupSummary> GetOperationGroupsAsync(ReportSource source, int id, string operation,
        int plantId, string? secondOp)
    {
        // Own context: called from a Blazor circuit, where the scoped one lives as long as the page.
        await using var db = await _dbFactory.CreateDbContextAsync();
        var loaded = await LoadOperationPartsAsync(db, source, id, operation, plantId);
        if (loaded == null) return new OperationGroupSummary(0, [], []);
        var secondOps = loaded.Parts.GroupBy(SecondOpKey)
            .OrderBy(g => g.Key == NoneGroup).ThenBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new OperationGroupCount(g.Key, g.Count())).ToList();
        var parts = FilterSecondOp(loaded.Parts, secondOp);
        var groups = IsGroupedOperation(operation)
            ? parts.GroupBy(p => GroupKey(operation, p))
                .Select(g => new OperationGroupCount(g.Key, g.Count())).ToList()
            : [];
        return new OperationGroupSummary(parts.Count, groups, secondOps);
    }

    private sealed record OperationParts(string SourceName, List<LaserPartGroup> Parts, List<Plant> Plants);

    /// <summary>
    /// One entry per part number for the operation, sorted for printing. Batches print stock
    /// (MTS) parts and Schedules print non-stock (MTO) parts.
    /// </summary>
    private async Task<OperationParts?> LoadOperationPartsAsync(ApplicationDbContext db,
        ReportSource source, int id, string operation, int? plantId)
    {
        var plants     = await db.Plants.AsNoTracking().ToListAsync();
        var plantNames = plants.ToDictionary(p => p.Id, p => p.Name);

        string name, pdfFolder;
        List<(PartLineItem Part, string Label, int ParentQty)> rows;
        if (source == ReportSource.Batch)
        {
            var batch = await db.Batches.AsNoTracking()
                .Include(b => b.BatchProducts)
                    .ThenInclude(bp => bp.Parts)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (batch == null) return null;
            name      = batch.Name;
            pdfFolder = batch.LocalPdfFolder ?? Path.Combine(_localPdfPath, "Batches", name);
            rows = batch.BatchProducts
                .SelectMany(bp => bp.Parts
                    .Where(p => p.IsStock && (plantId == null || MadeAtPlantId(p, batch.PlantId) == plantId))
                    .Select(p => (p, bp.ProductName ?? "", bp.Qty)))
                .ToList();
        }
        else
        {
            var schedule = await db.Schedules.AsNoTracking()
                .Include(s => s.ScheduleOrders)
                    .ThenInclude(so => so.Parts)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (schedule == null) return null;
            name      = schedule.Name;
            pdfFolder = schedule.LocalPdfFolder ?? Path.Combine(_localPdfPath, "Schedules", name);
            rows = schedule.ScheduleOrders
                .SelectMany(so => so.Parts
                    .Where(p => !p.IsStock && (plantId == null || MadeAtPlantId(p, schedule.PlantId) == plantId))
                    .Select(p => (p, so.OrderNumber ?? "", so.Qty)))
                .ToList();
        }

        var parts = rows
            .Where(x => x.Part.Operations != null &&
                        x.Part.Operations.Contains(operation, StringComparison.OrdinalIgnoreCase))
            .GroupBy(x => x.Part.PartNumber)
            .Select(g => BuildGroup(g.Key,
                g.First().Part.Title,
                g.First().Part.Description,
                g.First().Part.Thickness,
                g.First().Part.Material,
                g.First().Part.StructCode,
                g.First().Part.IsStock,
                ResolvePlantDisplay(g.First().Part.PlantId, g.First().Part.PlantIdRaw, plantNames),
                g.GroupBy(x => x.Label)
                 .Select(og => (og.Key, og.Sum(x => x.Part.Qty * x.ParentQty)))
                 .OrderBy(o => o.Key)
                 .ToList(),
                Path.Combine(pdfFolder, g.Key + ".pdf"),
                secondOperation: g.First().Part.SecondOperation))
            .ToList();

        return new OperationParts(name, SortGroups(parts, operation), plants);
    }

    // ── Per-order category reports (one ScheduleOrder's own Sub-Assembly / Part rows) ──

    public async Task<byte[]?> GenerateOrderSubAssembliesReportAsync(int scheduleOrderId, bool hideStock)
    {
        var order = await _db.ScheduleOrders
            .Include(so => so.Schedule)
            .Include(so => so.Parts)
            .FirstOrDefaultAsync(so => so.Id == scheduleOrderId);
        if (order == null) return null;

        var plantNames = await _db.Plants.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.Name);

        var pdfFolder = order.Schedule.LocalPdfFolder
            ?? Path.Combine(_localPdfPath, "Schedules", order.Schedule.Name);

        var groups = BuildGridOrderedGroups(
            order.Parts.Where(p => p.Category.Equals("Assembly", StringComparison.OrdinalIgnoreCase)
                                 && (!hideStock || !p.IsStock)),
            order.OrderNumber, pdfFolder, plantNames);

        if (groups.Count == 0)
        {
            _log.LogWarning("No sub-assemblies found for order '{Order}'", order.OrderNumber);
            return null;
        }

        _log.LogInformation("Generating Sub-Assemblies report for order '{Order}': {Count} parts", order.OrderNumber, groups.Count);
        return BuildReport(groups, order.OrderNumber, isSchedule: true, "Sub-Assemblies", _log);
    }

    public async Task<byte[]?> GenerateOrderAssembliesAndPartsReportAsync(int scheduleOrderId, bool hideStock)
    {
        var order = await _db.ScheduleOrders
            .Include(so => so.Schedule)
            .Include(so => so.Parts)
            .FirstOrDefaultAsync(so => so.Id == scheduleOrderId);
        if (order == null) return null;

        var plantNames = await _db.Plants.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.Name);

        var pdfFolder = order.Schedule.LocalPdfFolder
            ?? Path.Combine(_localPdfPath, "Schedules", order.Schedule.Name);

        var groups = BuildGridOrderedGroups(
            order.Parts.Where(p => (p.Category.Equals("Assembly", StringComparison.OrdinalIgnoreCase) ||
                                     p.Category.Equals("Part", StringComparison.OrdinalIgnoreCase))
                                 && (!hideStock || !p.IsStock)),
            order.OrderNumber, pdfFolder, plantNames);

        if (groups.Count == 0)
        {
            _log.LogWarning("No assemblies/parts found for order '{Order}'", order.OrderNumber);
            return null;
        }

        _log.LogInformation("Generating Assemblies & Parts report for order '{Order}': {Count} parts", order.OrderNumber, groups.Count);
        return BuildReport(groups, order.OrderNumber, isSchedule: true, "Assemblies & Parts", _log);
    }

    /// <summary>
    /// Groups/sorts the same way the Orders grid does on screen (FlatSchedulePartRow's
    /// CategoryOrder/ThicknessOrder + the grid's GridSortColumns: CategoryOrder, IsStock,
    /// Operations, ThicknessOrder) so a printed report matches what the user sees.
    /// </summary>
    private static List<LaserPartGroup> BuildGridOrderedGroups(
        IEnumerable<PartLineItem> parts, string orderLabel, string pdfFolder,
        IReadOnlyDictionary<int, string> plantNames)
    {
        return parts
            .GroupBy(p => p.PartNumber)
            .Select(g =>
            {
                var first = g.First();
                return new
                {
                    CategoryOrder = first.Category.ToLowerInvariant() switch
                    {
                        "product"  => 0,
                        "assembly" => 1,
                        "part"     => 2,
                        _          => 3,
                    },
                    first.IsStock,
                    first.Operations,
                    ThicknessOrder = ParseThicknessOrder(first.Thickness),
                    Group = BuildGroup(g.Key,
                        first.Title, first.Description, first.Thickness, first.Material, first.StructCode,
                        first.IsStock, ResolvePlantDisplay(first.PlantId, first.PlantIdRaw, plantNames),
                        new List<(string Label, int Qty)> { (orderLabel, g.Sum(p => p.Qty)) },
                        Path.Combine(pdfFolder, g.Key + ".pdf")),
                };
            })
            .OrderBy(x => x.CategoryOrder)
            .ThenBy(x => x.IsStock)
            .ThenBy(x => x.Operations, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.ThicknessOrder)
            .Select(x => x.Group)
            .ToList();
    }

    /// <summary>Mirrors FlatSchedulePartRow.ThicknessOrder — handles plain numbers and "num/den" fractions.</summary>
    private static double ParseThicknessOrder(string? thickness)
    {
        if (string.IsNullOrWhiteSpace(thickness)) return double.MaxValue;
        if (double.TryParse(thickness, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            return d;
        var slash = thickness.IndexOf('/');
        if (slash > 0
            && double.TryParse(thickness.AsSpan(0, slash).Trim(), out var num)
            && double.TryParse(thickness.AsSpan(slash + 1).Trim(), out var den)
            && den != 0)
            return num / den;
        return double.MaxValue;
    }

    // ── Batches (Batch → BatchProduct → PartLineItem) ────────────────────────

    /// <summary>
    /// Prints every part across a schedule whose Plant value is a custom, unresolved entry
    /// (typed via the "Other…" option on the Orders grid rather than picked from the Plant
    /// list) matching the given raw text exactly — e.g. "Order From Plant 2". Spans every
    /// category/operation, sorted by part number.
    /// </summary>
    public async Task<byte[]?> GenerateScheduleCustomPlantReportAsync(string scheduleName, string plantValue, bool hideStock)
    {
        var schedules = await _db.Schedules
            .Where(s => s.Name == scheduleName)
            .Include(s => s.ScheduleOrders)
                .ThenInclude(so => so.Parts)
            .ToListAsync();

        if (schedules.Count == 0) return null;

        var pdfFolder = schedules[0].LocalPdfFolder
            ?? Path.Combine(_localPdfPath, "Schedules", scheduleName);

        var groups = schedules
            .SelectMany(s => s.ScheduleOrders)
            .SelectMany(so => so.Parts
                .Where(p => p.PlantId == null && p.PlantIdRaw == plantValue && (!hideStock || !p.IsStock))
                .Select(p => new { Part = p, Label = so.OrderNumber, ParentQty = so.Qty }))
            .GroupBy(x => x.Part.PartNumber)
            .Select(g => BuildGroup(g.Key,
                g.First().Part.Title,
                g.First().Part.Description,
                g.First().Part.Thickness,
                g.First().Part.Material,
                g.First().Part.StructCode,
                g.First().Part.IsStock,
                plantValue,
                g.GroupBy(x => x.Label)
                 .Select(og => (og.Key, og.Sum(x => x.Part.Qty * x.ParentQty)))
                 .OrderBy(o => o.Key)
                 .ToList(),
                Path.Combine(pdfFolder, g.Key + ".pdf")))
            .OrderBy(g => g.PartNumber)
            .ToList();

        if (groups.Count == 0)
        {
            _log.LogWarning("No parts with custom plant '{Plant}' found for schedule '{Name}'", plantValue, scheduleName);
            return null;
        }

        _log.LogInformation("Generating custom-plant report for schedule '{Name}', plant '{Plant}': {Count} parts",
            scheduleName, plantValue, groups.Count);
        return BuildReport(groups, scheduleName, isSchedule: true, "Custom Report", _log);
    }

    /// <summary>
    /// Prints every part across a batch whose Plant value is a custom, unresolved entry
    /// (typed via the "Other…" option on the Batches grid rather than picked from the Plant
    /// list) matching the given raw text exactly — e.g. "Order From Plant 2". Spans every
    /// category/operation, sorted by part number.
    /// </summary>
    public async Task<byte[]?> GenerateBatchCustomPlantReportAsync(string batchName, string plantValue, bool hideNonStock)
    {
        var batches = await _db.Batches
            .Where(b => b.Name == batchName)
            .Include(b => b.BatchProducts)
                .ThenInclude(bp => bp.Parts)
            .ToListAsync();

        if (batches.Count == 0) return null;

        var pdfFolder = batches[0].LocalPdfFolder
            ?? Path.Combine(_localPdfPath, "Batches", batchName);

        var groups = batches
            .SelectMany(b => b.BatchProducts)
            .SelectMany(bp => bp.Parts
                .Where(p => p.PlantId == null && p.PlantIdRaw == plantValue && (!hideNonStock || p.IsStock))
                .Select(p => new { Part = p, Label = bp.ProductName, ParentQty = bp.Qty }))
            .GroupBy(x => x.Part.PartNumber)
            .Select(g => BuildGroup(g.Key,
                g.First().Part.Title,
                g.First().Part.Description,
                g.First().Part.Thickness,
                g.First().Part.Material,
                g.First().Part.StructCode,
                g.First().Part.IsStock,
                plantValue,
                g.GroupBy(x => x.Label)
                 .Select(og => (og.Key, og.Sum(x => x.Part.Qty * x.ParentQty)))
                 .OrderBy(o => o.Key)
                 .ToList(),
                Path.Combine(pdfFolder, g.Key + ".pdf")))
            .OrderBy(g => g.PartNumber)
            .ToList();

        if (groups.Count == 0)
        {
            _log.LogWarning("No parts with custom plant '{Plant}' found for batch '{Name}'", plantValue, batchName);
            return null;
        }

        _log.LogInformation("Generating custom-plant report for batch '{Name}', plant '{Plant}': {Count} parts",
            batchName, plantValue, groups.Count);
        return BuildReport(groups, batchName, isSchedule: false, "Custom Report", _log);
    }

    // ── Per-operation sort ────────────────────────────────────────────────────

    private static List<LaserPartGroup> SortGroups(List<LaserPartGroup> groups, string operation)
        => operation.ToLowerInvariant() switch
        {
            // Group key in the sort keeps each exact label contiguous: "3/16" and "10 GA" both parse
            // to MaxValue and would otherwise interleave by material.
            "laser" or "iron worker"    => [.. groups.OrderBy(g => GroupKey(operation, g) == NoneGroup)
                                                     .ThenBy(g => ParseThickness(g.Thickness))
                                                     .ThenBy(g => GroupKey(operation, g), StringComparer.Ordinal)
                                                     .ThenBy(g => g.Material)],
            "machine shop" or "bandsaw" => [.. groups.OrderBy(g => GroupKey(operation, g) == NoneGroup)
                                                     .ThenBy(g => GroupKey(operation, g), StringComparer.Ordinal)
                                                     .ThenBy(g => g.Material)],
            _                           => [.. groups.OrderBy(g => g.PartNumber)],
        };

    /// <summary>Laser and Iron Worker split by thickness, Machine Shop and Bandsaw by struct code.</summary>
    public static bool IsGroupedOperation(string operation) =>
        operation.ToLowerInvariant() is "laser" or "iron worker" or "machine shop" or "bandsaw";

    public static string GroupLabel(string operation) =>
        operation.ToLowerInvariant() is "laser" or "iron worker" ? "Thickness" : "Struct Code";

    /// <summary>The part's thickness or struct code exactly as imported (trimmed), or NoneGroup when blank.</summary>
    private static string GroupKey(string operation, LaserPartGroup part)
    {
        var value = operation.ToLowerInvariant() is "laser" or "iron worker" ? part.Thickness : part.StructCode;
        return string.IsNullOrWhiteSpace(value) ? NoneGroup : value.Trim();
    }

    /// <summary>The part's 2nd Operation exactly as imported (trimmed), or NoneGroup when blank.</summary>
    private static string SecondOpKey(LaserPartGroup part) =>
        string.IsNullOrWhiteSpace(part.SecondOperation) ? NoneGroup : part.SecondOperation.Trim();

    /// <summary>Null secondOp = any 2nd Operation.</summary>
    private static List<LaserPartGroup> FilterSecondOp(List<LaserPartGroup> parts, string? secondOp) =>
        secondOp == null ? parts : parts.Where(p => SecondOpKey(p) == secondOp).ToList();

    private static string SafeFileNamePart(string value) =>
        string.Concat(value.Select(c => Path.GetInvalidFileNameChars().Contains(c) || c == '"' ? '_' : c));

    // ── Per-operation report builder ──────────────────────────────────────────

    private static byte[] BuildReport(List<LaserPartGroup> groups, string sourceName,
        bool isSchedule, string operation, ILogger log, string? secondOp = null)
        => operation.ToLowerInvariant() switch
        {
            // Add cases here for operations that need a different layout, e.g.:
            // "purchased" => BuildTablePdf(groups, sourceName, isSchedule, log),
            _ => BuildLaserPdf(groups, sourceName, isSchedule, operation, secondOp, log),
        };

    // ── PDF assembly ─────────────────────────────────────────────────────────

    private static byte[] BuildLaserPdf(List<LaserPartGroup> parts, string sourceName, bool isSchedule,
        string operation, string? secondOp, ILogger log)
    {
        var output = new PdfDocument();
        output.PageSettings.Size = PdfPageSize.Letter;
        output.PageSettings.Margins.All = 0;

        // parts arrive sorted by SortGroups, so each group is contiguous.
        var sections = IsGroupedOperation(operation)
            ? parts.GroupBy(p => (string?)GroupKey(operation, p)).ToList()
            : parts.GroupBy(_ => (string?)null).ToList();
        bool separators = sections.Count > 1;

        foreach (var section in sections)
        {
            int firstPage = output.Pages.Count;
            int count = section.Count();
            if (separators)
                AppendSeparatorPage(output, section.Key!, count, sourceName, isSchedule, operation, secondOp);

            foreach (var part in section)
                AppendPartPages(output, part, sourceName, isSchedule, operation);

            if (section.Key != null && output.Pages.Count > firstPage)
            {
                var bookmark = output.Bookmarks.Add($"{GroupLabel(operation)} {section.Key} — {count} part{(count != 1 ? "s" : "")}");
                bookmark.Destination = new PdfDestination(output.Pages[firstPage]);
            }
        }

        using var ms = new MemoryStream();
        output.Save(ms);
        output.Close();
        return ms.ToArray();
    }

    private static void AppendPartPages(PdfDocument output, LaserPartGroup part, string sourceName, bool isSchedule, string operation)
    {
        if (part.PdfPath is null)
        {
            AppendTextOnlyPage(output, part, sourceName, isSchedule, operation);
            return;
        }

        byte[] srcBytes = File.ReadAllBytes(part.PdfPath);
        var srcDoc = new PdfLoadedDocument(srcBytes);
        try
        {
            if (srcDoc.Pages.Count == 0) { AppendTextOnlyPage(output, part, sourceName, isSchedule, operation); return; }

            // Each source page becomes its own duplex sheet: drawing (front) + cover (back).
            // A multi-page drawing (e.g. a two-sheet assembly) needs one cover PER page, not
            // one cover after all pages — otherwise duplex printing pairs the wrong pages
            // together. Mirrors TravellerAppendDrawingPages.
            for (int i = 0; i < srcDoc.Pages.Count; i++)
            {
                var srcPage = srcDoc.Pages[i] as PdfLoadedPage;
                if (srcPage == null) continue;

                // Build template before importing so it can be drawn onto the cover page.
                var template = srcPage.CreateTemplate();

                // /Rotate is NOT incorporated into the template dimensions, so we must
                // swap width/height when the page is stored rotated 90° or 270°.
                bool rotated = srcPage.Rotation == PdfPageRotateAngle.RotateAngle90 ||
                               srcPage.Rotation == PdfPageRotateAngle.RotateAngle270;
                bool isLandscape = rotated ? template.Height > template.Width
                                           : template.Width  > template.Height;

                // Front: full-size drawing page
                output.ImportPage(srcDoc, i);

                // Back: cover page with thumbnail + qty breakdown
                var cover = output.Pages.Add();
                DrawCoverPage(cover, template, rotated, isLandscape, part, sourceName, isSchedule, operation);
            }
        }
        finally
        {
            srcDoc.Close();
        }
    }

    /// <summary>Divider sheet at the start of each thickness / struct-code group, plus a blank back for duplex.</summary>
    private static void AppendSeparatorPage(PdfDocument output, string groupKey, int partCount,
        string sourceName, bool isSchedule, string operation, string? secondOp)
    {
        var page   = output.Pages.Add();
        var g      = page.Graphics;
        float pw   = page.GetClientSize().Width;
        float ph   = page.GetClientSize().Height;
        float margin = 50f;

        var fontHeader = new PdfStandardFont(PdfFontFamily.Helvetica,  9);
        var fontLabel  = new PdfStandardFont(PdfFontFamily.Helvetica, 14);
        var fontCount  = new PdfStandardFont(PdfFontFamily.Helvetica, 12);
        var grayBrush  = new PdfSolidBrush(new PdfColor(130, 130, 130));
        var grayPen    = new PdfPen(new PdfColor(200, 200, 200), 1f);

        var source = $"{(isSchedule ? "Schedule" : "Batch")} {sourceName}";
        var header = secondOp == null
            ? operation.ToUpperInvariant()
            : $"{operation.ToUpperInvariant()} · 2ND OP {secondOp.ToUpperInvariant()}";
        g.DrawString(header, fontHeader, grayBrush, new PointF(margin, margin));
        g.DrawString(source, fontHeader, grayBrush, new PointF(pw - margin - fontHeader.MeasureString(source).Width, margin));
        g.DrawLine(grayPen, new PointF(margin, margin + 16f), new PointF(pw - margin, margin + 16f));

        // Struct codes arrive as "HS-1141140188     1-1/4 X 1-1/4 X 0.188 HOLLOW STRUCTURAL": code on the
        // first line, description underneath. Either line shrinks to fit between the margins.
        var keyParts = groupKey.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries);
        var keyLines = new List<(string, PdfFont, PdfBrush)>
            { (keyParts[0], FitFont(keyParts[0], 40f, pw - 2 * margin), PdfBrushes.Black) };
        if (keyParts.Length > 1)
            keyLines.Add((keyParts[1].Trim(), FitFont(keyParts[1].Trim(), 24f, pw - 2 * margin), PdfBrushes.Black));

        float cy = ph * 0.38f;
        foreach (var (text, font, brush) in new[] { (GroupLabel(operation), (PdfFont)fontLabel, (PdfBrush)grayBrush) }
                     .Concat(keyLines)
                     .Append(($"{partCount} part{(partCount != 1 ? "s" : "")}", fontCount, grayBrush)))
        {
            var size = font.MeasureString(text);
            g.DrawString(text, font, brush, new PointF((pw - size.Width) / 2f, cy));
            cy += size.Height + 12f;
        }

        output.Pages.Add(); // blank back for duplex alignment
    }

    /// <summary>Bold Helvetica at <paramref name="size"/>, stepped down until <paramref name="text"/> fits <paramref name="maxWidth"/>.</summary>
    private static PdfFont FitFont(string text, float size, float maxWidth)
    {
        var font = new PdfStandardFont(PdfFontFamily.Helvetica, size, PdfFontStyle.Bold);
        while (size > 10f && font.MeasureString(text).Width > maxWidth)
            font = new PdfStandardFont(PdfFontFamily.Helvetica, size -= 2f, PdfFontStyle.Bold);
        return font;
    }

    /// <summary>
    /// Part has no local PDF — single page with a "No Drawing" placeholder box plus
    /// the same details block, followed by a blank back for duplex alignment. Mirrors
    /// TravellerAppendTextOnlyPage/TravellerDrawTextOnlyPage.
    /// </summary>
    private static void AppendTextOnlyPage(PdfDocument output, LaserPartGroup part, string sourceName, bool isSchedule, string operation)
    {
        DrawTextOnlyPage(output.Pages.Add(), part, sourceName, isSchedule, operation);
        output.Pages.Add(); // blank back for duplex alignment
    }

    private static void DrawTextOnlyPage(PdfPage page, LaserPartGroup part, string sourceName, bool isSchedule, string operation)
    {
        var g      = page.Graphics;
        float pw   = page.GetClientSize().Width;
        float ph   = page.GetClientSize().Height;
        float margin = 30f;
        float boxW = pw * 0.44f;
        float boxH = ph - margin * 2f;

        // Grey "No Drawing" placeholder
        g.DrawRectangle(
            new PdfPen(new PdfColor(200, 200, 200), 0.5f),
            new PdfSolidBrush(new PdfColor(245, 245, 245)),
            new RectangleF(margin, margin, boxW, boxH));

        var noDrawFont = new PdfStandardFont(PdfFontFamily.Helvetica, 13, PdfFontStyle.Italic);
        var noDrawSize = noDrawFont.MeasureString("No Drawing");
        g.DrawString("No Drawing", noDrawFont,
            new PdfSolidBrush(new PdfColor(160, 160, 160)),
            new PointF(margin + (boxW - noDrawSize.Width) / 2f,
                       margin + (boxH - noDrawSize.Height) / 2f));

        // Vertical divider
        float divX = margin + boxW + 15f;
        g.DrawLine(new PdfPen(new PdfColor(210, 210, 210), 1f),
            new PointF(divX, margin), new PointF(divX, ph - margin));

        DrawDetails(g, part, divX + 15f, margin + 30f, pw, margin, sourceName, isSchedule, operation);
    }

    private static void DrawCoverPage(PdfPage cover, PdfTemplate template,
        bool rotated, bool isLandscape, LaserPartGroup part, string sourceName, bool isSchedule, string operation)
    {
        if (isLandscape)
            DrawCoverPageHorizontal(cover, template, rotated, part, sourceName, isSchedule, operation);
        else
            DrawCoverPageVertical(cover, template, rotated, part, sourceName, isSchedule, operation);
    }

    /// <summary>
    /// Landscape source PDF: thumbnail on top, details below.
    /// </summary>
    private static void DrawCoverPageHorizontal(PdfPage cover, PdfTemplate template, bool rotated, LaserPartGroup part, string sourceName, bool isSchedule, string operation)
    {
        var g  = cover.Graphics;
        float pw = cover.GetClientSize().Width;
        float ph = cover.GetClientSize().Height;

        float margin = 20f;
        float divY   = ph * 0.55f;

        // Visual dimensions — swap stored W/H when the page carries a 90°/270° /Rotate.
        float vW = rotated ? template.Height : template.Width;
        float vH = rotated ? template.Width  : template.Height;

        float thumbMaxW = pw - margin * 2f;
        float thumbMaxH = divY - margin * 2f;
        float scale  = Math.Min(thumbMaxW / vW, thumbMaxH / vH);
        float thumbW = vW * scale;
        float thumbH = vH * scale;
        float thumbX = (pw - thumbW) / 2f;
        float thumbY = margin + (thumbMaxH - thumbH) / 2f;

        DrawTemplate(g, template, rotated, thumbX, thumbY, thumbW, thumbH);
        g.DrawRectangle(new PdfPen(new PdfColor(180, 180, 180), 0.5f),
            new RectangleF(thumbX, thumbY, thumbW, thumbH));

        // Horizontal divider
        g.DrawLine(new PdfPen(new PdfColor(210, 210, 210), 1f),
            new PointF(margin, divY), new PointF(pw - margin, divY));

        // Details — bottom section
        DrawDetails(g, part, margin + 10f, divY + 18f, pw, margin, sourceName, isSchedule, operation);
    }

    /// <summary>
    /// Portrait source PDF: thumbnail on left, details on right.
    /// </summary>
    private static void DrawCoverPageVertical(PdfPage cover, PdfTemplate template, bool rotated, LaserPartGroup part, string sourceName, bool isSchedule, string operation)
    {
        var g  = cover.Graphics;
        float pw = cover.GetClientSize().Width;
        float ph = cover.GetClientSize().Height;

        float margin = 20f;
        float divX   = pw * 0.50f;

        // Visual dimensions — swap stored W/H when the page carries a 90°/270° /Rotate.
        float vW = rotated ? template.Height : template.Width;
        float vH = rotated ? template.Width  : template.Height;

        float thumbMaxW = divX - margin * 2f;
        float thumbMaxH = ph - margin * 2f;
        float scale  = Math.Min(thumbMaxW / vW, thumbMaxH / vH);
        float thumbW = vW * scale;
        float thumbH = vH * scale;
        float thumbX = margin + (thumbMaxW - thumbW) / 2f;
        float thumbY = (ph - thumbH) / 2f;

        DrawTemplate(g, template, rotated, thumbX, thumbY, thumbW, thumbH);
        g.DrawRectangle(new PdfPen(new PdfColor(180, 180, 180), 0.5f),
            new RectangleF(thumbX, thumbY, thumbW, thumbH));

        // Vertical divider
        g.DrawLine(new PdfPen(new PdfColor(210, 210, 210), 1f),
            new PointF(divX, margin), new PointF(divX, ph - margin));

        // Details — right section
        DrawDetails(g, part, divX + 18f, 50f, pw, margin, sourceName, isSchedule, operation);
    }

    /// <summary>
    /// Draws a template into the rectangle (tx, ty, tw, th), applying a 90° CW rotation
    /// transform when the source page carries a /Rotate 90 attribute.
    /// </summary>
    private static void DrawTemplate(PdfGraphics g, PdfTemplate template, bool rotated,
        float tx, float ty, float tw, float th)
    {
        if (!rotated)
        {
            g.DrawPdfTemplate(template, new PointF(tx, ty), new SizeF(tw, th));
            return;
        }

        // For /Rotate=90: content is stored portrait (template.Width × template.Height)
        // but must render landscape.  Apply a 90° CW rotation around the top-right corner
        // of the target box so the rendered result fills (tx, ty, tw, th).
        //
        //   After TranslateTransform(tx+tw, ty) + RotateTransform(90):
        //     local +x → screen down,  local +y → screen left
        //   DrawPdfTemplate at (0,0) with size (th, tw) fills the box correctly because:
        //     th units down  = tw units right in visual  (= the landscape width)  — wait,
        //   Actually stored W maps along local X (down), stored H along local Y (left):
        //     stored W (template.Width)  * scale = th  → renders as the visual height
        //     stored H (template.Height) * scale = tw  → renders as the visual width
        g.Save();
        g.TranslateTransform(tx + tw, ty);
        g.RotateTransform(90);
        g.DrawPdfTemplate(template, new PointF(0, 0), new SizeF(th, tw));
        g.Restore();
    }

    private static void DrawDetails(PdfGraphics g, LaserPartGroup part,
        float bx, float by, float pw, float margin, string sourceName, bool isSchedule, string operation)
    {
        var fontTitle  = new PdfStandardFont(PdfFontFamily.Helvetica, 16, PdfFontStyle.Bold);
        var fontBold11 = new PdfStandardFont(PdfFontFamily.Helvetica, 11, PdfFontStyle.Bold);
        var fontReg10  = new PdfStandardFont(PdfFontFamily.Helvetica, 10);
        var fontReg9   = new PdfStandardFont(PdfFontFamily.Helvetica, 9);
        var black      = PdfBrushes.Black;
        var grayBrush  = new PdfSolidBrush(new PdfColor(110, 110, 110));
        var grayPen    = new PdfPen(new PdfColor(200, 200, 200), 0.5f);

        // Part number
        g.DrawString(part.PartNumber, fontTitle, black, new PointF(bx, by));
        by += 26f;

        // Title
        if (!string.IsNullOrWhiteSpace(part.Title))
        {
            g.DrawString(part.Title, fontReg10, black, new PointF(bx, by));
            by += 18f;
        }

        // Description — only if it says something the title doesn't already say
        if (!string.IsNullOrWhiteSpace(part.Description) &&
            !part.Description.Trim().Equals(part.Title?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            g.DrawString(part.Description, fontReg10, black, new PointF(bx, by));
            by += 18f;
        }
        by += 10f;

        // Thickness, Material, and source name
        if (!string.IsNullOrWhiteSpace(part.Thickness))
        {
            g.DrawString("Thickness", fontReg9, grayBrush, new PointF(bx, by));
            g.DrawString(part.Thickness, fontReg10, black, new PointF(bx + 62f, by));
            by += 17f;
        }
        if (!string.IsNullOrWhiteSpace(part.Material))
        {
            g.DrawString("Material", fontReg9, grayBrush, new PointF(bx, by));
            g.DrawString(part.Material, fontReg10, black, new PointF(bx + 62f, by));
            by += 17f;
        }
        g.DrawString("Type", fontReg9, grayBrush, new PointF(bx, by));
        g.DrawString(StockLabel(part.IsStock), fontReg10, black, new PointF(bx + 62f, by));
        by += 17f;
        if (!string.IsNullOrWhiteSpace(part.PlantDisplay))
        {
            g.DrawString("Plant", fontReg9, grayBrush, new PointF(bx, by));
            g.DrawString(part.PlantDisplay, fontReg10, black, new PointF(bx + 62f, by));
            by += 17f;
        }
        g.DrawString("Operation", fontReg9, grayBrush, new PointF(bx, by));
        g.DrawString(operation,   fontReg10, black,    new PointF(bx + 62f, by));
        by += 17f;
        string sourceLabel = isSchedule ? "Schedule" : "Batch";
        g.DrawString(sourceLabel, fontReg9, grayBrush, new PointF(bx, by));
        g.DrawString(sourceName,  fontReg10, black,    new PointF(bx + 62f, by));
        by += 17f;

        by += 16f;

        // Total qty
        g.DrawString($"Total Qty:  {part.TotalQty}", fontBold11, black, new PointF(bx, by));
        by += 20f;

        // Separator
        g.DrawLine(grayPen, new PointF(bx, by), new PointF(pw - margin, by));
        by += 10f;

        // Per-order / per-product rows
        float qtyColX = pw - margin - 45f;
        foreach (var (label, qty) in part.Orders)
        {
            g.DrawString(label, fontReg9, black, new PointF(bx, by));
            g.DrawString($"{qty} pcs", fontReg9, black, new PointF(qtyColX, by));
            by += 15f;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// The plant that makes a part: its resolved PlantId, or the owning Batch/Schedule's plant
    /// when the Plant field is blank. A custom typed value (PlantId null, PlantIdRaw set)
    /// belongs to no plant, since the Custom Report covers those.
    /// </summary>
    private static int? MadeAtPlantId(PartLineItem p, int ownerPlantId) =>
        p.PlantId ?? (string.IsNullOrWhiteSpace(p.PlantIdRaw) ? ownerPlantId : null);

    private static string PlantName(int plantId, IReadOnlyDictionary<int, string> plantNames) =>
        plantNames.TryGetValue(plantId, out var name) ? name : $"Plant #{plantId}";

    private static string PlantFileSuffix(int? plantId, IEnumerable<Plant> plants) =>
        plants.FirstOrDefault(p => p.Id == plantId) is { } plant ? $"-{plant.Code}" : "";

    private static string HandoffStatusText(HandoffStatus? status) => status switch
    {
        HandoffStatus.Draft    => "Handoff queued, not yet sent",
        HandoffStatus.Pending  => "Handoff pending acceptance",
        HandoffStatus.Accepted => "Handoff accepted",
        HandoffStatus.Rejected => "Handoff rejected",
        _                      => "No handoff yet",
    };

    private static LaserPartGroup BuildGroup(
        string partNumber,
        string? title,
        string? description,
        string? thickness,
        string? material,
        string? structCode,
        bool isStock,
        string? plantDisplay,
        List<(string Label, int Qty)> orders,
        string pdfPath,
        string? secondOperation = null) => new()
    {
        PartNumber   = partNumber,
        Title        = title,
        Description  = description,
        Thickness    = thickness,
        Material     = material,
        StructCode   = structCode,
        SecondOperation = secondOperation,
        IsStock      = isStock,
        PlantDisplay = plantDisplay,
        TotalQty     = orders.Sum(o => o.Qty),
        Orders       = orders,
        PdfPath      = File.Exists(pdfPath) ? pdfPath : null,
    };

    /// <summary>
    /// Resolves the Plant label to print on a report: the matched Plant's name if PlantId
    /// resolves to a known plant, otherwise the free-typed PlantIdRaw value (if any), so
    /// custom/unmatched plant text still shows up on drawings.
    /// </summary>
    private static string? ResolvePlantDisplay(int? plantId, string? plantIdRaw, IReadOnlyDictionary<int, string> plantNames)
    {
        if (plantId is int id && plantNames.TryGetValue(id, out var name)) return name;
        return string.IsNullOrWhiteSpace(plantIdRaw) ? null : plantIdRaw;
    }

    private static string StockLabel(bool isStock) => isStock ? "Make to Stock" : "Make to Order";

    private static double ParseThickness(string? thickness)
    {
        if (string.IsNullOrWhiteSpace(thickness)) return double.MaxValue;
        var numeric = thickness.Split(' ')[0];
        if (double.TryParse(numeric, NumberStyles.Number, CultureInfo.InvariantCulture, out double d))
            return d;
        return double.MaxValue;
    }

    private sealed class LaserPartGroup
    {
        public string PartNumber   { get; init; } = "";
        public string? Title       { get; init; }
        public string? Description { get; init; }
        public string? Thickness   { get; init; }
        public string? Material    { get; init; }
        public string? StructCode  { get; init; }
        public string? SecondOperation { get; init; }
        public bool IsStock        { get; init; }
        public string? PlantDisplay { get; init; }
        public int TotalQty        { get; init; }
        public List<(string Label, int Qty)> Orders { get; init; } = new();
        public string? PdfPath     { get; init; }
    }

    private static string BuildProductLabel(TravellerProduct product) =>
        string.IsNullOrWhiteSpace(product.CoverTitle)
            ? product.CoverNumber
            : $"{product.CoverNumber} ({product.CoverTitle})";

    private readonly record struct PartOrderQty(PartLineItem Part, string OrderNumber, int Qty);

    /// <summary>
    /// Builds one printable drawing entry (assembly or fabricated component) from all
    /// PartLineItem rows sharing a PartNumber within a product group, summing quantity
    /// across every order/parent that references it.
    /// </summary>
    private static TravellerComponent BuildTravellerComponent(
        IGrouping<string, PartOrderQty> g, string pdfFolder, IReadOnlyDictionary<int, string> plantNames,
        int ownerPlantId, IReadOnlyDictionary<int, HandoffStatus> handoffStatus)
    {
        var path = Path.Combine(pdfFolder, g.Key + ".pdf");
        var madeAt = MadeAtPlantId(g.First().Part, ownerPlantId) is int madeAtId && madeAtId != ownerPlantId
            ? PlantName(madeAtId, plantNames)
            : null;
        return new TravellerComponent
        {
            PartNumber     = g.Key,
            Title          = g.First().Part.Title,
            Description    = g.First().Part.Description,
            Thickness      = g.First().Part.Thickness,
            Material       = g.First().Part.Material,
            StructCode     = g.First().Part.StructCode,
            IsStock        = g.First().Part.IsStock,
            PlantDisplay   = ResolvePlantDisplay(g.First().Part.PlantId, g.First().Part.PlantIdRaw, plantNames),
            TotalQty       = g.Sum(x => x.Part.Qty * x.Qty),
            OrderBreakdown = g.GroupBy(x => x.OrderNumber)
                              .Select(og => (og.Key, og.Sum(x => x.Part.Qty * x.Qty)))
                              .ToList(),
            PdfPath        = File.Exists(path) ? path : null,
            MadeAtPlant    = madeAt,
            // One status per order the part appears in; usually they all agree.
            HandoffStatus  = madeAt == null ? null : string.Join(" / ", g
                .Select(x => x.Part.ScheduleOrderId).Distinct()
                .Select(id => HandoffStatusText(id is int i && handoffStatus.TryGetValue(i, out var s) ? s : null))
                .Distinct()),
        };
    }

    // ── Traveller PDF builder ─────────────────────────────────────────────────

    private static byte[] BuildTravellerPdf(List<TravellerProduct> products, string scheduleName, ILogger log)
    {
        var output = new PdfDocument();
        output.PageSettings.Size = PdfPageSize.Letter;
        output.PageSettings.Margins.All = 0;

        foreach (var product in products)
        {
            // Product cover page (front) + blank back for duplex
            TravellerAppendProductCoverPage(output, product, scheduleName);

            // Assembly drawings first — one page (or "No Drawing" placeholder) per distinct
            // Category="Assembly" row, not just the first one found.
            foreach (var asm in product.Assemblies)
            {
                var asmInfo = new TravellerPageInfo(
                    PartNumber: asm.PartNumber,
                    Title:      asm.Title,
                    Description: asm.Description,
                    Thickness:  asm.Thickness,
                    Material:   asm.Material,
                    StructCode: asm.StructCode,
                    IsStock:    asm.IsStock,
                    PlantDisplay: asm.PlantDisplay,
                    TotalQty:   asm.TotalQty,
                    Orders:     asm.OrderBreakdown,
                    SourceName: scheduleName,
                    IsAssembly: true,
                    AlsoIn:     asm.AlsoInProducts,
                    MadeAtPlant:   asm.MadeAtPlant,
                    HandoffStatus: asm.HandoffStatus);

                if (asm.PdfPath != null)
                    TravellerAppendDrawingPages(output, asm.PdfPath, asmInfo, log);
                else
                    TravellerAppendTextOnlyPage(output, asmInfo);
            }

            // Fabricated components (bandsaw / iron worker)
            foreach (var comp in product.Components)
            {
                var info = new TravellerPageInfo(
                    PartNumber: comp.PartNumber,
                    Title:      comp.Title,
                    Description: comp.Description,
                    Thickness:  comp.Thickness,
                    Material:   comp.Material,
                    StructCode: comp.StructCode,
                    IsStock:    comp.IsStock,
                    PlantDisplay: comp.PlantDisplay,
                    TotalQty:   comp.TotalQty,
                    Orders:     comp.OrderBreakdown,
                    SourceName: scheduleName,
                    IsAssembly: false,
                    AlsoIn:     comp.AlsoInProducts,
                    MadeAtPlant:   comp.MadeAtPlant,
                    HandoffStatus: comp.HandoffStatus);

                if (comp.PdfPath != null)
                    TravellerAppendDrawingPages(output, comp.PdfPath, info, log);
                else
                    TravellerAppendTextOnlyPage(output, info);
            }
        }

        using var ms = new MemoryStream();
        output.Save(ms);
        output.Close();
        return ms.ToArray();
    }

    private static void TravellerAppendDrawingPages(PdfDocument output, string pdfPath,
        TravellerPageInfo info, ILogger log)
    {
        byte[] srcBytes;
        try { srcBytes = File.ReadAllBytes(pdfPath); }
        catch (Exception ex)
        {
            log.LogWarning("Could not read PDF {Path}: {Msg}", pdfPath, ex.Message);
            TravellerAppendTextOnlyPage(output, info);
            return;
        }

        var srcDoc = new PdfLoadedDocument(srcBytes);
        try
        {
            if (srcDoc.Pages.Count == 0) { TravellerAppendTextOnlyPage(output, info); return; }

            // Each PDF page becomes its own duplex sheet: details cover (front) + drawing (back)
            for (int i = 0; i < srcDoc.Pages.Count; i++)
            {
                var srcPage = srcDoc.Pages[i] as PdfLoadedPage;
                if (srcPage == null) continue;

                var template = srcPage.CreateTemplate();
                bool rotated = srcPage.Rotation == PdfPageRotateAngle.RotateAngle90 ||
                               srcPage.Rotation == PdfPageRotateAngle.RotateAngle270;
                bool isLandscape = rotated ? template.Height > template.Width
                                           : template.Width  > template.Height;

                var cover = output.Pages.Add();
                TravellerDrawCoverPage(cover, template, rotated, isLandscape, info);
                output.ImportPage(srcDoc, i);
            }
        }
        finally { srcDoc.Close(); }
    }

    private static void TravellerAppendTextOnlyPage(PdfDocument output, TravellerPageInfo info)
    {
        TravellerDrawTextOnlyPage(output.Pages.Add(), info);
        output.Pages.Add(); // blank back for duplex alignment
    }

    private static void TravellerDrawCoverPage(PdfPage cover, PdfTemplate template,
        bool rotated, bool isLandscape, TravellerPageInfo info)
    {
        if (isLandscape)
            TravellerDrawCoverPageHorizontal(cover, template, rotated, info);
        else
            TravellerDrawCoverPageVertical(cover, template, rotated, info);
    }

    private static void TravellerDrawCoverPageHorizontal(PdfPage cover, PdfTemplate template,
        bool rotated, TravellerPageInfo info)
    {
        var g     = cover.Graphics;
        float pw  = cover.GetClientSize().Width;
        float ph  = cover.GetClientSize().Height;
        float margin = 20f;
        float divY   = ph * 0.55f;

        float vW = rotated ? template.Height : template.Width;
        float vH = rotated ? template.Width  : template.Height;
        float scale  = Math.Min((pw - margin * 2f) / vW, (divY - margin * 2f) / vH);
        float thumbW = vW * scale;
        float thumbH = vH * scale;
        float thumbX = (pw - thumbW) / 2f;
        float thumbY = margin + ((divY - margin * 2f) - thumbH) / 2f;

        DrawTemplate(g, template, rotated, thumbX, thumbY, thumbW, thumbH);
        g.DrawRectangle(new PdfPen(new PdfColor(180, 180, 180), 0.5f),
            new RectangleF(thumbX, thumbY, thumbW, thumbH));
        g.DrawLine(new PdfPen(new PdfColor(210, 210, 210), 1f),
            new PointF(margin, divY), new PointF(pw - margin, divY));

        TravellerDrawDetails(g, info, margin + 10f, divY + 18f, pw, margin);
    }

    private static void TravellerDrawCoverPageVertical(PdfPage cover, PdfTemplate template,
        bool rotated, TravellerPageInfo info)
    {
        var g     = cover.Graphics;
        float pw  = cover.GetClientSize().Width;
        float ph  = cover.GetClientSize().Height;
        float margin = 20f;
        float divX   = pw * 0.50f;

        float vW = rotated ? template.Height : template.Width;
        float vH = rotated ? template.Width  : template.Height;
        float scale  = Math.Min((divX - margin * 2f) / vW, (ph - margin * 2f) / vH);
        float thumbW = vW * scale;
        float thumbH = vH * scale;
        float thumbX = margin + ((divX - margin * 2f) - thumbW) / 2f;
        float thumbY = (ph - thumbH) / 2f;

        DrawTemplate(g, template, rotated, thumbX, thumbY, thumbW, thumbH);
        g.DrawRectangle(new PdfPen(new PdfColor(180, 180, 180), 0.5f),
            new RectangleF(thumbX, thumbY, thumbW, thumbH));
        g.DrawLine(new PdfPen(new PdfColor(210, 210, 210), 1f),
            new PointF(divX, margin), new PointF(divX, ph - margin));

        TravellerDrawDetails(g, info, divX + 18f, 50f, pw, margin);
    }

    private static void TravellerDrawTextOnlyPage(PdfPage page, TravellerPageInfo info)
    {
        var g      = page.Graphics;
        float pw   = page.GetClientSize().Width;
        float ph   = page.GetClientSize().Height;
        float margin = 30f;
        float boxW = pw * 0.44f;
        float boxH = ph - margin * 2f;

        // Grey "No Drawing" placeholder
        g.DrawRectangle(
            new PdfPen(new PdfColor(200, 200, 200), 0.5f),
            new PdfSolidBrush(new PdfColor(245, 245, 245)),
            new RectangleF(margin, margin, boxW, boxH));

        var noDrawFont = new PdfStandardFont(PdfFontFamily.Helvetica, 13, PdfFontStyle.Italic);
        var noDrawSize = noDrawFont.MeasureString("No Drawing");
        g.DrawString("No Drawing", noDrawFont,
            new PdfSolidBrush(new PdfColor(160, 160, 160)),
            new PointF(margin + (boxW - noDrawSize.Width) / 2f,
                       margin + (boxH - noDrawSize.Height) / 2f));

        // Vertical divider
        float divX = margin + boxW + 15f;
        g.DrawLine(new PdfPen(new PdfColor(210, 210, 210), 1f),
            new PointF(divX, margin), new PointF(divX, ph - margin));

        TravellerDrawDetails(g, info, divX + 15f, margin + 30f, pw, margin);
    }

    private static void TravellerDrawDetails(PdfGraphics g, TravellerPageInfo info,
        float bx, float by, float pw, float margin)
    {
        var fontTitle  = new PdfStandardFont(PdfFontFamily.Helvetica, 16, PdfFontStyle.Bold);
        var fontBold11 = new PdfStandardFont(PdfFontFamily.Helvetica, 11, PdfFontStyle.Bold);
        var fontReg10  = new PdfStandardFont(PdfFontFamily.Helvetica, 10);
        var fontReg9   = new PdfStandardFont(PdfFontFamily.Helvetica,  9);
        var fontItal9  = new PdfStandardFont(PdfFontFamily.Helvetica,  9, PdfFontStyle.Italic);
        var black      = PdfBrushes.Black;
        var grayBrush  = new PdfSolidBrush(new PdfColor(110, 110, 110));
        var grayPen    = new PdfPen(new PdfColor(200, 200, 200), 0.5f);

        // Cross-plant: solid black banner (survives B&W printing) so nobody here fabricates it.
        if (info.MadeAtPlant != null)
        {
            var bannerText = $"MADE AT {info.MadeAtPlant.ToUpperInvariant()} — DO NOT FABRICATE";
            float bannerW  = pw - margin - bx;
            var fontBanner = new PdfStandardFont(PdfFontFamily.Helvetica, 11, PdfFontStyle.Bold);
            if (fontBanner.MeasureString(bannerText).Width > bannerW - 12f)
                fontBanner = new PdfStandardFont(PdfFontFamily.Helvetica, 8.5f, PdfFontStyle.Bold);
            g.DrawRectangle(PdfBrushes.Black, new RectangleF(bx, by - 4f, bannerW, 20f));
            g.DrawString(bannerText, fontBanner, PdfBrushes.White, new PointF(bx + 6f, by));
            by += 22f;
            if (info.HandoffStatus != null)
            {
                g.DrawString(info.HandoffStatus, fontItal9, grayBrush, new PointF(bx, by));
                by += 14f;
            }
            by += 8f;
        }

        g.DrawString(info.PartNumber, fontTitle, black, new PointF(bx, by));
        by += 26f;

        if (!string.IsNullOrWhiteSpace(info.Title))
        {
            g.DrawString(info.Title, fontReg10, black, new PointF(bx, by));
            by += 18f;
        }
        if (!string.IsNullOrWhiteSpace(info.Description) &&
            !info.Description.Trim().Equals(info.Title?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            g.DrawString(info.Description, fontReg10, black, new PointF(bx, by));
            by += 18f;
        }
        by += 8f;

        if (!string.IsNullOrWhiteSpace(info.StructCode))
        {
            g.DrawString("Struct Code", fontReg9, grayBrush, new PointF(bx, by));
            g.DrawString(info.StructCode, fontReg10, black, new PointF(bx + 72f, by));
            by += 17f;
        }
        if (!string.IsNullOrWhiteSpace(info.Thickness))
        {
            g.DrawString("Thickness",  fontReg9, grayBrush, new PointF(bx, by));
            g.DrawString(info.Thickness, fontReg10, black,  new PointF(bx + 72f, by));
            by += 17f;
        }
        if (!string.IsNullOrWhiteSpace(info.Material))
        {
            g.DrawString("Material",   fontReg9, grayBrush, new PointF(bx, by));
            g.DrawString(info.Material, fontReg10, black,   new PointF(bx + 72f, by));
            by += 17f;
        }
        g.DrawString("Type",          fontReg9, grayBrush, new PointF(bx, by));
        g.DrawString(StockLabel(info.IsStock), fontReg10, black, new PointF(bx + 72f, by));
        by += 17f;
        if (!string.IsNullOrWhiteSpace(info.PlantDisplay))
        {
            g.DrawString("Plant",       fontReg9, grayBrush, new PointF(bx, by));
            g.DrawString(info.PlantDisplay, fontReg10, black, new PointF(bx + 72f, by));
            by += 17f;
        }
        g.DrawString("Schedule",      fontReg9,  grayBrush, new PointF(bx, by));
        g.DrawString(info.SourceName, fontReg10, black,     new PointF(bx + 72f, by));
        by += 24f;

        g.DrawString($"Total Qty:  {info.TotalQty}", fontBold11, black, new PointF(bx, by));
        by += 20f;
        g.DrawLine(grayPen, new PointF(bx, by), new PointF(pw - margin, by));
        by += 10f;

        float qtyColX = pw - margin - 45f;
        foreach (var (orderNum, qty) in info.Orders)
        {
            g.DrawString(orderNum,   fontReg9, black, new PointF(bx, by));
            g.DrawString($"{qty} pcs", fontReg9, black, new PointF(qtyColX, by));
            by += 15f;
        }

        if (info.AlsoIn.Count > 0)
        {
            by += 10f;
            g.DrawLine(grayPen, new PointF(bx, by), new PointF(pw - margin, by));
            by += 8f;
            g.DrawString("Also in:", fontItal9, grayBrush, new PointF(bx, by));
            by += 13f;
            foreach (var entry in info.AlsoIn)
            {
                g.DrawString($"{entry.ProductLabel} — {entry.OrderNumber}, {entry.Qty} pcs",
                    fontItal9, grayBrush, new PointF(bx + 8f, by));
                by += 13f;
            }
        }
    }

    private static void TravellerAppendProductCoverPage(PdfDocument output, TravellerProduct product, string scheduleName)
    {
        TravellerDrawProductCover(output.Pages.Add(), product, scheduleName);
        output.Pages.Add(); // blank back for duplex
    }

    private static void TravellerDrawProductCover(PdfPage page, TravellerProduct product, string scheduleName)
    {
        var g      = page.Graphics;
        float pw   = page.GetClientSize().Width;
        float ph   = page.GetClientSize().Height;
        float margin = 50f;

        var fontHeader = new PdfStandardFont(PdfFontFamily.Helvetica,  9);
        var fontLarge  = new PdfStandardFont(PdfFontFamily.Helvetica, 26, PdfFontStyle.Bold);
        var fontTitle  = new PdfStandardFont(PdfFontFamily.Helvetica, 14);
        var fontBold12 = new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Bold);
        var fontReg10  = new PdfStandardFont(PdfFontFamily.Helvetica, 10);
        var black      = PdfBrushes.Black;
        var grayBrush  = new PdfSolidBrush(new PdfColor(130, 130, 130));
        var darkBrush  = new PdfSolidBrush(new PdfColor( 80,  80,  80));
        var grayPen    = new PdfPen(new PdfColor(200, 200, 200), 1f);

        // Header strip
        g.DrawString("SHOP TRAVELLER", fontHeader, grayBrush, new PointF(margin, margin));
        g.DrawString(scheduleName, fontHeader, grayBrush,
            new PointF(pw - margin - fontHeader.MeasureString(scheduleName).Width, margin));
        g.DrawLine(grayPen, new PointF(margin, margin + 16f), new PointF(pw - margin, margin + 16f));

        // Centered block ~32% from top
        float cy = ph * 0.32f;

        var pnSize = fontLarge.MeasureString(product.CoverNumber);
        g.DrawString(product.CoverNumber, fontLarge, black,
            new PointF((pw - pnSize.Width) / 2f, cy));
        cy += pnSize.Height + 10f;

        if (!string.IsNullOrWhiteSpace(product.CoverTitle))
        {
            var titleSize = fontTitle.MeasureString(product.CoverTitle);
            g.DrawString(product.CoverTitle, fontTitle, darkBrush,
                new PointF((pw - titleSize.Width) / 2f, cy));
            cy += titleSize.Height + 16f;
        }

        if (!string.IsNullOrWhiteSpace(product.CoverDescription) &&
            !product.CoverDescription.Trim().Equals(product.CoverTitle?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            var descSize = fontReg10.MeasureString(product.CoverDescription);
            g.DrawString(product.CoverDescription, fontReg10, darkBrush,
                new PointF((pw - descSize.Width) / 2f, cy));
            cy += descSize.Height + 16f;
        }

        var schedText = "Schedule  " + scheduleName;
        var schedSize = fontReg10.MeasureString(schedText);
        g.DrawString(schedText, fontReg10, grayBrush,
            new PointF((pw - schedSize.Width) / 2f, cy));
        cy += 40f;

        g.DrawLine(grayPen, new PointF(margin * 2f, cy), new PointF(pw - margin * 2f, cy));
        cy += 20f;

        int totalQty = product.Orders.Sum(o => o.Qty);
        float labelX = pw / 2f - 100f;
        float qtyX   = pw / 2f + 60f;

        foreach (var (orderNum, qty) in product.Orders)
        {
            g.DrawString(orderNum, fontReg10, black, new PointF(labelX, cy));
            g.DrawString($"{qty} pcs", fontReg10, black, new PointF(qtyX, cy));
            cy += 18f;
        }

        if (product.Orders.Count > 1)
        {
            cy += 4f;
            g.DrawLine(grayPen, new PointF(labelX, cy), new PointF(qtyX + 50f, cy));
            cy += 8f;
            g.DrawString("Total", fontBold12, black, new PointF(labelX, cy));
            g.DrawString($"{totalQty} pcs", fontBold12, black, new PointF(qtyX, cy));
            cy += 18f;
        }

        // Parts made at another plant: what the assembler is waiting on.
        float maxY = ph - margin;
        foreach (var plantGroup in product.Arriving.GroupBy(a => a.MadeAtPlant))
        {
            cy += 28f;
            if (cy + 40f > maxY) break;
            var header = $"ARRIVING FROM {plantGroup.Key.ToUpperInvariant()}";
            g.DrawRectangle(PdfBrushes.Black, new RectangleF(margin * 2f, cy - 4f, pw - margin * 4f, 20f));
            g.DrawString(header, fontBold12, PdfBrushes.White, new PointF(margin * 2f + 6f, cy - 1f));
            cy += 24f;

            var rows = plantGroup.ToList();
            for (int i = 0; i < rows.Count; i++)
            {
                if (cy + 15f > maxY)
                {
                    g.DrawString($"…and {rows.Count - i} more", fontReg10, grayBrush, new PointF(margin * 2f, cy));
                    break;
                }
                var a = rows[i];
                g.DrawString(a.PartNumber, fontReg10, black, new PointF(margin * 2f, cy));
                if (!string.IsNullOrWhiteSpace(a.Title))
                    g.DrawString(a.Title, fontReg10, darkBrush, new PointF(margin * 2f + 150f, cy));
                g.DrawString($"{a.Qty} pcs", fontReg10, black, new PointF(pw - margin * 2f - 45f, cy));
                cy += 15f;
            }
        }
    }

    // ── Traveller data types ──────────────────────────────────────────────────

    private sealed class TravellerProduct
    {
        public string  ProductKey          { get; init; } = "";
        // Top-level item identity shown on the standalone cover page — independent of
        // whichever sub-assembly drawings are attached below.
        public string  CoverNumber         { get; init; } = "";
        public string? CoverTitle          { get; init; }
        public string? CoverDescription    { get; init; }
        public List<(string OrderNumber, int Qty)> Orders     { get; init; } = new();
        // Every Category="Assembly" row in this product's orders — each gets its own
        // drawing page (or "No Drawing" placeholder), printed right after the cover.
        public List<TravellerComponent>            Assemblies { get; init; } = new();
        public List<TravellerComponent>            Components { get; init; } = new();
        // Non-stock parts made at another plant, listed on the cover.
        public List<ArrivingPart>                  Arriving   { get; init; } = new();
    }

    private sealed record ArrivingPart(string PartNumber, string? Title, string MadeAtPlant, int Qty);

    private sealed class TravellerComponent
    {
        public string  PartNumber     { get; init; } = "";
        public string? Title          { get; init; }
        public string? Description    { get; init; }
        public string? Thickness      { get; init; }
        public string? Material       { get; init; }
        public string? StructCode     { get; init; }
        public bool    IsStock        { get; init; }
        public string? PlantDisplay   { get; init; }
        public int     TotalQty       { get; init; }
        public List<(string OrderNumber, int Qty)> OrderBreakdown { get; init; } = new();
        public string? PdfPath        { get; init; }
        public List<AlsoInEntry> AlsoInProducts { get; set; } = new();
        /// <summary>Plant name when made somewhere other than the Schedule's plant, else null.</summary>
        public string? MadeAtPlant    { get; init; }
        public string? HandoffStatus  { get; init; }
    }

    private readonly record struct AlsoInEntry(string ProductLabel, string OrderNumber, int Qty);

    private sealed record TravellerPageInfo(
        string PartNumber,
        string? Title,
        string? Description,
        string? Thickness,
        string? Material,
        string? StructCode,
        bool IsStock,
        string? PlantDisplay,
        int TotalQty,
        List<(string OrderNumber, int Qty)> Orders,
        string SourceName,
        bool IsAssembly,
        IReadOnlyList<AlsoInEntry> AlsoIn,
        string? MadeAtPlant,
        string? HandoffStatus);
}
