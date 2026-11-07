using HorstMFG.Core.Entities;
using HorstMFG.Infrastructure.Data;
using HorstMFG.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HorstMFG.Web.Tests;

/// <summary>
/// Drives NestingSyncService with Bridge-shaped payloads against an EF InMemory database, covering
/// a Radan project being saved, edited and saved again, and finalized. Payloads are built the way
/// RpdService.BuildSyncPayload builds them: a nest only lists the parts currently on it, so a part
/// taken off a nest simply stops appearing under that nest rather than being sent with Qty 0.
/// </summary>
public class NestingSyncServiceTests
{
    private const int StationId = 1;
    private const int PlantId = 1;
    private const string ProjectDir = @"C:\Radan Projects\2026-11-06";
    private const string ProjectPath = ProjectDir + @"\2026-11-06.rpd";
    private const string OldProjectDir = @"C:\Radan Projects\2026-11-05";

    // Radan part IDs within the current project.
    private const long RadanA = 1;
    private const long RadanB = 2;
    private const long RadanC = 3;

    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory = new InMemoryDbContextFactory();
    private readonly NestingSyncService _service;

    private int _orderItemA, _orderItemB, _batchItemC;

    public NestingSyncServiceTests()
    {
        _service = new NestingSyncService(_dbFactory);
        using var db = _dbFactory.CreateDbContext();
        db.NestingStations.Add(new NestingStation { Id = StationId, PlantId = PlantId, Name = "Laser 1", ProjectPath = ProjectPath });
        var a = TrackedOrderItem(RadanA, qtyRequired: 10);
        var b = TrackedOrderItem(RadanB, qtyRequired: 5);
        var c = new BatchItem { QtyRequired = 8, RadanIdNumber = (int)RadanC, IsInRadanProject = true, NestingStationId = StationId };
        db.OrderItems.AddRange(a, b);
        db.BatchItems.Add(c);
        db.SaveChanges();
        (_orderItemA, _orderItemB, _batchItemC) = (a.Id, b.Id, c.Id);
    }

    // ── Saving a nest ────────────────────────────────────────────────────────

    [Fact]
    public async Task FirstSave_SetsQtyNestedFromNests()
    {
        await SyncAsync(("101", RadanA, 4), ("101", RadanB, 2), ("101", RadanC, 3));

        Assert.Equal(4, await OrderQtyNestedAsync(_orderItemA));
        Assert.Equal(2, await OrderQtyNestedAsync(_orderItemB));
        Assert.Equal(3, await BatchQtyNestedAsync(_batchItemC));
    }

    [Fact]
    public async Task PartOnSeveralNests_SumsAcrossNests()
    {
        await SyncAsync(("101", RadanA, 4), ("102", RadanA, 3));

        Assert.Equal(7, await OrderQtyNestedAsync(_orderItemA));
    }

    [Fact]
    public async Task Resave_WithLowerQtyOnNest_LowersQtyNested()
    {
        await SyncAsync(("101", RadanA, 4), ("101", RadanB, 2));
        await SyncAsync(("101", RadanA, 1), ("101", RadanB, 2));

        Assert.Equal(1, await OrderQtyNestedAsync(_orderItemA));
        Assert.Equal(2, await OrderQtyNestedAsync(_orderItemB));
    }

    [Fact]
    public async Task Resave_WithHigherQtyOnNest_RaisesQtyNested()
    {
        await SyncAsync(("101", RadanA, 4));
        await SyncAsync(("101", RadanA, 6));

        Assert.Equal(6, await OrderQtyNestedAsync(_orderItemA));
    }

    // ── Parts removed from a nest after it was saved ─────────────────────────

    [Fact]
    public async Task OrderPartRemovedFromNest_WhileOtherPartsStayOnIt_QtyNestedDropsToZero()
    {
        await SyncAsync(("101", RadanA, 4), ("101", RadanB, 2));
        await SyncAsync(("101", RadanB, 2));   // A taken off nest 101; the nest itself still exists

        Assert.Equal(0, await OrderQtyNestedAsync(_orderItemA));
        Assert.Equal(2, await OrderQtyNestedAsync(_orderItemB));
        Assert.False(await HasNestedPartAsync("101", orderItemId: _orderItemA));
    }

