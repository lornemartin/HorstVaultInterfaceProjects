/*
 * HorstMFG Data Transfer
 *
 * Brings the production history in a legacy RadanMaster SQL Server database into a HorstMFG
 * PostgreSQL database, for one plant. Meant for an environment made with HorstMFG.EnvTool, e.g.
 *
 *   EnvTool:  clone horstmfg horstmfg_legacy --skip-pdfs
 *             clear horstmfg_legacy --no-backup
 *             set-radan-root horstmfg_legacy all "C:\Radan Projects Legacy"
 *   then:     dotnet run -- preview --database horstmfg_legacy --plant "Plant 1"
 *             dotnet run -- run     --database horstmfg_legacy --plant "Plant 1"
 *
 * Usage (run from tools/HorstMFG.DataTransfer/):
 *
 *   dotnet run -- status  [--database <name>]
 *   dotnet run -- preview --plant <name> [--since yyyy-MM-dd] [--database <name>]
 *   dotnet run -- run     --plant <name> [--since yyyy-MM-dd] [--database <name>] [--confirm <database>]
 *
 * Options:
 *   --plant <name>       Plant to load into (must exist). Everything transferred belongs to it.
 *   --since yyyy-MM-dd   Only orders entered on or after this date — plus every open order,
 *                        whenever it was entered.
 *   --database <name>    PostgreSQL database to load into. Default: the one in the connection string.
 *   --confirm <database> Answers run's "type the database name" prompt up front.
 *   --sql-conn "<str>"   Override the SQL Server connection string.
 *
 * Configuration:
 *   PostgreSQL → the web project's appsettings.json (../../src/HorstMFG.Web/appsettings.json),
 *                ConnectionStrings:DefaultConnection
 *   SQL Server → appsettings.json here, ConnectionStrings:SourceDatabase
 *
 * What comes across:
 *   Parts                       → parts            (one per file name; existing ones are reused)
 *   Nests' project folders      → radan_projects   (status Legacy; RadanMaster only kept the folder)
 *   Nests                       → nests            ("2234.drg" → "2234", in its project)
 *   Orders (IsBatch=1)          → nest_batches     (+ a bare batch per name, as a manual batch gets)
 *   Orders (IsBatch=0)          → nest_orders      (+ bare schedules / schedule orders)
 *   OrderItems                  → batch_items / order_items
 *   NestedParts                 → nested_parts
 *
 * Items RadanMaster still had in a Radan project come across as not in Radan, without their
 * Radan ID: those projects belong to the legacy system. Their nested quantities stay.
 *
 * Nothing is written if a check fails, and the whole transfer is one transaction, so a failed
 * run leaves nothing behind. A plant that already has nesting data is refused; clear the
 * environment (EnvTool clear) to run again.
 */

using HorstMFG.Core.Entities;
using HorstMFG.Core.Enums;
using HorstMFG.DataTransfer;
using HorstMFG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

// ── Configuration ─────────────────────────────────────────────────────────────

var webSettings = Path.GetFullPath(Path.Combine(
    Directory.GetCurrentDirectory(), "..", "..", "src", "HorstMFG.Web", "appsettings.json"));

var configBuilder = new ConfigurationBuilder();
if (File.Exists(webSettings))
    configBuilder.AddJsonFile(webSettings, optional: false);
if (File.Exists("appsettings.json"))
    configBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
var config = configBuilder.Build();

if (args.Length == 0) { PrintUsage(); return 1; }

var command   = args[0].ToLowerInvariant();
var plantName = GetArg(args, "--plant");
var sinceStr  = GetArg(args, "--since");

