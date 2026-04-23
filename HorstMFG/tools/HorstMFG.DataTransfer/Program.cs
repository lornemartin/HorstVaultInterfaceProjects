/*
 * HorstMFG Data Transfer
 *
 * Transfers nesting (production) data from the old RadanMaster SQL Server
 * database into the new HorstMFG PostgreSQL database.
 *
 * Usage (run from tools/HorstMFG.DataTransfer/):
 *
 *   dotnet run -- status
 *       Shows record counts in both source (SQL Server) and destination (PostgreSQL).
 *
 *   dotnet run -- preview --plant <name> [--since yyyy-MM-dd]
 *       Shows what would be transferred without writing anything.
 *
 *   dotnet run -- run --plant <name> [--since yyyy-MM-dd]
 *       Performs the data transfer. NOT idempotent — run `status` first to
 *       confirm the destination is empty before proceeding.
 *
 * Options:
 *   --plant <name>      Target plant name (must exist in PostgreSQL). Required
 *                       for preview and run. All nests, nest-batches, and
 *                       nest-orders will be assigned to this plant.
 *   --since yyyy-MM-dd  Only transfer orders entered on or after this date.
 *                       Parts and nests are still filtered to only those
 *                       referenced by the qualifying orders.
 *   --sql-conn "<str>"  Override the SQL Server connection string from
 *                       appsettings.json.
 *
 * Configuration:
 *   PostgreSQL connection string → read from the web project's appsettings.json
 *       (../../src/HorstMFG.Web/appsettings.json), key: ConnectionStrings.DefaultConnection
 *   SQL Server connection string → read from local appsettings.json,
 *       key: ConnectionStrings.SourceDatabase
 *
 * Schema mapping:
 *   Parts                        → parts
 *   Nests                        → nests          (PlantId = --plant)
 *   Orders (IsBatch=1)           → nest_batches   (creates stub batches if no name match)
 *   Orders (IsBatch=0)           → nest_orders    (creates stub schedules/schedule_orders)
 *   OrderItems (under batch)     → batch_items
 *   OrderItems (under schedule)  → order_items
 *   NestedParts                  → nested_parts
 *
 * The NestOrderItems junction table in the old database is not transferred;
 * NestedParts (which carries the Qty) supersedes it.
 */

using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using HorstMFG.Core.Entities;
using HorstMFG.Infrastructure.Data;

// ── Configuration ─────────────────────────────────────────────────────────────

var webSettings = Path.GetFullPath(Path.Combine(
    Directory.GetCurrentDirectory(), "..", "..", "src", "HorstMFG.Web", "appsettings.json"));

var configBuilder = new ConfigurationBuilder();
if (File.Exists(webSettings))
    configBuilder.AddJsonFile(webSettings, optional: false);
else if (File.Exists("appsettings.json"))
    configBuilder.AddJsonFile("appsettings.json", optional: false);

if (File.Exists("appsettings.json"))
    configBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

var config = configBuilder.Build();

var pgConnStr = config.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection (PostgreSQL) missing from appsettings.json.");
var sqlConnStr = config.GetConnectionString("SourceDatabase")
    ?? "Server=.\\SQLEXPRESS;Database=RadanMaster;Integrated Security=true;TrustServerCertificate=true";

// ── PostgreSQL context ────────────────────────────────────────────────────────

var dbOpts = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseNpgsql(pgConnStr)
    .Options;

// ── Argument parsing ──────────────────────────────────────────────────────────

if (args.Length == 0) { PrintUsage(); return 1; }

var command    = args[0].ToLowerInvariant();
var plantName  = GetArg(args, "--plant");
var sinceStr   = GetArg(args, "--since");
var sqlConnArg = GetArg(args, "--sql-conn");

if (sqlConnArg is not null)
    sqlConnStr = sqlConnArg;

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
    "clear"   => await ClearAsync(),
    "preview" => plantName is null ? RequirePlant() : await PreviewAsync(plantName, since),
    "run"     => plantName is null ? RequirePlant() : await RunAsync(plantName, since),
    _         => (PrintUsage(), 1).Item2,
};

// ─────────────────────────────────────────────────────────────────────────────
// STATUS
// ─────────────────────────────────────────────────────────────────────────────

