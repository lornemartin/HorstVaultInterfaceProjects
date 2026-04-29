namespace HorstMFG.Core.DTOs;

/// <summary>
/// Mirrors the JSON shape that <c>HorstMFG.VaultGateway</c> POSTs to
/// <c>/api/internal/vault-bom-result</c>. One callback per ScheduleOrder/BatchProduct.
/// </summary>
public class VaultBomCallbackPayload
{
    public string JobId { get; set; } = "";
    public int TrackingId { get; set; }
    public bool Found { get; set; }
    public string? ErrorMessage { get; set; }
    public List<VaultBomLine> Lines { get; set; } = new();
}

public class VaultBomLine
{
    public string Level { get; set; } = "";
    public string Number { get; set; } = "";
    public string Title { get; set; } = "";
    public string ItemDescription { get; set; } = "";
    public string Category { get; set; } = "";
    public string Thickness { get; set; } = "";
    public string Material { get; set; } = "";
    public string Operations { get; set; } = "";
    public int Qty { get; set; }
    public string StructCode { get; set; } = "";
    public string PlantId { get; set; } = "";
    public bool IsStock { get; set; }
    public bool RequiresPdf { get; set; }
    public string Comment { get; set; } = "";
    public string DateModified { get; set; } = "";
    public string LifecycleState { get; set; } = "";
    public string StockName { get; set; } = "";
    public string Keywords { get; set; } = "";
    public string Notes { get; set; } = "";
    public string Revision { get; set; } = "";
}