var pg = new NpgsqlConnectionStringBuilder(config.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection (PostgreSQL) missing from appsettings.json."));
if (GetArg(args, "--database") is { } database) pg.Database = database;
var dbOpts = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(pg.ConnectionString).Options;

var reader = new LegacyReader(GetArg(args, "--sql-conn")
    ?? config.GetConnectionString("SourceDatabase")
    ?? "Server=.\\SQLEXPRESS;Database=RadanMaster;Integrated Security=true;TrustServerCertificate=true");

DateTime? since = null;
if (sinceStr is not null)
{
    if (!DateTime.TryParse(sinceStr, out var d))
    {
        Console.Error.WriteLine($"Invalid --since date: {sinceStr}. Expected format: yyyy-MM-dd");
        return 1;
    }
    since = d.Date;
}

// ── Dispatch ──────────────────────────────────────────────────────────────────

return command switch
{
    "status"  => await StatusAsync(),
    "clear"   => Fail("clear has moved to HorstMFG.EnvTool: `clear <database>` empties a whole environment."),
    "preview" => plantName is null ? RequirePlant() : await PreviewAsync(plantName),
    "run"     => plantName is null ? RequirePlant() : await RunAsync(plantName),
    _         => PrintUsage(),
};

// ─────────────────────────────────────────────────────────────────────────────
// STATUS
// ─────────────────────────────────────────────────────────────────────────────

async Task<int> StatusAsync()
{
    Console.WriteLine("Source — RadanMaster (SQL Server):");
    await using (var sql = reader.Open())
    {
        await sql.OpenAsync();
        PrintRow("Parts",           await LegacyReader.ScalarAsync(sql, "SELECT COUNT(*) FROM Parts"));
        PrintRow("Nests",           await LegacyReader.ScalarAsync(sql, "SELECT COUNT(*) FROM Nests"));
        PrintRow("Orders (batch)",  await LegacyReader.ScalarAsync(sql, "SELECT COUNT(*) FROM Orders WHERE IsBatch=1"));
        PrintRow("Orders (sched.)", await LegacyReader.ScalarAsync(sql, "SELECT COUNT(*) FROM Orders WHERE IsBatch=0"));
        PrintRow("  open",          await LegacyReader.ScalarAsync(sql, "SELECT COUNT(*) FROM Orders WHERE IsComplete=0"));
        PrintRow("OrderItems",      await LegacyReader.ScalarAsync(sql, "SELECT COUNT(*) FROM OrderItems"));
        PrintRow("NestedParts",     await LegacyReader.ScalarAsync(sql, "SELECT COUNT(*) FROM NestedParts"));
    }

    Console.WriteLine();
    Console.WriteLine($"Destination — {pg.Database} on {pg.Host} (PostgreSQL):");
    await using var db = new ApplicationDbContext(dbOpts);
    foreach (var plant in await db.Plants.OrderBy(p => p.Name).ToListAsync())
    {
        Console.WriteLine($"  {plant.Name}:");
        PrintRow("  radan_projects", await db.RadanProjects.CountAsync(p => p.PlantId == plant.Id));
        PrintRow("  nests",          await db.Nests.CountAsync(n => n.PlantId == plant.Id));
        PrintRow("  nest_batches",   await db.NestBatches.CountAsync(b => b.PlantId == plant.Id));
        PrintRow("  nest_orders",    await db.NestOrders.CountAsync(o => o.PlantId == plant.Id));
    }
    PrintRow("parts (all plants)",        await db.Parts.CountAsync());
    PrintRow("batch_items (all plants)",  await db.BatchItems.CountAsync());
    PrintRow("order_items (all plants)",  await db.OrderItems.CountAsync());
    PrintRow("nested_parts (all plants)", await db.NestedParts.CountAsync());
    return 0;

    static void PrintRow(string label, int count) => Console.WriteLine($"  {label,-26} {count,8:N0}");
}

// ─────────────────────────────────────────────────────────────────────────────
// PREVIEW / RUN
// ─────────────────────────────────────────────────────────────────────────────

async Task<int> PreviewAsync(string plant)
{
    var check = await CheckAsync(plant);
    if (check == null) return 1;
    Console.WriteLine(check.Problems.Count == 0 ? "\nPreview only — nothing written. `run` would do the above." : "\n`run` would refuse until the errors above are fixed.");
    return check.Problems.Count == 0 ? 0 : 1;
}

async Task<int> RunAsync(string plant)
{
    var check = await CheckAsync(plant);
    if (check == null) return 1;
    if (check.Problems.Count > 0) { Console.Error.WriteLine("\nNothing written: fix the errors above first."); return 1; }

    if (GetArg(args, "--confirm") != pg.Database)
    {
        Console.Write($"\nThis writes into {pg.Database} on {pg.Host}. Type {pg.Database} to continue: ");
        if (Console.ReadLine()?.Trim() != pg.Database) { Console.WriteLine("Cancelled."); return 1; }
    }

    await using var db = new ApplicationDbContext(dbOpts);
    await WriteAsync(db, check.Plan, check.Plant.Id, check.UserId);
    Console.WriteLine($"\nTransfer complete. `dotnet run -- status --database {pg.Database}` shows the totals.");
    return 0;
}

/// <summary>Reads the source and checks the destination; prints what a run would do. Null if it
/// couldn't get that far (no such plant, no users).</summary>
async Task<Check?> CheckAsync(string plantArg)
{
    Console.WriteLine($"Destination: {pg.Database} on {pg.Host}, plant {plantArg}" +
                      (since.HasValue ? $", orders since {since:yyyy-MM-dd} plus all open orders" : ", all orders"));

    await using var db = new ApplicationDbContext(dbOpts);
    var plant = await db.Plants.FirstOrDefaultAsync(x => x.Name == plantArg);
    if (plant is null)
    {
        Console.Error.WriteLine($"Error: plant '{plantArg}' not found. Available: " +
            string.Join(", ", await db.Plants.Select(x => x.Name).ToListAsync()));
        return null;
    }
    var userId = await db.Users.OrderBy(u => u.Id).Select(u => (int?)u.Id).FirstOrDefaultAsync();
    if (userId is null) { Console.Error.WriteLine("Error: no users in the destination; bare batches need one as their importer."); return null; }

    Console.WriteLine("\nReading RadanMaster...");
    var src = await reader.ReadAsync(since);
    var plan = LegacyTransform.Build(src);
    var orphans = await reader.CountOrphanNestedPartsAsync();

    var problems = new List<string>(plan.Errors);
    var warnings = new List<string>();

    // A plant is loaded once. (Parts are shared by all plants, so they don't count.)
    var existing = new (string Table, int Count)[]
    {
        ("radan_projects", await db.RadanProjects.CountAsync(p => p.PlantId == plant.Id)),
        ("nests",          await db.Nests.CountAsync(n => n.PlantId == plant.Id)),
        ("nest_batches",   await db.NestBatches.CountAsync(b => b.PlantId == plant.Id)),
        ("nest_orders",    await db.NestOrders.CountAsync(o => o.PlantId == plant.Id)),
    }.Where(e => e.Count > 0).ToList();
    if (existing.Count > 0)
        problems.Add($"{plant.Name} already has nesting data in {pg.Database} (" +
                     string.Join(", ", existing.Select(e => $"{e.Count:N0} {e.Table}")) +
                     "). Clear the environment with EnvTool first, or transfer into a fresh one.");

    // A station with no folder of its own here would work in its bridge's usual (real) folder.
    foreach (var s in await db.NestingStations.Where(s => s.PlantId == plant.Id && (s.RadanProjectsRootPath == null || s.RadanProjectsRootPath == "")).ToListAsync())
        warnings.Add($"Station '{s.Name}' has no Radan projects folder in {pg.Database}, so its bridge will use its own " +
                     $"setting — probably the real projects folder. EnvTool: set-radan-root {pg.Database} {s.Id} <folder>");

    var fileNames = plan.Parts.Where(p => p.FileName != null).Select(p => p.FileName!).ToList();
    var reusedParts = await db.Parts.CountAsync(p => p.FileName != null && fileNames.Contains(p.FileName));

    var batchOrders = plan.Orders.Count(o => o.IsBatch);
    Console.WriteLine($"""

        Would transfer:
          Orders         {plan.Orders.Count,8:N0}  ({batchOrders:N0} batch, {plan.Orders.Count - batchOrders:N0} schedule; {plan.Orders.Count(o => !o.IsComplete):N0} open)
          Order items    {plan.Items.Count,8:N0}
          Parts          {plan.Parts.Count,8:N0}  ({reusedParts:N0} already here, reused)
          Projects       {plan.Projects.Count,8:N0}  (status Legacy)
          Nests          {plan.Nests.Count,8:N0}
          Nested parts   {plan.NestedParts.Count,8:N0}

        Notes:
          {plan.TakenOutOfRadan:N0} item(s) were in a Radan project in RadanMaster; they come across as not in Radan.
          {plan.OverNestedItems:N0} item(s) have more nested than required; copied as they are.
          {orphans:N0} NestedParts row(s) in RadanMaster belong to deleted order items; not transferred.
        """);
    foreach (var w in warnings) Console.WriteLine($"Warning: {w}");
    foreach (var p in problems) Console.Error.WriteLine($"Error: {p}");

    return new Check(plant, userId.Value, plan, problems);
}

// ─────────────────────────────────────────────────────────────────────────────
// WRITE
// ─────────────────────────────────────────────────────────────────────────────

async Task WriteAsync(ApplicationDbContext db, TransferPlan plan, int plantId, int userId)
{
    // Rows are added explicitly and the tracker is cleared after each chunk, so change detection
    // would only slow tens of thousands of inserts down.
    db.ChangeTracker.AutoDetectChangesEnabled = false;
    await using var tx = await db.Database.BeginTransactionAsync();
    var orders = plan.Orders.ToDictionary(o => o.Id);

    // ── Parts ──
    var existingParts = (await db.Parts.Where(p => p.FileName != null).Select(p => new { p.Id, p.FileName }).ToListAsync())
        .GroupBy(p => p.FileName!).ToDictionary(g => g.Key, g => g.Min(p => p.Id));
    var partIds = new Dictionary<int, int>();
    var newParts = new List<(int, Part)>();
    foreach (var p in plan.Parts)
    {
        if (p.FileName != null && existingParts.TryGetValue(p.FileName, out var id)) { partIds[p.Id] = id; continue; }
        newParts.Add((p.Id, new Part
        {
            FileName    = p.FileName,
            Description = p.Description,
            Material    = p.Material,
            Thickness   = p.Thickness.HasValue ? (decimal?)p.Thickness.Value : null,
            Thumbnail   = p.Thumbnail,
            HasBends    = p.HasBends,
        }));
    }
    await SaveAsync(db, "Parts", newParts, e => e.Id, partIds);

    // ── Projects and nests ──
    var projectIds = new Dictionary<string, int>(StringComparer.Ordinal);
    await SaveAsync(db, "Projects", plan.Projects.Select(p => (p.Dir, new RadanProject
    {
        PlantId     = plantId,
        ProjectDir  = p.Dir,
        RpdPath     = null,
        ProjectName = p.Name,
        Status      = RadanProjectStatus.Legacy,
        CreatedDate = p.CreatedUtc,
    })).ToList(), e => e.Id, projectIds);

    var nestIds = new Dictionary<int, int>();
    await SaveAsync(db, "Nests", plan.Nests.Select(n => (n.LegacyId, new Nest
    {
        NestName       = n.Name,
        NestPath       = null,   // RadanMaster only kept the project folder, not the nest's .drg
        Thumbnail      = n.Thumbnail,
        PlantId        = plantId,
        RadanProjectId = n.ProjectDir == null ? null : projectIds[n.ProjectDir],
        CreatedDate    = n.CreatedUtc,
    })).ToList(), e => e.Id, nestIds);

    // ── Batch orders → bare batches + nest_batches ──
    static string NameOf(string? name) => string.IsNullOrWhiteSpace(name) ? "(unnamed)" : name.Trim();

    var batchOrders = plan.Orders.Where(o => o.IsBatch).ToList();
    var batchIds = (await db.Batches.Where(b => b.PlantId == plantId).Select(b => new { b.Id, b.Name }).ToListAsync())
        .GroupBy(b => b.Name, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.Min(b => b.Id), StringComparer.OrdinalIgnoreCase);
    await SaveAsync(db, "Batches", batchOrders
        .GroupBy(o => NameOf(o.BatchName), StringComparer.OrdinalIgnoreCase)
        .Where(g => !batchIds.ContainsKey(g.Key))
        .Select(g => (g.Key, new Batch
        {
            Name               = g.Key,
            PlantId            = plantId,
            ImportedByUserId   = userId,
            ImportDate         = LegacyTransform.ToUtc(g.Min(o => o.EntryDate)),
            ReadyForProduction = true,   // the shape a release (or a manual batch) gives
        })).ToList(), e => e.Id, batchIds);

    var nestBatchIds = new Dictionary<int, int>();
    await SaveAsync(db, "Nest batches", batchOrders.Select(o => (o.Id, new NestBatch
    {
        BatchId       = batchIds[NameOf(o.BatchName)],
        PlantId       = plantId,
        EntryDate     = LegacyTransform.ToUtc(o.EntryDate),
        DueDate       = LegacyTransform.ToUtc(o.DueDate),
        IsComplete    = o.IsComplete,
        CompletedDate = LegacyTransform.ToUtc(o.DateCompleted),
    })).ToList(), e => e.Id, nestBatchIds);

    // ── Schedule orders → bare schedules + schedule orders + nest_orders ──
    var schedOrders = plan.Orders.Where(o => !o.IsBatch).ToList();
    var scheduleIds = (await db.Schedules.Where(s => s.PlantId == plantId).Select(s => new { s.Id, s.Name }).ToListAsync())
        .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.Min(s => s.Id), StringComparer.OrdinalIgnoreCase);
    await SaveAsync(db, "Schedules", schedOrders
        .GroupBy(o => NameOf(o.ScheduleName), StringComparer.OrdinalIgnoreCase)
        .Where(g => !scheduleIds.ContainsKey(g.Key))
        .Select(g => (g.Key, new Schedule
        {
            Name               = g.Key,
            PlantId            = plantId,
            ImportedByUserId   = userId,
            ImportDate         = LegacyTransform.ToUtc(g.Min(o => o.EntryDate)),
            ReadyForProduction = true,
        })).ToList(), e => e.Id, scheduleIds);

    (int, string) OrderKey(SrcOrder o) => (scheduleIds[NameOf(o.ScheduleName)], LegacyTransform.OrderNumber(o));
    var scheduleOrderIds = (await db.ScheduleOrders.Where(so => so.PlantId == plantId).Select(so => new { so.Id, so.ScheduleId, so.OrderNumber }).ToListAsync())
        .GroupBy(so => (so.ScheduleId, so.OrderNumber)).ToDictionary(g => g.Key, g => g.Min(so => so.Id));
    await SaveAsync(db, "Schedule orders", schedOrders
        .Select(OrderKey).Distinct()
        .Where(k => !scheduleOrderIds.ContainsKey(k))
        .Select(k => (k, new ScheduleOrder { ScheduleId = k.Item1, PlantId = plantId, OrderNumber = k.Item2, Qty = 1 }))
        .ToList(), e => e.Id, scheduleOrderIds);

    var nestOrderIds = new Dictionary<int, int>();
    await SaveAsync(db, "Nest orders", schedOrders.Select(o => (o.Id, new NestOrder
    {
        ScheduleOrderId = scheduleOrderIds[OrderKey(o)],
        PlantId         = plantId,
        EntryDate       = LegacyTransform.ToUtc(o.EntryDate),
        DueDate         = LegacyTransform.ToUtc(o.DueDate),
        IsComplete      = o.IsComplete,
        CompletedDate   = LegacyTransform.ToUtc(o.DateCompleted),
    })).ToList(), e => e.Id, nestOrderIds);

    // ── Items ──
    var batchItemIds = new Dictionary<int, int>();
    await SaveAsync(db, "Batch items", plan.Items.Where(i => orders[i.Source.OrderId].IsBatch).Select(i => (i.Source.Id, new BatchItem
    {
        NestBatchId      = nestBatchIds[i.Source.OrderId],
        PartId           = partIds[i.PartLegacyId],
        QtyRequired      = i.Source.QtyRequired,
        QtyNested        = i.Source.QtyNested,
        IsComplete       = i.Source.IsComplete,
        IsInRadanProject = false,
        RadanIdNumber    = i.RadanIdNumber,
        Notes            = i.Source.Notes,
    })).ToList(), e => e.Id, batchItemIds);

    var orderItemIds = new Dictionary<int, int>();
    await SaveAsync(db, "Order items", plan.Items.Where(i => !orders[i.Source.OrderId].IsBatch).Select(i => (i.Source.Id, new OrderItem
    {
        NestOrderId      = nestOrderIds[i.Source.OrderId],
        PartId           = partIds[i.PartLegacyId],
        QtyRequired      = i.Source.QtyRequired,
        QtyNested        = i.Source.QtyNested,
        IsComplete       = i.Source.IsComplete,
        IsInRadanProject = false,
        RadanIdNumber    = i.RadanIdNumber,
        Notes            = i.Source.Notes,
    })).ToList(), e => e.Id, orderItemIds);

    // ── Nested parts ──
    await SaveAsync(db, "Nested parts", plan.NestedParts.Select(np => (np.Id, new NestedPart
    {
        NestId      = nestIds[np.NestId],
        BatchItemId = batchItemIds.TryGetValue(np.OrderItemId, out var b) ? b : null,
        OrderItemId = orderItemIds.TryGetValue(np.OrderItemId, out var o) ? o : null,
        Qty         = np.Qty,
    })).ToList(), e => e.Id, new Dictionary<int, int>());

    Console.WriteLine("  Committing...");
    await tx.CommitAsync();
}