async Task<int> StatusAsync()
{
    Console.WriteLine("Source — RadanMaster (SQL Server):");
    await using var sql = OpenSqlConnection();
    await sql.OpenAsync();
    PrintRow("Parts",        await SqlCountAsync(sql, "Parts"));
    PrintRow("Nests",        await SqlCountAsync(sql, "Nests"));
    PrintRow("Orders (batch)",   await SqlCountAsync(sql, "Orders WHERE IsBatch=1"));
    PrintRow("Orders (sched.)",  await SqlCountAsync(sql, "Orders WHERE IsBatch=0"));
    PrintRow("OrderItems",   await SqlCountAsync(sql, "OrderItems"));
    PrintRow("NestedParts",  await SqlCountAsync(sql, "NestedParts"));

    Console.WriteLine();
    Console.WriteLine("Destination — HorstMFG (PostgreSQL):");
    await using var db = new ApplicationDbContext(dbOpts);
    PrintRow("parts",        await db.Parts.CountAsync());
    PrintRow("nests",        await db.Nests.CountAsync());
    PrintRow("nest_batches", await db.NestBatches.CountAsync());
    PrintRow("nest_orders",  await db.NestOrders.CountAsync());
    PrintRow("batch_items",  await db.BatchItems.CountAsync());
    PrintRow("order_items",  await db.OrderItems.CountAsync());
    PrintRow("nested_parts", await db.NestedParts.CountAsync());

    return 0;

    static void PrintRow(string label, int count) =>
        Console.WriteLine($"  {label,-22} {count,8:N0}");
}

// ─────────────────────────────────────────────────────────────────────────────
// CLEAR
// ─────────────────────────────────────────────────────────────────────────────

async Task<int> ClearAsync()
{
    await using var db = new ApplicationDbContext(dbOpts);

    // Confirm before wiping
    var counts = new (string Table, int Count)[]
    {
        ("nested_parts", await db.NestedParts.CountAsync()),
        ("batch_items",  await db.BatchItems.CountAsync()),
        ("order_items",  await db.OrderItems.CountAsync()),
        ("nest_batches", await db.NestBatches.CountAsync()),
        ("nest_orders",  await db.NestOrders.CountAsync()),
        ("nests",        await db.Nests.CountAsync()),
        ("parts",        await db.Parts.CountAsync()),
    };

    var total = counts.Sum(c => c.Count);
    if (total == 0)
    {
        Console.WriteLine("Nesting tables are already empty.");
        return 0;
    }

    Console.WriteLine("The following nesting data will be permanently deleted:");
    foreach (var (t, c) in counts)
        Console.WriteLine($"  {t,-22} {c,8:N0}");
    Console.Write("\nType YES to confirm: ");
    var answer = Console.ReadLine();
    if (answer?.Trim() != "YES")
    {
        Console.WriteLine("Aborted.");
        return 1;
    }

    // Delete in reverse FK order using raw SQL for speed
    await db.Database.ExecuteSqlRawAsync("DELETE FROM nested_parts");
    Console.WriteLine("  nested_parts cleared.");
    await db.Database.ExecuteSqlRawAsync("DELETE FROM batch_items");
    Console.WriteLine("  batch_items cleared.");
    await db.Database.ExecuteSqlRawAsync("DELETE FROM order_items");
    Console.WriteLine("  order_items cleared.");
    await db.Database.ExecuteSqlRawAsync("DELETE FROM nest_batches");
    Console.WriteLine("  nest_batches cleared.");
    await db.Database.ExecuteSqlRawAsync("DELETE FROM nest_orders");
    Console.WriteLine("  nest_orders cleared.");
    await db.Database.ExecuteSqlRawAsync("DELETE FROM nests");
    Console.WriteLine("  nests cleared.");
    await db.Database.ExecuteSqlRawAsync("DELETE FROM parts");
    Console.WriteLine("  parts cleared.");

    Console.WriteLine("Done. You can now re-run the transfer.");
    return 0;
}

// ─────────────────────────────────────────────────────────────────────────────
// PREVIEW
// ─────────────────────────────────────────────────────────────────────────────

async Task<int> PreviewAsync(string plant, DateTime? sinceDate)
{
    Console.WriteLine($"Preview — plant: {plant}" + (sinceDate.HasValue ? $", since: {sinceDate:yyyy-MM-dd}" : " (all records)"));

    var src = await ReadSourceAsync(sinceDate);
    PrintSourceSummary(src);

    await using var db = new ApplicationDbContext(dbOpts);
    var p = await db.Plants.FirstOrDefaultAsync(x => x.Name == plant);
    if (p is null)
    {
        Console.Error.WriteLine($"\nError: plant '{plant}' not found. Available: " +
            string.Join(", ", await db.Plants.Select(x => x.Name).ToListAsync()));
        return 1;
    }
    Console.WriteLine($"\nTarget plant: {p.Name} (Id={p.Id}, Code={p.Code})");

    var existParts = await db.Parts.CountAsync();
    var existNests = await db.Nests.CountAsync();
    if (existParts > 0 || existNests > 0)
        Console.WriteLine($"\nWARNING: destination already has {existParts:N0} parts and {existNests:N0} nests.");

    // Stub analysis
    var batchNames = src.Orders.Where(o => o.IsBatch)
                               .Select(o => o.BatchName ?? "(unnamed)")
                               .Distinct().ToList();
    var existBatchNames = await db.Set<Batch>().Select(b => b.Name).ToListAsync();
    var stubBatches = batchNames.Except(existBatchNames, StringComparer.OrdinalIgnoreCase).Count();

    var schedNames = src.Orders.Where(o => !o.IsBatch)
                               .Select(o => o.ScheduleName ?? "(unnamed)")
                               .Distinct().ToList();
    var existSchedNames = await db.Set<Schedule>().Select(s => s.Name).ToListAsync();
    var stubSchedules = schedNames.Except(existSchedNames, StringComparer.OrdinalIgnoreCase).Count();

    Console.WriteLine($"\nBatch orders:    {batchNames.Count} unique batch names — " +
        $"{batchNames.Count - stubBatches} match existing batches, {stubBatches} will be created as stubs.");
    Console.WriteLine($"Schedule orders: {schedNames.Count} unique schedule names — " +
        $"{schedNames.Count - stubSchedules} match existing schedules, {stubSchedules} will be created as stubs.");

    return 0;
}

