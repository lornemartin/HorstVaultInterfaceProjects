using HorstMFG.Core.DTOs;
using HorstMFG.Core.Entities;
using HorstMFG.Core.Enums;
using HorstMFG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HorstMFG.Web.Services;

/// <summary>
/// Receives a single VaultGateway callback (one ScheduleOrder or BatchProduct's BOM),
/// creates the corresponding <see cref="PartLineItem"/> rows, and copies referenced
/// PDFs to the local folder. Idempotent — re-imports replace existing PartLineItems
/// for the parent.
/// </summary>
public class VaultBomIngestService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly BomImportJobTracker _jobs;
    private readonly BomPdfCopyService _pdfCopy;
    private readonly ILogger<VaultBomIngestService> _log;

    public VaultBomIngestService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        BomImportJobTracker jobs,
        BomPdfCopyService pdfCopy,
        ILogger<VaultBomIngestService> log)
    {
        _dbFactory = dbFactory;
        _jobs = jobs;
        _pdfCopy = pdfCopy;
        _log = log;
    }

    public async Task IngestSingleAsync(VaultBomCallbackPayload payload, CancellationToken ct = default)
    {
        var importType = _jobs.GetImportType(payload.JobId);
        if (importType is null)
        {
            _log.LogWarning("Vault callback for unknown jobId {JobId} — ignoring", payload.JobId);
            return;
        }

        if (importType == BomType.MakeToStock)
            await IngestForScheduleOrderAsync(payload, ct);
        else
            await IngestForBatchProductAsync(payload, ct);
    }

    private async Task IngestForScheduleOrderAsync(VaultBomCallbackPayload payload, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var order = await db.ScheduleOrders
            .Include(so => so.Schedule).ThenInclude(s => s.Plant)
            .Include(so => so.Parts)
            .FirstOrDefaultAsync(so => so.Id == payload.TrackingId, ct);

        if (order is null)
        {
            _log.LogWarning("Vault callback trackingId={TrackingId} did not match any ScheduleOrder", payload.TrackingId);
            return;
        }

        if (!payload.Found)
        {
            order.Notes = AppendNote(order.Notes, $"Vault import failed: {payload.ErrorMessage}");
            order.VaultBomImported = false;
            order.LastImportError = payload.ErrorMessage ?? "Item not found in Vault.";
            await db.SaveChangesAsync(ct);
            _log.LogInformation("ScheduleOrder {Id} ({Order}/{Product}) — Vault item not found: {Err}",
                order.Id, order.OrderNumber, order.ProductNumber, payload.ErrorMessage);
            return;
        }

        // Idempotent re-import: replace any existing children.
        if (order.Parts.Count > 0)
            db.PartLineItems.RemoveRange(order.Parts);

        var newItems = BuildPartLineItems(payload.Lines).ToList();
        await ResolvePlantIdsAsync(db, newItems, ct);
        var partNumbers = newItems.Select(p => p.PartNumber).Distinct().ToList();
        var pdfsOnShare = await _pdfCopy.WhichExistOnShareAsync(partNumbers, ct);
        foreach (var line in newItems)
        {
            line.ScheduleOrderId = order.Id;
            line.HasPdf = pdfsOnShare.Contains(line.PartNumber);
            db.PartLineItems.Add(line);
        }
        order.VaultBomImported = true;
        order.LastImportError = null;
        await db.SaveChangesAsync(ct);

        var (copied, total) = await _pdfCopy.CopyForScheduleAsync(order.Schedule.Plant.Name, order.Schedule.Name, partNumbers, ct);
        _log.LogInformation(
            "Ingested ScheduleOrder {Id} ({Order}/{Product}) — {Lines} parts, copied {Copied}/{Total} PDFs",
            order.Id, order.OrderNumber, order.ProductNumber, partNumbers.Count, copied, total);
    }

    private async Task IngestForBatchProductAsync(VaultBomCallbackPayload payload, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var product = await db.BatchProducts
            .Include(bp => bp.Batch).ThenInclude(b => b.Plant)
            .Include(bp => bp.Parts)
            .FirstOrDefaultAsync(bp => bp.Id == payload.TrackingId, ct);

        if (product is null)
        {
            _log.LogWarning("Vault callback trackingId={TrackingId} did not match any BatchProduct", payload.TrackingId);
            return;
        }

        if (!payload.Found)
        {
            product.Notes = AppendNote(product.Notes, $"Vault import failed: {payload.ErrorMessage}");
            product.VaultBomImported = false;
            product.LastImportError = payload.ErrorMessage ?? "Item not found in Vault.";
            await db.SaveChangesAsync(ct);
            _log.LogInformation("BatchProduct {Id} ({Product}) — Vault item not found: {Err}",
                product.Id, product.ProductName, payload.ErrorMessage);
            return;
        }

        if (product.Parts.Count > 0)
            db.PartLineItems.RemoveRange(product.Parts);

        var newItems = BuildPartLineItems(payload.Lines).ToList();
        await ResolvePlantIdsAsync(db, newItems, ct);
        var partNumbers = newItems.Select(p => p.PartNumber).Distinct().ToList();
        var pdfsOnShare = await _pdfCopy.WhichExistOnShareAsync(partNumbers, ct);
        foreach (var line in newItems)
        {
            line.BatchProductId = product.Id;
            line.HasPdf = pdfsOnShare.Contains(line.PartNumber);
            db.PartLineItems.Add(line);
        }
        product.VaultBomImported = true;
        product.LastImportError = null;
        await db.SaveChangesAsync(ct);

        var (copied, total) = await _pdfCopy.CopyForBatchAsync(product.Batch.Plant.Name, product.Batch.Name, partNumbers, ct);
        _log.LogInformation(
            "Ingested BatchProduct {Id} ({Product}) — {Lines} parts, copied {Copied}/{Total} PDFs",
            product.Id, product.ProductName, partNumbers.Count, copied, total);
    }

    /// <summary>
    /// Resolves each item's PlantIdRaw (Vault's raw "Plant ID" text) to our own Plant.Id via
    /// BomFieldMappers.MapVaultPlantCode + Plant.Code. Left null when Vault's value is blank,
    /// unrecognized, or "Plant 1&amp;2" — PlantIdRaw still preserves the raw text either way.
    /// </summary>
    private static async Task ResolvePlantIdsAsync(ApplicationDbContext db, List<PartLineItem> items, CancellationToken ct)
    {
        if (items.Count == 0) return;
        var codeToId = await db.Plants.AsNoTracking()
            .ToDictionaryAsync(p => p.Code, p => p.Id, StringComparer.OrdinalIgnoreCase, ct);
        foreach (var item in items)
        {
            var code = BomFieldMappers.MapVaultPlantCode(item.PlantIdRaw);
            if (code != null && codeToId.TryGetValue(code, out var id))
                item.PlantId = id;
        }
    }

    /// <summary>
    /// Project Vault BOM lines into <see cref="PartLineItem"/> entities, applying the
    /// same filters, transformations, and dedup as the legacy TSV parser
    /// (BomService.ParseExportFile lines 114-210):
    /// skip Purchased rows; derive thickness from structural code for Laser ops;
    /// strip CAD extensions; apply stock-name override; collapse parts that appear
    /// under multiple parents into a single row with summed Qty. The Level=1 row
    /// (the product itself) is preserved as a Category="Product" PartLineItem so
    /// the schedule grid can render the order header.
    /// </summary>
    private static List<PartLineItem> BuildPartLineItems(IEnumerable<VaultBomLine> lines)
    {
        var levelToQty = new Dictionary<string, int>();
        var dedupedByNumber = new Dictionary<string, PartLineItem>(StringComparer.OrdinalIgnoreCase);

        foreach (var l in lines)
        {
            if (l.Category.Equals("Purchased", StringComparison.OrdinalIgnoreCase)) continue;

            var number = string.IsNullOrEmpty(l.StockName)
                ? BomFieldMappers.StripCadExtension(l.Number)
                : l.StockName;

            // Vault reports each line's Qty relative to its own immediate parent, not
            // pre-multiplied through the whole assembly chain — so a part nested under a
            // sub-assembly that's itself needed multiple times must have its raw Qty
            // multiplied by that parent's own (already-resolved) absolute qty. Tracked by
            // BOM Level (e.g. "1.2.3") rather than part number: a level uniquely identifies
            // one placement in the tree, so — unlike the legacy TSV path's number-based
            // lookup — this needs no order-dependent "divide to undo double counting" hack.
            int rawQty = l.Qty < 1 ? 1 : l.Qty;
            int parentQty = 1;
            if (l.Level.Contains('.'))
            {
                var parentLevel = l.Level[..l.Level.LastIndexOf('.')];
                if (levelToQty.TryGetValue(parentLevel, out var pq)) parentQty = pq;
            }
            int qty = rawQty * parentQty;
            levelToQty[l.Level] = qty;

            if (dedupedByNumber.TryGetValue(number, out var existing))
            {
                existing.Qty += qty;
            }
            else
            {
                var thickness = BomFieldMappers.DeriveThicknessForLaser(l.Operations, l.Thickness, l.StructCode);
                var item = new PartLineItem
                {
                    PartNumber  = number,
                    Title       = l.Title,
                    Description = l.ItemDescription,
                    Category    = l.Category,
                    Qty         = qty,
                    Material    = l.Material,
                    Thickness   = thickness,
                    StructCode  = l.StructCode,
                    Operations  = l.Operations,
                    IsStock     = l.IsStock,
                    RequiresPdf = l.RequiresPdf,
                    Notes       = string.IsNullOrEmpty(l.Notes) ? null : l.Notes,
                    PlantIdRaw  = string.IsNullOrWhiteSpace(l.PlantId) ? null : l.PlantId,
                };
                dedupedByNumber[number] = item;
            }
        }

        return dedupedByNumber.Values.ToList();
    }

    private static string AppendNote(string? existing, string addition)
        => string.IsNullOrEmpty(existing) ? addition : existing + "\n" + addition;
}
