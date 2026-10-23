using HorstMFG.Core.DTOs;
using HorstMFG.Core.Entities;
using HorstMFG.Core.Enums;
using HorstMFG.Infrastructure.Data;
using HorstMFG.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace HorstMFG.Web.Tests;

/// <summary>
/// Drives VaultBomIngestService end to end (callback payload → saved PartLineItems) against
/// an EF InMemory database and an empty temp folder standing in for the PDF share.
/// </summary>
public class VaultBomIngestServiceTests : IDisposable
{
    private const string JobId = "test-job";
    private const int TrackingId = 10;
    private const string ProductName = "PROD-A";
    private const int Plant1Id = 1;
    private const int Plant2Id = 2;

    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "HorstMFG.Tests", Guid.NewGuid().ToString("N"));
    private readonly InMemoryDbContextFactory _dbFactory = new();

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, recursive: true); } catch { }
    }

    // ── Plant-specific field selection ───────────────────────────────────────

    [Theory]
    [InlineData(BomType.MakeToStock)]
    [InlineData(BomType.MakeToOrder)]
    public async Task Plant1Import_UsesPlant1Fields(BomType bomType)
    {
        var part = await IngestSinglePartAsync(bomType, Plant1Id, l =>
        {
            l.PlantId = "Plant 1";   l.IsStock = true;
            l.P2PlantId = "Plant 2"; l.P2IsStock = false;
        });

        Assert.Equal("Plant 1", part.PlantIdRaw);
        Assert.Equal(Plant1Id, part.PlantId);
        Assert.True(part.IsStock);
    }

    [Theory]
    [InlineData(BomType.MakeToStock)]
    [InlineData(BomType.MakeToOrder)]
    public async Task Plant2Import_UsesPlant2Fields(BomType bomType)
    {
        var part = await IngestSinglePartAsync(bomType, Plant2Id, l =>
        {
            l.PlantId = "Plant 1";   l.IsStock = true;
            l.P2PlantId = "Plant 2"; l.P2IsStock = false;
        });

        Assert.Equal("Plant 2", part.PlantIdRaw);
        Assert.Equal(Plant2Id, part.PlantId);
        Assert.False(part.IsStock);
    }

    [Fact]
    public async Task Plant2Import_CrossPlantP2PlantId_IsKept()
    {
        var part = await IngestSinglePartAsync(BomType.MakeToStock, Plant2Id, l => l.P2PlantId = "Plant 1");

        Assert.Equal("Plant 1", part.PlantIdRaw);
        Assert.Equal(Plant1Id, part.PlantId);
    }

    [Theory]
    [InlineData(Plant1Id, "Plant 1")]
    [InlineData(Plant2Id, "Plant 2")]
    public async Task BlankPlantId_DefaultsToImportingPlant(int importingPlantId, string expectedRaw)
    {
        // The other plant's field is set, to prove it isn't used as the fallback.
        var part = await IngestSinglePartAsync(BomType.MakeToStock, importingPlantId, l =>
        {
            l.PlantId = importingPlantId == Plant1Id ? "  " : "Plant 1";
            l.P2PlantId = importingPlantId == Plant2Id ? "" : "Plant 2";
        });

        Assert.Equal(expectedRaw, part.PlantIdRaw);
        Assert.Equal(importingPlantId, part.PlantId);
    }

    // ── Stock exceptions ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(BomType.MakeToStock)]
    [InlineData(BomType.MakeToOrder)]
    public async Task Plant1Import_ProductInP1Exceptions_FlipsStockToMakeToOrder(BomType bomType)
    {
        var part = await IngestSinglePartAsync(bomType, Plant1Id, l =>
        {
            l.IsStock = true;
            l.P1StockException = $"OTHER|{ProductName}";
        });

        Assert.False(part.IsStock);
    }

    [Theory]
    [InlineData(BomType.MakeToStock)]
    [InlineData(BomType.MakeToOrder)]
    public async Task Plant2Import_ProductInP2Exceptions_FlipsMakeToOrderToStock(BomType bomType)
    {
        var part = await IngestSinglePartAsync(bomType, Plant2Id, l =>
        {
            l.P2IsStock = false;
            l.P2StockException = ProductName;
        });

        Assert.True(part.IsStock);
    }

    [Fact]
    public async Task Plant1Import_IgnoresP2Exceptions()
    {
        var part = await IngestSinglePartAsync(BomType.MakeToStock, Plant1Id, l =>
        {
            l.IsStock = true;
            l.P2StockException = ProductName;
        });

        Assert.True(part.IsStock);
    }

    [Fact]
    public async Task Plant2Import_IgnoresP1Exceptions()
    {
        var part = await IngestSinglePartAsync(BomType.MakeToStock, Plant2Id, l =>
        {
            l.P2IsStock = true;
            l.P1StockException = ProductName;
        });

        Assert.True(part.IsStock);
    }

    [Fact]
    public async Task ProductNotInExceptions_KeepsBaseStockFlag()
    {
        var part = await IngestSinglePartAsync(BomType.MakeToOrder, Plant1Id, l =>
        {
            l.IsStock = true;
            l.P1StockException = "PROD-B|PROD-C";
        });

        Assert.True(part.IsStock);
    }

    // ── New columns ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(BomType.MakeToStock)]
    [InlineData(BomType.MakeToOrder)]
    public async Task SecondOperationAndFinishOps_ArePersisted(BomType bomType)
    {
        var part = await IngestSinglePartAsync(bomType, Plant1Id, l =>
        {
            l.SecondOperation = "Brake";
            l.FinishOps = "zinc";
        });

        Assert.Equal("Brake", part.SecondOperation);
        Assert.Equal("zinc", part.FinishOps);
    }

    // ── Harness ──────────────────────────────────────────────────────────────

    private async Task<PartLineItem> IngestSinglePartAsync(BomType bomType, int importingPlantId, Action<VaultBomLine> configurePart)
    {
        var line = new VaultBomLine { Level = "1.1", Number = "PART-1.ipt", Category = "Part", Qty = 2 };
        configurePart(line);

        var parts = await IngestAsync(bomType, importingPlantId, line);
        return Assert.Single(parts);
    }

    /// <summary>Seeds both plants plus one Schedule/ScheduleOrder (MTS) or Batch/BatchProduct (MTO)
    /// for <paramref name="importingPlantId"/>, ingests a payload of the product row plus
    /// <paramref name="partLines"/>, and returns the saved non-product PartLineItems.</summary>
    private async Task<List<PartLineItem>> IngestAsync(BomType bomType, int importingPlantId, params VaultBomLine[] partLines)
    {
        await using (var db = _dbFactory.CreateDbContext())
        {
            db.Plants.AddRange(
                new Plant { Id = Plant1Id, Name = "Plant 1 - Main", Code = "P1" },
                new Plant { Id = Plant2Id, Name = "Plant 2", Code = "P2" });

            if (bomType == BomType.MakeToStock)
            {
                db.Schedules.Add(new Schedule { Id = 1, Name = "Schedule 1", PlantId = importingPlantId, ImportedByUserId = 1 });
                db.ScheduleOrders.Add(new ScheduleOrder
                {
                    Id = TrackingId, ScheduleId = 1, PlantId = importingPlantId,
                    OrderNumber = "ORDER-1", ProductNumber = ProductName,
                });
            }
            else
            {
                db.Batches.Add(new Batch { Id = 1, Name = "Batch 1", PlantId = importingPlantId, ImportedByUserId = 1 });
                db.BatchProducts.Add(new BatchProduct
                {
                    Id = TrackingId, BatchId = 1, PlantId = importingPlantId, ProductName = ProductName,
                });
            }
            await db.SaveChangesAsync();
        }

        var jobs = new BomImportJobTracker();
        jobs.Register(JobId, bomType);

        var shareDir = Directory.CreateDirectory(Path.Combine(_tempRoot, "share")).FullName;
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FileSystemPaths:PdfSharePath"] = shareDir,
                ["FileSystemPaths:LocalPdfPath"] = Path.Combine(_tempRoot, "local"),
            })
            .Build();

        var service = new VaultBomIngestService(
            _dbFactory,
            jobs,
            new BomPdfCopyService(config, NullLogger<BomPdfCopyService>.Instance),
            NullLogger<VaultBomIngestService>.Instance);

        var lines = new List<VaultBomLine> { new() { Level = "1", Number = ProductName, Category = "Product", Qty = 1 } };
        lines.AddRange(partLines);

        await service.IngestSingleAsync(new VaultBomCallbackPayload
        {
            JobId = JobId, TrackingId = TrackingId, Found = true, Lines = lines,
        });

        await using var verify = _dbFactory.CreateDbContext();
        return await verify.PartLineItems.AsNoTracking()
            .Where(p => p.PartNumber != ProductName)
            .ToListAsync();
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