// ─────────────────────────────────────────────────────────────────────────────
// RUN
// ─────────────────────────────────────────────────────────────────────────────

async Task<int> RunAsync(string plant, DateTime? sinceDate)
{
    Console.WriteLine($"Transfer — plant: {plant}" + (sinceDate.HasValue ? $", since: {sinceDate:yyyy-MM-dd}" : " (all records)"));

    await using var db = new ApplicationDbContext(dbOpts);

    var targetPlant = await db.Plants.FirstOrDefaultAsync(x => x.Name == plant);
    if (targetPlant is null)
    {
        Console.Error.WriteLine($"Error: plant '{plant}' not found. Available: " +
            string.Join(", ", await db.Plants.Select(x => x.Name).ToListAsync()));
        return 1;
    }

    var systemUser = await db.Users.OrderBy(u => u.Id).FirstOrDefaultAsync();
    if (systemUser is null)
    {
        Console.Error.WriteLine("Error: no users found in PostgreSQL. Cannot create stub batches/schedules.");
        return 1;
    }

    // Guard against partial previous runs
    var existingPartCount = await db.Parts.CountAsync();
    var existingNestCount = await db.Nests.CountAsync();
    if (existingPartCount > 0 || existingNestCount > 0)
    {
        Console.Error.WriteLine($"Error: destination already contains {existingPartCount:N0} parts and {existingNestCount:N0} nests.");
        Console.Error.WriteLine("Run `dotnet run -- clear` to remove nesting data before transferring.");
        return 1;
    }

    // ── Read source ───────────────────────────────────────────────────────────
    Console.WriteLine("\nReading source data from SQL Server...");
    var src = await ReadSourceAsync(sinceDate);
    PrintSourceSummary(src);

    if (src.Orders.Count == 0)
    {
        Console.WriteLine("Nothing to transfer.");
        return 0;
    }

    // ── Build reference sets (only transfer what's actually needed) ───────────
    var referencedPartIds = src.OrderItems.Select(oi => oi.PartId).ToHashSet();
    var referencedNestIds = src.NestedParts.Select(np => np.NestId).ToHashSet();
    var partsToTransfer   = src.Parts.Where(p => referencedPartIds.Contains(p.Id)).ToList();
    var nestsToTransfer   = src.Nests.Where(n => referencedNestIds.Contains(n.Id)).ToList();

    // old OrderId → IsBatch (for resolving NestedParts later)
    var orderIsBatch = src.Orders.ToDictionary(o => o.Id, o => o.IsBatch);

    // ── 1. Parts ──────────────────────────────────────────────────────────────
    Console.WriteLine($"\n[1/6] Parts ({partsToTransfer.Count:N0})...");
    var partIdMap = new Dictionary<int, int>();
    var partPairs = new List<(int OldId, Part Entity)>(partsToTransfer.Count);

    foreach (var sp in partsToTransfer)
    {
        var e = new Part
        {
            FileName    = sp.FileName,
            Description = sp.Description,
            Material    = sp.Material,
            Thickness   = sp.Thickness.HasValue ? (decimal?)sp.Thickness.Value : null,
            Thumbnail   = sp.Thumbnail,
            HasBends    = sp.HasBends,
        };
        db.Parts.Add(e);
        partPairs.Add((sp.Id, e));
    }
    await db.SaveChangesAsync();
    foreach (var (oldId, e) in partPairs) partIdMap[oldId] = e.Id;
    Console.WriteLine($"  Done. {partPairs.Count:N0} parts inserted.");

    // ── 2. Nests ──────────────────────────────────────────────────────────────
    Console.WriteLine($"\n[2/6] Nests ({nestsToTransfer.Count:N0})...");
    var nestIdMap = new Dictionary<int, int>();
    var nestPairs = new List<(int OldId, Nest Entity)>(nestsToTransfer.Count);

    foreach (var sn in nestsToTransfer)
    {
        var e = new Nest
        {
            NestName    = sn.NestName ?? "(unnamed)",
            NestPath    = sn.NestPath,
            Thumbnail   = sn.Thumbnail,
            PlantId     = targetPlant.Id,
            CreatedDate = DateTime.UtcNow,
        };
        db.Nests.Add(e);
        nestPairs.Add((sn.Id, e));

        if (nestPairs.Count % 500 == 0)
        {
            await db.SaveChangesAsync();
            Console.WriteLine($"  {nestPairs.Count:N0} / {nestsToTransfer.Count:N0}...");
        }
    }
    await db.SaveChangesAsync();
    foreach (var (oldId, e) in nestPairs) nestIdMap[oldId] = e.Id;
    Console.WriteLine($"  Done. {nestPairs.Count:N0} nests inserted.");

    // ── 3. Batch Orders → NestBatches ─────────────────────────────────────────
    var batchOrders = src.Orders.Where(o => o.IsBatch).ToList();
    Console.WriteLine($"\n[3/6] Batch orders → nest_batches ({batchOrders.Count:N0})...");

    // Cache existing Batch records by name for matching
    // Use TryAdd to handle any pre-existing duplicate names gracefully
    var existingBatches = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    foreach (var b in await db.Set<Batch>().ToListAsync())
        existingBatches.TryAdd(b.Name, b.Id);
    var nestBatchIdMap = new Dictionary<int, int>(); // old Order.Id → new NestBatch.Id
    var nestBatchPairs = new List<(int OldOrderId, NestBatch Entity)>(batchOrders.Count);

    foreach (var o in batchOrders)
    {
        var batchName = (o.BatchName ?? "(unnamed)").Trim();

        if (!existingBatches.TryGetValue(batchName, out var batchId))
        {
            var stub = new Batch
            {
                Name               = batchName,
                PlantId            = targetPlant.Id,
                ImportedByUserId   = systemUser.Id,
                ImportDate         = DateTime.SpecifyKind(o.EntryDate, DateTimeKind.Utc),
                ReadyForProduction = o.IsComplete,
                LocalPdfFolder     = null,
            };
            db.Set<Batch>().Add(stub);
            await db.SaveChangesAsync();
            batchId = stub.Id;
            existingBatches[batchName] = batchId;
        }

        var nb = new NestBatch
        {
            BatchId       = batchId,
            PlantId       = targetPlant.Id,
            EntryDate     = DateTime.SpecifyKind(o.EntryDate, DateTimeKind.Utc),
            DueDate       = o.DueDate.HasValue ? DateTime.SpecifyKind(o.DueDate.Value, DateTimeKind.Utc) : null,
            IsComplete    = o.IsComplete,
            CompletedDate = o.DateCompleted.HasValue ? DateTime.SpecifyKind(o.DateCompleted.Value, DateTimeKind.Utc) : null,
        };
        db.NestBatches.Add(nb);
        nestBatchPairs.Add((o.Id, nb));
    }
    await db.SaveChangesAsync();
    foreach (var (oldOrderId, e) in nestBatchPairs) nestBatchIdMap[oldOrderId] = e.Id;
    Console.WriteLine($"  Done. {nestBatchPairs.Count:N0} nest_batches inserted.");

    // ── 4. Schedule Orders → NestOrders ───────────────────────────────────────
    var schedOrders = src.Orders.Where(o => !o.IsBatch).ToList();
    Console.WriteLine($"\n[4/6] Schedule orders → nest_orders ({schedOrders.Count:N0})...");

    // Phase 4a: resolve / batch-create stub Schedules (one per unique name)
    var existingSchedules = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    foreach (var s in await db.Set<Schedule>().ToListAsync())
        existingSchedules.TryAdd(s.Name, s.Id);

    var schedNameToEarliest = schedOrders
        .GroupBy(o => (o.ScheduleName ?? "(unnamed)").Trim())
        .ToDictionary(g => g.Key, g => g.Min(o => o.EntryDate));

    var newSchedPairs = new List<(string Name, Schedule Entity)>();
    foreach (var (name, earliest) in schedNameToEarliest.Where(kv => !existingSchedules.ContainsKey(kv.Key)))
    {
        var stub = new Schedule
        {
            Name               = name,
            PlantId            = targetPlant.Id,
            ImportedByUserId   = systemUser.Id,
            ImportDate         = DateTime.SpecifyKind(earliest, DateTimeKind.Utc),
            ReadyForProduction = false,
            LocalPdfFolder     = null,
        };
        db.Set<Schedule>().Add(stub);
        newSchedPairs.Add((name, stub));
    }
    await db.SaveChangesAsync(); // one batch for all new schedules
    foreach (var (name, stub) in newSchedPairs)
        existingSchedules[name] = stub.Id;
    Console.WriteLine($"  Schedules: {existingSchedules.Count:N0} resolved ({newSchedPairs.Count:N0} stubs created).");

    // Phase 4b: resolve / batch-create stub ScheduleOrders
    // The schedule_orders table has no unique constraint on (schedule_id, order_number),
    // so duplicates may exist from prior BOM imports — TryAdd keeps the first match.
    var existingSchedOrders = new Dictionary<(int, string), int>();
    foreach (var so in await db.Set<ScheduleOrder>().ToListAsync())
        existingSchedOrders.TryAdd((so.ScheduleId, so.OrderNumber), so.Id);

    var neededSchedOrders = schedOrders
        .Select(o => (
            SchedId:  existingSchedules[(o.ScheduleName ?? "(unnamed)").Trim()],
            OrderNum: (o.OrderNumber ?? o.Id.ToString()).Trim()
        ))
        .Distinct()
        .Where(k => !existingSchedOrders.ContainsKey((k.SchedId, k.OrderNum)))
        .ToList();

    var newSOPairs = new List<((int, string) Key, ScheduleOrder Entity)>(neededSchedOrders.Count);
    foreach (var (schedId, orderNum) in neededSchedOrders)
    {
        var so = new ScheduleOrder { ScheduleId = schedId, OrderNumber = orderNum, Qty = 1 };
        db.Set<ScheduleOrder>().Add(so);
        newSOPairs.Add(((schedId, orderNum), so));

        if (newSOPairs.Count % 500 == 0)
        {
            await db.SaveChangesAsync();
            Console.WriteLine($"  ScheduleOrders: {newSOPairs.Count:N0} / {neededSchedOrders.Count:N0}...");
        }
    }
    await db.SaveChangesAsync();
    foreach (var (key, so) in newSOPairs)
        existingSchedOrders.TryAdd(key, so.Id);
    Console.WriteLine($"  ScheduleOrders: {neededSchedOrders.Count:N0} stubs created.");

    // Phase 4c: create NestOrders in batches
    var nestOrderIdMap = new Dictionary<int, int>();
    var nestOrderPairs = new List<(int OldOrderId, NestOrder Entity)>(schedOrders.Count);

    foreach (var o in schedOrders)
    {
        var schedName = (o.ScheduleName ?? "(unnamed)").Trim();
        var orderNum  = (o.OrderNumber ?? o.Id.ToString()).Trim();

        if (!existingSchedules.TryGetValue(schedName, out var scheduleId)) continue;
        if (!existingSchedOrders.TryGetValue((scheduleId, orderNum), out var schedOrderId)) continue;

        var no = new NestOrder
        {
            ScheduleOrderId = schedOrderId,
            PlantId         = targetPlant.Id,
            EntryDate       = DateTime.SpecifyKind(o.EntryDate, DateTimeKind.Utc),
            DueDate         = o.DueDate.HasValue ? DateTime.SpecifyKind(o.DueDate.Value, DateTimeKind.Utc) : null,
            IsComplete      = o.IsComplete,
            CompletedDate   = o.DateCompleted.HasValue ? DateTime.SpecifyKind(o.DateCompleted.Value, DateTimeKind.Utc) : null,
        };
        db.NestOrders.Add(no);
        nestOrderPairs.Add((o.Id, no));

        if (nestOrderPairs.Count % 500 == 0)
        {
            await db.SaveChangesAsync();
            Console.WriteLine($"  NestOrders: {nestOrderPairs.Count:N0} / {schedOrders.Count:N0}...");
        }
    }
    await db.SaveChangesAsync();
    foreach (var (oldOrderId, e) in nestOrderPairs) nestOrderIdMap[oldOrderId] = e.Id;
    Console.WriteLine($"  Done. {nestOrderPairs.Count:N0} nest_orders inserted.");

    // ── 5. OrderItems → BatchItems / OrderItems ───────────────────────────────
    Console.WriteLine($"\n[5/6] Order items ({src.OrderItems.Count:N0})...");

    // old OrderItem.Id → new entity Id (in whichever table it went to)
    var batchItemIdMap = new Dictionary<int, int>(); // old → new BatchItem.Id
    var orderItemIdMap = new Dictionary<int, int>(); // old → new OrderItem.Id

    var batchItemPairs = new List<(int OldId, BatchItem Entity)>();
    var orderItemPairs = new List<(int OldId, OrderItem Entity)>();
    int itemCount = 0;

    foreach (var si in src.OrderItems)
    {
        if (!orderIsBatch.TryGetValue(si.OrderId, out var isBatch))
            continue; // order was filtered out

        if (!partIdMap.TryGetValue(si.PartId, out var newPartId))
            continue; // part not in transfer set (shouldn't happen)

        if (isBatch)
        {
            if (!nestBatchIdMap.TryGetValue(si.OrderId, out var nestBatchId))
                continue;

            var e = new BatchItem
            {
                NestBatchId      = nestBatchId,
                PartId           = newPartId,
                QtyRequired      = si.QtyRequired,
                QtyNested        = si.QtyNested,
                IsComplete       = si.IsComplete,
                IsInRadanProject = si.IsInProject,
                RadanIdNumber    = si.RadanIdNumber == 0 ? null : si.RadanIdNumber,
                Notes            = si.Notes,
            };
            db.BatchItems.Add(e);
            batchItemPairs.Add((si.Id, e));
        }
        else
        {
            if (!nestOrderIdMap.TryGetValue(si.OrderId, out var nestOrderId))
                continue;

            var e = new OrderItem
            {
                NestOrderId      = nestOrderId,
                PartId           = newPartId,
                QtyRequired      = si.QtyRequired,
                QtyNested        = si.QtyNested,
                IsComplete       = si.IsComplete,
                IsInRadanProject = si.IsInProject,
                RadanIdNumber    = si.RadanIdNumber == 0 ? null : si.RadanIdNumber,
                Notes            = si.Notes,
            };
            db.OrderItems.Add(e);
            orderItemPairs.Add((si.Id, e));
        }

        itemCount++;
        if (itemCount % 500 == 0)
        {
            await db.SaveChangesAsync();
            Console.WriteLine($"  {itemCount:N0} / {src.OrderItems.Count:N0}...");
        }
    }
    await db.SaveChangesAsync();
    foreach (var (oldId, e) in batchItemPairs) batchItemIdMap[oldId] = e.Id;
    foreach (var (oldId, e) in orderItemPairs) orderItemIdMap[oldId] = e.Id;
    Console.WriteLine($"  Done. {batchItemPairs.Count:N0} batch_items, {orderItemPairs.Count:N0} order_items inserted.");

    // ── 6. NestedParts ────────────────────────────────────────────────────────
    Console.WriteLine($"\n[6/6] Nested parts ({src.NestedParts.Count:N0})...");

    var nestedPairsList = new List<(int OldId, NestedPart Entity)>(src.NestedParts.Count);
    int npCount = 0;

    foreach (var snp in src.NestedParts)
    {
        if (!nestIdMap.TryGetValue(snp.NestId, out var newNestId))
            continue; // nest not in transfer set

        int? newBatchItemId = null;
        int? newOrderItemId = null;

        if (batchItemIdMap.TryGetValue(snp.OrderItemId, out var bii))
            newBatchItemId = bii;
        else if (orderItemIdMap.TryGetValue(snp.OrderItemId, out var oii))
            newOrderItemId = oii;
        else
            continue; // item not transferred

        var e = new NestedPart
        {
            NestId      = newNestId,
            BatchItemId = newBatchItemId,
            OrderItemId = newOrderItemId,
            Qty         = snp.Qty,
        };
        db.NestedParts.Add(e);
        nestedPairsList.Add((snp.Id, e));

        npCount++;
        if (npCount % 1000 == 0)
        {
            await db.SaveChangesAsync();
            Console.WriteLine($"  {npCount:N0} / {src.NestedParts.Count:N0}...");
        }
    }
    await db.SaveChangesAsync();
    Console.WriteLine($"  Done. {nestedPairsList.Count:N0} nested_parts inserted.");

    Console.WriteLine("\nTransfer complete.");
    return 0;
}

