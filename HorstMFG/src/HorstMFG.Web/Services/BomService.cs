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
    private readonly string _localPdfPath;
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
        _localPdfPath = config["FileSystemPaths:LocalPdfPath"] ?? @"C:\HorstMFG\PDFs\";
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
        ["SH-312"] = "0.312",
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
            string thickness = items[5];
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

            // Derive thickness from structural code for Laser ops
            if (ops.Equals("Laser", StringComparison.OrdinalIgnoreCase) &&
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
            line.HasPdf = File.Exists(Path.Combine(_pdfSharePath, line.Number + ".pdf"));
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
        var localFolder = Path.Combine(_localPdfPath, "Batches", name);

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
                ProductName = productName,
                Qty = (level1Line?.Qty ?? 1) * (batchQty < 1 ? 1 : batchQty),
            };
            _db.BatchProducts.Add(product);
            await _db.SaveChangesAsync();

            // All non-Level-1 lines in this group become PartLineItems
            var childLines = group.Where(l => l.Level.Contains('.')).ToList();
            foreach (var line in childLines)
            {
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
                };
                _db.PartLineItems.Add(item);
            }
        }

        await _db.SaveChangesAsync();

        // Copy PDFs to local folder
        var partNumbers = lines.Select(l => l.Number).Distinct().ToList();
        var (copied, total) = await _pdfCopy.CopyForBatchAsync(name, partNumbers);

        _log.LogInformation(
            "Imported batch '{Name}' with {GroupCount} products. Copied {Copied}/{Total} PDFs to {Folder}",
            name, lines.GroupBy(l => GetLevel1Prefix(l.Level)).Count(), copied, total, localFolder);

        return batch;
    }

    public async Task<Schedule> ImportScheduleAsync(string name, string orderNumber, int orderQty, int plantId, int userId, List<BomExportLine> lines)
    {
        await using var _db = await _dbFactory.CreateDbContextAsync();
        var localFolder = Path.Combine(_localPdfPath, "Schedules", name);

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

        var order = new ScheduleOrder
        {
            ScheduleId = schedule.Id,
            OrderNumber = orderNumber,
            Qty = orderQty < 1 ? 1 : orderQty,
        };
        _db.ScheduleOrders.Add(order);
        await _db.SaveChangesAsync();

        foreach (var line in lines)
        {
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
            };
            _db.PartLineItems.Add(item);
        }

        await _db.SaveChangesAsync();

        var partNumbers = lines.Select(l => l.Number).Distinct().ToList();
        var (copied, total) = await _pdfCopy.CopyForScheduleAsync(name, partNumbers);

        _log.LogInformation(
            "Imported schedule '{Name}' (order {OrderNumber}) with {Count} items. Copied {Copied}/{Total} PDFs to {Folder}",
            name, orderNumber, lines.Count, copied, total, localFolder);

        return schedule;
    }

    public async Task<List<FlatSchedulePartRow>> GetFlatSchedulePartsAsync(
        int? plantId = null, DateTime? fromDate = null, DateTime? toDate = null, string? searchTerm = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var term = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();

        // Direct projection: single SQL JOIN, no tracked entities, no in-memory mapping
        var rows = await db.Set<PartLineItem>()
            .Where(p => p.ScheduleOrderId != null)
            .Where(p => !plantId.HasValue || p.ScheduleOrder!.Schedule.PlantId == plantId.Value)
            .Where(p => !fromDate.HasValue || p.ScheduleOrder!.Schedule.ImportDate >= fromDate.Value.ToUniversalTime())
            .Where(p => !toDate.HasValue || p.ScheduleOrder!.Schedule.ImportDate < toDate.Value.ToUniversalTime().AddDays(1))
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
                ScheduleOrderId = p.ScheduleOrderId!.Value,
                OrderNumber = p.ScheduleOrder!.OrderNumber,
                OrderQty = p.ScheduleOrder!.Qty,
                ProductNumber = p.ScheduleOrder!.ProductNumber,
                VaultBomImported = p.ScheduleOrder!.VaultBomImported,
                PartLineItemId = p.Id,
                PartNumber = p.PartNumber,
                Description = p.Description,
                Category = p.Category,
                Material = p.Material,
                Thickness = p.Thickness,
                Operations = p.Operations,
                Qty = p.Qty,
                IsStock = p.IsStock,
                HasPdf = p.HasPdf,
                Notes = p.Notes,
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
                ScheduleOrderId    = so.Id,
                OrderNumber        = so.OrderNumber,
                OrderQty           = so.Qty,
                ProductNumber      = so.ProductNumber,
                ProductDescription = null,
                VaultBomImported   = so.VaultBomImported,
                PartLineItemId     = 0,
                PartNumber         = "",
                Description        = "",
                Category           = "",
                Material           = "",
                Thickness          = "",
                Operations         = "",
                Qty                = 0,
                IsStock            = false,
                HasPdf             = false,
                Notes              = so.Notes,
            })
            .ToListAsync();

        rows.AddRange(emptyOrders);
        return rows;
    }

    public async Task<List<FlatBatchPartRow>> GetFlatBatchPartsAsync(
        int? plantId = null, DateTime? fromDate = null, DateTime? toDate = null, string? searchTerm = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var term = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();

        // Direct projection: single SQL JOIN, no tracked entities, no in-memory mapping
        var rows = await db.Set<PartLineItem>()
            .Where(p => p.BatchProductId != null)
            .Where(p => !plantId.HasValue || p.BatchProduct!.Batch.PlantId == plantId.Value)
            .Where(p => !fromDate.HasValue || p.BatchProduct!.Batch.ImportDate >= fromDate.Value.ToUniversalTime())
            .Where(p => !toDate.HasValue || p.BatchProduct!.Batch.ImportDate < toDate.Value.ToUniversalTime().AddDays(1))
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
                BatchProductId = p.BatchProductId!.Value,
                ProductName = p.BatchProduct!.ProductName,
                ProductQty = p.BatchProduct!.Qty,
                VaultBomImported = p.BatchProduct!.VaultBomImported,
                PartLineItemId = p.Id,
                PartNumber = p.PartNumber,
                Description = p.Description,
                Category = p.Category,
                Material = p.Material,
                Thickness = p.Thickness,
                Operations = p.Operations,
                Qty = p.Qty,
                IsStock = p.IsStock,
                HasPdf = p.HasPdf,
                Notes = p.Notes,
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
            .Where(bp => term == null ||
                EF.Functions.ILike(bp.Batch.Name, $"%{term}%") ||
                EF.Functions.ILike(bp.ProductName, $"%{term}%"))
            .Select(bp => new FlatBatchPartRow
            {
                BatchId          = bp.BatchId,
                BatchName        = bp.Batch.Name,
                BatchImportDate  = bp.Batch.ImportDate,
                BatchReleased    = bp.Batch.ReadyForProduction,
                BatchProductId   = bp.Id,
                ProductName      = bp.ProductName,
                ProductQty       = bp.Qty,
                VaultBomImported = bp.VaultBomImported,
                PartLineItemId   = 0,
                PartNumber       = "",
                Description      = "",
                Category         = "",
                Material         = "",
                Thickness        = "",
                Operations       = "",
                Qty              = 0,
                IsStock          = false,
                HasPdf           = false,
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
        await db.Set<Batch>()
            .Where(b => b.Id == batchId)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.Name, trimmed));
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
            EntryDate = DateTime.UtcNow
        };
        db.NestBatches.Add(nestBatch);
        await db.SaveChangesAsync(); // get nestBatch.Id

        // Aggregate qty across all products — same part number in multiple products becomes one BatchItem
        var batchPartQtys = batch.BatchProducts
            .SelectMany(bp => bp.Parts
                .Where(p => p.Operations == "Laser" && p.IsStock)
                .Select(p => (Line: p, TotalQty: p.Qty * bp.Qty)))
            .GroupBy(x => x.Line.PartNumber, StringComparer.OrdinalIgnoreCase)
            .Select(g => (Sample: g.First().Line, TotalQty: g.Sum(x => x.TotalQty)));

        foreach (var (sampleLine, totalQty) in batchPartQtys)
        {
            var part = await FindOrCreatePartAsync(db, sampleLine);
            db.BatchItems.Add(new BatchItem
            {
                NestBatchId = nestBatch.Id,
                PartId      = part.Id,
                QtyRequired = totalQty
            });
        }

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

        foreach (var order in schedule.ScheduleOrders)
        {
            var nestOrder = new NestOrder
            {
                ScheduleOrderId = order.Id,
                PlantId         = schedule.PlantId,
                EntryDate       = DateTime.UtcNow
            };
            db.NestOrders.Add(nestOrder);
            await db.SaveChangesAsync(); // get nestOrder.Id

            // Aggregate qty within the order — same part number appearing multiple times becomes one OrderItem
            var orderPartQtys = order.Parts
                .Where(p => p.Operations == "Laser" && !p.IsStock)
                .GroupBy(p => p.PartNumber, StringComparer.OrdinalIgnoreCase)
                .Select(g => (Sample: g.First(), TotalQty: g.Sum(p => p.Qty) * order.Qty));

            foreach (var (sampleLine, totalQty) in orderPartQtys)
            {
                var part = await FindOrCreatePartAsync(db, sampleLine);
                db.OrderItems.Add(new OrderItem
                {
                    NestOrderId = nestOrder.Id,
                    PartId      = part.Id,
                    QtyRequired = totalQty
                });
            }
        }

        await db.SaveChangesAsync();
        _log.LogInformation("Schedule '{Name}' (Id={Id}) released to production.", schedule.Name, schedule.Id);
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
