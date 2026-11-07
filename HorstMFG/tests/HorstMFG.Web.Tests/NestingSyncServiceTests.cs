using HorstMFG.Core.Entities;
using HorstMFG.Core.Enums;
using HorstMFG.Infrastructure.Data;
using HorstMFG.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HorstMFG.Web.Tests;

/// <summary>
/// Drives NestingSyncService with Bridge-shaped payloads against an EF InMemory database, covering
/// a Radan project being saved, edited and saved again, finalized, and re-synced after the fact.
/// Payloads are built the way RpdService.BuildSyncPayload builds them: a nest only lists the parts
/// currently on it, so a part taken off a nest simply stops appearing under that nest.
///
/// The project paths are only compared as text; nothing here touches the disk.
/// </summary>
public class NestingSyncServiceTests
{
    private const int StationId = 1;
    private const int PlantId = 1;
    private const string Project1 = @"C:\Radan Projects\2026-11-05\2026-11-05.rpd";
    private const string Project2 = @"C:\Radan Projects\2026-11-06\2026-11-06.rpd";
    private const string Project3 = @"C:\Radan Projects\2026-11-07\2026-11-07.rpd";

    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory = new InMemoryDbContextFactory();
    private readonly NestingSyncService _service;

    private readonly int _orderItemA, _orderItemB, _batchItemC;

    public NestingSyncServiceTests()
    {
        _service = new NestingSyncService(_dbFactory);
        using var db = _dbFactory.CreateDbContext();
        db.NestingStations.Add(new NestingStation { Id = StationId, PlantId = PlantId, Name = "Laser 1", ProjectPath = Project1 });
        var a = new OrderItem { QtyRequired = 10 };
        var b = new OrderItem { QtyRequired = 5 };
        var c = new BatchItem { QtyRequired = 8 };
        db.OrderItems.AddRange(a, b);
        db.BatchItems.Add(c);
        db.SaveChanges();
        (_orderItemA, _orderItemB, _batchItemC) = (a.Id, b.Id, c.Id);
    }

    // ── Saving a nest ────────────────────────────────────────────────────────

    [Fact]
    public async Task FirstSave_SetsQtyNestedFromNests()
    {
        await SendAsync(("Order", _orderItemA, 1), ("Order", _orderItemB, 2), ("Batch", _batchItemC, 3));
        await SyncAsync(Project1, ("101", 1, 4), ("101", 2, 2), ("101", 3, 3));

        Assert.Equal(4, await OrderQtyNestedAsync(_orderItemA));
        Assert.Equal(2, await OrderQtyNestedAsync(_orderItemB));
        Assert.Equal(3, await BatchQtyNestedAsync(_batchItemC));
    }

    [Fact]
    public async Task PartOnSeveralNests_SumsAcrossNests()
    {
        await SendAsync(("Order", _orderItemA, 1));
        await SyncAsync(Project1, ("101", 1, 4), ("102", 1, 3));

        Assert.Equal(7, await OrderQtyNestedAsync(_orderItemA));
    }

    [Fact]
    public async Task Resave_WithLowerQtyOnNest_LowersQtyNested()
    {
        await SendAsync(("Order", _orderItemA, 1), ("Order", _orderItemB, 2));
        await SyncAsync(Project1, ("101", 1, 4), ("101", 2, 2));
        await SyncAsync(Project1, ("101", 1, 1), ("101", 2, 2));

        Assert.Equal(1, await OrderQtyNestedAsync(_orderItemA));
        Assert.Equal(2, await OrderQtyNestedAsync(_orderItemB));
    }

    [Fact]
    public async Task Resave_WithHigherQtyOnNest_RaisesQtyNested()
    {
        await SendAsync(("Order", _orderItemA, 1));
        await SyncAsync(Project1, ("101", 1, 4));
        await SyncAsync(Project1, ("101", 1, 6));

        Assert.Equal(6, await OrderQtyNestedAsync(_orderItemA));
    }

    [Fact]
    public async Task PartsWithNoPlacement_AreIgnored()
    {
        await SendAsync(("Order", _orderItemA, 1));
        await SyncAsync(Project1, ("101", 1, 4), ("101", 99, 7));   // part 99 was added in Radan by hand

        Assert.Equal(4, await OrderQtyNestedAsync(_orderItemA));
        Assert.Equal(1, await RowCountAsync());
    }