// ─────────────────────────────────────────────────────────────────────────────
// READ SOURCE DATA FROM SQL SERVER
// ─────────────────────────────────────────────────────────────────────────────

async Task<SourceData> ReadSourceAsync(DateTime? sinceDate)
{
    await using var sql = OpenSqlConnection();
    await sql.OpenAsync();

    var orders = await ReadOrdersAsync(sql, sinceDate);
    var orderIds = orders.Select(o => o.Id).ToHashSet();

    var orderItems  = await ReadOrderItemsAsync(sql, orderIds);
    var partIds     = orderItems.Select(oi => oi.PartId).ToHashSet();
    var parts       = await ReadPartsAsync(sql, partIds);

    var orderItemIds = orderItems.Select(oi => oi.Id).ToHashSet();
    var nestedParts  = await ReadNestedPartsAsync(sql, orderItemIds);
    var nestIds      = nestedParts.Select(np => np.NestId).ToHashSet();
    var nests        = await ReadNestsAsync(sql, nestIds);

    return new SourceData(parts, nests, orders, orderItems, nestedParts);
}

async Task<List<SrcOrder>> ReadOrdersAsync(SqlConnection sql, DateTime? since)
{
    var where = since.HasValue
        ? $"WHERE EntryDate >= '{since.Value:yyyy-MM-dd}'"
        : "";
    var cmd = sql.CreateCommand();
    cmd.CommandText = $"SELECT ID, OrderNumber, ScheduleName, BatchName, EntryDate, DueDate, IsComplete, IsBatch, DateCompleted FROM Orders {where}";
    cmd.CommandTimeout = 120;

    var list = new List<SrcOrder>();
    await using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
    {
        list.Add(new SrcOrder(
            Id:            r.GetInt32("ID"),
            OrderNumber:   r.IsDBNull("OrderNumber")   ? null : r.GetString("OrderNumber"),
            ScheduleName:  r.IsDBNull("ScheduleName")  ? null : r.GetString("ScheduleName"),
            BatchName:     r.IsDBNull("BatchName")      ? null : r.GetString("BatchName"),
            EntryDate:     r.GetDateTime("EntryDate"),
            DueDate:       r.IsDBNull("DueDate")         ? null : r.GetDateTime("DueDate"),
            IsComplete:    r.GetBoolean("IsComplete"),
            IsBatch:       r.GetBoolean("IsBatch"),
            DateCompleted: r.IsDBNull("DateCompleted")   ? null : r.GetDateTime("DateCompleted")
        ));
    }
    return list;
}