/// <summary>Inserts rows in chunks, recording each new ID under its legacy key.</summary>
static async Task SaveAsync<TKey, TEntity>(ApplicationDbContext db, string label, List<(TKey Key, TEntity Entity)> rows,
                                           Func<TEntity, int> id, Dictionary<TKey, int> ids)
    where TKey : notnull where TEntity : class
{
    const int Chunk = 1000;
    for (var start = 0; start < rows.Count; start += Chunk)
    {
        var slice = rows.GetRange(start, Math.Min(Chunk, rows.Count - start));
        db.Set<TEntity>().AddRange(slice.Select(r => r.Entity));
        await db.SaveChangesAsync();
        foreach (var (key, entity) in slice) ids[key] = id(entity);
        db.ChangeTracker.Clear();
        if (rows.Count > Chunk) Console.Write($"\r  {label}: {start + slice.Count:N0} / {rows.Count:N0}");
    }
    Console.WriteLine($"\r  {label}: {rows.Count:N0} inserted.          ");
}

// ─────────────────────────────────────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────────────────────────────────────

static string? GetArg(string[] args, string flag)
{
    var idx = Array.IndexOf(args, flag);
    return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
}

static int RequirePlant() => Fail("Error: --plant <name> is required.");

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    return 1;
}

static int PrintUsage()
{
    Console.WriteLine("""
        Usage:
          dotnet run -- status  [--database <name>]
          dotnet run -- preview --plant <name> [--since yyyy-MM-dd] [--database <name>]
          dotnet run -- run     --plant <name> [--since yyyy-MM-dd] [--database <name>] [--confirm <database>]

        Options:
          --plant <name>        Plant to load into (must exist)
          --since yyyy-MM-dd    Only orders on/after this date, plus all open orders
          --database <name>     PostgreSQL database to load into (default: the connection string's)
          --confirm <database>  Answer run's confirmation prompt up front
          --sql-conn "<str>"    Override the SQL Server connection string

        Examples:
          dotnet run -- preview --database horstmfg_legacy --plant "Plant 1"
          dotnet run -- run     --database horstmfg_legacy --plant "Plant 1" --since 2024-01-01
        """);
    return 1;
}

record Check(Plant Plant, int UserId, TransferPlan Plan, List<string> Problems);
