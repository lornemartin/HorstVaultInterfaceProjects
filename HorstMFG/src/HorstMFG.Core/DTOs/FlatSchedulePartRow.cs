namespace HorstMFG.Core.DTOs;

/// <summary>
/// Denormalized flat row for the Orders (DailySchedule) SfGrid with two-level grouping.
/// Group Level 1: ScheduleId  (Schedule name/date/release state)
/// Group Level 2: ScheduleOrderId  (Order number, qty, product info)
/// Row: one PartLineItem
/// </summary>
public class FlatSchedulePartRow
{
    // ── Level-1 group (Schedule) ─────────────────────────────────────────────
    public int ScheduleId { get; set; }
    public string ScheduleName { get; set; } = string.Empty;
    public DateTime ScheduleImportDate { get; set; }
    public bool ScheduleReleased { get; set; }

    // ── Level-2 group (ScheduleOrder) ───────────────────────────────────────
    public int ScheduleOrderId { get; set; }
    public string? OrderNumber { get; set; }
    public int OrderQty { get; set; }
    public string? ProductNumber { get; set; }      // from the "Product" category part
    public string? ProductDescription { get; set; } // from the "Product" category part
    public int ScheduleOrderPlantId { get; set; }

    // ── Part row ────────────────────────────────────────────────────────────
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

    public double ThicknessOrder
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Thickness)) return double.MaxValue;
            if (double.TryParse(Thickness, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var d))
                return d;
            var slash = Thickness.IndexOf('/');
            if (slash > 0
                && double.TryParse(Thickness.AsSpan(0, slash).Trim(), out var num)
                && double.TryParse(Thickness.AsSpan(slash + 1).Trim(), out var den)
                && den != 0)
                return num / den;
            return double.MaxValue;
        }
    }
}