async Task<List<SrcOrderItem>> ReadOrderItemsAsync(SqlConnection sql, HashSet<int> orderIds)
{
    if (orderIds.Count == 0) return [];

    var cmd = sql.CreateCommand();
    cmd.CommandText = """
        SELECT ID, OrderID, IsComplete, QtyRequired, QtyNested, IsInProject, Notes, PartID, RadanIDNumber
        FROM OrderItems
        WHERE OrderID IN (SELECT value FROM STRING_SPLIT(@ids, ','))
        """;
    cmd.Parameters.AddWithValue("@ids", string.Join(',', orderIds));
    cmd.CommandTimeout = 120;

    var list = new List<SrcOrderItem>();
    await using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
    {
        list.Add(new SrcOrderItem(
            Id:           r.GetInt32("ID"),
            OrderId:      r.GetInt32("OrderID"),
            IsComplete:   r.GetBoolean("IsComplete"),
            QtyRequired:  r.GetInt32("QtyRequired"),
            QtyNested:    r.GetInt32("QtyNested"),
            IsInProject:  r.GetBoolean("IsInProject"),
            Notes:        r.IsDBNull("Notes") ? null : r.GetString("Notes"),
            PartId:       r.GetInt32("PartID"),
            RadanIdNumber: r.GetInt32("RadanIDNumber")
        ));
    }
    return list;
}

