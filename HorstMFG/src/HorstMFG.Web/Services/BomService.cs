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
    private readonly ApplicationDbContext _db;
    private readonly ILogger<BomService> _log;
    private readonly string _pdfSharePath;
    private readonly string _localPdfPath;
    private readonly string _powerJobsScriptPath;

    public BomService(ApplicationDbContext db, ILogger<BomService> log, IConfiguration config)
    {
        _db = db;
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
        var allLines = new List<BomExportLine>();
        var deduped = new List<BomExportLine>();
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

            // All items are imported regardless of stock status.
            // Display graying (stock vs non-stock) is handled per-tab on the Orders page.
            bool isProcessed = false;

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
                IsProcessed = isProcessed,
            };

            // Skip purchased items
            if (category.Equals("Purchased", StringComparison.OrdinalIgnoreCase))
                continue;

            // Calculate quantity based on parent quantity
            line.Qty = GetCalculatedQty(line, deduped);

            // Deduplicate: same number+parent already seen?
            int dupIndex = allLines.FindIndex(x =>
                x.Number == number && x.Parent == parent);

            if (dupIndex < 0)
            {
                // Not a duplicate parent/child combo
                int existingIndex = deduped.FindIndex(x => x.Number == number);
                if (existingIndex >= 0)
                {
                    // Part already in deduped list — add quantity
                    allLines.Add(line);
                    deduped[existingIndex].Qty += line.Qty;
                }
                else
                {
                    allLines.Add(line);
                    deduped.Add(line);
                }
            }
            else
            {
                // Duplicate parent/child — adjust for multiple instances
                allLines.Add(line);
                int relationCount = allLines.Count(x => x.Number == number && x.Parent == parent);
                int existingIndex = deduped.FindIndex(x => x.Number == number);
                if (existingIndex >= 0)
                {
                    deduped[existingIndex].Qty += line.Qty / relationCount;
                }
            }
        }

        // Check PDF existence and set sort order (<top> items pinned first)
        foreach (var line in deduped)
        {
            line.HasPdf = PdfExistsOnShare(line.Number);
            line.SortOrder = line.Parent == "<top>" ? 0 : 1;
        }

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

    public async Task<Batch> ImportBatchAsync(string name, int plantId, int userId, List<BomExportLine> lines)
    {
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
                    IsProcessed = line.IsProcessed,
                };
                _db.PartLineItems.Add(item);
            }
        }

        await _db.SaveChangesAsync();

        // Copy PDFs to local folder
        var partNumbers = lines.Select(l => l.Number).Distinct().ToList();
        var copyResults = CopyPdfsToLocalFolder(partNumbers, localFolder);
        int copied = copyResults.Count(r => r.HasPdf);

        _log.LogInformation(
            "Imported batch '{Name}' with {GroupCount} products. Copied {Copied}/{Total} PDFs to {Folder}",
            name, lines.GroupBy(l => GetLevel1Prefix(l.Level)).Count(), copied, partNumbers.Count, localFolder);

        return batch;
    }

    public async Task<Schedule> ImportScheduleAsync(string name, string orderNumber, int plantId, int userId, List<BomExportLine> lines)
    {
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
                IsProcessed = line.IsProcessed,
            };
            _db.PartLineItems.Add(item);
        }

        await _db.SaveChangesAsync();

        var partNumbers = lines.Select(l => l.Number).Distinct().ToList();
        var copyResults = CopyPdfsToLocalFolder(partNumbers, localFolder);
        int copied = copyResults.Count(r => r.HasPdf);

        _log.LogInformation(
            "Imported schedule '{Name}' (order {OrderNumber}) with {Count} items. Copied {Copied}/{Total} PDFs to {Folder}",
            name, orderNumber, lines.Count, copied, partNumbers.Count, localFolder);

        return schedule;
    }

    private List<(string PartNumber, bool HasPdf)> CopyPdfsToLocalFolder(IEnumerable<string> partNumbers, string destinationFolder)
    {
        var results = new List<(string, bool)>();
        try
        {
            Directory.CreateDirectory(destinationFolder);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not create local PDF folder: {Folder}", destinationFolder);
            return results;
        }

        foreach (var partNumber in partNumbers)
        {
            var sourcePath = Path.Combine(_pdfSharePath, partNumber + ".pdf");
            var destPath = Path.Combine(destinationFolder, partNumber + ".pdf");
            bool hasPdf = false;

            if (File.Exists(sourcePath))
            {
                try
                {
                    File.Copy(sourcePath, destPath, overwrite: true);
                    hasPdf = true;
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Failed to copy PDF for {PartNumber}", partNumber);
                }
            }

            results.Add((partNumber, hasPdf));
        }

        return results;
    }

    public async Task<List<ExportTreeItem>> GetBatchTreeItemsAsync(int? plantId = null, bool includeProcessed = false)
    {
        var query = _db.Batches
            .Include(b => b.Plant)
            .Include(b => b.ImportedByUser)
            .Include(b => b.BatchProducts)
                .ThenInclude(bp => bp.Parts)
            .AsQueryable();

        if (plantId.HasValue)
            query = query.Where(b => b.PlantId == plantId.Value);

        var batches = await query.OrderByDescending(b => b.ImportDate).ToListAsync();

        var result = new List<ExportTreeItem>();
        int treeId = 1;

        // Group same-named batches under one header row
        var groups = batches
            .GroupBy(b => b.Name)
            .OrderByDescending(g => g.Max(b => b.ImportDate));

        foreach (var group in groups)
        {
            var allProducts = group.SelectMany(b => b.BatchProducts).ToList();
            var visibleParts = allProducts
                .SelectMany(bp => bp.Parts)
                .Where(p => includeProcessed || !p.IsProcessed)
                .ToList();

            if (visibleParts.Count == 0 && !includeProcessed)
                continue;

            var latest = group.OrderByDescending(b => b.ImportDate).First();
            int batchTreeId = treeId++;
            result.Add(new ExportTreeItem
            {
                TreeId = batchTreeId,
                TreeParentId = null,
                IsBatchRow = true,
                BatchName = group.Key,
                ImportDate = latest.ImportDate,
                PlantName = latest.Plant?.Name,
                ImportedBy = latest.ImportedByUser?.FullName,
                ReadyForProduction = latest.ReadyForProduction,
                ItemCount = visibleParts.Count,
            });

            foreach (var product in allProducts.OrderBy(p => p.ProductName))
            {
                var productParts = product.Parts
                    .Where(p => includeProcessed || !p.IsProcessed)
                    .ToList();

                if (productParts.Count == 0 && !includeProcessed)
                    continue;

                int productTreeId = treeId++;
                result.Add(new ExportTreeItem
                {
                    TreeId = productTreeId,
                    TreeParentId = batchTreeId,
                    IsProductRow = true,
                    ProductName = product.ProductName,
                    ItemCount = productParts.Count,
                });

                foreach (var part in productParts.OrderBy(p => p.PartNumber))
                {
                    result.Add(new ExportTreeItem
                    {
                        TreeId = treeId++,
                        TreeParentId = productTreeId,
                        Number = part.PartNumber,
                        Title = part.Title,
                        Description = part.Description,
                        Category = part.Category,
                        Material = part.Material,
                        Thickness = part.Thickness,
                        Operations = part.Operations,
                        Qty = part.Qty,
                        IsStock = part.IsStock,
                        HasPdf = part.HasPdf,
                        Notes = part.Notes,
                        IsProcessed = part.IsProcessed,
                    });
                }
            }
        }

        return result;
    }

    public async Task<List<ExportTreeItem>> GetScheduleTreeItemsAsync(int? plantId = null, bool includeProcessed = false)
    {
        var query = _db.Schedules
            .Include(s => s.Plant)
            .Include(s => s.ImportedByUser)
            .Include(s => s.ScheduleOrders)
                .ThenInclude(so => so.Parts)
            .AsQueryable();

        if (plantId.HasValue)
            query = query.Where(s => s.PlantId == plantId.Value);

        var schedules = await query.OrderByDescending(s => s.ImportDate).ToListAsync();

        var result = new List<ExportTreeItem>();
        int treeId = 1;

        // Group same-named schedules under one header row
        var groups = schedules
            .GroupBy(s => s.Name)
            .OrderByDescending(g => g.Max(s => s.ImportDate));

        foreach (var group in groups)
        {
            var allOrders = group.SelectMany(s => s.ScheduleOrders).ToList();
            var visibleParts = allOrders
                .SelectMany(so => so.Parts)
                .Where(p => includeProcessed || !p.IsProcessed)
                .ToList();

            if (visibleParts.Count == 0 && !includeProcessed)
                continue;

            var latest = group.OrderByDescending(s => s.ImportDate).First();
            int scheduleTreeId = treeId++;
            result.Add(new ExportTreeItem
            {
                TreeId = scheduleTreeId,
                TreeParentId = null,
                IsBatchRow = true,
                BatchName = group.Key,
                ImportDate = latest.ImportDate,
                PlantName = latest.Plant?.Name,
                ImportedBy = latest.ImportedByUser?.FullName,
                ReadyForProduction = latest.ReadyForProduction,
                ItemCount = visibleParts.Count,
            });

            foreach (var schedOrder in allOrders.OrderBy(so => so.OrderNumber))
            {
                var orderParts = schedOrder.Parts
                    .Where(p => includeProcessed || !p.IsProcessed)
                    .ToList();

                if (orderParts.Count == 0 && !includeProcessed)
                    continue;

                int orderTreeId = treeId++;
                result.Add(new ExportTreeItem
                {
                    TreeId = orderTreeId,
                    TreeParentId = scheduleTreeId,
                    IsProductRow = true,
                    OrderNumber = schedOrder.OrderNumber,
                    ProductName = schedOrder.OrderNumber,
                    ItemCount = orderParts.Count,
                });

                foreach (var part in orderParts.OrderBy(p => p.PartNumber))
                {
                    result.Add(new ExportTreeItem
                    {
                        TreeId = treeId++,
                        TreeParentId = orderTreeId,
                        Number = part.PartNumber,
                        Title = part.Title,
                        Description = part.Description,
                        Category = part.Category,
                        Material = part.Material,
                        Thickness = part.Thickness,
                        Operations = part.Operations,
                        Qty = part.Qty,
                        IsStock = part.IsStock,
                        HasPdf = part.HasPdf,
                        Notes = part.Notes,
                        IsProcessed = part.IsProcessed,
                    });
                }
            }
        }

        return result;
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
