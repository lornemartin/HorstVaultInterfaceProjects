using HorstMFG.Core.Entities;
using HorstMFG.Core.Enums;
using HorstMFG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HorstMFG.Web.Services;

// ── Shared sync payload DTOs ────────────────────────────────────────────────
// Used both by NestingPanel.razor (manual Sync/Retrieve commands, panel-driven)
// and by BridgeHub (AutoSync/Finalize, which must apply regardless of whether
// any browser panel happens to be connected — see NestingSyncService below).

public record SyncPartDto(long RadanIdNumber, int QtyNested);

public class NestEntryDto
{
    public long RadanIdNumber { get; set; }
    public int Qty { get; set; }
}

public class SyncNestDto
{
    public string NestName { get; set; } = "";
    public string? NestPath { get; set; }
    public byte[]? ThumbnailBytes { get; set; }
    public List<NestEntryDto> Parts { get; set; } = new();
}

public class SyncPayloadDto
{
    public List<SyncPartDto> Parts { get; set; } = new();
    public List<SyncNestDto> Nests { get; set; } = new();
    /// <summary>
    /// The .rpd path the bridge actually read this payload from. Null on payloads from a
    /// not-yet-updated bridge (pre-dating this field) — treated as the station's active project.
    /// </summary>
    public string? ProjectPath { get; set; }
}

public class FinalizeResultDto
{
    public SyncPayloadDto Sync { get; set; } = new();
    public List<int> ClearedItemIds { get; set; } = new();
    public List<int> AdjustedItemIds { get; set; } = new();
    public string NewProjectName { get; set; } = "";
    public string NewProjectPath { get; set; } = "";
}

/// <summary>One item the bridge added to (or updated in) the Radan project, with the ID Radan gave it.</summary>
public record SendToNestingResultDto(int ItemId, long RadanIdNumber, bool MissingSymFile, bool VerifiedInProject);

public class RetrieveFromNestingResultDto
{
    public SyncPayloadDto Sync { get; set; } = new();
    /// <summary>Items removed from the project outright (nothing of them was nested).</summary>
    public List<int> ClearedItemIds { get; set; } = new();
    /// <summary>Items whose Radan part stays, cut back to the qty already nested.</summary>
    public List<int> AdjustedItemIds { get; set; } = new();
}

