using HorstMFG.Core.DTOs;
using HorstMFG.Core.Entities;
using HorstMFG.Core.Enums;
using HorstMFG.Core.Interfaces;
using HorstMFG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HorstMFG.Web.Services;

public class BomService : IBomService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<BomService> _log;

    public BomService(ApplicationDbContext db, ILogger<BomService> log)
    {
        _db = db;
        _log = log;
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

            // Determine if this line should be processed based on MTO/MTS + IsStock
            bool isProcessed = bomType switch
            {
                BomType.MakeToOrder => !isStock,
                BomType.MakeToStock => isStock,
                _ => true
            };

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

        _log.LogInformation("Parsed {Count} BOM lines from export file", deduped.Count);
        return deduped;
    }

    private static int GetCalculatedQty(BomExportLine item, List<BomExportLine> itemList)
    {
        if (item.Parent == "<top>")
            return item.Qty;

        var parentItem = itemList.FirstOrDefault(i => i.Number == item.Parent);
        if (parentItem == null)
        {
            // Try without extension
            string parentNoExt = Path.GetFileNameWithoutExtension(item.Parent);
            parentItem = itemList.FirstOrDefault(i => i.Number == parentNoExt);
            if (parentItem == null)
                parentItem = itemList.FirstOrDefault(i => i.Number.Contains(item.Parent));
        }

        if (parentItem != null)
            return item.Qty * parentItem.Qty;

        return item.Qty;
    }

    public async Task<BomImportBatch> ImportBatchAsync(
        string name, BomType bomType, int plantId, int userId, List<BomExportLine> lines)
    {
        var batch = new BomImportBatch
        {
            Name = name,
            BomType = bomType,
            PlantId = plantId,
            ImportedByUserId = userId,
            ImportDate = DateTime.UtcNow,
        };
        _db.BomImportBatches.Add(batch);
        await _db.SaveChangesAsync();

        // Get or create Parts for each line
        var partNumbers = lines.Select(l => l.Number).Distinct().ToList();
        var existingParts = await _db.Parts
            .Where(p => partNumbers.Contains(p.Number))
            .ToDictionaryAsync(p => p.Number);

        foreach (var line in lines)
        {
            if (!existingParts.TryGetValue(line.Number, out var part))
            {
                part = new Part
                {
                    Number = line.Number,
                    Title = line.Title,
                    Description = line.ItemDescription,
                    Category = ParseCategory(line.Category),
                    Material = line.Material,
                    Thickness = ParseThickness(line.Thickness),
                    StructCode = line.StructCode,
                    Operations = line.Operations,
                    IsStock = line.IsStock,
                    LifecycleState = line.LifecycleState,
                    Keywords = line.Keywords,
                    Notes = line.Notes,
                };
                _db.Parts.Add(part);
                existingParts[line.Number] = part;
            }
            else
            {
                // Update existing part with latest data
                part.Title = line.Title;
                part.Description = line.ItemDescription;
                part.Category = ParseCategory(line.Category);
                part.Material = line.Material;
                part.Thickness = ParseThickness(line.Thickness);
                part.StructCode = line.StructCode;
                part.Operations = line.Operations;
                part.IsStock = line.IsStock;
                part.LifecycleState = line.LifecycleState;
                part.Keywords = line.Keywords;
                part.Notes = line.Notes;
                part.ModifiedDate = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();

        // Create BomLineItems
        // Build parent lookup: number -> BomLineItem (for setting ParentId FK)
        var lineItemsByNumber = new Dictionary<string, BomLineItem>();

        foreach (var line in lines)
        {
            var part = existingParts[line.Number];
            var bomLineItem = new BomLineItem
            {
                BatchId = batch.Id,
                PartId = part.Id,
                Number = line.Number,
                ParentNumber = line.Parent == "<top>" ? null : line.Parent,
                UnitQty = line.Qty,
                RequiresPdf = line.RequiresPdf,
                HasPdf = false,
                Level = line.Level.Split('.').Length,
                IsProcessed = line.IsProcessed,
            };

            // Set parent reference if not top-level
            if (line.Parent != "<top>" && lineItemsByNumber.TryGetValue(line.Parent, out var parentLineItem))
            {
                bomLineItem.Parent = parentLineItem;
            }

            _db.BomLineItems.Add(bomLineItem);
            lineItemsByNumber.TryAdd(line.Number, bomLineItem);
        }

        await _db.SaveChangesAsync();

        _log.LogInformation("Imported batch '{Name}' with {Count} line items (type: {BomType})",
            name, lines.Count, bomType);

        return batch;
    }

    private static PartCategory ParseCategory(string category) => category switch
    {
        "Product" => PartCategory.Product,
        "Assembly" => PartCategory.Assembly,
        "Part" => PartCategory.Part,
        "Purchased" => PartCategory.Purchased,
        _ => PartCategory.Other,
    };

    private static decimal? ParseThickness(string thickness)
    {
        if (string.IsNullOrWhiteSpace(thickness)) return null;
        // Remove " in" suffix if present (e.g., "0.062 in")
        thickness = thickness.Replace(" in", "").Trim();
        if (decimal.TryParse(thickness, out var val))
            return val;
        return null;
    }

    public async Task<IEnumerable<BomImportBatch>> GetBatchesAsync(int? plantId = null)
    {
        var query = _db.BomImportBatches
            .Include(b => b.Plant)
            .Include(b => b.ImportedByUser)
            .AsQueryable();

        if (plantId.HasValue)
            query = query.Where(b => b.PlantId == plantId.Value);

        return await query.OrderByDescending(b => b.ImportDate).ToListAsync();
    }

    public async Task<BomImportBatch?> GetBatchByIdAsync(int id)
    {
        return await _db.BomImportBatches
            .Include(b => b.Plant)
            .Include(b => b.ImportedByUser)
            .Include(b => b.LineItems)
                .ThenInclude(l => l.Part)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<IEnumerable<BomLineItem>> GetBomTreeAsync(int batchId)
    {
        return await _db.BomLineItems
            .Include(l => l.Part)
            .Include(l => l.Children)
            .Where(l => l.BatchId == batchId)
            .OrderBy(l => l.Level)
            .ThenBy(l => l.Number)
            .ToListAsync();
    }

    public async Task FinalizeBatchAsync(int batchId)
    {
        var batch = await _db.BomImportBatches.FindAsync(batchId);
        if (batch is null) return;
        batch.IsFinalized = true;
        batch.FinalizedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