    [Fact]
    public async Task BatchPartRemovedFromNest_WhileOtherPartsStayOnIt_QtyNestedDropsToZero()
    {
        await SyncAsync(("101", RadanA, 4), ("101", RadanC, 3));
        await SyncAsync(("101", RadanA, 4));   // C taken off nest 101

        Assert.Equal(0, await BatchQtyNestedAsync(_batchItemC));
        Assert.Equal(4, await OrderQtyNestedAsync(_orderItemA));
    }

    [Fact]
    public async Task PartRemovedFromOneOfItsNests_KeepsQtyFromTheOthers()
    {
        await SyncAsync(("101", RadanA, 4), ("101", RadanB, 2), ("102", RadanA, 3));
        await SyncAsync(("101", RadanB, 2), ("102", RadanA, 3));   // A taken off 101 only

        Assert.Equal(3, await OrderQtyNestedAsync(_orderItemA));
    }

    [Fact]
    public async Task PartMovedToAnotherNest_IsCountedOnce()
    {
        await SyncAsync(("101", RadanA, 4), ("101", RadanB, 2));
        await SyncAsync(("101", RadanB, 2), ("102", RadanA, 4));

        Assert.Equal(4, await OrderQtyNestedAsync(_orderItemA));
    }

    [Fact]
    public async Task PartSentWithZeroQtyOnNest_IsRemovedFromIt()
    {
        await SyncAsync(("101", RadanA, 4), ("101", RadanB, 2));
        await SyncAsync(("101", RadanA, 0), ("101", RadanB, 2));

        Assert.Equal(0, await OrderQtyNestedAsync(_orderItemA));
    }

    // ── Nests deleted after they were saved ──────────────────────────────────

    [Fact]
    public async Task NestDeleted_QtyNestedDropsByThatNest()
    {
        await SyncAsync(("101", RadanA, 4), ("102", RadanA, 3), ("102", RadanB, 2));
        await SyncAsync(("101", RadanA, 4));   // nest 102 deleted

        Assert.Equal(4, await OrderQtyNestedAsync(_orderItemA));
        Assert.Equal(0, await OrderQtyNestedAsync(_orderItemB));
    }

    [Fact]
    public async Task EveryNestDeleted_QtyNestedIsZero()
    {
        await SyncAsync(("101", RadanA, 4), ("101", RadanC, 3));
        await SyncAsync(new SyncPayloadDto
        {
            ProjectPath = ProjectPath,
            Parts = { new SyncPartDto(RadanA, 0), new SyncPartDto(RadanC, 0) },
        });

        Assert.Equal(0, await OrderQtyNestedAsync(_orderItemA));
        Assert.Equal(0, await BatchQtyNestedAsync(_batchItemC));
    }

    [Fact]
    public async Task EmptyProject_ZeroesQtyNested()
    {
        await SyncAsync(("101", RadanA, 4));
        await SyncAsync(new SyncPayloadDto { ProjectPath = ProjectPath });

        Assert.Equal(0, await OrderQtyNestedAsync(_orderItemA));
    }

    // ── Projects changed after the fact ──────────────────────────────────────

    [Fact]
    public async Task NestsFromAnEarlierProject_AreKeptWhenTheCurrentProjectIsSynced()
    {
        // A was nested in an earlier, finalized project and has been sent to the current one for more.
        await using (var db = await _dbFactory.CreateDbContextAsync())
        {
            var oldNest = new Nest { NestName = "90", NestPath = OldProjectDir + @"\nests\90.drg", PlantId = PlantId };
            db.Nests.Add(oldNest);
            await db.SaveChangesAsync();
            db.NestedParts.Add(new NestedPart { NestId = oldNest.Id, OrderItemId = _orderItemA, Qty = 5 });
            await db.SaveChangesAsync();
        }

        await SyncAsync(("101", RadanA, 3));
        Assert.Equal(8, await OrderQtyNestedAsync(_orderItemA));

        await SyncAsync(("101", RadanB, 2));   // A taken off the current nest; old history stays
        Assert.Equal(5, await OrderQtyNestedAsync(_orderItemA));
    }

