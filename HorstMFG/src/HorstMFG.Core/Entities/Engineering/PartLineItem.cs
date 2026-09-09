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

    /// <summary>Resolved from PlantIdRaw via a Vault-string-to-Plant.Code lookup. Null when
    /// Vault's value is blank, unrecognized, or "Plant 1&amp;2" (ambiguous — a single FK can't
    /// represent both plants; PlantIdRaw still preserves that Vault flagged it as shared).</summary>
    public int? PlantId { get; set; }
    /// <summary>Vault's raw "Plant ID" custom-property text for this line, verbatim.</summary>
    public string? PlantIdRaw { get; set; }

    public int? BatchProductId { get; set; }
    public int? ScheduleOrderId { get; set; }

    public BatchProduct? BatchProduct { get; set; }
    public ScheduleOrder? ScheduleOrder { get; set; }
    public Plant? Plant { get; set; }
}
