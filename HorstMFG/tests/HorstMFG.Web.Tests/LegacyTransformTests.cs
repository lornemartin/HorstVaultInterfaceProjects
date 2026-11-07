using HorstMFG.DataTransfer;

namespace HorstMFG.Web.Tests;

/// <summary>
/// How legacy RadanMaster rows become the rows the data transfer writes: which orders come across,
/// how nests map onto projects, and what happens to items RadanMaster still had in Radan.
/// </summary>
public class LegacyTransformTests
{
    private static readonly DateTime Jan2020 = new(2020, 1, 15);
    private static readonly DateTime Jun2025 = new(2025, 6, 1);

    private static SrcOrder Order(int id, DateTime entered, bool complete = true, bool batch = false) =>
        new(id, $"ORD{id}", batch ? null : "SCHED", batch ? "BATCH" : null, entered, null, complete, batch, null);

    private static SrcOrderItem Item(int id, int orderId, int partId, bool inProject = false, int radanId = 0, int required = 2, int nested = 2) =>
        new(id, orderId, false, required, nested, inProject, null, partId, radanId);

    private static SrcPart Part(int id, string? file) => new(id, file, null, null, null, null, false);

    private static SrcNest Nest(int id, string name, string dir) => new(id, name, dir, null);

    private static SourceData Source(List<SrcOrder> orders, List<SrcOrderItem> items, List<SrcPart> parts,
                                     List<SrcNest>? nests = null, List<SrcNestedPart>? nested = null) =>
        new(parts, nests ?? [], orders, items, nested ?? []);

    [Fact]
    public void Since_KeepsLaterOrders_AndEveryOpenOrder()
    {
        var orders = new[]
        {
            Order(1, Jan2020),                    // old and complete: dropped
            Order(2, Jan2020, complete: false),   // old but still open: kept
            Order(3, Jun2025),                    // recent: kept
        };

        var selected = LegacyTransform.SelectOrders(orders, since: new DateTime(2024, 1, 1));

        Assert.Equal([2, 3], selected.Select(o => o.Id));
    }

    [Fact]
    public void NoSince_KeepsEverything()
    {
        Assert.Equal(2, LegacyTransform.SelectOrders([Order(1, Jan2020), Order(2, Jun2025)], since: null).Count);
    }

    [Theory]
    [InlineData("2234.drg", "2234")]
    [InlineData("2234.DRG", "2234")]
    [InlineData(" 17 ", "17")]
    [InlineData("", "(unnamed)")]
    [InlineData(null, "(unnamed)")]
    public void NestName_DropsTheDrgExtension_LikeTheBridgeNamesNests(string? legacy, string expected)
    {
        Assert.Equal(expected, LegacyTransform.NestName(legacy));
    }

