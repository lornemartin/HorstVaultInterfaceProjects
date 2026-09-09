namespace HorstMFG.Core.Entities;

public class BatchProduct
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    /// <summary>Inherited from the parent Batch at creation time.</summary>
    public int PlantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Qty { get; set; } = 1;
    public bool VaultBomImported { get; set; }
    public string? Notes { get; set; }

    /// <summary>
    /// Set when the most recent Vault BOM query failed (e.g. item not found). Cleared
    /// whenever a new import attempt is dispatched or the import succeeds. Lets pollers
    /// detect a terminal failure immediately instead of waiting for a timeout.
    /// </summary>
    public string? LastImportError { get; set; }

    public Batch Batch { get; set; } = null!;
    public Plant Plant { get; set; } = null!;
    public ICollection<PartLineItem> Parts { get; set; } = new List<PartLineItem>();
}
