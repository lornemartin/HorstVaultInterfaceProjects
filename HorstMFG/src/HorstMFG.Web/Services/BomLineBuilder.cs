using HorstMFG.Core.DTOs;
using HorstMFG.Core.Entities;

namespace HorstMFG.Web.Services;

/// <summary>
/// Turns one product's Vault BOM lines into <see cref="PartLineItem"/>s. Shared by every import
/// path — the VaultGateway callback (Excel imports, re-imports) and the BOM Import page's
/// uploaded export file — so they can't drift apart.
/// </summary>
public static class BomLineBuilder
{
    /// <summary>
    /// Skip Purchased rows; derive thickness from structural code for Laser ops; strip CAD
    /// extensions; apply stock-name override; collapse parts that appear under multiple parents
    /// into a single row with summed Qty. The Level=1 row (the product itself) is preserved as a
    /// Category="Product" PartLineItem so the schedule grid can render the order header.
    /// Plant ID and Is Stock come from the importing plant's own Vault properties (P2 … for
    /// Plant 2, the originals for Plant 1), with Is Stock adjusted by that plant's Stock
    /// Exception list for this top-level product. A blank Plant ID defaults to the importing plant.
    /// PlantId is left for <see cref="ResolvePlantIds"/>.
    /// </summary>
    public static List<PartLineItem> Build(IEnumerable<VaultBomLine> lines, Plant importingPlant, string? productName)
    {
        bool isPlant2 = importingPlant.Code.Equals("P2", StringComparison.OrdinalIgnoreCase);
        string defaultPlantIdRaw = isPlant2 ? "Plant 2" : "Plant 1";
        var usages = new AssemblyUsageCollector();
        var dedupedByNumber = new Dictionary<string, PartLineItem>(StringComparer.OrdinalIgnoreCase);

        foreach (var l in lines)
        {
            if (l.Category.Equals("Purchased", StringComparison.OrdinalIgnoreCase)) continue;

            var number = PartNumberOf(l);

            // Vault reports each line's Qty relative to its own immediate parent, not
            // pre-multiplied through the whole assembly chain — so a part nested under a
            // sub-assembly that's itself needed multiple times must have its raw Qty
            // multiplied by that parent's own (already-resolved) absolute qty. Tracked by
            // BOM Level (e.g. "1.2.3") rather than part number: a level uniquely identifies
            // one placement in the tree, so this needs no order-dependent "divide to undo
            // double counting" hack. The collector also records the per-sub-assembly split
            // that the merge below loses.
            int qty = usages.Add(l.Level, number, l.Qty < 1 ? 1 : l.Qty);

            if (dedupedByNumber.TryGetValue(number, out var existing))
            {
                existing.Qty += qty;
            }
            else
            {
                var thickness = BomFieldMappers.DeriveSheetThickness(l.Operations, l.Thickness, l.StructCode);
                var plantIdRaw = isPlant2 ? l.P2PlantId : l.PlantId;
                var isStock = BomFieldMappers.ResolveIsStock(
                    isPlant2 ? l.P2IsStock : l.IsStock,
                    isPlant2 ? l.P2StockException : l.P1StockException,
                    productName);
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
                    SecondOperation = l.SecondOperation,
                    FinishOps   = l.FinishOps,
                    IsStock     = isStock,
                    RequiresPdf = l.RequiresPdf,
                    Notes       = string.IsNullOrEmpty(l.Notes) ? null : l.Notes,
                    PlantIdRaw  = BomFieldMappers.IsImportingPlantValue(plantIdRaw) ? defaultPlantIdRaw : plantIdRaw!.Trim(),
                };
                dedupedByNumber[number] = item;
            }
        }

        foreach (var item in dedupedByNumber.Values)
            item.AssemblyUsages = AssemblyUsageCollector.ToEntities(usages.For(item.PartNumber));
        return dedupedByNumber.Values.ToList();
    }

    /// <summary>The part number a line imports under: its Stock Name if set, else its number without the CAD extension.</summary>
    public static string PartNumberOf(VaultBomLine line) =>
        string.IsNullOrEmpty(line.StockName) ? BomFieldMappers.StripCadExtension(line.Number) : line.StockName;

    /// <summary>
    /// Resolves each item's PlantIdRaw (Vault's raw "Plant ID" text) to our own Plant.Id via
    /// BomFieldMappers.MapVaultPlantCode + Plant.Code (<paramref name="plantCodeToId"/>). Left null
    /// when the value is unrecognized (PlantIdRaw still preserves the raw text); blank and
    /// "Plant 1&amp;2" were already replaced with the importing plant by <see cref="Build"/>.
    /// </summary>
    public static void ResolvePlantIds(IEnumerable<PartLineItem> items, IReadOnlyDictionary<string, int> plantCodeToId)
    {
        foreach (var item in items)
        {
            var code = BomFieldMappers.MapVaultPlantCode(item.PlantIdRaw);
            if (code != null && plantCodeToId.TryGetValue(code, out var id))
                item.PlantId = id;
        }
    }

    /// <summary>
    /// Parses a Vault Item Export tab-delimited file (ItemExport's "Export Item Direct") into
    /// raw BOM lines, one per BOM placement, in file order. Same column layout as the
    /// VaultGateway's VaultBomQueryService: columns 20-25 (P2 Plant ID … FinishOps) are only in
    /// files from the current ItemExport, so older 20-column files leave those fields blank.
    /// </summary>
    public static List<VaultBomLine> ParseExportFile(Stream fileStream)
    {
        var result = new List<VaultBomLine>();
        using var reader = new StreamReader(fileStream);
        reader.ReadLine(); // header

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var cols = line.Replace("\"", "").Split('\t');
            if (cols.Length < 19) continue;

            int qty = 1;
            if (int.TryParse(cols[8], out var q) && q > 0) qty = q;

            result.Add(new VaultBomLine
            {
                Level            = cols[0],
                Number           = cols[1],
                Title            = cols[2],
                ItemDescription  = cols[3],
                Category         = cols[4],
                Thickness        = cols[5],
                Material         = cols[6],
                Operations       = cols[7],
                Qty              = qty,
                StructCode       = cols[9],
                PlantId          = cols[10],
                IsStock          = cols[11].Equals("True", StringComparison.OrdinalIgnoreCase),
                RequiresPdf      = !cols[12].Equals("False", StringComparison.OrdinalIgnoreCase),
                Comment          = cols[13],
                DateModified     = cols[14],
                LifecycleState   = cols[15],
                StockName        = cols[16],
                Keywords         = cols[17],
                Notes            = cols[18],
                Revision         = Col(cols, 19),
                P2PlantId        = Col(cols, 20),
                P2IsStock        = Col(cols, 21).Equals("True", StringComparison.OrdinalIgnoreCase),
                P1StockException = Col(cols, 22),
                P2StockException = Col(cols, 23),
                SecondOperation  = Col(cols, 24),
                FinishOps        = Col(cols, 25),
            });
        }
        return result;
    }

    private static string Col(string[] cols, int index) => cols.Length > index ? cols[index] : "";
}