    [Fact]
    public async Task SyncForAProjectOtherThanTheActiveOne_IsRejectedAndChangesNothing()
    {
        await SyncAsync(("101", RadanA, 4));

        var rejection = await _service.ApplySyncPayloadAsync(StationId, PlantId,
            Payload(OldProjectDir + @"\2026-11-05.rpd", ("101", RadanA, 1)));

        Assert.NotNull(rejection);
        Assert.Equal(4, await OrderQtyNestedAsync(_orderItemA));
    }

    [Fact]
    public async Task ItemsNoLongerInTheProject_AreNotTouched()
    {
        await SyncAsync(("101", RadanA, 4), ("101", RadanB, 2));
        await using (var db = await _dbFactory.CreateDbContextAsync())
        {
            var b = await db.OrderItems.SingleAsync(i => i.Id == _orderItemB);
            b.IsInRadanProject = false;   // e.g. pulled back out of Radan from the nesting page
            await db.SaveChangesAsync();
        }

        await SyncAsync(("101", RadanA, 4));

        Assert.Equal(2, await OrderQtyNestedAsync(_orderItemB));
    }

    // ── Finalize ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Finalize_KeepsFinalQtys_ClearsTracking_AndAdvancesTheProject()
    {
        await SyncAsync(("101", RadanA, 4), ("101", RadanB, 2));

        const string newPath = @"C:\Radan Projects\2026-11-07\2026-11-07.rpd";
        var final = Payload(ProjectPath, ("101", RadanA, 4));   // B taken off before finalizing
        var rejection = await _service.ApplyFinalizeAsync(StationId, PlantId, new FinalizeResultDto
        {
            Sync = final, NewProjectName = "2026-11-07", NewProjectPath = newPath,
        });

        Assert.Null(rejection);
        await using var db = await _dbFactory.CreateDbContextAsync();
        var a = await db.OrderItems.SingleAsync(i => i.Id == _orderItemA);
        var b = await db.OrderItems.SingleAsync(i => i.Id == _orderItemB);
        Assert.Equal(4, a.QtyNested);
        Assert.Equal(0, b.QtyNested);
        Assert.All(new[] { a, b }, i => { Assert.False(i.IsInRadanProject); Assert.Null(i.NestingStationId); });
        Assert.Equal((int)RadanA, a.RadanIdNumber);   // nested: keeps its Radan ID as history
        Assert.Null(b.RadanIdNumber);                 // never nested: freed up
        Assert.Equal(newPath, (await db.NestingStations.SingleAsync()).ProjectPath);
    }

    [Fact]
    public async Task SyncAfterFinalize_LeavesTheFinalizedItemsAlone()
    {
        await SyncAsync(("101", RadanA, 4));
        const string newPath = @"C:\Radan Projects\2026-11-07\2026-11-07.rpd";
        await _service.ApplyFinalizeAsync(StationId, PlantId, new FinalizeResultDto
        {
            Sync = Payload(ProjectPath, ("101", RadanA, 4)), NewProjectName = "2026-11-07", NewProjectPath = newPath,
        });

        // The new project starts its part IDs at 1 again, so RadanA's ID now belongs to a different part.
        await SyncAsync(Payload(newPath));

        Assert.Equal(4, await OrderQtyNestedAsync(_orderItemA));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static OrderItem TrackedOrderItem(long radanId, int qtyRequired) => new()
    {
        QtyRequired = qtyRequired, RadanIdNumber = (int)radanId, IsInRadanProject = true, NestingStationId = StationId,
    };

    private Task SyncAsync(params (string Nest, long RadanId, int Qty)[] placements) =>
        SyncAsync(Payload(ProjectPath, placements));

    private async Task SyncAsync(SyncPayloadDto payload)
    {
        var rejection = await _service.ApplySyncPayloadAsync(StationId, PlantId, payload);
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

    private async Task<bool> HasNestedPartAsync(string nestName, int orderItemId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.NestedParts.AnyAsync(np => np.Nest.NestName == nestName && np.OrderItemId == orderItemId);
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