    // ── Parts removed from a nest after it was saved ─────────────────────────

    [Fact]
    public async Task OrderPartRemovedFromNest_WhileOtherPartsStayOnIt_QtyNestedDropsToZero()
    {
        await SendAsync(("Order", _orderItemA, 1), ("Order", _orderItemB, 2));
        await SyncAsync(Project1, ("101", 1, 4), ("101", 2, 2));
        await SyncAsync(Project1, ("101", 2, 2));   // A taken off nest 101; the nest itself still exists

        Assert.Equal(0, await OrderQtyNestedAsync(_orderItemA));
        Assert.Equal(2, await OrderQtyNestedAsync(_orderItemB));
    }

    [Fact]
    public async Task BatchPartRemovedFromNest_WhileOtherPartsStayOnIt_QtyNestedDropsToZero()
    {
        await SendAsync(("Order", _orderItemA, 1), ("Batch", _batchItemC, 3));
        await SyncAsync(Project1, ("101", 1, 4), ("101", 3, 3));
        await SyncAsync(Project1, ("101", 1, 4));   // C taken off nest 101

        Assert.Equal(0, await BatchQtyNestedAsync(_batchItemC));
        Assert.Equal(4, await OrderQtyNestedAsync(_orderItemA));
    }

    [Fact]
    public async Task PartRemovedFromOneOfItsNests_KeepsQtyFromTheOthers()
    {
        await SendAsync(("Order", _orderItemA, 1), ("Order", _orderItemB, 2));
        await SyncAsync(Project1, ("101", 1, 4), ("101", 2, 2), ("102", 1, 3));
        await SyncAsync(Project1, ("101", 2, 2), ("102", 1, 3));   // A taken off 101 only

        Assert.Equal(3, await OrderQtyNestedAsync(_orderItemA));
    }

    [Fact]
    public async Task PartMovedToAnotherNest_IsCountedOnce()
    {
        await SendAsync(("Order", _orderItemA, 1), ("Order", _orderItemB, 2));
        await SyncAsync(Project1, ("101", 1, 4), ("101", 2, 2));
        await SyncAsync(Project1, ("101", 2, 2), ("102", 1, 4));

        Assert.Equal(4, await OrderQtyNestedAsync(_orderItemA));
    }

    [Fact]
    public async Task PartSentWithZeroQtyOnNest_IsRemovedFromIt()
    {
        await SendAsync(("Order", _orderItemA, 1), ("Order", _orderItemB, 2));
        await SyncAsync(Project1, ("101", 1, 4), ("101", 2, 2));
        await SyncAsync(Project1, ("101", 1, 0), ("101", 2, 2));

        Assert.Equal(0, await OrderQtyNestedAsync(_orderItemA));
    }

    // ── Nests deleted after they were saved ──────────────────────────────────

    [Fact]
    public async Task NestDeleted_QtyNestedDropsByThatNest()
    {
        await SendAsync(("Order", _orderItemA, 1), ("Order", _orderItemB, 2));
        await SyncAsync(Project1, ("101", 1, 4), ("102", 1, 3), ("102", 2, 2));
        await SyncAsync(Project1, ("101", 1, 4));   // nest 102 deleted

        Assert.Equal(4, await OrderQtyNestedAsync(_orderItemA));
        Assert.Equal(0, await OrderQtyNestedAsync(_orderItemB));
    }

    [Fact]
    public async Task EveryNestDeleted_QtyNestedIsZero()
    {
        await SendAsync(("Order", _orderItemA, 1), ("Batch", _batchItemC, 3));
        await SyncAsync(Project1, ("101", 1, 4), ("101", 3, 3));
        await SyncAsync(Payload(Project1));

        Assert.Equal(0, await OrderQtyNestedAsync(_orderItemA));
        Assert.Equal(0, await BatchQtyNestedAsync(_batchItemC));
    }

    // ── Several projects ─────────────────────────────────────────────────────

    [Fact]
    public async Task NestNamesAndPartIdsRepeatedInTheNextProject_DontMixTheProjects()
    {
        await SendAsync(("Order", _orderItemA, 1));
        await SyncAsync(Project1, ("101", 1, 4));
        await FinalizeAsync(Project1, Project2, ("101", 1, 4));

        // Radan starts the new project's part IDs at 1 again, and this nest number happens to repeat.
        await SendAsync(("Order", _orderItemB, 1));
        await SyncAsync(Project2, ("101", 1, 2));

        Assert.Equal(4, await OrderQtyNestedAsync(_orderItemA));
        Assert.Equal(2, await OrderQtyNestedAsync(_orderItemB));
    }