async Task<List<SrcPart>> ReadPartsAsync(SqlConnection sql, HashSet<int> partIds)
{
    if (partIds.Count == 0) return [];

    var cmd = sql.CreateCommand();
    cmd.CommandText = """
        SELECT ID, FileName, Description, Material, Thickness, Thumbnail, HasBends
        FROM Parts
        WHERE ID IN (SELECT value FROM STRING_SPLIT(@ids, ','))
        """;
    cmd.Parameters.AddWithValue("@ids", string.Join(',', partIds));
    cmd.CommandTimeout = 120;

    var list = new List<SrcPart>();
    await using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
    {
        list.Add(new SrcPart(
            Id:          r.GetInt32("ID"),
            FileName:    r.IsDBNull("FileName")    ? null : r.GetString("FileName"),
            Description: r.IsDBNull("Description") ? null : r.GetString("Description"),
            Material:    r.IsDBNull("Material")    ? null : r.GetString("Material"),
            Thickness:   r.IsDBNull("Thickness")   ? null : r.GetDouble("Thickness"),
            Thumbnail:   r.IsDBNull("Thumbnail")   ? null : (byte[])r["Thumbnail"],
            HasBends:    r.GetBoolean("HasBends")
        ));
    }
    return list;
}

async Task<List<SrcNestedPart>> ReadNestedPartsAsync(SqlConnection sql, HashSet<int> orderItemIds)
{
    if (orderItemIds.Count == 0) return [];

    var cmd = sql.CreateCommand();
    cmd.CommandText = """
        SELECT ID, Qty, Nest_ID, OrderItem_ID
        FROM NestedParts
        WHERE OrderItem_ID IN (SELECT value FROM STRING_SPLIT(@ids, ','))
        """;
    cmd.Parameters.AddWithValue("@ids", string.Join(',', orderItemIds));
    cmd.CommandTimeout = 120;

    var list = new List<SrcNestedPart>();
    await using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
    {
        list.Add(new SrcNestedPart(
            Id:          r.GetInt32("ID"),
            Qty:         r.GetInt32("Qty"),
            NestId:      r.IsDBNull("Nest_ID")       ? 0 : r.GetInt32("Nest_ID"),
            OrderItemId: r.IsDBNull("OrderItem_ID")  ? 0 : r.GetInt32("OrderItem_ID")
        ));
    }
    return list.Where(np => np.NestId > 0 && np.OrderItemId > 0).ToList();
}

