namespace HorstMFG.Core.DTOs;

/// <summary>
/// Represents a single line parsed from a Vault Item Export tab-delimited file.
/// </summary>
public class BomExportLine
{
    public string Level { get; set; } = string.Empty;
    public string Parent { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Thickness { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string Operations { get; set; } = string.Empty;
    public int Qty { get; set; }
    public string StructCode { get; set; } = string.Empty;
    public string PlantId { get; set; } = string.Empty;
    public bool IsStock { get; set; }
    public bool RequiresPdf { get; set; } = true;
    public string Comment { get; set; } = string.Empty;
    public DateTime? DateModified { get; set; }
    public string LifecycleState { get; set; } = string.Empty;
    public string StockName { get; set; } = string.Empty;
    public string Keywords { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Revision { get; set; } = string.Empty;
    public bool IsProcessed { get; set; }
}