    [Fact]
    public async Task ItemNestedInThreeProjects_SumsAllOfThem()
    {
        await SendAsync(("Order", _orderItemA, 1));
        await SyncAsync(Project1, ("101", 1, 4));
        await FinalizeAsync(Project1, Project2, ("101", 1, 4));

        await SendAsync(("Order", _orderItemA, 5));
        await SyncAsync(Project2, ("201", 5, 3));
        await FinalizeAsync(Project2, Project3, ("201", 5, 3));

        await SendAsync(("Order", _orderItemA, 1));
        await SyncAsync(Project3, ("301", 1, 2));

        Assert.Equal(9, await OrderQtyNestedAsync(_orderItemA));
    }

    [Fact]
    public async Task SyncForAProjectOtherThanTheActiveOne_IsRejectedAndChangesNothing()
    {
        await SendAsync(("Order", _orderItemA, 1));
        await SyncAsync(Project1, ("101", 1, 4));

        var rejection = await _service.ApplySyncPayloadAsync(StationId, Payload(Project2, ("101", 1, 1)));

        Assert.NotNull(rejection);
        Assert.Equal(4, await OrderQtyNestedAsync(_orderItemA));
    }

    [Fact]
    public async Task SyncWithNoActiveProject_IsRejected()
    {
        await using (var db = await _dbFactory.CreateDbContextAsync())
        {
            (await db.NestingStations.SingleAsync()).ProjectPath = null;
            await db.SaveChangesAsync();
        }

        Assert.NotNull(await _service.ApplySyncPayloadAsync(StationId, Payload(Project1)));
    }

    // ── Re-syncing a finalized project ───────────────────────────────────────

    [Fact]
    public async Task ResyncOfFinalizedProject_ChangesOnlyThatProject()
    {
        await SendAsync(("Order", _orderItemA, 1), ("Order", _orderItemB, 2));
        await SyncAsync(Project1, ("101", 1, 4), ("101", 2, 2));
        await FinalizeAsync(Project1, Project2, ("101", 1, 4), ("101", 2, 2));

        await SendAsync(("Order", _orderItemA, 1));     // A's next 3 nested in the new project
        await SyncAsync(Project2, ("201", 1, 3));
        Assert.Equal(7, await OrderQtyNestedAsync(_orderItemA));

        // Nest 101 in the old project is edited in Radan: A taken off it.
        var rejection = await _service.ApplyProjectResyncAsync(PlantId, Payload(Project1, ("101", 2, 2)));

        Assert.Null(rejection);
        Assert.Equal(3, await OrderQtyNestedAsync(_orderItemA));
        Assert.Equal(2, await OrderQtyNestedAsync(_orderItemB));
    }

    [Fact]
    public async Task ResyncOfFinalizedProject_LeavesTheActiveProjectWorking()
    {
        await SendAsync(("Order", _orderItemA, 1));
        await SyncAsync(Project1, ("101", 1, 4));
        await FinalizeAsync(Project1, Project2, ("101", 1, 4));
        await SendAsync(("Order", _orderItemB, 1));
        await SyncAsync(Project2, ("201", 1, 2));

        await _service.ApplyProjectResyncAsync(PlantId, Payload(Project1, ("101", 1, 5)));
        await SyncAsync(Project2, ("201", 1, 3));

        Assert.Equal(5, await OrderQtyNestedAsync(_orderItemA));
        Assert.Equal(3, await OrderQtyNestedAsync(_orderItemB));
    }

    [Fact]
    public async Task ResyncOfLegacyProject_IsRejectedAndChangesNothing()
    {
        await SeedLegacyNestAsync(_orderItemA, qty: 5);

        var rejection = await _service.ApplyProjectResyncAsync(PlantId,
            Payload(@"S:\Radan Projects\2024-01-10\2024-01-10.rpd"));

        Assert.NotNull(rejection);
        Assert.Equal(5, await OrderQtyNestedAsync(_orderItemA));
    }

    [Fact]
    public async Task ResyncOfUnknownProject_IsRejected()
    {
        Assert.NotNull(await _service.ApplyProjectResyncAsync(PlantId, Payload(@"C:\Elsewhere\x\x.rpd")));
    }

