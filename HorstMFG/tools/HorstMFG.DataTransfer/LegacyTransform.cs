namespace HorstMFG.DataTransfer;

/// <summary>A legacy Radan project folder, which becomes a radan_projects row with status Legacy.</summary>
public record PlannedProject(string Dir, string Name, DateTime CreatedUtc);

/// <summary>A nest to create. ProjectDir is null only if the legacy row had no folder.</summary>
public record PlannedNest(int LegacyId, string Name, string? ProjectDir, byte[]? Thumbnail, DateTime CreatedUtc);

/// <summary>An order item to create; Radan fields already adjusted (see <see cref="LegacyTransform.Build"/>).</summary>
public record PlannedItem(SrcOrderItem Source, int PartLegacyId, int? RadanIdNumber);

public class TransferPlan
{
    public List<SrcOrder> Orders { get; init; } = [];
    /// <summary>One per distinct file name; <see cref="PartFor"/> maps every legacy part ID onto these.</summary>
    public List<SrcPart> Parts { get; init; } = [];
    public Dictionary<int, int> PartFor { get; init; } = [];
    public List<PlannedProject> Projects { get; init; } = [];
    public List<PlannedNest> Nests { get; init; } = [];
    public List<PlannedItem> Items { get; init; } = [];
    public List<SrcNestedPart> NestedParts { get; init; } = [];

    /// <summary>Items RadanMaster still had in a live Radan project. They come across as not in
    /// Radan: those projects belong to the legacy system, not to this environment's stations.</summary>
    public int TakenOutOfRadan { get; init; }
    public int OverNestedItems { get; init; }
    /// <summary>Problems that would make the transfer fail; nothing is written while there are any.</summary>
    public List<string> Errors { get; init; } = [];
}

/// <summary>
/// Turns legacy RadanMaster rows into what the transfer writes. No database access, so it can be
/// tested on its own; Program reads the source and writes the result.
/// </summary>
public static class LegacyTransform
{
    /// <summary>Orders entered on or after <paramref name="since"/>, plus every open order whenever
    /// it was entered — an open order is still being worked on, so it must come across.</summary>
    public static List<SrcOrder> SelectOrders(IEnumerable<SrcOrder> orders, DateTime? since) =>
        orders.Where(o => since == null || o.EntryDate >= since || !o.IsComplete).ToList();

