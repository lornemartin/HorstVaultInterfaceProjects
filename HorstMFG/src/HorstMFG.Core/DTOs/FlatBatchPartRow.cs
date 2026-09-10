namespace HorstMFG.Core.DTOs;

/// <summary>
/// Denormalized flat row for the Batches SfGrid with two-level grouping.
/// Group Level 1: BatchId  (Batch name/date/release state)
/// Group Level 2: BatchProductId  (Product name, qty)
/// Row: one PartLineItem
/// </summary>
public class FlatBatchPartRow
{
    // ── Level-1 group (Batch) ────────────────────────────────────────────────
    public int BatchId { get; set; }
    public string BatchName { get; set; } = string.Empty;
    public DateTime BatchImportDate { get; set; }
    public bool BatchReleased { get; set; }

    // ── Level-2 group (BatchProduct) ─────────────────────────────────────────
    public int BatchProductId { get; set; }
    public string? ProductName { get; set; }
    public int ProductQty { get; set; }
    public int BatchProductPlantId { get; set; }

    // ── Part row ─────────────────────────────────────────────────────────────
    public int PartLineItemId { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string Thickness { get; set; } = string.Empty;
    public string Operations { get; set; } = string.Empty;
    public int Qty { get; set; }
    public bool IsStock { get; set; }
    public bool HasPdf { get; set; }
    public bool RequiresPdf { get; set; }
    public string? Notes { get; set; }
    public int? PlantId { get; set; }

    // ── Import status ────────────────────────────────────────────────────────
    public bool VaultBomImported { get; set; }

    // ── Sort helpers ─────────────────────────────────────────────────────────
    public int CategoryOrder => Category.ToLowerInvariant() switch
    {
        "product"  => 0,
        "assembly" => 1,
        "part"     => 2,
        _          => 3,
    };
}
