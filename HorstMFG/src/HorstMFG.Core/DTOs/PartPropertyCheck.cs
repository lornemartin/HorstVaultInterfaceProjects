namespace HorstMFG.Core.DTOs;

/// <summary>
/// Vault properties a manufactured part can't be made without. Shared by the engineering grids
/// and the BOM Import preview so they flag the same parts. Only Category = Part is checked
/// (assemblies and products legitimately have no operation or material), and parts whose
/// operation is Purchased are skipped. 2nd Operation, Finish and Description are optional.
/// </summary>
public static class PartPropertyCheck
{
    /// <summary>The missing properties as a display list ("Operation, Material"), or "" when complete.</summary>
    public static string Missing(string? category, string? operations, string? thickness, string? structCode, string? material)
    {
        if (!string.Equals(category?.Trim(), "Part", StringComparison.OrdinalIgnoreCase)) return "";

        var ops = operations?.Trim();
        if (string.Equals(ops, "Purchased", StringComparison.OrdinalIgnoreCase)) return "";

        var missing = new List<string>(3);
        // Vault fills an unset Operations with "N/A", so that counts as missing too.
        if (string.IsNullOrEmpty(ops) || string.Equals(ops, "N/A", StringComparison.OrdinalIgnoreCase))
            missing.Add("Operation");
        if (string.IsNullOrWhiteSpace(thickness) && string.IsNullOrWhiteSpace(structCode))
            missing.Add("Thickness or Struct Code");
        if (string.IsNullOrWhiteSpace(material))
            missing.Add("Material");
        return string.Join(", ", missing);
    }
}