    [Fact]
    public async Task LegacyHistory_CountsTowardQtyNested_AndSurvivesSyncs()
    {
        await SeedLegacyNestAsync(_orderItemA, qty: 5);
        await SendAsync(("Order", _orderItemA, 1));

        await SyncAsync(Project1, ("101", 1, 3));
        Assert.Equal(8, await OrderQtyNestedAsync(_orderItemA));

        await SyncAsync(Payload(Project1));   // everything removed from the current project
        Assert.Equal(5, await OrderQtyNestedAsync(_orderItemA));
    }

    // ── Active project ───────────────────────────────────────────────────────

    [Fact]
    public async Task SetActiveProject_RefusesAFinalizedProject()
    {
        await SendAsync(("Order", _orderItemA, 1));
        await FinalizeAsync(Project1, Project2, ("101", 1, 4));

        Assert.NotNull(await _service.SetActiveProjectAsync(StationId, Project1));
        Assert.Equal(Project2, await StationProjectAsync());
    }

    [Fact]
    public async Task SetActiveProject_RefusesALegacyProject()
    {
        await SeedLegacyNestAsync(_orderItemA, qty: 5);

        Assert.NotNull(await _service.SetActiveProjectAsync(StationId, @"S:\Radan Projects\2024-01-10\2024-01-10.rpd"));
        Assert.Equal(Project1, await StationProjectAsync());
    }

    [Fact]
    public async Task SetActiveProject_AcceptsANewProject()
    {
        Assert.Null(await _service.SetActiveProjectAsync(StationId, Project3));
        Assert.Equal(Project3, await StationProjectAsync());

        await SendAsync(("Order", _orderItemA, 1));
        await SyncAsync(Project3, ("301", 1, 2));
        Assert.Equal(2, await OrderQtyNestedAsync(_orderItemA));
    }

    // ── Retrieve ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Retrieve_ClearedItemLosesItsPlacement_AdjustedItemKeepsIt()
    {
        await SendAsync(("Order", _orderItemA, 1), ("Order", _orderItemB, 2));
        await SyncAsync(Project1, ("101", 1, 4));

        var rejection = await _service.ApplyRetrieveAsync(StationId, "Order", new RetrieveFromNestingResultDto
        {
            Sync = Payload(Project1, ("101", 1, 4)),
            ClearedItemIds = { _orderItemB },    // nothing nested: its Radan part is deleted
            AdjustedItemIds = { _orderItemA },   // part kept, cut back to the 4 nested
        });
        Assert.Null(rejection);

        await using var db = await _dbFactory.CreateDbContextAsync();
        var a = await db.OrderItems.SingleAsync(i => i.Id == _orderItemA);
        var b = await db.OrderItems.SingleAsync(i => i.Id == _orderItemB);
        Assert.False(a.IsInRadanProject);
        Assert.False(b.IsInRadanProject);
        Assert.Null(b.RadanIdNumber);
        Assert.True(await db.RadanPlacements.AnyAsync(p => p.OrderItemId == _orderItemA));
        Assert.False(await db.RadanPlacements.AnyAsync(p => p.OrderItemId == _orderItemB));

        // The adjusted part is still in Radan, so later edits to its nest still count.
        await SyncAsync(Project1, ("101", 1, 2));
        Assert.Equal(2, await OrderQtyNestedAsync(_orderItemA));
    }