    public static TransferPlan Build(SourceData src)
    {
        var errors = new List<string>();
        var orders = src.Orders.ToDictionary(o => o.Id);
        var partsById = src.Parts.ToDictionary(p => p.Id);

        // Parts: one per file name, as the app looks them up by file name.
        var partFor = new Dictionary<int, int>();
        var firstByFile = new Dictionary<string, int>(StringComparer.Ordinal);
        var parts = new List<SrcPart>();
        foreach (var p in src.Parts.OrderBy(p => p.Id))
        {
            if (p.FileName != null && firstByFile.TryGetValue(p.FileName, out var first)) { partFor[p.Id] = first; continue; }
            if (p.FileName != null) firstByFile[p.FileName] = p.Id;
            partFor[p.Id] = p.Id;
            parts.Add(p);
        }

        // Items under the selected orders.
        var items = new List<PlannedItem>();
        int takenOut = 0;
        foreach (var i in src.OrderItems)
        {
            if (!orders.ContainsKey(i.OrderId)) continue;
            if (!partFor.TryGetValue(i.PartId, out var partId))
            {
                errors.Add($"Order item {i.Id} refers to part {i.PartId}, which wasn't read.");
                continue;
            }
            if (i.IsInProject) takenOut++;
            var radanId = i.IsInProject || i.RadanIdNumber == 0 ? (int?)null : i.RadanIdNumber;
            items.Add(new PlannedItem(i, partId, radanId));
        }
        var itemOrder = items.ToDictionary(i => i.Source.Id, i => orders[i.Source.OrderId]);

        // Nests that hold any of those items. Dated by their project folder's name (Radan projects
        // are named by date), or failing that by the earliest order nested in them.
        var nestsById = src.Nests.ToDictionary(n => n.Id);
        var nestedParts = src.NestedParts.Where(np => itemOrder.ContainsKey(np.OrderItemId) && nestsById.ContainsKey(np.NestId)).ToList();
        var earliestOrder = nestedParts
            .GroupBy(np => np.NestId)
            .ToDictionary(g => g.Key, g => g.Min(np => itemOrder[np.OrderItemId].EntryDate));

        var nests = earliestOrder.Keys.OrderBy(id => id).Select(id =>
        {
            var n = nestsById[id];
            var dir = ProjectDir(n.NestPath);
            var date = (dir == null ? null : ProjectDate(ProjectName(dir))) ?? earliestOrder[id];
            return new PlannedNest(n.Id, NestName(n.NestName), dir, n.Thumbnail, ToUtc(date));
        }).ToList();

        foreach (var dup in nests.GroupBy(n => (n.ProjectDir, n.Name)).Where(g => g.Count() > 1))
            errors.Add($"Nest '{dup.Key.Name}' appears {dup.Count()} times in {dup.Key.ProjectDir ?? "(no folder)"} " +
                       $"(legacy IDs {string.Join(", ", dup.Select(n => n.LegacyId))}).");

        var projects = nests
            .Where(n => n.ProjectDir != null)
            .GroupBy(n => n.ProjectDir!, StringComparer.Ordinal)
            .Select(g => new PlannedProject(g.Key, ProjectName(g.Key), g.Min(n => n.CreatedUtc)))
            .OrderBy(p => p.CreatedUtc)
            .ToList();

        return new TransferPlan
        {
            Orders          = src.Orders,
            Parts           = parts,
            PartFor         = partFor,
            Projects        = projects,
            Nests           = nests,
            Items           = items,
            NestedParts     = nestedParts,
            TakenOutOfRadan = takenOut,
            OverNestedItems = items.Count(i => i.Source.QtyNested > i.Source.QtyRequired),
            Errors          = errors,
        };
    }

    /// <summary>"2234.drg" → "2234": the bridge names nests by Radan's nest number.</summary>
    public static string NestName(string? legacyName)
    {
        var name = (legacyName ?? "").Trim();
        if (name.EndsWith(".drg", StringComparison.OrdinalIgnoreCase)) name = name[..^4];
        return name == "" ? "(unnamed)" : name;
    }

    public static string? ProjectDir(string? nestPath)
    {
        var dir = nestPath?.Trim().TrimEnd('\\', '/');
        return string.IsNullOrEmpty(dir) ? null : dir;
    }

    /// <summary>The folder's own name: "S:\Radan Projects\2026-11-04(4)" → "2026-11-04(4)".</summary>
    public static string ProjectName(string dir) => dir[(dir.LastIndexOfAny(['\\', '/']) + 1)..];

    /// <summary>The date a project folder is named for: "2026-11-04(4)" → 2026-11-04. Null if the
    /// name doesn't start with one.</summary>
    public static DateTime? ProjectDate(string projectName) =>
        projectName.Length >= 10 && DateTime.TryParseExact(projectName[..10], "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d)
            ? d : null;

    /// <summary>An order's number, or its legacy ID when it has none (a schedule order needs one).</summary>
    public static string OrderNumber(SrcOrder o) =>
        string.IsNullOrWhiteSpace(o.OrderNumber) ? o.Id.ToString() : o.OrderNumber.Trim();

    /// <summary>RadanMaster stored local times; PostgreSQL wants UTC.</summary>
    public static DateTime ToUtc(DateTime local) => DateTime.SpecifyKind(local, DateTimeKind.Local).ToUniversalTime();

    public static DateTime? ToUtc(DateTime? local) => local.HasValue ? ToUtc(local.Value) : null;
}
