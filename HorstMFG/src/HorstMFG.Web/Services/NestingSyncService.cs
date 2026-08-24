using HorstMFG.Core.Entities;
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
}

public class FinalizeResultDto
{
    public SyncPayloadDto Sync { get; set; } = new();
    public List<int> ClearedItemIds { get; set; } = new();
    public List<int> AdjustedItemIds { get; set; } = new();
    public string NewProjectName { get; set; } = "";
    public string NewProjectPath { get; set; } = "";
}

/// <summary>
/// Applies bridge sync/finalize payloads to the database. Deliberately independent of any
/// Blazor component's lifecycle — AutoSync and Finalize results must be applied whether or not
/// a browser panel happens to be connected at the moment they arrive, since both can originate
/// spontaneously (a file-watcher event, or a Finalize command whose initiating browser tab may
/// have since closed/refreshed/disconnected). Called directly from BridgeHub for those two
/// cases; NestingPanel.razor calls it too for its own manually-triggered Sync/Retrieve commands.
/// </summary>
public class NestingSyncService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public NestingSyncService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task ApplySyncPayloadAsync(int stationId, int plantIdOverride, SyncPayloadDto sync)
    {
        if (sync.Parts.Count == 0) return;
        await using var db = await _dbFactory.CreateDbContextAsync();

        var station = await db.NestingStations.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == stationId);

        // Resolve plant ID from the station so admin users (plantIdOverride==0) work correctly.
        var nestPlantId = plantIdOverride > 0 ? plantIdOverride : (station?.PlantId ?? 0);

        // Project folder the station is currently pointed at (parent of the .rpd file). Used to
        // scope stale-nest cleanup below to nests that could plausibly appear in this payload —
        // a nest from an earlier, already-finalized project never will, so it must never be
        // treated as "stale" just because this payload doesn't mention it.
        var currentProjectDir = station?.ProjectPath != null ? Path.GetDirectoryName(station.ProjectPath) : null;

        // One DB item = one Radan part (see SendToNestingHandler), so every RadanIdNumber maps
        // to exactly one tracked item — no cross-item qty splitting needed.
        //
        // A sync payload always describes the WHOLE shared Radan project — both order and batch
        // parts together — regardless of which panel (Order or Batch page) happened to trigger it.
        // Both blocks below therefore always run, unconditionally: each only ever touches its own
        // table, so running both from either panel is safe, and it's required — Finalize in
        // particular clears tracking for both item types unconditionally right after this call, so
        // skipping either branch here would permanently lose that type's final nested quantities.
        {
            var items = await db.OrderItems
                .Where(i => i.NestingStationId == stationId && i.RadanIdNumber != null && i.IsInRadanProject)
                .ToListAsync();
            var itemIds = items.Select(i => i.Id).ToList();
            // GroupBy/First instead of ToDictionary: tolerate leftover legacy items that still
            // share a RadanIdNumber from before the one-item-one-Radan-part cutover.
            var itemsByRadanId = items.GroupBy(i => i.RadanIdNumber!.Value).ToDictionary(g => g.Key, g => g.First());

            var allNestedParts = await db.NestedParts
                .Where(np => np.OrderItemId.HasValue && itemIds.Contains(np.OrderItemId.Value))
                .ToListAsync();

            var touchedNestIds = new HashSet<int>();

            foreach (var syncNest in sync.Nests)
            {
                var nest = await db.Nests
                    .FirstOrDefaultAsync(n => n.NestName == syncNest.NestName && n.PlantId == nestPlantId);
                if (nest == null)
                {
                    nest = new Nest { NestName = syncNest.NestName, NestPath = syncNest.NestPath, Thumbnail = syncNest.ThumbnailBytes, PlantId = nestPlantId };
                    db.Nests.Add(nest);
                    await db.SaveChangesAsync();
                }
                else
                {
                    if (syncNest.NestPath != null) nest.NestPath = syncNest.NestPath;
                    if (syncNest.ThumbnailBytes != null) nest.Thumbnail = syncNest.ThumbnailBytes;
                }
                touchedNestIds.Add(nest.Id);

                foreach (var entry in syncNest.Parts)
                {
                    if (!itemsByRadanId.TryGetValue((int)entry.RadanIdNumber, out var item)) continue;

                    var np = allNestedParts.FirstOrDefault(x => x.NestId == nest.Id && x.OrderItemId == item.Id);
                    if (entry.Qty > 0)
                    {
                        if (np == null)
                        {
                            var newNp = new NestedPart { NestId = nest.Id, OrderItemId = item.Id, Qty = entry.Qty };
                            db.NestedParts.Add(newNp);
                            allNestedParts.Add(newNp);
                        }
                        else
                            np.Qty = entry.Qty;
                    }
                    else if (np != null)
                    {
                        db.NestedParts.Remove(np);
                        allNestedParts.Remove(np);
                    }
                }
            }

            // A sync payload is authoritative for every nest a tracked item's RadanIdNumber
            // currently appears in — but only within the CURRENT project. Any leftover NestedPart
            // on a nest this sync didn't touch means that nest no longer exists in the live Radan
            // project, UNLESS the nest belongs to an earlier (already-finalized) project, which
            // will never appear in any future payload and must be preserved as history.
            var staleParts = await FilterStaleAsync(db, allNestedParts, touchedNestIds, currentProjectDir);
            foreach (var stale in staleParts)
            {
                db.NestedParts.Remove(stale);
                allNestedParts.Remove(stale);
            }

            foreach (var item in items)
                item.QtyNested = allNestedParts.Where(np => np.OrderItemId == item.Id).Sum(np => np.Qty);
        }
        {
            var items = await db.BatchItems
                .Where(i => i.NestingStationId == stationId && i.RadanIdNumber != null && i.IsInRadanProject)
                .ToListAsync();
            var itemIds = items.Select(i => i.Id).ToList();
            var itemsByRadanId = items.GroupBy(i => i.RadanIdNumber!.Value).ToDictionary(g => g.Key, g => g.First());

            var allNestedParts = await db.NestedParts
                .Where(np => np.BatchItemId.HasValue && itemIds.Contains(np.BatchItemId.Value))
                .ToListAsync();

            var touchedNestIds = new HashSet<int>();

            foreach (var syncNest in sync.Nests)
            {
                var nest = await db.Nests
                    .FirstOrDefaultAsync(n => n.NestName == syncNest.NestName && n.PlantId == nestPlantId);
                if (nest == null)
                {
                    nest = new Nest { NestName = syncNest.NestName, NestPath = syncNest.NestPath, Thumbnail = syncNest.ThumbnailBytes, PlantId = nestPlantId };
                    db.Nests.Add(nest);
                    await db.SaveChangesAsync();
                }
                else
                {
                    if (syncNest.NestPath != null) nest.NestPath = syncNest.NestPath;
                    if (syncNest.ThumbnailBytes != null) nest.Thumbnail = syncNest.ThumbnailBytes;
                }
                touchedNestIds.Add(nest.Id);

                foreach (var entry in syncNest.Parts)
                {
                    if (!itemsByRadanId.TryGetValue((int)entry.RadanIdNumber, out var item)) continue;

                    var np = allNestedParts.FirstOrDefault(x => x.NestId == nest.Id && x.BatchItemId == item.Id);
                    if (entry.Qty > 0)
                    {
                        if (np == null)
                        {
                            var newNp = new NestedPart { NestId = nest.Id, BatchItemId = item.Id, Qty = entry.Qty };
                            db.NestedParts.Add(newNp);
                            allNestedParts.Add(newNp);
                        }
                        else
                            np.Qty = entry.Qty;
                    }
                    else if (np != null)
                    {
                        db.NestedParts.Remove(np);
                        allNestedParts.Remove(np);
                    }
                }
            }

            var staleParts = await FilterStaleAsync(db, allNestedParts, touchedNestIds, currentProjectDir);
            foreach (var stale in staleParts)
            {
                db.NestedParts.Remove(stale);
                allNestedParts.Remove(stale);
            }

            foreach (var item in items)
                item.QtyNested = allNestedParts.Where(np => np.BatchItemId == item.Id).Sum(np => np.Qty);
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Applies a Finalize result: writes final NestedPart rows for both item types (via
    /// ApplySyncPayloadAsync), clears IsInRadanProject/NestingStationId for every item at the
    /// station, and advances the station's recorded active project to the new one. Must run
    /// regardless of whether a browser panel is connected — Finalize's real-world effect (Radan
    /// rotating to a new project) is irreversible, so the DB bookkeeping can't be allowed to
    /// silently no-op just because nobody's browser tab was open to catch the result.
    /// </summary>
    public async Task ApplyFinalizeAsync(int stationId, int plantIdOverride, FinalizeResultDto result)
    {
        await ApplySyncPayloadAsync(stationId, plantIdOverride, result.Sync);

        await using var db = await _dbFactory.CreateDbContextAsync();

        var nestedQty = result.Sync.Parts.ToDictionary(p => (int)p.RadanIdNumber, p => p.QtyNested);

        var orderItems = await db.OrderItems
            .Where(i => i.NestingStationId == stationId)
            .ToListAsync();
        foreach (var item in orderItems)
        {
            var qty = item.RadanIdNumber.HasValue
                   && nestedQty.TryGetValue(item.RadanIdNumber.Value, out var q) ? q : 0;
            item.IsInRadanProject = false;
            item.NestingStationId = null;
            if (qty == 0) item.RadanIdNumber = null;
        }

        var batchItems = await db.BatchItems
            .Where(i => i.NestingStationId == stationId)
            .ToListAsync();
        foreach (var item in batchItems)
        {
            var qty = item.RadanIdNumber.HasValue
                   && nestedQty.TryGetValue(item.RadanIdNumber.Value, out var q) ? q : 0;
            item.IsInRadanProject = false;
            item.NestingStationId = null;
            if (qty == 0) item.RadanIdNumber = null;
        }

        var station = await db.NestingStations.FindAsync(stationId);
        if (station != null)
        {
            station.ProjectName = result.NewProjectName;
            station.ProjectPath = result.NewProjectPath;
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Of the given NestedParts not touched by this sync, returns only the ones whose Nest
    /// belongs to the currently active project — those are truly stale (removed from the live
    /// project). NestedParts on nests from a different (older, already-finalized) project are
    /// left alone, since that project will never appear in a future payload to "confirm" them.
    /// A nest whose project can't be determined (unresolved NestPath) is also left alone —
    /// safer to leave a rare leftover row than risk deleting real history.
    /// </summary>
    private static async Task<List<NestedPart>> FilterStaleAsync(
        ApplicationDbContext db, List<NestedPart> allNestedParts, HashSet<int> touchedNestIds, string? currentProjectDir)
    {
        var untouched = allNestedParts.Where(np => !touchedNestIds.Contains(np.NestId)).ToList();
        if (untouched.Count == 0 || currentProjectDir == null) return new List<NestedPart>();

        var nestIds = untouched.Select(np => np.NestId).Distinct().ToList();
        var nestPaths = await db.Nests.AsNoTracking()
            .Where(n => nestIds.Contains(n.Id))
            .Select(n => new { n.Id, n.NestPath })
            .ToDictionaryAsync(n => n.Id, n => n.NestPath);

        return untouched.Where(np =>
        {
            if (!nestPaths.TryGetValue(np.NestId, out var nestPath) || nestPath == null) return false;
            var nestProjectDir = Path.GetDirectoryName(Path.GetDirectoryName(nestPath));
            return string.Equals(nestProjectDir, currentProjectDir, StringComparison.OrdinalIgnoreCase);
        }).ToList();
    }
}
