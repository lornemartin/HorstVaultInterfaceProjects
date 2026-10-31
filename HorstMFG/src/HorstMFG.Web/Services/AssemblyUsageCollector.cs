using HorstMFG.Core.DTOs;
using HorstMFG.Core.Entities;

namespace HorstMFG.Web.Services;

/// <summary>
/// Records how many of each part go into each sub-assembly while BOM lines are read in tree
/// order, before the import merges a part used in several places into one line. Quantities
/// are tracked by BOM Level ("1.2.3"), which identifies one placement in the tree.
/// </summary>
public sealed class AssemblyUsageCollector
{
    private readonly Dictionary<string, (string Number, int Qty)> _byLevel = new();
    private readonly Dictionary<string, Dictionary<string, (int Qty, int DirectQty)>> _byPart =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Records one placement. rawQty is relative to the line's immediate parent, as Vault reports
    /// it; returns the absolute qty (multiplied through every parent, including the product).
    /// </summary>
    public int Add(string level, string number, int rawQty)
    {
        var parentLevel = ParentLevel(level);
        int qty = rawQty * (parentLevel != null && _byLevel.TryGetValue(parentLevel, out var parent) ? parent.Qty : 1);
        _byLevel[level] = (number, qty);

        // Sub-assembly ancestors only: a level without a dot is the top-level product.
        for (var p = parentLevel; p != null && p.Contains('.'); p = ParentLevel(p))
        {
            if (!_byLevel.TryGetValue(p, out var asm)) continue;
            if (!_byPart.TryGetValue(number, out var usages))
                _byPart[number] = usages = new(StringComparer.OrdinalIgnoreCase);
            var (total, direct) = usages.GetValueOrDefault(asm.Number);
            usages[asm.Number] = (total + qty, direct + (p == parentLevel ? qty : 0));
        }
        return qty;
    }

    public List<AssemblyUsage> For(string number) =>
        _byPart.TryGetValue(number, out var usages)
            ? usages.Select(u => new AssemblyUsage(u.Key, u.Value.Qty, u.Value.DirectQty)).ToList()
            : new();

    public static List<PartLineItemAssemblyUsage> ToEntities(IEnumerable<AssemblyUsage> usages) =>
        usages.Select(u => new PartLineItemAssemblyUsage
        {
            AssemblyNumber = u.AssemblyNumber,
            Qty = u.Qty,
            DirectQty = u.DirectQty,
        }).ToList();

    private static string? ParentLevel(string level) =>
        level.Contains('.') ? level[..level.LastIndexOf('.')] : null;
}