    [Fact]
    public void Nests_GroupIntoOneLegacyProjectPerFolder_DatedByTheFolderName()
    {
        var src = Source(
            orders: [Order(1, Jun2025), Order(2, Jan2020)],
            items:  [Item(10, 1, partId: 100), Item(20, 2, partId: 100)],
            parts:  [Part(100, "P-100")],
            nests:  [Nest(1, "5.drg", @"S:\Radan Projects\2025-06-01\"), Nest(2, "6.drg", @"S:\Radan Projects\2025-06-01")],
            nested: [new(1, 2, NestId: 1, OrderItemId: 10), new(2, 2, NestId: 2, OrderItemId: 20)]);

        var plan = LegacyTransform.Build(src);

        var project = Assert.Single(plan.Projects);
        Assert.Equal(@"S:\Radan Projects\2025-06-01", project.Dir);   // trailing slash doesn't make a second project
        Assert.Equal("2025-06-01", project.Name);
        Assert.Equal(LegacyTransform.ToUtc(new DateTime(2025, 6, 1)), project.CreatedUtc);
        Assert.Equal(["5", "6"], plan.Nests.Select(n => n.Name));
        Assert.All(plan.Nests, n => Assert.Equal(project.Dir, n.ProjectDir));
        Assert.All(plan.Nests, n => Assert.Equal(project.CreatedUtc, n.CreatedUtc));
        Assert.Empty(plan.Errors);
    }

    [Fact]
    public void FolderNotNamedByDate_IsDatedByItsEarliestOrder()
    {
        var src = Source(
            orders: [Order(1, Jun2025), Order(2, Jan2020)],
            items:  [Item(10, 1, partId: 100), Item(20, 2, partId: 100)],
            parts:  [Part(100, "P-100")],
            nests:  [Nest(1, "5.drg", @"S:\Radan Projects\Miscellaneous")],
            nested: [new(1, 1, NestId: 1, OrderItemId: 10), new(2, 1, NestId: 1, OrderItemId: 20)]);

        Assert.Equal(LegacyTransform.ToUtc(Jan2020), Assert.Single(LegacyTransform.Build(src).Projects).CreatedUtc);
    }

    [Theory]
    [InlineData("2026-11-04(4)", "2026-11-04")]
    [InlineData("2026-11-04", "2026-11-04")]
    [InlineData("Miscellaneous", null)]
    [InlineData("2026-13-01", null)]
    public void ProjectDate_ComesFromTheFolderName(string name, string? expected)
    {
        Assert.Equal(expected == null ? null : DateTime.Parse(expected), LegacyTransform.ProjectDate(name));
    }

    [Fact]
    public void BlankOrderNumber_FallsBackToTheLegacyId()
    {
        Assert.Equal("42", LegacyTransform.OrderNumber(Order(42, Jun2025) with { OrderNumber = "  " }));
        Assert.Equal("42", LegacyTransform.OrderNumber(Order(42, Jun2025) with { OrderNumber = null }));
        Assert.Equal("A-1", LegacyTransform.OrderNumber(Order(42, Jun2025) with { OrderNumber = " A-1 " }));
    }

    [Fact]
    public void SameNestNumber_InDifferentProjects_IsFine()
    {
        var src = Source(
            orders: [Order(1, Jun2025)],
            items:  [Item(10, 1, partId: 100)],
            parts:  [Part(100, "P-100")],
            nests:  [Nest(1, "5.drg", @"M:\Radan Projects\A"), Nest(2, "5.drg", @"S:\Radan Projects\B")],
            nested: [new(1, 1, NestId: 1, OrderItemId: 10), new(2, 1, NestId: 2, OrderItemId: 10)]);

        var plan = LegacyTransform.Build(src);

        Assert.Equal(2, plan.Projects.Count);
        Assert.Empty(plan.Errors);
    }

    [Fact]
    public void SameNestTwiceInOneProject_IsAnError()
    {
        var src = Source(
            orders: [Order(1, Jun2025)],
            items:  [Item(10, 1, partId: 100)],
            parts:  [Part(100, "P-100")],
            nests:  [Nest(1, "5.drg", @"S:\P"), Nest(2, "5", @"S:\P")],
            nested: [new(1, 1, NestId: 1, OrderItemId: 10), new(2, 1, NestId: 2, OrderItemId: 10)]);

        Assert.Contains(LegacyTransform.Build(src).Errors, e => e.Contains("'5' appears 2 times"));
    }

    [Fact]
    public void ItemsStillInARadanProject_ComeAcrossOutOfRadan_WithoutTheirRadanId()
    {
        var src = Source(
            orders: [Order(1, Jun2025, complete: false)],
            items:  [Item(10, 1, partId: 100, inProject: true, radanId: 88), Item(11, 1, partId: 100, radanId: 12)],
            parts:  [Part(100, "P-100")]);

        var plan = LegacyTransform.Build(src);

        Assert.Equal(1, plan.TakenOutOfRadan);
        Assert.Null(plan.Items.Single(i => i.Source.Id == 10).RadanIdNumber);
        Assert.Equal(12, plan.Items.Single(i => i.Source.Id == 11).RadanIdNumber);   // finished work keeps its history
    }

    [Fact]
    public void PartsWithTheSameFileName_BecomeOnePart()
    {
        var src = Source(
            orders: [Order(1, Jun2025)],
            items:  [Item(10, 1, partId: 100), Item(11, 1, partId: 101)],
            parts:  [Part(100, "P-100"), Part(101, "P-100")]);

        var plan = LegacyTransform.Build(src);

        Assert.Single(plan.Parts);
        Assert.All(plan.Items, i => Assert.Equal(100, i.PartLegacyId));
    }

    [Fact]
    public void NestedRowsForItemsOutsideTheSelection_AreLeftOut()
    {
        var src = Source(
            orders: [Order(1, Jun2025)],
            items:  [Item(10, 1, partId: 100)],
            parts:  [Part(100, "P-100")],
            nests:  [Nest(1, "5.drg", @"S:\P"), Nest(2, "6.drg", @"S:\Q")],
            nested: [new(1, 1, NestId: 1, OrderItemId: 10), new(2, 1, NestId: 2, OrderItemId: 999)]);

        var plan = LegacyTransform.Build(src);

        Assert.Equal([1], plan.NestedParts.Select(np => np.Id));
        Assert.Equal([1], plan.Nests.Select(n => n.LegacyId));
        Assert.Single(plan.Projects);
    }

    [Fact]
    public void OverNestedItems_AreCounted()
    {
        var src = Source(
            orders: [Order(1, Jun2025)],
            items:  [Item(10, 1, partId: 100, required: 2, nested: 3), Item(11, 1, partId: 100)],
            parts:  [Part(100, "P-100")]);

        Assert.Equal(1, LegacyTransform.Build(src).OverNestedItems);
    }
}
