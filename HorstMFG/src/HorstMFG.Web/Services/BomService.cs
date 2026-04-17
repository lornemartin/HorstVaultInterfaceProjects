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
    private readonly ILogger<BomService> _log;
    private readonly string _pdfSharePath;
    private readonly string _localPdfPath;
    private readonly string _powerJobsScriptPath;

    public BomService(IDbContextFactory<ApplicationDbContext> dbFactory, ILogger<BomService> log, IConfiguration config)
    {
        _dbFactory = dbFactory;
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
        var (copied, total) = await CopyPdfsToLocalFolderAsync(partNumbers, localFolder);

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
        var (copied, total) = await CopyPdfsToLocalFolderAsync(partNumbers, localFolder);

        _log.LogInformation(
            "Imported schedule '{Name}' (order {OrderNumber}) with {Count} items. Copied {Copied}/{Total} PDFs to {Folder}",
            name, orderNumber, lines.Count, copied, total, localFolder);

        return schedule;
    }

    private async Task<(int Copied, int Total)> CopyPdfsToLocalFolderAsync(IEnumerable<string> partNumbers, string destinationFolder)
    {
        var partList = partNumbers.ToList();

        try
        {
            Directory.CreateDirectory(destinationFolder);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not create local PDF folder: {Folder}", destinationFolder);
            return (0, partList.Count);
        }

        int copied = 0;
        await Parallel.ForEachAsync(partList,
            new ParallelOptions { MaxDegreeOfParallelism = 4 },
            async (partNumber, ct) =>
            {
                var sourcePath = Path.Combine(_pdfSharePath, partNumber + ".pdf");
                var destPath = Path.Combine(destinationFolder, partNumber + ".pdf");
                try
                {
                    if (!await Task.Run(() => File.Exists(sourcePath), ct)) return;
                    await Task.Run(() => File.Copy(sourcePath, destPath, overwrite: true), ct);
                    Interlocked.Increment(ref copied);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Failed to copy PDF for {PartNumber}", partNumber);
                }
            });

        return (copied, partList.Count);
    }

    public async Task<List<ExportTreeItem>> GetBatchTreeItemsAsync(int? plantId = null, DateTime? fromDate = null, DateTime? toDate = null, string? searchTerm = null)
    {
        await using var _db = await _dbFactory.CreateDbContextAsync();

        // Load header rows only — children fetched on-demand via GetBatchChildrenByParentTreeIdAsync
        var query = _db.Batches
            .Include(b => b.Plant)
            .Include(b => b.ImportedByUser)
            .AsQueryable();

        if (plantId.HasValue)
            query = query.Where(b => b.PlantId == plantId.Value);
        if (fromDate.HasValue)
            query = query.Where(b => b.ImportDate >= fromDate.Value.ToUniversalTime());
        if (toDate.HasValue)
            query = query.Where(b => b.ImportDate < toDate.Value.ToUniversalTime().AddDays(1));
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(b =>
                EF.Functions.ILike(b.Name, $"%{term}%") ||
                b.BatchProducts.Any(bp =>
                    bp.Parts.Any(p =>
                        EF.Functions.ILike(p.PartNumber, $"%{term}%") ||
                        (p.Description != null && EF.Functions.ILike(p.Description, $"%{term}%")))));
        }

        var batches = await query.OrderByDescending(b => b.ImportDate).ToListAsync();
        _log.LogInformation("GetBatchTreeItemsAsync: found {Count} batches (plantId={PlantId}, from={From}, to={To})",
            batches.Count, plantId, fromDate, toDate);

        // Fast part count per batch name
        var batchNames = batches.Select(b => b.Name).Distinct().ToList();
        var partCounts = batchNames.Count > 0
            ? await _db.Set<PartLineItem>()
                .Where(p => p.BatchProductId.HasValue)
                .Where(p => batchNames.Contains(p.BatchProduct!.Batch.Name))
                .GroupBy(p => p.BatchProduct!.Batch.Name)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Name, x => x.Count)
            : new Dictionary<string, int>();

        var result = new List<ExportTreeItem>();

        foreach (var group in batches.GroupBy(b => b.Name).OrderByDescending(g => g.Max(b => b.ImportDate)))
        {
            var latest = group.OrderByDescending(b => b.ImportDate).First();
            result.Add(new ExportTreeItem
            {
                TreeId = latest.Id,  // DB id — stable, used by CustomAdaptor load-on-demand
                TreeParentId = null,
                IsBatchRow = true,
                IsExpanded = false,
                HasChildren = true,
                BatchName = group.Key,
                ImportDate = latest.ImportDate,
                PlantName = latest.Plant?.Name,
                ImportedBy = latest.ImportedByUser?.FullName,
                ReadyForProduction = latest.ReadyForProduction,
                ItemCount = partCounts.GetValueOrDefault(group.Key, 0),
            });
        }

        return result;
    }

    public async Task<List<ExportTreeItem>> GetBatchChildrenByParentTreeIdAsync(int parentTreeId)
    {
        await using var _db = await _dbFactory.CreateDbContextAsync();
        const int productOffset = 1_000_000;
        const int partOffset = 100_000_000;

        if (parentTreeId < productOffset)
        {
            // Parent is a Batch row — return BatchProduct children
            // Resolve batch name so we include all batches with the same name (matching grouping behavior)
            int batchId = parentTreeId;
            var parentBatch = await _db.Batches
                .Where(b => b.Id == batchId)
                .Select(b => new { b.Name, b.ReadyForProduction })
                .FirstOrDefaultAsync();

            if (parentBatch is null) return new List<ExportTreeItem>();

            var products = await _db.Set<BatchProduct>()
                .Where(bp => bp.Batch.Name == parentBatch.Name)
                .OrderBy(bp => bp.ProductName)
                .ToListAsync();

            if (products.Count == 0) return new List<ExportTreeItem>();

            var productIds = products.Select(p => p.Id).ToList();
            var partCounts = await _db.Set<PartLineItem>()
                .Where(p => productIds.Contains(p.BatchProductId!.Value))
                .GroupBy(p => p.BatchProductId!.Value)
                .Select(g => new { ProductId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ProductId, x => x.Count);

            var result = new List<ExportTreeItem>();
            foreach (var product in products)
            {
                int count = partCounts.GetValueOrDefault(product.Id, 0);
                if (count == 0) continue;
                result.Add(new ExportTreeItem
                {
                    TreeId = productOffset + product.Id,
                    TreeParentId = batchId,
                    IsProductRow = true,
                    HasChildren = count > 0,
                    ProductName = product.ProductName,
                    ParentQty = product.Qty,
                    ItemCount = count,
                    ReadyForProduction = parentBatch.ReadyForProduction,
                });
            }
            return result;
        }
        else
        {
            // Parent is a BatchProduct row — return PartLineItem children
            int productId = parentTreeId - productOffset;
            var batchReleased = await _db.Set<BatchProduct>()
                .Where(bp => bp.Id == productId)
                .Select(bp => bp.Batch.ReadyForProduction)
                .FirstOrDefaultAsync();

            var parts = await _db.Set<PartLineItem>()
                .Where(p => p.BatchProductId == productId)
                .OrderBy(p => p.PartNumber)
                .ToListAsync();

            return parts.Select(part => new ExportTreeItem
            {
                TreeId = partOffset + part.Id,
                TreeParentId = parentTreeId,
                HasChildren = false,
                Number = part.PartNumber,
                Title = part.Title,
                Description = part.Description,
                Category = part.Category,
                CategoryOrder = CategoryOrder(part.Category),
                Material = part.Material,
                Thickness = part.Thickness,
                Operations = part.Operations,
                Qty = part.Qty,
                IsStock = part.IsStock,
                HasPdf = part.HasPdf,
                Notes = part.Notes,
                ReadyForProduction = batchReleased,
            }).ToList();
        }
    }

    public async Task<List<ExportTreeItem>> GetBatchChildrenAsync(string batchName, int parentTreeId, int nextTreeId)
    {
        await using var _db = await _dbFactory.CreateDbContextAsync();
        var products = await _db.Set<BatchProduct>()
            .Include(bp => bp.Parts)
            .Where(bp => bp.Batch.Name == batchName)
            .OrderBy(bp => bp.ProductName)
            .ToListAsync();

        var result = new List<ExportTreeItem>();
        int treeId = nextTreeId;

        foreach (var product in products)
        {
            var productParts = product.Parts
                .OrderBy(p => p.PartNumber)
                .ToList();

            if (productParts.Count == 0) continue;

            int productTreeId = treeId++;
            result.Add(new ExportTreeItem
            {
                TreeId = productTreeId,
                TreeParentId = parentTreeId,
                IsProductRow = true,
                ProductName = product.ProductName,
                ParentQty = product.Qty,
                ItemCount = productParts.Count,
            });

            foreach (var part in productParts)
            {
                result.Add(new ExportTreeItem
                {
                    TreeId = treeId++,
                    TreeParentId = productTreeId,
                    Number = part.PartNumber,
                    Title = part.Title,
                    Description = part.Description,
                    Category = part.Category,
                    CategoryOrder = CategoryOrder(part.Category),
                    Material = part.Material,
                    Thickness = part.Thickness,
                    Operations = part.Operations,
                    Qty = part.Qty,
                    IsStock = part.IsStock,
                    HasPdf = part.HasPdf,
                    Notes = part.Notes,
                });
            }
        }

        return result;
    }

    public async Task<List<ExportTreeItem>> GetScheduleTreeItemsAsync(int? plantId = null, DateTime? fromDate = null, DateTime? toDate = null, string? searchTerm = null)
    {
        await using var _db = await _dbFactory.CreateDbContextAsync();

        // Load header rows only — children fetched on-demand via GetScheduleChildrenAsync
        var query = _db.Schedules
            .Include(s => s.Plant)
            .Include(s => s.ImportedByUser)
            .AsQueryable();

        if (plantId.HasValue)
            query = query.Where(s => s.PlantId == plantId.Value);
        if (fromDate.HasValue)
            query = query.Where(s => s.ImportDate >= fromDate.Value.ToUniversalTime());
        if (toDate.HasValue)
            query = query.Where(s => s.ImportDate < toDate.Value.ToUniversalTime().AddDays(1));
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(s =>
                EF.Functions.ILike(s.Name, $"%{term}%") ||
                s.ScheduleOrders.Any(so =>
                    EF.Functions.ILike(so.OrderNumber, $"%{term}%") ||
                    so.Parts.Any(p =>
                        EF.Functions.ILike(p.PartNumber, $"%{term}%") ||
                        (p.Description != null && EF.Functions.ILike(p.Description, $"%{term}%")))));
        }

        var schedules = await query.OrderByDescending(s => s.ImportDate).ToListAsync();

        // Fast part count per schedule for ItemCount display
        var scheduleNames = schedules.Select(s => s.Name).Distinct().ToList();
        var partCounts = scheduleNames.Count > 0
            ? await _db.Set<PartLineItem>()
                .Where(p => p.ScheduleOrderId.HasValue)
                .Where(p => scheduleNames.Contains(p.ScheduleOrder!.Schedule.Name))
                .GroupBy(p => p.ScheduleOrder!.Schedule.Name)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Name, x => x.Count)
            : new Dictionary<string, int>();

        var result = new List<ExportTreeItem>();

        foreach (var group in schedules.GroupBy(s => s.Name).OrderByDescending(g => g.Max(s => s.ImportDate)))
        {
            var latest = group.OrderByDescending(s => s.ImportDate).First();
            result.Add(new ExportTreeItem
            {
                TreeId = latest.Id,  // DB id — stable, used by Web API load-on-demand
                TreeParentId = null,
                IsBatchRow = true,
                IsExpanded = false,
                HasChildren = true,
                BatchName = group.Key,
                ImportDate = latest.ImportDate,
                PlantName = latest.Plant?.Name,
                ImportedBy = latest.ImportedByUser?.FullName,
                ReadyForProduction = latest.ReadyForProduction,
                ItemCount = partCounts.GetValueOrDefault(group.Key, 0),
            });
        }

        return result;
    }

    public async Task<List<ExportTreeItem>> GetScheduleChildrenByParentTreeIdAsync(int parentTreeId)
    {
        await using var _db = await _dbFactory.CreateDbContextAsync();
        const int orderOffset = 1_000_000;
        const int partOffset = 100_000_000;

        if (parentTreeId < orderOffset)
        {
            // Parent is a Schedule row — return ScheduleOrder children
            int scheduleId = parentTreeId;
            var parentSchedule = await _db.Schedules
                .Where(s => s.Id == scheduleId)
                .Select(s => new { s.ReadyForProduction })
                .FirstOrDefaultAsync();

            var orders = await _db.Set<ScheduleOrder>()
                .Where(so => so.ScheduleId == scheduleId)
                .OrderBy(so => so.OrderNumber)
                .ToListAsync();

            if (orders.Count == 0) return new List<ExportTreeItem>();

            var orderIds = orders.Select(o => o.Id).ToList();
            var partCounts = await _db.Set<PartLineItem>()
                .Where(p => orderIds.Contains(p.ScheduleOrderId!.Value))
                .GroupBy(p => p.ScheduleOrderId!.Value)
                .Select(g => new { OrderId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.OrderId, x => x.Count);

            // For each order, find the top-level product part (Category = "Product")
            var topItems = await _db.Set<PartLineItem>()
                .Where(p => orderIds.Contains(p.ScheduleOrderId!.Value) &&
                            p.Category.ToLower() == "product")
                .Select(p => new { p.ScheduleOrderId, p.PartNumber, p.Description })
                .ToDictionaryAsync(p => p.ScheduleOrderId!.Value);

            var result = new List<ExportTreeItem>();
            foreach (var order in orders)
            {
                int count = partCounts.GetValueOrDefault(order.Id, 0);
                if (count == 0) continue;
                topItems.TryGetValue(order.Id, out var topItem);
                result.Add(new ExportTreeItem
                {
                    TreeId = orderOffset + order.Id,
                    TreeParentId = scheduleId,
                    IsProductRow = true,
                    HasChildren = count > 0,
                    OrderNumber = order.OrderNumber,
                    ProductName = order.OrderNumber,
                    ParentQty = order.Qty,
                    ItemCount = count,
                    ReadyForProduction = parentSchedule?.ReadyForProduction ?? false,
                    Number = topItem?.PartNumber,
                    Description = topItem?.Description,
                });
            }
            return result;
        }
        else
        {
            // Parent is a ScheduleOrder row — return PartLineItem children
            int orderId = parentTreeId - orderOffset;
            var scheduleReleased = await _db.Set<ScheduleOrder>()
                .Where(so => so.Id == orderId)
                .Select(so => so.Schedule.ReadyForProduction)
                .FirstOrDefaultAsync();

            var parts = await _db.Set<PartLineItem>()
                .Where(p => p.ScheduleOrderId == orderId)
                .OrderBy(p => p.PartNumber)
                .ToListAsync();

            return parts.Select(part => new ExportTreeItem
            {
                TreeId = partOffset + part.Id,
                TreeParentId = parentTreeId,
                HasChildren = false,
                Number = part.PartNumber,
                Title = part.Title,
                Description = part.Description,
                Category = part.Category,
                CategoryOrder = CategoryOrder(part.Category),
                Material = part.Material,
                Thickness = part.Thickness,
                Operations = part.Operations,
                Qty = part.Qty,
                IsStock = part.IsStock,
                HasPdf = part.HasPdf,
                Notes = part.Notes,
                ReadyForProduction = scheduleReleased,
            }).ToList();
        }
    }

    public async Task<List<ExportTreeItem>> GetScheduleChildrenAsync(string scheduleName, int parentTreeId, int nextTreeId)
    {
        await using var _db = await _dbFactory.CreateDbContextAsync();
        var orders = await _db.Set<ScheduleOrder>()
            .Include(so => so.Parts)
            .AsSplitQuery()
            .Where(so => so.Schedule.Name == scheduleName)
            .OrderBy(so => so.OrderNumber)
            .ToListAsync();

        var result = new List<ExportTreeItem>();
        int treeId = nextTreeId;

        foreach (var order in orders)
        {
            var orderParts = order.Parts
                .OrderBy(p => p.PartNumber)
                .ToList();

            if (orderParts.Count == 0) continue;

            int orderTreeId = treeId++;
            result.Add(new ExportTreeItem
            {
                TreeId = orderTreeId,
                TreeParentId = parentTreeId,
                IsProductRow = true,
                OrderNumber = order.OrderNumber,
                ProductName = order.OrderNumber,
                ParentQty = order.Qty,
                ItemCount = orderParts.Count,
            });

            foreach (var part in orderParts)
            {
                result.Add(new ExportTreeItem
                {
                    TreeId = treeId++,
                    TreeParentId = orderTreeId,
                    Number = part.PartNumber,
                    Title = part.Title,
                    Description = part.Description,
                    Category = part.Category,
                    CategoryOrder = CategoryOrder(part.Category),
                    Material = part.Material,
                    Thickness = part.Thickness,
                    Operations = part.Operations,
                    Qty = part.Qty,
                    IsStock = part.IsStock,
                    HasPdf = part.HasPdf,
                    Notes = part.Notes,
                });
            }
        }

        return result;
    }

    private static int CategoryOrder(string? category) => category?.ToLowerInvariant() switch
    {
        "product"  => 0,
        "assembly" => 1,
        "part"     => 2,
        _          => int.MaxValue,
    };

    public async Task UpdateBatchProductQtyAsync(int batchProductId, int qty)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Set<BatchProduct>()
            .Where(bp => bp.Id == batchProductId)
            .ExecuteUpdateAsync(s => s.SetProperty(bp => bp.Qty, qty));
    }

    public async Task UpdateScheduleOrderQtyAsync(int scheduleOrderId, int qty)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Set<ScheduleOrder>()
            .Where(so => so.Id == scheduleOrderId)
            .ExecuteUpdateAsync(s => s.SetProperty(so => so.Qty, qty));
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
            // Refresh any fields that may have been missing on a previous release
            if (part.Thickness is null)  part.Thickness   = ParseThickness(line.Thickness);
            if (part.Material is null)   part.Material     = line.Material;
            if (part.Description is null) part.Description = line.Description;
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
