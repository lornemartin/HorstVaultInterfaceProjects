namespace HorstMFG.Core.Entities;

/// <summary>
/// How many of a part go into one sub-assembly of its product/order, captured at BOM import
/// before parts used in several places are merged into one PartLineItem. There is a row for
/// every sub-assembly the part sits under, at any depth, so a nested part counts toward each
/// enclosing sub-assembly. The top-level product itself is not recorded: whatever the part's
/// Qty exceeds the sum of its DirectQty values by is used directly in the product.
/// </summary>
public class PartLineItemAssemblyUsage
{
    public int Id { get; set; }
    public int PartLineItemId { get; set; }
    /// <summary>The sub-assembly's part number, matching its own PartLineItem.PartNumber.</summary>
    public string AssemblyNumber { get; set; } = string.Empty;
    /// <summary>Everything under the sub-assembly, nested or not. Same units as PartLineItem.Qty:
    /// per unit of the parent BatchProduct/ScheduleOrder.</summary>
    public int Qty { get; set; }
    /// <summary>The part of Qty where the sub-assembly is the part's immediate parent.</summary>
    public int DirectQty { get; set; }

    public PartLineItem PartLineItem { get; set; } = null!;
}