async Task<List<SrcNest>> ReadNestsAsync(SqlConnection sql, HashSet<int> nestIds)
{
    if (nestIds.Count == 0) return [];

    var cmd = sql.CreateCommand();
    cmd.CommandText = """
        SELECT ID, nestName, nestPath, Thumbnail
        FROM Nests
        WHERE ID IN (SELECT value FROM STRING_SPLIT(@ids, ','))
        """;
    cmd.Parameters.AddWithValue("@ids", string.Join(',', nestIds));
    cmd.CommandTimeout = 120;

    var list = new List<SrcNest>();
    await using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
    {
        list.Add(new SrcNest(
            Id:        r.GetInt32("ID"),
            NestName:  r.IsDBNull("nestName")  ? null : r.GetString("nestName"),
            NestPath:  r.IsDBNull("nestPath")  ? null : r.GetString("nestPath"),
            Thumbnail: r.IsDBNull("Thumbnail") ? null : (byte[])r["Thumbnail"]
        ));
    }
    return list;
}

// ─────────────────────────────────────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────────────────────────────────────

SqlConnection OpenSqlConnection() => new(sqlConnStr);

static async Task<int> SqlCountAsync(SqlConnection sql, string tableOrExpr)
{
    var cmd = sql.CreateCommand();
    cmd.CommandText = $"SELECT COUNT(*) FROM {tableOrExpr}";
    return (int)(await cmd.ExecuteScalarAsync())!;
}