    // ── Finalize ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Finalize_KeepsFinalQtys_ClearsTracking_AndAdvancesTheProject()
    {
        await SendAsync(("Order", _orderItemA, 1), ("Order", _orderItemB, 2));
        await SyncAsync(Project1, ("101", 1, 4), ("101", 2, 2));

        await FinalizeAsync(Project1, Project2, ("101", 1, 4));   // B taken off before finalizing

        await using var db = await _dbFactory.CreateDbContextAsync();
        var a = await db.OrderItems.SingleAsync(i => i.Id == _orderItemA);
        var b = await db.OrderItems.SingleAsync(i => i.Id == _orderItemB);
        Assert.Equal(4, a.QtyNested);
        Assert.Equal(0, b.QtyNested);
        Assert.All(new[] { a, b }, i =>
        {
            Assert.False(i.IsInRadanProject);
            Assert.Null(i.NestingStationId);
            Assert.Null(i.RadanIdNumber);   // the ID only meant something in the old project
        });
        var old = await db.RadanProjects.SingleAsync(p => p.RpdPath == Project1);
        Assert.Equal(RadanProjectStatus.Finalized, old.Status);
        Assert.Equal(2, await db.RadanPlacements.CountAsync(p => p.RadanProjectId == old.Id));
        Assert.Equal(RadanProjectStatus.Open, (await db.RadanProjects.SingleAsync(p => p.RpdPath == Project2)).Status);
        Assert.Equal(Project2, (await db.NestingStations.SingleAsync()).ProjectPath);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Stands in for a SendToNesting result: Radan gave each item the given part ID in the active project.</summary>
    private async Task SendAsync(params (string ItemType, int ItemId, long RadanId)[] sent)
    {
        foreach (var group in sent.GroupBy(s => s.ItemType))
        {
            var rejection = await _service.RecordSentToNestingAsync(StationId, group.Key,
                group.Select(s => new SendToNestingResultDto(s.ItemId, s.RadanId, false, true)).ToList());
            Assert.Null(rejection);
        }
    }

    private Task SyncAsync(string projectPath, params (string Nest, long RadanId, int Qty)[] placements) =>
        SyncAsync(Payload(projectPath, placements));

    private async Task SyncAsync(SyncPayloadDto payload)
    {
        var rejection = await _service.ApplySyncPayloadAsync(StationId, payload);
        Assert.Null(rejection);
    }

    private async Task FinalizeAsync(string projectPath, string newProjectPath, params (string Nest, long RadanId, int Qty)[] placements)
    {
        var rejection = await _service.ApplyFinalizeAsync(StationId, new FinalizeResultDto
        {
            Sync = Payload(projectPath, placements),
            NewProjectName = Path.GetFileNameWithoutExtension(newProjectPath),
            NewProjectPath = newProjectPath,
        });
        Assert.Null(rejection);
    }

    /// <summary>Builds a payload the way the Bridge does: every placement is listed under its nest,
    /// and each part's project-wide QtyNested is the sum of its placements.</summary>
    private static SyncPayloadDto Payload(string projectPath, params (string Nest, long RadanId, int Qty)[] placements)
    {
        var projectDir = Path.GetDirectoryName(projectPath)!;
        var payload = new SyncPayloadDto { ProjectPath = projectPath };
        foreach (var group in placements.GroupBy(p => p.RadanId))
            payload.Parts.Add(new SyncPartDto(group.Key, group.Sum(p => p.Qty)));
        foreach (var group in placements.GroupBy(p => p.Nest))
            payload.Nests.Add(new SyncNestDto
            {
                NestName = group.Key,
                NestPath = Path.Combine(projectDir, "nests", group.Key + ".drg"),
                Parts = group.Select(p => new NestEntryDto { RadanIdNumber = p.RadanId, Qty = p.Qty }).ToList(),
            });
        return payload;
    }

    /// <summary>A RadanMaster-era nest as DataTransfer imports it: a Legacy project with no placements.</summary>
    private async Task SeedLegacyNestAsync(int orderItemId, int qty)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var project = new RadanProject
        {
            PlantId = PlantId, ProjectDir = @"S:\Radan Projects\2024-01-10", ProjectName = "2024-01-10",
            Status = RadanProjectStatus.Legacy,
        };
        var nest = new Nest { NestName = "1022", PlantId = PlantId, RadanProject = project };
        db.NestedParts.Add(new NestedPart { Nest = nest, OrderItemId = orderItemId, Qty = qty });
        (await db.OrderItems.SingleAsync(i => i.Id == orderItemId)).QtyNested = qty;
        await db.SaveChangesAsync();
    }

    private async Task<int> OrderQtyNestedAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return (await db.OrderItems.SingleAsync(i => i.Id == id)).QtyNested;
    }

    private async Task<int> BatchQtyNestedAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return (await db.BatchItems.SingleAsync(i => i.Id == id)).QtyNested;
    }

    private async Task<int> RowCountAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.NestedParts.CountAsync();
    }

    private async Task<string?> StationProjectAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return (await db.NestingStations.SingleAsync()).ProjectPath;
    }

    private sealed class InMemoryDbContextFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly DbContextOptions<ApplicationDbContext> _options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        public ApplicationDbContext CreateDbContext() => new(_options);
    }
}