/// <summary>
/// Applies bridge results to the database. Deliberately independent of any Blazor component's
/// lifecycle — AutoSync and Finalize results must be applied whether or not a browser panel
/// happens to be connected at the moment they arrive. Called directly from BridgeHub for those
/// two cases; NestingPanel.razor calls it for its own manually-triggered commands.
///
/// Radan numbers parts and nests per project, so everything is scoped to a RadanProject: a
/// RadanPlacement says which item a Radan part ID stands for in that project, and nests are found
/// by project and name. A sync payload is authoritative for its own project's nests only; nests
/// in other projects (earlier ones, or legacy RadanMaster history) are never touched by it.
/// </summary>
public class NestingSyncService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public NestingSyncService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// Points the station at a project. Refuses finalized and legacy projects — those are changed
    /// with a re-sync (ApplyProjectResyncAsync), never by making them active again.
    /// </summary>
    /// <returns>Null if set, otherwise why not.</returns>
    public async Task<string?> SetActiveProjectAsync(int stationId, string rpdPath)
    {
        rpdPath = rpdPath.Trim();
        await using var db = await _dbFactory.CreateDbContextAsync();
        var station = await db.NestingStations.FirstOrDefaultAsync(s => s.Id == stationId);
        if (station == null) return $"Station {stationId} not found.";

        var existing = await FindProjectAsync(db, station.PlantId, rpdPath);
        if (existing is { Status: not RadanProjectStatus.Open })
            return $"'{existing.ProjectName}' is {existing.Status.ToString().ToLowerInvariant()} and can't be made " +
                   "the active project." + (existing.Status == RadanProjectStatus.Finalized
                       ? " Edit it in Radan and re-sync it instead." : "");

        await GetOrCreateOpenProjectAsync(db, station.PlantId, rpdPath);
        station.ProjectPath = rpdPath;
        station.ProjectName = Path.GetFileNameWithoutExtension(rpdPath);
        await db.SaveChangesAsync();
        return null;
    }

    /// <summary>Applies a sync of the station's active project (manual Sync, AutoSync, Retrieve, Finalize).</summary>
    /// <returns>Null if applied, otherwise why it was refused.</returns>
    public async Task<string?> ApplySyncPayloadAsync(int stationId, SyncPayloadDto sync)
    {
        // NOTE: sync.Parts.Count == 0 is NOT a no-op — it's the live Radan project reporting it
        // now has zero parts at all. That must still flow through so stale NestedPart rows get
        // cleaned up and QtyNested gets recomputed down to 0.
        await using var db = await _dbFactory.CreateDbContextAsync();
        var (project, rejection) = await GetActiveProjectAsync(db, stationId, sync.ProjectPath);
        if (project == null) return rejection;

        await ApplyToProjectAsync(db, project, sync);
        return null;
    }

    /// <summary>
    /// Applies a sync of any non-legacy project, typically a finalized one edited in Radan after
    /// the fact. Only that project's nests change; its items' QtyNested is recomputed across all
    /// their projects.
    /// </summary>
    public async Task<string?> ApplyProjectResyncAsync(int stationId, SyncPayloadDto sync)
    {
        if (string.IsNullOrWhiteSpace(sync.ProjectPath)) return "Re-sync payload has no project path.";
        await using var db = await _dbFactory.CreateDbContextAsync();
        var station = await db.NestingStations.AsNoTracking().FirstOrDefaultAsync(s => s.Id == stationId);
        if (station == null) return $"Station {stationId} not found.";
        var project = await FindProjectAsync(db, station.PlantId, sync.ProjectPath);
        if (project == null)
            return $"'{sync.ProjectPath}' isn't a project HorstMFG has nested in.";
        if (project.Status == RadanProjectStatus.Legacy)
            return $"'{project.ProjectName}' was imported from RadanMaster and is history only; it can't be re-synced.";

        await ApplyToProjectAsync(db, project, sync);
        return null;
    }

    /// <summary>
    /// The status of each given .rpd's project in the station's plant, for the ones HorstMFG knows.
    /// Keyed by the path as given; projects never nested in are left out.
    /// </summary>
    public async Task<Dictionary<string, RadanProjectStatus>> GetProjectStatusesAsync(int stationId, IEnumerable<string> rpdPaths)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var station = await db.NestingStations.AsNoTracking().FirstOrDefaultAsync(s => s.Id == stationId);
        var result = new Dictionary<string, RadanProjectStatus>(StringComparer.OrdinalIgnoreCase);
        if (station == null) return result;

        var statusByDir = (await db.RadanProjects.AsNoTracking()
                .Where(p => p.PlantId == station.PlantId)
                .Select(p => new { p.ProjectDir, p.Status })
                .ToListAsync())
            .GroupBy(p => p.ProjectDir, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Status, StringComparer.OrdinalIgnoreCase);
        foreach (var path in rpdPaths)
            if (statusByDir.TryGetValue(ProjectDirOf(path), out var status))
                result[path] = status;
        return result;
    }

    /// <summary>Records the Radan part IDs the bridge got back for items it added to the active project.</summary>
    public async Task<string?> RecordSentToNestingAsync(int stationId, string itemType, IEnumerable<SendToNestingResultDto> results)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var (project, rejection) = await GetActiveProjectAsync(db, stationId, null);
        if (project == null) return rejection;

        var isBatch = IsBatch(itemType);
        var placements = await db.RadanPlacements.Where(p => p.RadanProjectId == project.Id).ToListAsync();
        foreach (var r in results)
        {
            var radanId = (int)r.RadanIdNumber;
            var placement = placements.FirstOrDefault(p => p.RadanIdNumber == radanId);
            if (placement == null)
            {
                placement = new RadanPlacement { RadanProject = project, RadanIdNumber = radanId };
                db.RadanPlacements.Add(placement);
                placements.Add(placement);
            }
            placement.OrderItemId = isBatch ? null : r.ItemId;
            placement.BatchItemId = isBatch ? r.ItemId : null;

            if (isBatch)
            {
                var item = await db.BatchItems.FindAsync(r.ItemId);
                if (item != null) { item.IsInRadanProject = true; item.RadanIdNumber = radanId; item.NestingStationId = stationId; }
            }
            else
            {
                var item = await db.OrderItems.FindAsync(r.ItemId);
                if (item != null) { item.IsInRadanProject = true; item.RadanIdNumber = radanId; item.NestingStationId = stationId; }
            }
        }
        await db.SaveChangesAsync();
        return null;
    }

    /// <summary>
    /// Applies a RetrieveFromNesting result. Cleared items had nothing nested, so their Radan part is
    /// gone and so is their placement. Adjusted items keep their part (cut back to what's nested)
    /// and their placement, so later syncs of this project still count it.
    /// </summary>
    public async Task<string?> ApplyRetrieveAsync(int stationId, string itemType, RetrieveFromNestingResultDto result)
    {
        var rejection = await ApplySyncPayloadAsync(stationId, result.Sync);
        if (rejection != null) return rejection;

        await using var db = await _dbFactory.CreateDbContextAsync();
        var (project, _) = await GetActiveProjectAsync(db, stationId, null);
        var isBatch = IsBatch(itemType);

        if (project != null)
        {
            var gone = await db.RadanPlacements
                .Where(p => p.RadanProjectId == project.Id && (isBatch
                    ? p.BatchItemId != null && result.ClearedItemIds.Contains(p.BatchItemId.Value)
                    : p.OrderItemId != null && result.ClearedItemIds.Contains(p.OrderItemId.Value)))
                .ToListAsync();
            db.RadanPlacements.RemoveRange(gone);
        }

        if (isBatch)
        {
            foreach (var item in await db.BatchItems.Where(i => result.ClearedItemIds.Contains(i.Id)).ToListAsync())
                { item.IsInRadanProject = false; item.RadanIdNumber = null; item.NestingStationId = null; }
            foreach (var item in await db.BatchItems.Where(i => result.AdjustedItemIds.Contains(i.Id)).ToListAsync())
                { item.IsInRadanProject = false; item.NestingStationId = null; }
        }
        else
        {
            foreach (var item in await db.OrderItems.Where(i => result.ClearedItemIds.Contains(i.Id)).ToListAsync())
                { item.IsInRadanProject = false; item.RadanIdNumber = null; item.NestingStationId = null; }
            foreach (var item in await db.OrderItems.Where(i => result.AdjustedItemIds.Contains(i.Id)).ToListAsync())
                { item.IsInRadanProject = false; item.NestingStationId = null; }
        }

        await db.SaveChangesAsync();
        return null;
    }

    /// <summary>
    /// Applies a Finalize result: writes the final nest rows, marks the project finalized (its
    /// placements stay, so it can be re-synced later), clears the station's items out of the
    /// "currently in Radan" state and makes the new project active. Must run regardless of
    /// whether a browser panel is connected — Radan has already rotated to the new project.
    /// </summary>
    /// <returns>Null if applied. A rejection reason if the embedded sync's project didn't match,
    /// in which case nothing is changed or advanced.</returns>
    public async Task<string?> ApplyFinalizeAsync(int stationId, FinalizeResultDto result)
    {
        var rejection = await ApplySyncPayloadAsync(stationId, result.Sync);
        if (rejection != null) return rejection;

        await using var db = await _dbFactory.CreateDbContextAsync();
        var (project, _) = await GetActiveProjectAsync(db, stationId, null);
        if (project != null)
        {
            project.Status = RadanProjectStatus.Finalized;
            project.FinalizedDate = DateTime.UtcNow;
        }

        foreach (var item in await db.OrderItems.Where(i => i.NestingStationId == stationId).ToListAsync())
            { item.IsInRadanProject = false; item.NestingStationId = null; item.RadanIdNumber = null; }
        foreach (var item in await db.BatchItems.Where(i => i.NestingStationId == stationId).ToListAsync())
            { item.IsInRadanProject = false; item.NestingStationId = null; item.RadanIdNumber = null; }

        var station = await db.NestingStations.FirstAsync(s => s.Id == stationId);
        await GetOrCreateOpenProjectAsync(db, station.PlantId, result.NewProjectPath);
        station.ProjectName = result.NewProjectName;
        station.ProjectPath = result.NewProjectPath;

        await db.SaveChangesAsync();
        return null;
    }

    // ── Core ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Makes the project's nest rows match the payload exactly, then recomputes QtyNested for every
    /// item that had or now has a row in it. A nest lists only the parts currently on it, so a part
    /// taken off a nest just stops appearing there; nests missing from the payload were deleted.
    /// Parts with no placement in this project aren't HorstMFG items and are ignored.
    /// </summary>
    private static async Task ApplyToProjectAsync(ApplicationDbContext db, RadanProject project, SyncPayloadDto sync)
    {
        var itemByRadanId = await db.RadanPlacements
            .Where(p => p.RadanProjectId == project.Id)
            .ToDictionaryAsync(p => p.RadanIdNumber, p => new ItemKey(p.OrderItemId, p.BatchItemId));

        var nests = await db.Nests.Where(n => n.RadanProjectId == project.Id).ToListAsync();
        var nestsByName = nests.ToDictionary(n => n.NestName);
        var nestIds = nests.Select(n => n.Id).ToList();
        var rows = await db.NestedParts.Where(np => nestIds.Contains(np.NestId)).ToListAsync();

        var affected = rows.Select(r => new ItemKey(r.OrderItemId, r.BatchItemId))
            .Concat(itemByRadanId.Values).ToHashSet();
        var seenNests = new HashSet<string>();

        foreach (var syncNest in sync.Nests)
        {
            if (!nestsByName.TryGetValue(syncNest.NestName, out var nest))
            {
                nest = new Nest { NestName = syncNest.NestName, PlantId = project.PlantId, RadanProject = project };
                db.Nests.Add(nest);
                nestsByName[nest.NestName] = nest;
            }
            if (syncNest.NestPath != null) nest.NestPath = syncNest.NestPath;
            if (syncNest.ThumbnailBytes != null) nest.Thumbnail = syncNest.ThumbnailBytes;
            seenNests.Add(nest.NestName);

            var wanted = syncNest.Parts
                .Where(e => e.Qty > 0 && itemByRadanId.ContainsKey((int)e.RadanIdNumber))
                .GroupBy(e => itemByRadanId[(int)e.RadanIdNumber])
                .ToDictionary(g => g.Key, g => g.Sum(e => e.Qty));

            foreach (var row in rows.Where(r => r.NestId == nest.Id && nest.Id != 0).ToList())
            {
                var key = new ItemKey(row.OrderItemId, row.BatchItemId);
                if (wanted.Remove(key, out var qty))
                    row.Qty = qty;
                else
                {
                    db.NestedParts.Remove(row);
                    rows.Remove(row);
                }
            }
            foreach (var (key, qty) in wanted)
                db.NestedParts.Add(new NestedPart { Nest = nest, OrderItemId = key.OrderItemId, BatchItemId = key.BatchItemId, Qty = qty });
        }

        var nestNameById = nests.ToDictionary(n => n.Id, n => n.NestName);
        foreach (var row in rows.Where(r => !seenNests.Contains(nestNameById[r.NestId])))
            db.NestedParts.Remove(row);

        await db.SaveChangesAsync();
        await RecomputeQtyNestedAsync(db, affected);
    }

    private static async Task RecomputeQtyNestedAsync(ApplicationDbContext db, HashSet<ItemKey> items)
    {
        var orderIds = items.Where(k => k.OrderItemId != null).Select(k => k.OrderItemId!.Value).ToList();
        var batchIds = items.Where(k => k.BatchItemId != null).Select(k => k.BatchItemId!.Value).ToList();

        var orderSums = await db.NestedParts.Where(np => np.OrderItemId != null && orderIds.Contains(np.OrderItemId.Value))
            .GroupBy(np => np.OrderItemId!.Value).Select(g => new { Id = g.Key, Qty = g.Sum(np => np.Qty) })
            .ToDictionaryAsync(x => x.Id, x => x.Qty);
        foreach (var item in await db.OrderItems.Where(i => orderIds.Contains(i.Id)).ToListAsync())
            item.QtyNested = orderSums.GetValueOrDefault(item.Id);

        var batchSums = await db.NestedParts.Where(np => np.BatchItemId != null && batchIds.Contains(np.BatchItemId.Value))
            .GroupBy(np => np.BatchItemId!.Value).Select(g => new { Id = g.Key, Qty = g.Sum(np => np.Qty) })
            .ToDictionaryAsync(x => x.Id, x => x.Qty);
        foreach (var item in await db.BatchItems.Where(i => batchIds.Contains(i.Id)).ToListAsync())
            item.QtyNested = batchSums.GetValueOrDefault(item.Id);

        await db.SaveChangesAsync();
    }

    // ── Projects ────────────────────────────────────────────────────────────

    /// <summary>
    /// The station's active project, refusing a payload read from some other project — applying it
    /// would put that project's part IDs onto this project's items. A payload with no project path
    /// (an older bridge) is taken to be for the active project.
    /// </summary>
    private static async Task<(RadanProject? Project, string? Rejection)> GetActiveProjectAsync(
        ApplicationDbContext db, int stationId, string? payloadProjectPath)
    {
        var station = await db.NestingStations.AsNoTracking().FirstOrDefaultAsync(s => s.Id == stationId);
        if (station == null) return (null, $"Station {stationId} not found.");
        if (string.IsNullOrWhiteSpace(station.ProjectPath))
            return (null, "This station has no active project. Use \"Set Active Project\" first.");

        if (!string.IsNullOrEmpty(payloadProjectPath) &&
            !string.Equals(payloadProjectPath, station.ProjectPath, StringComparison.OrdinalIgnoreCase))
        {
            return (null, $"Sync rejected: payload is for project '{payloadProjectPath}' but this station's " +
                          $"recorded active project is '{station.ProjectPath}'. Use \"Set Active Project\" " +
                          "if the project was changed outside of Finalize.");
        }

        var project = await GetOrCreateOpenProjectAsync(db, station.PlantId, station.ProjectPath);
        if (project.Status != RadanProjectStatus.Open)
            return (null, $"The station's active project '{project.ProjectName}' is {project.Status.ToString().ToLowerInvariant()}.");
        return (project, null);
    }

    private static async Task<RadanProject> GetOrCreateOpenProjectAsync(ApplicationDbContext db, int plantId, string rpdPath)
    {
        var project = await FindProjectAsync(db, plantId, rpdPath);
        if (project != null)
        {
            project.RpdPath ??= rpdPath;
            return project;
        }

        project = new RadanProject
        {
            PlantId = plantId,
            ProjectDir = ProjectDirOf(rpdPath),
            RpdPath = rpdPath,
            ProjectName = Path.GetFileNameWithoutExtension(rpdPath),
            Status = RadanProjectStatus.Open,
        };
        db.RadanProjects.Add(project);
        await db.SaveChangesAsync();
        return project;
    }

    private static Task<RadanProject?> FindProjectAsync(ApplicationDbContext db, int plantId, string rpdPath)
    {
        var dir = ProjectDirOf(rpdPath).ToLower();
        return db.RadanProjects.FirstOrDefaultAsync(p => p.PlantId == plantId && p.ProjectDir.ToLower() == dir);
    }

    private static string ProjectDirOf(string rpdPath) => Path.GetDirectoryName(rpdPath.Trim()) ?? rpdPath.Trim();

    private static bool IsBatch(string itemType) => string.Equals(itemType, "Batch", StringComparison.OrdinalIgnoreCase);

    private readonly record struct ItemKey(int? OrderItemId, int? BatchItemId);
}
