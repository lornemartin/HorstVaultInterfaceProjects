namespace HorstMFG.Core.Entities;

public class PartLineItem
{
    public int Id { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Qty { get; set; }
    public string Material { get; set; } = string.Empty;
    public string Thickness { get; set; } = string.Empty;
    public string StructCode { get; set; } = string.Empty;
    public string Operations { get; set; } = string.Empty;
    public bool IsStock { get; set; }
    public bool RequiresPdf { get; set; }
    public string? Notes { get; set; }
    public bool HasPdf { get; set; }
    public bool IsProcessed { get; set; }

    public int? BatchProductId { get; set; }
    public int? ScheduleOrderId { get; set; }

    public BatchProduct? BatchProduct { get; set; }
    public ScheduleOrder? ScheduleOrder { get; set; }
}