static void PrintSourceSummary(SourceData src)
{
    Console.WriteLine($"  Orders:      {src.Orders.Count:N0} " +
        $"({src.Orders.Count(o => o.IsBatch):N0} batch, {src.Orders.Count(o => !o.IsBatch):N0} schedule)");
    Console.WriteLine($"  OrderItems:  {src.OrderItems.Count:N0}");
    Console.WriteLine($"  Parts:       {src.Parts.Count:N0} (referenced)");
    Console.WriteLine($"  Nests:       {src.Nests.Count:N0} (referenced)");
    Console.WriteLine($"  NestedParts: {src.NestedParts.Count:N0}");
}

static string? GetArg(string[] args, string flag)
{
    var idx = Array.IndexOf(args, flag);
    return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
}

static int RequirePlant()
{
    Console.Error.WriteLine("Error: --plant <name> is required.");
    return 1;
}

static int PrintUsage()
{
    Console.WriteLine("""
        Usage:
          dotnet run -- status
          dotnet run -- clear
          dotnet run -- preview --plant <name> [--since yyyy-MM-dd]
          dotnet run -- run     --plant <name> [--since yyyy-MM-dd]

        Options:
          --plant <name>      Target plant name (must exist in PostgreSQL)
          --since yyyy-MM-dd  Only transfer orders on/after this date
          --sql-conn "<str>"  Override the SQL Server connection string

        Examples:
          dotnet run -- status
          dotnet run -- clear
          dotnet run -- preview --plant "Plant 1 - Main"
          dotnet run -- preview --plant "Plant 1 - Main" --since 2024-01-01
          dotnet run -- run     --plant "Plant 1 - Main" --since 2024-01-01
        """);
    return 1;
}

// ─────────────────────────────────────────────────────────────────────────────
// Source data records (read-only structs from SQL Server)
// ─────────────────────────────────────────────────────────────────────────────

record SrcOrder(
    int Id, string? OrderNumber, string? ScheduleName, string? BatchName,
    DateTime EntryDate, DateTime? DueDate, bool IsComplete, bool IsBatch, DateTime? DateCompleted);

record SrcOrderItem(
    int Id, int OrderId, bool IsComplete, int QtyRequired, int QtyNested,
    bool IsInProject, string? Notes, int PartId, int RadanIdNumber);

record SrcPart(
    int Id, string? FileName, string? Description, string? Material,
    double? Thickness, byte[]? Thumbnail, bool HasBends);

record SrcNest(int Id, string? NestName, string? NestPath, byte[]? Thumbnail);

record SrcNestedPart(int Id, int Qty, int NestId, int OrderItemId);

record SourceData(
    List<SrcPart> Parts,
    List<SrcNest> Nests,
    List<SrcOrder> Orders,
    List<SrcOrderItem> OrderItems,
    List<SrcNestedPart> NestedParts);
