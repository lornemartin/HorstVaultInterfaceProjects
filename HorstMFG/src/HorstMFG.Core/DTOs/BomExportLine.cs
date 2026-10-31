namespace HorstMFG.Core.DTOs;

/// <summary>
/// One preview row on the BOM Import page: a part as the import will create it (after the
/// shared VaultGateway processing), editable before import.
/// </summary>
public class BomExportLine
{
    /// <summary>The Level-1 prefix ("1", "2", …) of the product this row belongs to.</summary>
    public string ProductKey { get; set; } = string.Empty;
    /// <summary>The product's own (Level-1) row.</summary>
    public bool IsProductLine { get; set; }
    public string SecondOperation { get; set; } = string.Empty;
    public string FinishOps { get; set; } = string.Empty;
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
    public bool HasPdf { get; set; }
    public int SortOrder { get; set; }
    /// <summary>Per-sub-assembly quantities, kept from before duplicate lines were merged.</summary>
    public List<AssemblyUsage> AssemblyUsages { get; set; } = new();
}

/// <summary>See PartLineItemAssemblyUsage.</summary>
public sealed record AssemblyUsage(string AssemblyNumber, int Qty, int DirectQty);
