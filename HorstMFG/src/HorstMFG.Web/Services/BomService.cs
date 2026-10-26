using System.Diagnostics;
using HorstMFG.Core.DTOs;
using HorstMFG.Core.Entities;
using HorstMFG.Core.Enums;
using HorstMFG.Core.Interfaces;
using HorstMFG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HorstMFG.Web.Services;

public class BomService : IBomService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly BomPdfCopyService _pdfCopy;
    private readonly ILogger<BomService> _log;
    private readonly string _pdfSharePath;
    private readonly string _powerJobsScriptPath;

    public BomService(IDbContextFactory<ApplicationDbContext> dbFactory,
                      BomPdfCopyService pdfCopy,
                      ILogger<BomService> log,
                      IConfiguration config)
    {
        _dbFactory = dbFactory;
        _pdfCopy = pdfCopy;
        _log = log;
        _pdfSharePath = config["FileSystemPaths:PdfSharePath"] ?? @"S:\PDF Drawing Files\";
        _powerJobsScriptPath = config["PowerJobs:ScriptPath"] ?? @"\\HWVMWK02\Jobs\Horst.PrintOnDemand.ps1";
    }

    /// <summary>
    /// Thickness lookup from structural code, matching the legacy VaultItemProcessor logic.
    /// </summary>
    private static readonly Dictionary<string, string> StructCodeThickness = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SH-062"] = "0.062",
        ["SH-075"] = "0.075",
        ["SH-125"] = "0.120",
        ["SH-188"] = "0.188",
        ["SH-250"] = "0.250",
        ["SH-312"] = "0.313",
        ["SH-375"] = "0.375",
        ["SH-500"] = "0.500",
        ["SH-625"] = "0.625",
        ["SH-750"] = "0.750",
        ["SH-875"] = "0.875",
        ["SH-1000"] = "1.000",
        ["SH-1250"] = "1.250",
        ["SH-1500"] = "1.500",
    };

    public List<BomExportLine> ParseExportFile(Stream fileStream, BomType bomType)
    {
        var deduped = new List<BomExportLine>();
        var dedupedByNumber = new Dictionary<string, BomExportLine>(StringComparer.Ordinal);
        var parentChildSeen = new HashSet<(string Number, string Parent)>();
        var parentChildCount = new Dictionary<(string Number, string Parent), int>();
        var parentDict = new Dictionary<string, string>();

        using var reader = new StreamReader(fileStream);

        // Skip header line
        reader.ReadLine();

        int lineNum = 0;
        string? rawLine;
        while ((rawLine = reader.ReadLine()) != null)
        {
            rawLine = rawLine.Replace("\"", "");
            var items = rawLine.Split('\t');
            if (items.Length < 19) continue;

            lineNum++;

            string level = items[0];
            string number = items[1];
            string title = items[2];
            string itemDesc = items[3];
            string category = items[4];
            string thickness = BomFieldMappers.NormalizeThickness(items[5]);
            string material = items[6];
            string ops = items[7];
            string qtyString = items[8];
            string structCode = items[9];
            string plantId = items[10];
            string isStockString = items[11];
            bool isStock = isStockString.Equals("True", StringComparison.OrdinalIgnoreCase);
            string requiresPdfString = items[12];
            bool requiresPdf = !requiresPdfString.Equals("False", StringComparison.OrdinalIgnoreCase);
            string comment = items[13];
            string dateString = items[14];
            string lifecycleState = items[15];
            string stockName = items[16];
            string keywords = items[17];
            string notes = items[18];
            string revision = items.Length > 19 ? items[19] : "";

            // First line: comment is the order number, clear it from item
            if (lineNum == 1)
                comment = "";

            // Stock name overrides number
            if (!string.IsNullOrEmpty(stockName))
                number = stockName;

            int qty = 0;
            if (!string.IsNullOrEmpty(qtyString) && int.TryParse(qtyString, out int parsedQty))
                qty = parsedQty;
            if (qty == 0) qty = 1;

            DateTime? dateModified = null;
            if (!string.IsNullOrEmpty(dateString) && DateTime.TryParse(dateString, out var dt))
                dateModified = dt;

            // Determine parent from BOM level structure (e.g., "1", "1.1", "1.1.2")
            string parent = "";
            if (level == "1")
            {
                parent = "<top>";
            }

            if (level.Contains('.'))
            {
                string parentLevel = level[..level.LastIndexOf('.')];
                parentDict.TryAdd(level, number);
                if (parentDict.TryGetValue(parentLevel, out var p))
                    parent = p;
            }
            else
            {
                parentDict.TryAdd(level, number);
            }

            // Strip file extensions
            if (number.EndsWith(".ipt", StringComparison.OrdinalIgnoreCase) ||
                number.EndsWith(".iam", StringComparison.OrdinalIgnoreCase))
            {
                number = Path.GetFileNameWithoutExtension(number);
            }

            // Derive thickness from structural code for Laser and Shear ops
            if ((ops.Equals("Laser", StringComparison.OrdinalIgnoreCase) ||
                 ops.Equals("Shear", StringComparison.OrdinalIgnoreCase)) &&
                string.IsNullOrEmpty(thickness) &&
                !string.IsNullOrEmpty(structCode))
            {
                var codePrefix = structCode.Split(' ')[0];
                if (StructCodeThickness.TryGetValue(codePrefix, out var derivedThickness))
                    thickness = derivedThickness;
            }

            var line = new BomExportLine
            {
                Level = level,
                Parent = parent,
                Number = number,
                Title = title,
                ItemDescription = itemDesc,
                Category = category,
                Thickness = thickness,
                Material = material,
                Operations = ops,
                Qty = qty,
                StructCode = structCode,
                PlantId = plantId,
                IsStock = isStock,
                RequiresPdf = requiresPdf,
                Comment = comment,
                DateModified = dateModified,
                LifecycleState = lifecycleState,
                StockName = stockName,
                Keywords = keywords,
                Notes = notes,
                Revision = revision,
            };

            // Skip purchased items
            if (category.Equals("Purchased", StringComparison.OrdinalIgnoreCase))
                continue;

            // Calculate quantity based on parent quantity
            line.Qty = GetCalculatedQty(line, deduped);

            // Deduplicate: same number+parent already seen?
            var key = (number, parent);
            bool isDuplicate = parentChildSeen.Contains(key);
            parentChildSeen.Add(key);
            parentChildCount[key] = parentChildCount.GetValueOrDefault(key) + 1;

            if (!isDuplicate)
            {
                // First time seeing this (number, parent) combo
                if (dedupedByNumber.TryGetValue(number, out var existing))
                {
                    // Part already in deduped list under a different parent — add quantity
                    existing.Qty += line.Qty;
                }
                else
                {
                    deduped.Add(line);
                    dedupedByNumber[number] = line;
                }
            }
            else
            {
                // Duplicate parent/child — adjust for multiple instances
                int relationCount = parentChildCount[key];
                if (dedupedByNumber.TryGetValue(number, out var existing))
                {
                    existing.Qty += line.Qty / relationCount;
                }
            }
        }

        // Check PDF existence in parallel — avoids both sequential round-trips and a full
        // directory enumeration (which is expensive at 78k+ files on a network share)
        Parallel.ForEach(deduped, new ParallelOptions { MaxDegreeOfParallelism = 4 }, line =>
        {
            var pdfPath = Path.Combine(_pdfSharePath, line.Number + ".pdf");
            line.HasPdf = BomFieldMappers.FileExistsWithRetry(pdfPath, _log);
            line.SortOrder = line.Parent == "<top>" ? 0 : 1;
        });

        _log.LogInformation("Parsed {Count} BOM lines from export file ({MissingPdf} missing PDFs)",
            deduped.Count, deduped.Count(l => !l.HasPdf));
        return deduped;
    }

    private static int GetCalculatedQty(BomExportLine item, List<BomExportLine> itemList)
    {
        if (item.Parent == "<top>")
            return item.Qty;

        var parentItem = itemList.FirstOrDefault(i => i.Number == item.Parent);
        if (parentItem == null)
        {
            string parentNoExt = Path.GetFileNameWithoutExtension(item.Parent);
            parentItem = itemList.FirstOrDefault(i => i.Number == parentNoExt);
            if (parentItem == null)
                parentItem = itemList.FirstOrDefault(i => i.Number.Contains(item.Parent));
        }

        if (parentItem != null)
            return item.Qty * parentItem.Qty;

        return item.Qty;
    }

    public async Task<Batch> ImportBatchAsync(string name, int batchQty, int plantId, int userId, List<BomExportLine> lines)
    {
        await using var _db = await _dbFactory.CreateDbContextAsync();
        var plantName = await _db.Plants.Where(p => p.Id == plantId).Select(p => p.Name).FirstOrDefaultAsync() ?? "UnknownPlant";
        var localFolder = _pdfCopy.GetBatchFolder(plantName, name);
        var plantCodeToId = await _db.Plants.AsNoTracking()
            .ToDictionaryAsync(p => p.Code, p => p.Id, StringComparer.OrdinalIgnoreCase);

        var batch = await _db.Batches
            .FirstOrDefaultAsync(b => b.Name == name && b.PlantId == plantId);

        if (batch is null)
        {
            batch = new Batch
            {
                Name = name,
                PlantId = plantId,
                ImportedByUserId = userId,
                ImportDate = DateTime.UtcNow,
                LocalPdfFolder = localFolder,
            };
            _db.Batches.Add(batch);
            await _db.SaveChangesAsync();
        }

        // Group lines by their Level-1 ancestor prefix
        // Level-1 lines (no dot in level) become BatchProduct headers
        // Child lines are keyed by their top-level prefix (e.g., "1", "2")
        var groups = lines.GroupBy(l => GetLevel1Prefix(l.Level));

        foreach (var group in groups)
        {
            var level1Line = group.FirstOrDefault(l => !l.Level.Contains('.'));
            string productName = level1Line?.Number ?? group.Key;

            var product = new BatchProduct
            {
                BatchId = batch.Id,
                PlantId = plantId,
                ProductName = productName,
                Qty = (level1Line?.Qty ?? 1) * (batchQty < 1 ? 1 : batchQty),
            };
            _db.BatchProducts.Add(product);
            await _db.SaveChangesAsync();

            // All non-Level-1 lines in this group become PartLineItems
            var childLines = group.Where(l => l.Level.Contains('.')).ToList();
            foreach (var line in childLines)
            {
                var linePlantCode = BomFieldMappers.MapVaultPlantCode(line.PlantId);
                var item = new PartLineItem
                {
                    BatchProductId = product.Id,
                    PartNumber = line.Number,
                    Title = line.Title,
                    Description = line.ItemDescription,
                    Category = line.Category,
                    Qty = line.Qty,
                    Material = line.Material,
                    Thickness = line.Thickness,
                    StructCode = line.StructCode,
                    Operations = line.Operations,
                    IsStock = line.IsStock,
                    RequiresPdf = line.RequiresPdf,
                    Notes = string.IsNullOrEmpty(line.Notes) ? null : line.Notes,
                    HasPdf = line.HasPdf,
                    PlantIdRaw = string.IsNullOrWhiteSpace(line.PlantId) ? null : line.PlantId,
                    PlantId = linePlantCode != null && plantCodeToId.TryGetValue(linePlantCode, out var lpid) ? lpid : null,
                };
                _db.PartLineItems.Add(item);
            }
        }

        await _db.SaveChangesAsync();

        // Copy PDFs to local folder
        var partNumbers = lines.Select(l => l.Number).Distinct().ToList();
        var (copied, total) = await _pdfCopy.CopyForBatchAsync(plantName, name, partNumbers);

        _log.LogInformation(
            "Imported batch '{Name}' with {GroupCount} products. Copied {Copied}/{Total} PDFs to {Folder}",
            name, lines.GroupBy(l => GetLevel1Prefix(l.Level)).Count(), copied, total, localFolder);

        return batch;
    }

    public async Task<Schedule> ImportScheduleAsync(string name, string orderNumber, int orderQty, int plantId, int userId, List<BomExportLine> lines)
    {
        await using var _db = await _dbFactory.CreateDbContextAsync();
        var plantName = await _db.Plants.Where(p => p.Id == plantId).Select(p => p.Name).FirstOrDefaultAsync() ?? "UnknownPlant";
        var localFolder = _pdfCopy.GetScheduleFolder(plantName, name);
        var plantCodeToId = await _db.Plants.AsNoTracking()
            .ToDictionaryAsync(p => p.Code, p => p.Id, StringComparer.OrdinalIgnoreCase);

        var schedule = await _db.Schedules
            .FirstOrDefaultAsync(s => s.Name == name && s.PlantId == plantId);

        if (schedule is null)
        {
            schedule = new Schedule
            {
                Name = name,
                PlantId = plantId,
                ImportedByUserId = userId,
                ImportDate = DateTime.UtcNow,
                LocalPdfFolder = localFolder,
            };
            _db.Schedules.Add(schedule);
            await _db.SaveChangesAsync();
        }

        var level1Line = lines.FirstOrDefault(l => !l.Level.Contains('.'));
        var order = new ScheduleOrder
        {
            ScheduleId = schedule.Id,
            PlantId = plantId,
            OrderNumber = orderNumber,
            ProductNumber = level1Line?.Number,
            Qty = orderQty < 1 ? 1 : orderQty,
        };
        _db.ScheduleOrders.Add(order);
        await _db.SaveChangesAsync();

        foreach (var line in lines)
        {
            var linePlantCode = BomFieldMappers.MapVaultPlantCode(line.PlantId);
            var item = new PartLineItem
            {
                ScheduleOrderId = order.Id,
                PartNumber = line.Number,
                Title = line.Title,
                Description = line.ItemDescription,
                Category = line.Category,
                Qty = line.Qty,
                Material = line.Material,
                Thickness = line.Thickness,
                StructCode = line.StructCode,
                Operations = line.Operations,
                IsStock = line.IsStock,
                RequiresPdf = line.RequiresPdf,
                Notes = string.IsNullOrEmpty(line.Notes) ? null : line.Notes,
                HasPdf = line.HasPdf,
                PlantIdRaw = string.IsNullOrWhiteSpace(line.PlantId) ? null : line.PlantId,
                PlantId = linePlantCode != null && plantCodeToId.TryGetValue(linePlantCode, out var lpid) ? lpid : null,
            };
            _db.PartLineItems.Add(item);
        }

        await _db.SaveChangesAsync();

        var partNumbers = lines.Select(l => l.Number).Distinct().ToList();
        var (copied, total) = await _pdfCopy.CopyForScheduleAsync(plantName, name, partNumbers);

        _log.LogInformation(
            "Imported schedule '{Name}' (order {OrderNumber}) with {Count} items. Copied {Copied}/{Total} PDFs to {Folder}",
            name, orderNumber, lines.Count, copied, total, localFolder);

        return schedule;
    }

    public async Task<List<FlatSchedulePartRow>> GetFlatSchedulePartsAsync(
        int? plantId = null, DateTime? fromDate = null, DateTime? toDate = null, string? searchTerm = null,
        bool releasedOnly = false)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var term = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();

        // Self-healing: a cross-plant PlantId can land on a PartLineItem through paths that
        // don't go through the Update*PlantAsync methods below (BOM import pulling the plant
        // straight from Vault metadata, Excel re-import, etc.). Sweep this plant's own
        // schedules on every load so a missed handoff row still gets created.
        if (plantId.HasValue)
            await EnsureHandoffsForPlantSchedulesAsync(db, plantId.Value, fromDate, toDate);

        // Direct projection: single SQL JOIN, no tracked entities, no in-memory mapping
        var rows = await db.Set<PartLineItem>()
            .Where(p => p.ScheduleOrderId != null)
            .Where(p => !plantId.HasValue
                     || p.ScheduleOrder!.Schedule.PlantId == plantId.Value
                     || (p.PlantId == plantId.Value && !p.IsStock
                         && db.ScheduleOrderHandoffs.Any(h => h.ScheduleOrderId == p.ScheduleOrderId
                                                            && h.DestinationPlantId == plantId.Value
                                                            && (h.Status == HandoffStatus.Pending || h.Status == HandoffStatus.Accepted))))
            .Where(p => !fromDate.HasValue || p.ScheduleOrder!.Schedule.ImportDate >= fromDate.Value.ToUniversalTime())
            .Where(p => !toDate.HasValue || p.ScheduleOrder!.Schedule.ImportDate < toDate.Value.ToUniversalTime().AddDays(1))
            .Where(p => !releasedOnly || p.ScheduleOrder!.Schedule.ReadyForProduction)
            .Where(p => term == null ||
                EF.Functions.ILike(p.ScheduleOrder!.Schedule.Name, $"%{term}%") ||
                EF.Functions.ILike(p.ScheduleOrder!.OrderNumber, $"%{term}%") ||
                EF.Functions.ILike(p.PartNumber, $"%{term}%") ||
                (p.Description != null && EF.Functions.ILike(p.Description, $"%{term}%")))
            .OrderByDescending(p => p.ScheduleOrder!.Schedule.ImportDate)
            .ThenBy(p => p.ScheduleOrder!.Id)
            .ThenBy(p => p.Id)
            .Select(p => new FlatSchedulePartRow
            {
                ScheduleId = p.ScheduleOrder!.ScheduleId,
                ScheduleName = p.ScheduleOrder!.Schedule.Name,
                ScheduleImportDate = p.ScheduleOrder!.Schedule.ImportDate,
                ScheduleReleased = p.ScheduleOrder!.Schedule.ReadyForProduction,
                SchedulePlantId = p.ScheduleOrder!.Schedule.PlantId,
                ScheduleOrderId = p.ScheduleOrderId!.Value,
                OrderNumber = p.ScheduleOrder!.OrderNumber,
                OrderQty = p.ScheduleOrder!.Qty,
                ProductNumber = p.ScheduleOrder!.ProductNumber,
                ScheduleOrderPlantId = p.ScheduleOrder!.PlantId,
                VaultBomImported = p.ScheduleOrder!.VaultBomImported,
                PartLineItemId = p.Id,
                PartNumber = p.PartNumber,
                Description = p.Description,
                Category = p.Category,
                Material = p.Material,
                Thickness = p.Thickness,
                Operations = p.Operations,
                SecondOperation = p.SecondOperation,
                FinishOps = p.FinishOps,
                Qty = p.Qty,
                IsStock = p.IsStock,
                HasPdf = p.HasPdf,
                RequiresPdf = p.RequiresPdf,
                Notes = p.Notes,
                PlantId = p.PlantId,
                PlantIdRaw = p.PlantIdRaw,
            })
            .ToListAsync();

        // Resolve ProductDescription per order from the "Product" category part row.
        // ProductNumber is read directly from ScheduleOrder (reliable regardless of Vault category).
        var productLookup = rows
            .Where(r => r.Category.Equals("product", StringComparison.OrdinalIgnoreCase))
            .GroupBy(r => r.ScheduleOrderId)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var row in rows)
        {
            if (productLookup.TryGetValue(row.ScheduleOrderId, out var prod))
                row.ProductDescription = prod.Description;
        }

        // Surface ScheduleOrders that have no PartLineItems yet (placeholders from the
        // Excel import flow, or orders whose Vault BOM query returned "not found").
        // These need to appear as empty groups so the user can see + edit them.
        var seenOrderIds = rows.Select(r => r.ScheduleOrderId).ToHashSet();
        var emptyOrders = await db.Set<ScheduleOrder>()
            .Where(so => !seenOrderIds.Contains(so.Id))
            .Where(so => !plantId.HasValue || so.Schedule.PlantId == plantId.Value)
            .Where(so => !fromDate.HasValue || so.Schedule.ImportDate >= fromDate.Value.ToUniversalTime())
            .Where(so => !toDate.HasValue || so.Schedule.ImportDate < toDate.Value.ToUniversalTime().AddDays(1))
            .Where(so => !releasedOnly || so.Schedule.ReadyForProduction)
            .Where(so => term == null ||
                EF.Functions.ILike(so.Schedule.Name, $"%{term}%") ||
                EF.Functions.ILike(so.OrderNumber, $"%{term}%") ||
                (so.ProductNumber != null && EF.Functions.ILike(so.ProductNumber, $"%{term}%")))
            .Select(so => new FlatSchedulePartRow
            {
                ScheduleId         = so.ScheduleId,
                ScheduleName       = so.Schedule.Name,
                ScheduleImportDate = so.Schedule.ImportDate,
                ScheduleReleased   = so.Schedule.ReadyForProduction,
                SchedulePlantId    = so.Schedule.PlantId,
                ScheduleOrderId    = so.Id,
                OrderNumber        = so.OrderNumber,
                OrderQty           = so.Qty,
                ProductNumber      = so.ProductNumber,
                ScheduleOrderPlantId = so.PlantId,
                ProductDescription = null,
                VaultBomImported   = so.VaultBomImported,
                PartLineItemId     = 0,
                PartNumber         = "",
                Description        = "",
                Category           = "",
                Material           = "",
                Thickness          = "",
                Operations         = "",
                SecondOperation    = "",
                FinishOps          = "",
                Qty                = 0,
                IsStock            = false,
                HasPdf             = false,
                RequiresPdf        = false,
                Notes              = so.Notes,
            })
            .ToListAsync();

        rows.AddRange(emptyOrders);
        return rows;
    }

    public async Task<List<FlatBatchPartRow>> GetFlatBatchPartsAsync(
        int? plantId = null, DateTime? fromDate = null, DateTime? toDate = null, string? searchTerm = null,
        bool releasedOnly = false)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var term = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();

        // Self-healing: a cross-plant PlantId can land on a PartLineItem through paths that
        // don't go through the Update*PlantAsync methods below (BOM import pulling the plant
        // straight from Vault metadata, Excel re-import, etc.). Sweep this plant's own
        // batches on every load so a missed handoff row still gets created.
        if (plantId.HasValue)
            await EnsureHandoffsForPlantBatchesAsync(db, plantId.Value, fromDate, toDate);

        // Direct projection: single SQL JOIN, no tracked entities, no in-memory mapping
        var rows = await db.Set<PartLineItem>()
            .Where(p => p.BatchProductId != null)
            .Where(p => !plantId.HasValue
                     || p.BatchProduct!.Batch.PlantId == plantId.Value
                     || (p.PlantId == plantId.Value && p.IsStock
                         && db.BatchProductHandoffs.Any(h => h.BatchProductId == p.BatchProductId
                                                           && h.DestinationPlantId == plantId.Value
                                                           && (h.Status == HandoffStatus.Pending || h.Status == HandoffStatus.Accepted))))
            .Where(p => !fromDate.HasValue || p.BatchProduct!.Batch.ImportDate >= fromDate.Value.ToUniversalTime())
            .Where(p => !toDate.HasValue || p.BatchProduct!.Batch.ImportDate < toDate.Value.ToUniversalTime().AddDays(1))
            .Where(p => !releasedOnly || p.BatchProduct!.Batch.ReadyForProduction)
            .Where(p => term == null ||
                EF.Functions.ILike(p.BatchProduct!.Batch.Name, $"%{term}%") ||
                EF.Functions.ILike(p.BatchProduct!.ProductName, $"%{term}%") ||
                EF.Functions.ILike(p.PartNumber, $"%{term}%") ||
                (p.Description != null && EF.Functions.ILike(p.Description, $"%{term}%")))
            .OrderByDescending(p => p.BatchProduct!.Batch.ImportDate)
            .ThenBy(p => p.BatchProduct!.Id)
            .ThenBy(p => p.Id)
            .Select(p => new FlatBatchPartRow
            {
                BatchId = p.BatchProduct!.BatchId,
                BatchName = p.BatchProduct!.Batch.Name,
                BatchImportDate = p.BatchProduct!.Batch.ImportDate,
                BatchReleased = p.BatchProduct!.Batch.ReadyForProduction,
                BatchPlantId = p.BatchProduct!.Batch.PlantId,
                BatchProductId = p.BatchProductId!.Value,
                ProductName = p.BatchProduct!.ProductName,
                ProductQty = p.BatchProduct!.Qty,
                BatchProductPlantId = p.BatchProduct!.PlantId,
                VaultBomImported = p.BatchProduct!.VaultBomImported,
                PartLineItemId = p.Id,
                PartNumber = p.PartNumber,
                Description = p.Description,
                Category = p.Category,
                Material = p.Material,
                Thickness = p.Thickness,
                Operations = p.Operations,
                SecondOperation = p.SecondOperation,
                FinishOps = p.FinishOps,
                Qty = p.Qty,
                IsStock = p.IsStock,
                HasPdf = p.HasPdf,
                RequiresPdf = p.RequiresPdf,
                Notes = p.Notes,
                PlantId = p.PlantId,
                PlantIdRaw = p.PlantIdRaw,
            })
            .ToListAsync();

        // Surface BatchProducts that have no PartLineItems yet (Vault import pending
        // or "not found"). Empty groups so the user can see them in the grid.
        var seenProductIds = rows.Select(r => r.BatchProductId).ToHashSet();
        var emptyProducts = await db.Set<BatchProduct>()
            .Where(bp => !seenProductIds.Contains(bp.Id))
            .Where(bp => !plantId.HasValue || bp.Batch.PlantId == plantId.Value)
            .Where(bp => !fromDate.HasValue || bp.Batch.ImportDate >= fromDate.Value.ToUniversalTime())
            .Where(bp => !toDate.HasValue || bp.Batch.ImportDate < toDate.Value.ToUniversalTime().AddDays(1))
            .Where(bp => !releasedOnly || bp.Batch.ReadyForProduction)
            .Where(bp => term == null ||
                EF.Functions.ILike(bp.Batch.Name, $"%{term}%") ||
                EF.Functions.ILike(bp.ProductName, $"%{term}%"))
            .Select(bp => new FlatBatchPartRow
            {
                BatchId          = bp.BatchId,
                BatchName        = bp.Batch.Name,
                BatchImportDate  = bp.Batch.ImportDate,
                BatchReleased    = bp.Batch.ReadyForProduction,
                BatchPlantId     = bp.Batch.PlantId,
                BatchProductId   = bp.Id,
                ProductName      = bp.ProductName,
                ProductQty       = bp.Qty,
                BatchProductPlantId = bp.PlantId,
                VaultBomImported = bp.VaultBomImported,
                PartLineItemId   = 0,
                PartNumber       = "",
                Description      = "",
                Category         = "",
                Material         = "",
                Thickness        = "",
                Operations       = "",
                SecondOperation  = "",
                FinishOps        = "",
                Qty              = 0,
                // IsStock = true so the "Hide non-stock parts" filter (which matches on this
                // field) never hides a product that simply has no BOM imported yet.
                IsStock          = true,
                HasPdf           = false,
                RequiresPdf      = false,
                Notes            = bp.Notes,
            })
            .ToListAsync();

        rows.AddRange(emptyProducts);
        return rows;
    }

    public async Task UpdateBatchNameAsync(int batchId, string name)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrEmpty(trimmed)) return;

        await using var db = await _dbFactory.CreateDbContextAsync();
        var batch = await db.Set<Batch>()
            .Include(b => b.Plant)
            .FirstOrDefaultAsync(b => b.Id == batchId);
        if (batch is null || batch.Name == trimmed) return;

        var newFolder = _pdfCopy.GetBatchFolder(batch.Plant.Name, trimmed);
        MoveLocalPdfFolder(batch.LocalPdfFolder, newFolder);

        batch.Name = trimmed;
        batch.LocalPdfFolder = newFolder;
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Moves a batch/schedule's local PDF folder to match a rename, so LocalPdfFolder
    /// never drifts from where the files actually are. If the destination already
    /// exists (e.g. a later PDF copy/refresh already recreated it under the new name),
    /// merges file-by-file instead of a straight directory move.
    /// </summary>
    private void MoveLocalPdfFolder(string? oldFolder, string newFolder)
    {
        if (string.IsNullOrEmpty(oldFolder) || !Directory.Exists(oldFolder)) return;
        if (oldFolder.Equals(newFolder, StringComparison.OrdinalIgnoreCase)) return;

        try
        {
            if (!Directory.Exists(newFolder))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(newFolder)!);
                Directory.Move(oldFolder, newFolder);
                return;
            }

            // Destination already exists — merge instead of a straight move.
            foreach (var file in Directory.GetFiles(oldFolder))
                File.Copy(file, Path.Combine(newFolder, Path.GetFileName(file)), overwrite: true);
            Directory.Delete(oldFolder, recursive: true);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not move PDF folder from {Old} to {New}", oldFolder, newFolder);
        }
    }

    public async Task UpdateBatchProductQtyAsync(int batchProductId, int qty)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Set<BatchProduct>()
            .Where(bp => bp.Id == batchProductId)
            .ExecuteUpdateAsync(s => s.SetProperty(bp => bp.Qty, qty));
    }

    public async Task UpdateBatchProductNameAsync(int batchProductId, string productName)
    {
        var trimmed = productName.Trim();
        await using var db = await _dbFactory.CreateDbContextAsync();
        var product = await db.Set<BatchProduct>().FindAsync(batchProductId);
        if (product is null) return;
        if (product.ProductName == trimmed) return; // same value — no need to reset VaultBomImported
        product.ProductName = trimmed;
        product.VaultBomImported = false;
        await db.SaveChangesAsync();
    }

    public async Task UpdateScheduleOrderQtyAsync(int scheduleOrderId, int qty)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Set<ScheduleOrder>()
            .Where(so => so.Id == scheduleOrderId)
            .ExecuteUpdateAsync(s => s.SetProperty(so => so.Qty, qty));
    }

    public async Task UpdateScheduleOrderProductNumberAsync(int scheduleOrderId, string? productNumber)
    {
        var trimmed = string.IsNullOrWhiteSpace(productNumber) ? null : productNumber.Trim();
        await using var db = await _dbFactory.CreateDbContextAsync();
        var order = await db.Set<ScheduleOrder>().FindAsync(scheduleOrderId);
        if (order is null) return;
        if (order.ProductNumber == trimmed) return; // same value — no need to reset VaultBomImported
        order.ProductNumber = trimmed;
        order.VaultBomImported = false;
        await db.SaveChangesAsync();
    }

    public async Task UpdatePartIsStockAsync(int partLineItemId, bool isStock)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var part = await db.Set<PartLineItem>().FindAsync(partLineItemId);
        if (part is null) return;
        part.IsStock = isStock;
        await db.SaveChangesAsync();
    }

    public async Task UpdateBatchProductPlantAsync(int batchProductId, int plantId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var batchId = await db.Set<BatchProduct>()
            .Where(bp => bp.Id == batchProductId)
            .Select(bp => bp.BatchId)
            .FirstOrDefaultAsync();
        await db.Set<BatchProduct>()
            .Where(bp => bp.Id == batchProductId)
            .ExecuteUpdateAsync(s => s.SetProperty(bp => bp.PlantId, plantId));
        await db.Set<PartLineItem>()
            .Where(p => p.BatchProductId == batchProductId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.PlantId, plantId)
                .SetProperty(p => p.PlantIdRaw, (string?)null));
        if (batchId != 0)
            await EnsureBatchHandoffsAsync(db, batchId);
    }

    public async Task UpdateScheduleOrderPlantAsync(int scheduleOrderId, int plantId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var scheduleId = await db.Set<ScheduleOrder>()
            .Where(so => so.Id == scheduleOrderId)
            .Select(so => so.ScheduleId)
            .FirstOrDefaultAsync();
        await db.Set<ScheduleOrder>()
            .Where(so => so.Id == scheduleOrderId)
            .ExecuteUpdateAsync(s => s.SetProperty(so => so.PlantId, plantId));
        await db.Set<PartLineItem>()
            .Where(p => p.ScheduleOrderId == scheduleOrderId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.PlantId, plantId)
                .SetProperty(p => p.PlantIdRaw, (string?)null));
        if (scheduleId != 0)
            await EnsureScheduleHandoffsAsync(db, scheduleId);
    }

    public async Task UpdatePartPlantAsync(int partLineItemId, int? plantId, string? plantIdRaw)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var part = await db.Set<PartLineItem>()
            .AsNoTracking()
            .Include(p => p.BatchProduct)
            .Include(p => p.ScheduleOrder)
            .FirstOrDefaultAsync(p => p.Id == partLineItemId);
        if (part is null) return;

        await db.Set<PartLineItem>()
            .Where(p => p.Id == partLineItemId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.PlantId, plantId)
                .SetProperty(p => p.PlantIdRaw, plantIdRaw));

        if (part.BatchProductId.HasValue)
            await EnsureBatchHandoffsAsync(db, part.BatchProduct!.BatchId);
        else if (part.ScheduleOrderId.HasValue)
            await EnsureScheduleHandoffsAsync(db, part.ScheduleOrder!.ScheduleId);
    }

    public async Task<int> CountSiblingsByPartNumberAsync(int partLineItemId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var part = await db.Set<PartLineItem>()
            .AsNoTracking()
            .Include(p => p.BatchProduct)
            .Include(p => p.ScheduleOrder)
            .FirstOrDefaultAsync(p => p.Id == partLineItemId);
        if (part is null) return 0;

        if (part.BatchProductId.HasValue)
        {
            var batchId = part.BatchProduct!.BatchId;
            return await db.Set<PartLineItem>()
                .CountAsync(p => p.BatchProduct!.BatchId == batchId && p.PartNumber == part.PartNumber);
        }
        if (part.ScheduleOrderId.HasValue)
        {
            var schedId = part.ScheduleOrder!.ScheduleId;
            return await db.Set<PartLineItem>()
                .CountAsync(p => p.ScheduleOrder!.ScheduleId == schedId && p.PartNumber == part.PartNumber);
        }
        return 0;
    }

    public async Task UpdatePartIsStockForAllSiblingsAsync(int partLineItemId, bool isStock)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var part = await db.Set<PartLineItem>()
            .AsNoTracking()
            .Include(p => p.BatchProduct)
            .Include(p => p.ScheduleOrder)
            .FirstOrDefaultAsync(p => p.Id == partLineItemId);
        if (part is null) return;

        if (part.BatchProductId.HasValue)
        {
            var batchId = part.BatchProduct!.BatchId;
            await db.Set<PartLineItem>()
                .Where(p => p.BatchProduct!.BatchId == batchId && p.PartNumber == part.PartNumber)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsStock, isStock));
        }
        else if (part.ScheduleOrderId.HasValue)
        {
            var schedId = part.ScheduleOrder!.ScheduleId;
            await db.Set<PartLineItem>()
                .Where(p => p.ScheduleOrder!.ScheduleId == schedId && p.PartNumber == part.PartNumber)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsStock, isStock));
        }
    }

    public async Task UpdatePartPlantForAllSiblingsAsync(int partLineItemId, int? plantId, string? plantIdRaw)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var part = await db.Set<PartLineItem>()
            .AsNoTracking()
            .Include(p => p.BatchProduct)
            .Include(p => p.ScheduleOrder)
            .FirstOrDefaultAsync(p => p.Id == partLineItemId);
        if (part is null) return;

        if (part.BatchProductId.HasValue)
        {
            var batchId = part.BatchProduct!.BatchId;
            await db.Set<PartLineItem>()
                .Where(p => p.BatchProduct!.BatchId == batchId && p.PartNumber == part.PartNumber)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.PlantId, plantId)
                    .SetProperty(p => p.PlantIdRaw, plantIdRaw));
            await EnsureBatchHandoffsAsync(db, batchId);
        }
        else if (part.ScheduleOrderId.HasValue)
        {
            var schedId = part.ScheduleOrder!.ScheduleId;
            await db.Set<PartLineItem>()
                .Where(p => p.ScheduleOrder!.ScheduleId == schedId && p.PartNumber == part.PartNumber)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.PlantId, plantId)
                    .SetProperty(p => p.PlantIdRaw, plantIdRaw));
            await EnsureScheduleHandoffsAsync(db, schedId);
        }
    }

    public async Task MarkAllPartsAsStockForBatchProductAsync(int batchProductId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Set<PartLineItem>()
            .Where(p => p.BatchProductId == batchProductId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsStock, true));
    }

    public async Task MarkAllPartsAsStockForBatchAsync(int batchId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Set<PartLineItem>()
            .Where(p => p.BatchProduct!.BatchId == batchId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsStock, true));
    }

    public async Task MarkMatchingPartsAsStockAcrossBatchAsync(int batchProductId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var batchProduct = await db.Set<BatchProduct>().AsNoTracking().FirstOrDefaultAsync(bp => bp.Id == batchProductId);
        if (batchProduct is null) return;
        var partNumbers = await db.Set<PartLineItem>()
            .AsNoTracking()
            .Where(p => p.BatchProductId == batchProductId)
            .Select(p => p.PartNumber)
            .Distinct()
            .ToListAsync();
        if (partNumbers.Count == 0) return;
        await db.Set<PartLineItem>()
            .Where(p => p.BatchProduct!.BatchId == batchProduct.BatchId && partNumbers.Contains(p.PartNumber))
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsStock, true));
    }

    public async Task RemovePartLineItemAsync(int partLineItemId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var part = await db.Set<PartLineItem>().FindAsync(partLineItemId);
        if (part is null) return;
        db.Set<PartLineItem>().Remove(part);
        await db.SaveChangesAsync();
    }

    public async Task DeleteBatchAsync(int batchId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var localFolder = await db.Batches
            .Where(b => b.Id == batchId)
            .Select(b => b.LocalPdfFolder)
            .FirstOrDefaultAsync();

        await db.Set<NestBatch>()
            .Where(nb => nb.BatchId == batchId)
            .ExecuteDeleteAsync();
        await db.Set<PartLineItem>()
            .Where(p => p.BatchProduct!.BatchId == batchId)
            .ExecuteDeleteAsync();
        await db.Set<BatchProduct>()
            .Where(bp => bp.BatchId == batchId)
            .ExecuteDeleteAsync();
        await db.Batches.Where(b => b.Id == batchId).ExecuteDeleteAsync();

        DeleteFolder(localFolder);
    }

    public async Task DeleteBatchProductAsync(int batchProductId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var batchProduct = await db.Set<BatchProduct>()
            .Include(bp => bp.Batch)
            .Include(bp => bp.Parts)
            .FirstOrDefaultAsync(bp => bp.Id == batchProductId);
        if (batchProduct is null) return;

        var partNumbers = batchProduct.Parts.Select(p => p.PartNumber).ToHashSet();
        var localFolder = batchProduct.Batch.LocalPdfFolder;

        await db.Set<PartLineItem>()
            .Where(p => p.BatchProductId == batchProductId)
            .ExecuteDeleteAsync();
        await db.Set<BatchProduct>()
            .Where(bp => bp.Id == batchProductId)
            .ExecuteDeleteAsync();

        // Delete PDF files no longer referenced by any other part in this batch
        if (!string.IsNullOrEmpty(localFolder) && partNumbers.Count > 0)
        {
            var stillUsed = await db.Set<PartLineItem>()
                .Where(p => p.BatchProduct!.BatchId == batchProduct.BatchId
                         && partNumbers.Contains(p.PartNumber))
                .Select(p => p.PartNumber)
                .Distinct()
                .ToListAsync();

            foreach (var pn in partNumbers.Except(stillUsed))
                DeleteFile(Path.Combine(localFolder, pn + ".pdf"));
        }
    }

    public async Task DeleteScheduleAsync(int scheduleId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var localFolder = await db.Schedules
            .Where(s => s.Id == scheduleId)
            .Select(s => s.LocalPdfFolder)
            .FirstOrDefaultAsync();

        await db.Set<PartLineItem>()
            .Where(p => p.ScheduleOrder!.ScheduleId == scheduleId)
            .ExecuteDeleteAsync();
        await db.Set<ScheduleOrder>()
            .Where(so => so.ScheduleId == scheduleId)
            .ExecuteDeleteAsync();
        await db.Schedules.Where(s => s.Id == scheduleId).ExecuteDeleteAsync();

        DeleteFolder(localFolder);
    }

    public async Task DeleteScheduleOrderAsync(int scheduleOrderId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var scheduleOrder = await db.Set<ScheduleOrder>()
            .Include(so => so.Schedule)
            .Include(so => so.Parts)
            .FirstOrDefaultAsync(so => so.Id == scheduleOrderId);
        if (scheduleOrder is null) return;

        var partNumbers = scheduleOrder.Parts.Select(p => p.PartNumber).ToHashSet();
        var localFolder = scheduleOrder.Schedule.LocalPdfFolder;

        await db.Set<PartLineItem>()
            .Where(p => p.ScheduleOrderId == scheduleOrderId)
            .ExecuteDeleteAsync();
        await db.Set<ScheduleOrder>()
            .Where(so => so.Id == scheduleOrderId)
            .ExecuteDeleteAsync();

        // Delete PDF files no longer referenced by any other part in this schedule
        if (!string.IsNullOrEmpty(localFolder) && partNumbers.Count > 0)
        {
            var stillUsed = await db.Set<PartLineItem>()
                .Where(p => p.ScheduleOrder!.ScheduleId == scheduleOrder.ScheduleId
                         && partNumbers.Contains(p.PartNumber))
                .Select(p => p.PartNumber)
                .Distinct()
                .ToListAsync();

            foreach (var pn in partNumbers.Except(stillUsed))
                DeleteFile(Path.Combine(localFolder, pn + ".pdf"));
        }
    }

    // Only Plant 1 and Plant 2 have nesting/manufacturing capability. A part resolved to any
    // other plant (e.g. Plant 3) keeps the original skip-and-log behavior — unchanged. The
    // custom-plant-value feature is how Plant 3 work gets "ordered" outside this automated path.
    private static readonly string[] NestingCapablePlantCodes = ["P1", "P2"];

    /// <summary>
    /// Given a home plant, returns the other nesting-capable plant's Id — or null if the home
    /// plant itself isn't one of the nesting-capable pair, or the other one isn't configured/active.
    /// </summary>
    private static async Task<int?> GetOtherNestingCapablePlantIdAsync(ApplicationDbContext db, int homePlantId)
    {
        var homeCode = await db.Plants.Where(p => p.Id == homePlantId).Select(p => p.Code).FirstOrDefaultAsync();
        if (homeCode is null || !NestingCapablePlantCodes.Contains(homeCode, StringComparer.OrdinalIgnoreCase))
            return null;

        var otherCode = NestingCapablePlantCodes.FirstOrDefault(c => !string.Equals(c, homeCode, StringComparison.OrdinalIgnoreCase));
        if (otherCode is null) return null;

        return await db.Plants
            .Where(p => p.Code == otherCode && p.IsActive)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// True when a part has no Plant override (inherits the parent's) or its resolved PlantId
    /// matches the parent Schedule/Batch's own PlantId.
    /// </summary>
    private static bool PartMatchesParentPlant(PartLineItem part, int parentPlantId) =>
        (part.PlantId is null && part.PlantIdRaw is null) || part.PlantId == parentPlantId;

    /// <summary>
    /// Groups parts by PartNumber and buckets each group into "home" (matches parentPlantId),
    /// "other" (resolves to otherPlantId, the nesting-capable handoff destination), or neither
    /// (skipped — e.g. Plant 3, or an unresolved custom value). Qty is summed per bucket using
    /// each line's own ParentQty (order/product qty), matching the original per-release-type math.
    /// </summary>
    private static (List<(PartLineItem Sample, int TotalQty)> Home,
                     List<(PartLineItem Sample, int TotalQty)> Other,
                     int SkippedLines)
        BucketPartsByPlant(IEnumerable<(PartLineItem Part, int ParentQty)> parts, int parentPlantId, int? otherPlantId)
    {
        var home = new List<(PartLineItem Sample, int TotalQty)>();
        var other = new List<(PartLineItem Sample, int TotalQty)>();
        int skipped = 0;

        foreach (var byPartNumber in parts.GroupBy(x => x.Part.PartNumber, StringComparer.OrdinalIgnoreCase))
        {
            var members = byPartNumber.ToList();
            var homeMembers = members.Where(x => PartMatchesParentPlant(x.Part, parentPlantId)).ToList();
            var otherMembers = otherPlantId.HasValue
                ? members.Where(x => x.Part.PlantId == otherPlantId.Value).ToList()
                : [];

            if (homeMembers.Count > 0)
                home.Add((homeMembers[0].Part, homeMembers.Sum(x => x.Part.Qty * x.ParentQty)));
            if (otherMembers.Count > 0)
                other.Add((otherMembers[0].Part, otherMembers.Sum(x => x.Part.Qty * x.ParentQty)));

            skipped += members.Count - homeMembers.Count - otherMembers.Count;
        }

        return (home, other, skipped);
    }

    private async Task AddOrderItemsAsync(ApplicationDbContext db, int nestOrderId, List<(PartLineItem Sample, int TotalQty)> bucket)
    {
        foreach (var (sample, totalQty) in bucket)
        {
            var part = await FindOrCreatePartAsync(db, sample);
            db.OrderItems.Add(new OrderItem { NestOrderId = nestOrderId, PartId = part.Id, QtyRequired = totalQty });
        }
    }

    private async Task AddBatchItemsAsync(ApplicationDbContext db, int nestBatchId, List<(PartLineItem Sample, int TotalQty)> bucket)
    {
        foreach (var (sample, totalQty) in bucket)
        {
            var part = await FindOrCreatePartAsync(db, sample);
            db.BatchItems.Add(new BatchItem { NestBatchId = nestBatchId, PartId = part.Id, QtyRequired = totalQty });
        }
    }

    /// <summary>
    /// Scans every BatchProduct in the given Batch for *stock* parts (only stock parts are
    /// ever released for a Batch — see ReleaseBatchToProductionAsync) resolving to the other
    /// nesting-capable plant: creates a Draft BatchProductHandoff for any (BatchProduct,
    /// otherPlant) pair that doesn't already have one, and retires (deletes) any live
    /// Draft/Pending/Accepted-unreleased row that no longer has a matching part — e.g. the
    /// user switched the part(s) back to the home plant before ever notifying the other
    /// plant. Rejected and already-Released rows are permanent records and never retired.
    /// Called after any part-plant mutation so the handoff review surfaces pre-release.
    /// </summary>
    private async Task EnsureBatchHandoffsAsync(ApplicationDbContext db, int batchId)
    {
        var batch = await db.Batches
            .Include(b => b.BatchProducts).ThenInclude(bp => bp.Parts)
            .FirstOrDefaultAsync(b => b.Id == batchId);
        if (batch is null) return;

        var otherPlantId = await GetOtherNestingCapablePlantIdAsync(db, batch.PlantId);
        if (otherPlantId is null) return;

        foreach (var bp in batch.BatchProducts)
        {
            var hasMatch = bp.Parts.Any(p => p.PlantId == otherPlantId.Value && p.IsStock);
            var existing = await db.BatchProductHandoffs
                .Where(h => h.BatchProductId == bp.Id && h.DestinationPlantId == otherPlantId.Value)
                .OrderByDescending(h => h.Id)
                .FirstOrDefaultAsync();

            if (!hasMatch)
            {
                if (existing is not null && existing.Status != HandoffStatus.Rejected && existing.ReleasedDate is null)
                    db.BatchProductHandoffs.Remove(existing);
                continue;
            }

            if (existing is null)
                db.BatchProductHandoffs.Add(new BatchProductHandoff
                {
                    BatchProductId = bp.Id,
                    DestinationPlantId = otherPlantId.Value,
                    Status = HandoffStatus.Draft,
                });
        }
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// ScheduleOrder equivalent of <see cref="EnsureBatchHandoffsAsync"/> — but for a
    /// ScheduleOrder, it's the *non-stock* ("make to order") parts that are ever released
    /// (see ReleaseScheduleToProductionAsync), so those are the ones considered here.
    /// </summary>
    private async Task EnsureScheduleHandoffsAsync(ApplicationDbContext db, int scheduleId)
    {
        var schedule = await db.Schedules
            .Include(s => s.ScheduleOrders).ThenInclude(so => so.Parts)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);
        if (schedule is null) return;

        var otherPlantId = await GetOtherNestingCapablePlantIdAsync(db, schedule.PlantId);
        if (otherPlantId is null) return;

        foreach (var so in schedule.ScheduleOrders)
        {
            var hasMatch = so.Parts.Any(p => p.PlantId == otherPlantId.Value && !p.IsStock);
            var existing = await db.ScheduleOrderHandoffs
                .Where(h => h.ScheduleOrderId == so.Id && h.DestinationPlantId == otherPlantId.Value)
                .OrderByDescending(h => h.Id)
                .FirstOrDefaultAsync();

            if (!hasMatch)
            {
                if (existing is not null && existing.Status != HandoffStatus.Rejected && existing.ReleasedDate is null)
                    db.ScheduleOrderHandoffs.Remove(existing);
                continue;
            }

            if (existing is null)
                db.ScheduleOrderHandoffs.Add(new ScheduleOrderHandoff
                {
                    ScheduleOrderId = so.Id,
                    DestinationPlantId = otherPlantId.Value,
                    Status = HandoffStatus.Draft,
                });
        }
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Self-healing sweep called from <see cref="GetFlatBatchPartsAsync"/>, scoped to the same
    /// plant + date range the grid itself is about to show (not the plant's entire history).
    /// Set-based queries instead of a per-batch entity-graph load, so it stays cheap even when
    /// a plant has a large batch history. Only *stock* parts count (see
    /// <see cref="EnsureBatchHandoffsAsync"/>), and stale live rows are retired the same way.
    /// </summary>
    private async Task EnsureHandoffsForPlantBatchesAsync(ApplicationDbContext db, int plantId, DateTime? fromDate, DateTime? toDate)
    {
        var otherPlantId = await GetOtherNestingCapablePlantIdAsync(db, plantId);
        if (otherPlantId is null) return;

        var candidateBatchProductIds = await db.Set<PartLineItem>()
            .Where(p => p.BatchProductId != null && p.PlantId == otherPlantId.Value && p.IsStock)
            .Where(p => p.BatchProduct!.Batch.PlantId == plantId)
            .Where(p => !fromDate.HasValue || p.BatchProduct!.Batch.ImportDate >= fromDate.Value.ToUniversalTime())
            .Where(p => !toDate.HasValue || p.BatchProduct!.Batch.ImportDate < toDate.Value.ToUniversalTime().AddDays(1))
            .Select(p => p.BatchProductId!.Value)
            .Distinct()
            .ToListAsync();
        var candidateSet = candidateBatchProductIds.ToHashSet();

        // Retire: live (not Rejected, not Released) rows in view whose BatchProduct no longer
        // has a matching stock part destined for the other plant.
        var liveHandoffs = await db.BatchProductHandoffs
            .Where(h => h.DestinationPlantId == otherPlantId.Value
                     && h.Status != HandoffStatus.Rejected && h.ReleasedDate == null
                     && h.BatchProduct.Batch.PlantId == plantId
                     && (!fromDate.HasValue || h.BatchProduct.Batch.ImportDate >= fromDate.Value.ToUniversalTime())
                     && (!toDate.HasValue || h.BatchProduct.Batch.ImportDate < toDate.Value.ToUniversalTime().AddDays(1)))
            .ToListAsync();
        var toRetire = liveHandoffs.Where(h => !candidateSet.Contains(h.BatchProductId)).ToList();
        if (toRetire.Count > 0)
            db.BatchProductHandoffs.RemoveRange(toRetire);

        // Create: candidates with no existing row at all (any status) for this pair.
        List<int> missingIds = new();
        if (candidateBatchProductIds.Count > 0)
        {
            var existingIds = await db.BatchProductHandoffs
                .Where(h => h.DestinationPlantId == otherPlantId.Value && candidateBatchProductIds.Contains(h.BatchProductId))
                .Select(h => h.BatchProductId)
                .ToListAsync();
            missingIds = candidateBatchProductIds.Except(existingIds).ToList();
            foreach (var id in missingIds)
                db.BatchProductHandoffs.Add(new BatchProductHandoff
                {
                    BatchProductId = id,
                    DestinationPlantId = otherPlantId.Value,
                    Status = HandoffStatus.Draft,
                });
        }

        if (toRetire.Count > 0 || missingIds.Count > 0)
            await db.SaveChangesAsync();
    }

    /// <summary>
    /// ScheduleOrder equivalent of <see cref="EnsureHandoffsForPlantBatchesAsync"/> — only
    /// *non-stock* ("make to order") parts count, matching <see cref="EnsureScheduleHandoffsAsync"/>.
    /// </summary>
    private async Task EnsureHandoffsForPlantSchedulesAsync(ApplicationDbContext db, int plantId, DateTime? fromDate, DateTime? toDate)
    {
        var otherPlantId = await GetOtherNestingCapablePlantIdAsync(db, plantId);
        if (otherPlantId is null) return;

        var candidateScheduleOrderIds = await db.Set<PartLineItem>()
            .Where(p => p.ScheduleOrderId != null && p.PlantId == otherPlantId.Value && !p.IsStock)
            .Where(p => p.ScheduleOrder!.Schedule.PlantId == plantId)
            .Where(p => !fromDate.HasValue || p.ScheduleOrder!.Schedule.ImportDate >= fromDate.Value.ToUniversalTime())
            .Where(p => !toDate.HasValue || p.ScheduleOrder!.Schedule.ImportDate < toDate.Value.ToUniversalTime().AddDays(1))
            .Select(p => p.ScheduleOrderId!.Value)
            .Distinct()
            .ToListAsync();
        var candidateSet = candidateScheduleOrderIds.ToHashSet();

        var liveHandoffs = await db.ScheduleOrderHandoffs
            .Where(h => h.DestinationPlantId == otherPlantId.Value
                     && h.Status != HandoffStatus.Rejected && h.ReleasedDate == null
                     && h.ScheduleOrder.Schedule.PlantId == plantId
                     && (!fromDate.HasValue || h.ScheduleOrder.Schedule.ImportDate >= fromDate.Value.ToUniversalTime())
                     && (!toDate.HasValue || h.ScheduleOrder.Schedule.ImportDate < toDate.Value.ToUniversalTime().AddDays(1)))
            .ToListAsync();
        var toRetire = liveHandoffs.Where(h => !candidateSet.Contains(h.ScheduleOrderId)).ToList();
        if (toRetire.Count > 0)
            db.ScheduleOrderHandoffs.RemoveRange(toRetire);

        List<int> missingIds = new();
        if (candidateScheduleOrderIds.Count > 0)
        {
            var existingIds = await db.ScheduleOrderHandoffs
                .Where(h => h.DestinationPlantId == otherPlantId.Value && candidateScheduleOrderIds.Contains(h.ScheduleOrderId))
                .Select(h => h.ScheduleOrderId)
                .ToListAsync();
            missingIds = candidateScheduleOrderIds.Except(existingIds).ToList();
            foreach (var id in missingIds)
                db.ScheduleOrderHandoffs.Add(new ScheduleOrderHandoff
                {
                    ScheduleOrderId = id,
                    DestinationPlantId = otherPlantId.Value,
                    Status = HandoffStatus.Draft,
                });
        }

        if (toRetire.Count > 0 || missingIds.Count > 0)
            await db.SaveChangesAsync();
    }

    public async Task AcceptBatchProductHandoffAsync(int handoffId, int userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.BatchProductHandoffs
            .Where(h => h.Id == handoffId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(h => h.Status, HandoffStatus.Accepted)
                .SetProperty(h => h.AcceptedDate, DateTime.UtcNow)
                .SetProperty(h => h.AcceptedByUserId, userId));
    }

    public async Task RejectBatchProductHandoffAsync(int handoffId, int userId, string? reason)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.BatchProductHandoffs
            .Where(h => h.Id == handoffId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(h => h.Status, HandoffStatus.Rejected)
                .SetProperty(h => h.RejectedDate, DateTime.UtcNow)
                .SetProperty(h => h.RejectedByUserId, userId)
                .SetProperty(h => h.RejectionReason, reason));
    }

    public async Task<int> ResubmitBatchProductHandoffAsync(int batchProductId, int destinationPlantId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var latest = await db.BatchProductHandoffs
            .Where(h => h.BatchProductId == batchProductId && h.DestinationPlantId == destinationPlantId)
            .OrderByDescending(h => h.Id)
            .FirstOrDefaultAsync();
        if (latest is null || latest.Status != HandoffStatus.Rejected) return 0;

        var hasLiveParts = await db.Set<PartLineItem>()
            .AnyAsync(p => p.BatchProductId == batchProductId && p.PlantId == destinationPlantId && p.IsStock);
        if (!hasLiveParts) return 0;

        db.BatchProductHandoffs.Add(new BatchProductHandoff
        {
            BatchProductId = batchProductId,
            DestinationPlantId = destinationPlantId,
            Status = HandoffStatus.Pending,
        });
        await db.SaveChangesAsync();
        return 1;
    }

    public async Task AcceptScheduleOrderHandoffAsync(int handoffId, int userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.ScheduleOrderHandoffs
            .Where(h => h.Id == handoffId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(h => h.Status, HandoffStatus.Accepted)
                .SetProperty(h => h.AcceptedDate, DateTime.UtcNow)
                .SetProperty(h => h.AcceptedByUserId, userId));
    }

    public async Task RejectScheduleOrderHandoffAsync(int handoffId, int userId, string? reason)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.ScheduleOrderHandoffs
            .Where(h => h.Id == handoffId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(h => h.Status, HandoffStatus.Rejected)
                .SetProperty(h => h.RejectedDate, DateTime.UtcNow)
                .SetProperty(h => h.RejectedByUserId, userId)
                .SetProperty(h => h.RejectionReason, reason));
    }

    public async Task<int> ResubmitScheduleOrderHandoffAsync(int scheduleOrderId, int destinationPlantId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var latest = await db.ScheduleOrderHandoffs
            .Where(h => h.ScheduleOrderId == scheduleOrderId && h.DestinationPlantId == destinationPlantId)
            .OrderByDescending(h => h.Id)
            .FirstOrDefaultAsync();
        if (latest is null || latest.Status != HandoffStatus.Rejected) return 0;

        var hasLiveParts = await db.Set<PartLineItem>()
            .AnyAsync(p => p.ScheduleOrderId == scheduleOrderId && p.PlantId == destinationPlantId && !p.IsStock);
        if (!hasLiveParts) return 0;

        db.ScheduleOrderHandoffs.Add(new ScheduleOrderHandoff
        {
            ScheduleOrderId = scheduleOrderId,
            DestinationPlantId = destinationPlantId,
            Status = HandoffStatus.Pending,
        });
        await db.SaveChangesAsync();
        return 1;
    }

    public async Task ReleaseBatchToProductionAsync(int batchId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var batch = await db.Batches
            .Include(b => b.BatchProducts)
                .ThenInclude(bp => bp.Parts)
            .FirstOrDefaultAsync(b => b.Id == batchId)
            ?? throw new InvalidOperationException($"Batch {batchId} not found.");

        if (batch.ReadyForProduction)
            throw new InvalidOperationException($"Batch '{batch.Name}' has already been released to production.");

        batch.ReadyForProduction = true;

        var nestBatch = new NestBatch
        {
            BatchId   = batch.Id,
            PlantId   = batch.PlantId,
            EntryDate = DateTime.UtcNow,
        };
        db.NestBatches.Add(nestBatch);
        await db.SaveChangesAsync(); // get nestBatch.Id

        // Home-plant parts only — the other plant's accepted handoff parts are released
        // independently, whenever that plant is ready, via ReleaseBatchProductHandoffAsync.
        var homeBucket = new List<(PartLineItem Sample, int TotalQty)>();
        int skippedCount = 0;

        foreach (var bp in batch.BatchProducts)
        {
            var laserStockParts = bp.Parts
                .Where(p => p.Operations == "Laser" && p.IsStock)
                .Select(p => (Part: p, ParentQty: bp.Qty));

            var (home, _, skipped) = BucketPartsByPlant(laserStockParts, batch.PlantId, otherPlantId: null);
            homeBucket.AddRange(home);
            skippedCount += skipped;
        }

        if (skippedCount > 0)
            _log.LogInformation(
                "Batch '{Name}': {Count} part line(s) skipped at release — Plant differs from the batch's own Plant {PlantId}",
                batch.Name, skippedCount, batch.PlantId);

        await AddBatchItemsAsync(db, nestBatch.Id, homeBucket);

        await db.SaveChangesAsync();
        _log.LogInformation("Batch '{Name}' (Id={Id}) released to production.", batch.Name, batch.Id);
    }

    public async Task ReleaseScheduleToProductionAsync(int scheduleId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var schedule = await db.Schedules
            .Include(s => s.ScheduleOrders)
                .ThenInclude(so => so.Parts)
            .FirstOrDefaultAsync(s => s.Id == scheduleId)
            ?? throw new InvalidOperationException($"Schedule {scheduleId} not found.");

        if (schedule.ReadyForProduction)
            throw new InvalidOperationException($"Schedule '{schedule.Name}' has already been released to production.");

        schedule.ReadyForProduction = true;
        await db.SaveChangesAsync();

        // Home-plant parts only — the other plant's accepted handoff parts are released
        // independently, whenever that plant is ready, via ReleaseScheduleOrderHandoffAsync.
        foreach (var order in schedule.ScheduleOrders)
        {
            var nestOrder = new NestOrder
            {
                ScheduleOrderId = order.Id,
                PlantId         = schedule.PlantId,
                EntryDate       = DateTime.UtcNow,
            };
            db.NestOrders.Add(nestOrder);
            await db.SaveChangesAsync(); // get nestOrder.Id

            var laserParts = order.Parts
                .Where(p => p.Operations == "Laser" && !p.IsStock)
                .Select(p => (Part: p, ParentQty: order.Qty));

            var (home, _, skipped) = BucketPartsByPlant(laserParts, schedule.PlantId, otherPlantId: null);

            if (skipped > 0)
                _log.LogInformation(
                    "Schedule '{Name}' order {Order}: {Count} part line(s) skipped at release — Plant differs from the schedule's own Plant {PlantId}",
                    schedule.Name, order.OrderNumber, skipped, schedule.PlantId);

            await AddOrderItemsAsync(db, nestOrder.Id, home);
        }

        await db.SaveChangesAsync();
        _log.LogInformation("Schedule '{Name}' (Id={Id}) released to production.", schedule.Name, schedule.Id);
    }

    public async Task<int> NotifyBatchHandoffsAsync(int batchId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.BatchProductHandoffs
            .Where(h => h.BatchProduct.BatchId == batchId && h.Status == HandoffStatus.Draft)
            .ExecuteUpdateAsync(s => s.SetProperty(h => h.Status, HandoffStatus.Pending));
    }

    public async Task<int> NotifyScheduleHandoffsAsync(int scheduleId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.ScheduleOrderHandoffs
            .Where(h => h.ScheduleOrder.ScheduleId == scheduleId && h.Status == HandoffStatus.Draft)
            .ExecuteUpdateAsync(s => s.SetProperty(h => h.Status, HandoffStatus.Pending));
    }

    /// <summary>
    /// Releases every Accepted, not-yet-released BatchProductHandoff for (batchId,
    /// destinationPlantId) in one action — matching the existing "one release click per
    /// container" pattern (ReleaseBatchToProductionAsync creates a single NestBatch covering
    /// every BatchProduct; this does the same, just for the destination plant's accepted
    /// slice instead of the home plant's). Throws if nothing qualifies.
    /// </summary>
    public async Task<int> ReleaseBatchHandoffsAsync(int batchId, int destinationPlantId, int userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var handoffs = await db.BatchProductHandoffs
            .Include(h => h.BatchProduct).ThenInclude(bp => bp.Parts)
            .Where(h => h.BatchProduct.BatchId == batchId && h.DestinationPlantId == destinationPlantId
                     && h.Status == HandoffStatus.Accepted && h.ReleasedDate == null)
            .ToListAsync();
        if (handoffs.Count == 0)
            throw new InvalidOperationException("Nothing accepted and ready to release for this plant.");

        var combined = handoffs
            .SelectMany(h => h.BatchProduct.Parts
                .Where(p => p.Operations == "Laser" && p.IsStock && p.PlantId == destinationPlantId)
                .Select(p => (Part: p, ParentQty: h.BatchProduct.Qty)))
            .GroupBy(x => x.Part.PartNumber, StringComparer.OrdinalIgnoreCase)
            .Select(g => (Sample: g.First().Part, TotalQty: g.Sum(x => x.Part.Qty * x.ParentQty)))
            .ToList();

        var nestBatch = new NestBatch
        {
            BatchId   = batchId,
            PlantId   = destinationPlantId,
            EntryDate = DateTime.UtcNow,
        };
        db.NestBatches.Add(nestBatch);
        await db.SaveChangesAsync(); // get nestBatch.Id
        await AddBatchItemsAsync(db, nestBatch.Id, combined);

        foreach (var h in handoffs)
        {
            h.ReleasedDate = DateTime.UtcNow;
            h.ReleasedByUserId = userId;
        }
        await db.SaveChangesAsync();

        _log.LogInformation(
            "Batch {BatchId}: released {HandoffCount} accepted handoff(s) ({PartCount} part line(s)) to Plant {PlantId}",
            batchId, handoffs.Count, combined.Count, destinationPlantId);
        return nestBatch.Id;
    }

    /// <summary>
    /// Releases every Accepted, not-yet-released ScheduleOrderHandoff for (scheduleId,
    /// destinationPlantId) in one action — matching the existing "one release click per
    /// container" pattern (ReleaseScheduleToProductionAsync creates one NestOrder per
    /// ScheduleOrder in a single click; this does the same for the destination plant's
    /// accepted slice). Returns the new NestOrder ids. Throws if nothing qualifies.
    /// </summary>
    public async Task<List<int>> ReleaseScheduleHandoffsAsync(int scheduleId, int destinationPlantId, int userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var handoffs = await db.ScheduleOrderHandoffs
            .Include(h => h.ScheduleOrder).ThenInclude(so => so.Parts)
            .Where(h => h.ScheduleOrder.ScheduleId == scheduleId && h.DestinationPlantId == destinationPlantId
                     && h.Status == HandoffStatus.Accepted && h.ReleasedDate == null)
            .ToListAsync();
        if (handoffs.Count == 0)
            throw new InvalidOperationException("Nothing accepted and ready to release for this plant.");

        var newIds = new List<int>();
        foreach (var h in handoffs)
        {
            var matchingParts = h.ScheduleOrder.Parts
                .Where(p => p.Operations == "Laser" && !p.IsStock && p.PlantId == destinationPlantId)
                .GroupBy(p => p.PartNumber, StringComparer.OrdinalIgnoreCase)
                .Select(g => (Sample: g.First(), TotalQty: g.Sum(p => p.Qty) * h.ScheduleOrder.Qty))
                .ToList();

            var nestOrder = new NestOrder
            {
                ScheduleOrderId = h.ScheduleOrderId,
                PlantId         = destinationPlantId,
                EntryDate       = DateTime.UtcNow,
            };
            db.NestOrders.Add(nestOrder);
            await db.SaveChangesAsync(); // get nestOrder.Id
            await AddOrderItemsAsync(db, nestOrder.Id, matchingParts);
            newIds.Add(nestOrder.Id);

            h.ReleasedDate = DateTime.UtcNow;
            h.ReleasedByUserId = userId;
        }
        await db.SaveChangesAsync();

        _log.LogInformation(
            "Schedule {ScheduleId}: released {HandoffCount} accepted handoff(s) to Plant {PlantId}",
            scheduleId, handoffs.Count, destinationPlantId);
        return newIds;
    }

    private static decimal? ParseThickness(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        // Strip unit suffixes (e.g. "0.125 in" → "0.125")
        var cleaned = raw.Trim()
            .Replace(" in", "", StringComparison.OrdinalIgnoreCase)
            .Replace("in", "", StringComparison.OrdinalIgnoreCase)
            .Trim();
        return decimal.TryParse(cleaned, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var t) ? t : null;
    }

    private async Task<Part> FindOrCreatePartAsync(ApplicationDbContext db, PartLineItem line)
    {
        var part = await db.Parts.FirstOrDefaultAsync(p => p.FileName == line.PartNumber);
        if (part is null)
        {
            part = new Part
            {
                FileName    = line.PartNumber,
                Description = line.Description,
                Material    = line.Material,
                Thickness   = ParseThickness(line.Thickness),
            };
            db.Parts.Add(part);
        }
        else
        {
            // Always update from BOM data so changes in the source are reflected
            var thk = ParseThickness(line.Thickness);
            if (thk.HasValue)                              part.Thickness   = thk;
            if (!string.IsNullOrWhiteSpace(line.Material))    part.Material    = line.Material;
            if (!string.IsNullOrWhiteSpace(line.Description)) part.Description = line.Description;
        }
        await db.SaveChangesAsync();
        return part;
    }

    private void DeleteFolder(string? path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;
        try { Directory.Delete(path, recursive: true); }
        catch (Exception ex) { _log.LogWarning(ex, "Could not delete PDF folder {Path}", path); }
    }

    private void DeleteFile(string path)
    {
        if (!File.Exists(path)) return;
        try { File.Delete(path); }
        catch (Exception ex) { _log.LogWarning(ex, "Could not delete PDF file {Path}", path); }
    }

    public async Task<bool> RefreshPartPdfAsync(int partLineItemId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var part = await db.Set<PartLineItem>()
            .Include(p => p.BatchProduct!).ThenInclude(bp => bp.Batch).ThenInclude(b => b.Plant)
            .Include(p => p.ScheduleOrder!).ThenInclude(so => so.Schedule).ThenInclude(s => s.Plant)
            .FirstOrDefaultAsync(p => p.Id == partLineItemId);
        if (part is null) return false;

        if (!PdfExistsOnShare(part.PartNumber)) return false;

        if (part.BatchProductId.HasValue)
        {
            var batchId = part.BatchProduct!.BatchId;
            await _pdfCopy.CopyForBatchAsync(part.BatchProduct.Batch.Plant.Name, part.BatchProduct.Batch.Name, new[] { part.PartNumber });
            await db.Set<PartLineItem>()
                .Where(p => p.BatchProduct!.BatchId == batchId && p.PartNumber == part.PartNumber)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.HasPdf, true));
        }
        else if (part.ScheduleOrderId.HasValue)
        {
            var scheduleId = part.ScheduleOrder!.ScheduleId;
            await _pdfCopy.CopyForScheduleAsync(part.ScheduleOrder.Schedule.Plant.Name, part.ScheduleOrder.Schedule.Name, new[] { part.PartNumber });
            await db.Set<PartLineItem>()
                .Where(p => p.ScheduleOrder!.ScheduleId == scheduleId && p.PartNumber == part.PartNumber)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.HasPdf, true));
        }
        else
        {
            return false;
        }

        _log.LogInformation("Refreshed PDF for part {PartNumber} (PartLineItemId={Id})", part.PartNumber, partLineItemId);
        return true;
    }

    public bool PdfExistsOnShare(string partNumber)
    {
        var path = Path.Combine(_pdfSharePath, partNumber + ".pdf");
        return File.Exists(path);
    }

    public async Task<bool> GeneratePdfAsync(string partNumber)
    {
        _log.LogInformation("Triggering PowerJobs PDF generation for {PartNumber}", partNumber);

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-ExecutionPolicy Bypass -File \"{_powerJobsScriptPath}\" -PartName \"{partNumber}\" -OutputFolder \"{_pdfSharePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        try
        {
            using var process = Process.Start(psi);
            if (process is null) return false;

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _log.LogWarning("PowerJobs script failed for {PartNumber}. Exit code: {ExitCode}. Error: {Error}",
                    partNumber, process.ExitCode, error);
                return false;
            }

            _log.LogInformation("PowerJobs PDF generation completed for {PartNumber}", partNumber);
            return PdfExistsOnShare(partNumber);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to run PowerJobs script for {PartNumber}", partNumber);
            return false;
        }
    }

    private static string GetLevel1Prefix(string level)
    {
        var dotIndex = level.IndexOf('.');
        return dotIndex >= 0 ? level[..dotIndex] : level;
    }
}
