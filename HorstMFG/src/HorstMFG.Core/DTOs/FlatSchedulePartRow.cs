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
    public string? Notes { get; set; }
}
