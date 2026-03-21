/*
 * HorstMFG Data Seeder
 *
 * Usage (run from tools/HorstMFG.DataSeeder/):
 *
 *   dotnet run -- seed <dataset> [--batches 50] [--schedules 30] [--days 365]
 *       Generates fake batches and schedules named "[<dataset>] ..."
 *       Parts are sampled from real PartLineItems already in the DB.
 *       PDFs are copied from any existing local PDF folders that contain them.
 *
 *   dotnet run -- clear <dataset>
 *       Deletes all batches, schedules, and their PDF folders for that dataset.
 *
 *   dotnet run -- list
 *       Lists all datasets present in the DB (real data shows as "<production>").
 *
 * Swapping datasets:
 *   Type the dataset name prefix (e.g. "[demo]") in the grid search box to
 *   view only that dataset. Clear the search to see everything.
 *
 * Examples:
 *   dotnet run -- seed demo --batches 100 --schedules 60
 *   dotnet run -- seed perf --batches 500 --schedules 300 --days 730
 *   dotnet run -- clear demo
 *   dotnet run -- list
 */

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using HorstMFG.Core.Entities;
using HorstMFG.Infrastructure.Data;

// ── Config ────────────────────────────────────────────────────────────────────

// Look for the web project's appsettings.json relative to CWD (dotnet run sets
// CWD to the project directory, i.e. tools/HorstMFG.DataSeeder/).
var webSettings = Path.GetFullPath(
    Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "src", "HorstMFG.Web", "appsettings.json"));

var configBuilder = new ConfigurationBuilder();
if (File.Exists(webSettings))
    configBuilder.AddJsonFile(webSettings, optional: false);
else if (File.Exists("appsettings.json"))
    configBuilder.AddJsonFile("appsettings.json", optional: false);
else
{
    Console.Error.WriteLine("Cannot find appsettings.json. Run from tools/HorstMFG.DataSeeder/ or place an appsettings.json here.");
    return 1;
}

var config    = configBuilder.Build();
var connStr   = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection missing.");
var pdfBase   = config["FileSystemPaths:LocalPdfPath"] ?? @"C:\HorstMFG\PDFs\";

// ── DB context ────────────────────────────────────────────────────────────────

var dbOpts = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseNpgsql(connStr)
    .Options;

// ── Argument parsing ──────────────────────────────────────────────────────────

if (args.Length == 0) { PrintUsage(); return 1; }

var command = args[0].ToLowerInvariant();
var dataset = args.Length > 1 && !args[1].StartsWith("--") ? args[1] : null;
int batchCount    = GetIntArg(args, "--batches",   50);
int scheduleCount = GetIntArg(args, "--schedules", 30);
int daysSpread    = GetIntArg(args, "--days",      365);

// ── Dispatch ──────────────────────────────────────────────────────────────────

return command switch
{
    "seed"  => dataset is null ? (PrintUsage(), 1).Item2
                               : await SeedAsync(dataset, batchCount, scheduleCount, daysSpread),
    "clear" => dataset is null ? (PrintUsage(), 1).Item2
                               : await ClearAsync(dataset),
    "list"  => await ListAsync(),
    _       => (PrintUsage(), 1).Item2,
};

// ─────────────────────────────────────────────────────────────────────────────
// SEED
// ─────────────────────────────────────────────────────────────────────────────

async Task<int> SeedAsync(string ds, int numBatches, int numSchedules, int days)
{
    var prefix = Prefix(ds);
    Console.WriteLine($"Seeding dataset [{ds}]: {numBatches} batches, {numSchedules} schedules over {days} days.");
    Console.WriteLine($"Config: {webSettings}");
    Console.WriteLine($"PDF base: {pdfBase}");

    await using var db = new ApplicationDbContext(dbOpts);

    // ── Load templates from real data ─────────────────────────────────────────

    var realParts = await db.Set<PartLineItem>()
        .Where(p => !p.PartNumber.StartsWith("SEED-"))
        .AsNoTracking()
        .ToListAsync();

    if (realParts.Count == 0)
    {
        Console.Error.WriteLine("No real PartLineItems found. Import at least one real batch or schedule first.");
        return 1;
    }

    Console.WriteLine($"Template pool: {realParts.Count} parts.");

    var realProductNames = await db.Set<BatchProduct>()
        .Select(bp => bp.ProductName)
        .Distinct()
        .AsNoTracking()
        .ToListAsync();
    realProductNames = realProductNames
        .Where(n => n.StartsWith("BAT", StringComparison.OrdinalIgnoreCase))
        .ToList();

    if (realProductNames.Count == 0)
        realProductNames = new List<string> { "BAT-001", "BAT-002", "BAT-003", "BAT-004", "BAT-005" };

    var plants = await db.Set<Plant>().Where(p => p.IsActive).AsNoTracking().ToListAsync();
    var users  = await db.Set<ApplicationUser>().AsNoTracking().ToListAsync();

    if (plants.Count == 0 || users.Count == 0)
    {
        Console.Error.WriteLine("No active plants or users found. Cannot seed.");
        return 1;
    }

    // ── Build PDF lookup: partNumber → source file path ───────────────────────

    var pdfLookup = BuildPdfLookup(pdfBase);
    Console.WriteLine($"Found {pdfLookup.Count} unique part PDFs in local folders.");

    var rng  = new Random();
    var now  = DateTime.UtcNow;
    int pdfsCopied = 0;

    // ── Generate Batches ──────────────────────────────────────────────────────

    Console.WriteLine($"\nGenerating {numBatches} batches...");
    for (int i = 0; i < numBatches; i++)
    {
        var date   = now.AddDays(-rng.NextDouble() * days);
        var letter = (char)('A' + (i % 26));
        var name   = $"{prefix}{date:yyyy-MM-dd} Batch {letter}{(i >= 26 ? i / 26 : "")}";
        var plant  = plants[rng.Next(plants.Count)];
        var user   = users[rng.Next(users.Count)];
        var folder = Path.Combine(pdfBase, "Batches", name);

        var batch = new Batch
        {
            Name               = name,
            PlantId            = plant.Id,
            ImportedByUserId   = user.Id,
            ImportDate         = date,
            LocalPdfFolder     = folder,
            ReadyForProduction = rng.NextDouble() > 0.3,
        };
        db.Set<Batch>().Add(batch);
        await db.SaveChangesAsync();

        Directory.CreateDirectory(folder);

        int productCount = rng.Next(8, 16);
        for (int p = 0; p < productCount; p++)
        {
            var productName = realProductNames[rng.Next(realProductNames.Count)];
            var product = new BatchProduct
            {
                BatchId     = batch.Id,
                ProductName = productName,
                Qty         = rng.Next(10, 201),
            };
            db.Set<BatchProduct>().Add(product);
            await db.SaveChangesAsync();

            int partCount = rng.Next(5, 21);
            var partPool  = SampleWithoutRepeat(realParts, partCount, rng);
            foreach (var template in partPool)
            {
                bool hasPdf = CopyPdf(template.PartNumber, folder, pdfLookup);
                if (hasPdf) pdfsCopied++;

                db.Set<PartLineItem>().Add(new PartLineItem
                {
                    BatchProductId = product.Id,
                    PartNumber     = template.PartNumber,
                    Title          = template.Title,
                    Description    = template.Description,
                    Category       = template.Category,
                    Qty            = rng.Next(1, 5),
                    Material       = template.Material,
                    Thickness      = template.Thickness,
                    StructCode     = template.StructCode,
                    Operations     = template.Operations,
                    IsStock        = template.IsStock,
                    RequiresPdf    = template.RequiresPdf,
                    HasPdf         = hasPdf,
                    IsProcessed    = false,
                });
            }
            await db.SaveChangesAsync();
        }

        if ((i + 1) % 10 == 0)
            Console.WriteLine($"  {i + 1}/{numBatches} batches created...");
    }

    // ── Generate Schedules ────────────────────────────────────────────────────

    Console.WriteLine($"\nGenerating {numSchedules} schedules...");
    for (int i = 0; i < numSchedules; i++)
    {
        var date   = now.AddDays(-rng.NextDouble() * days);
        var letter = (char)('A' + (i % 26));
        var name   = $"{prefix}{date:yyyy-MM-dd} Schedule {letter}{(i >= 26 ? i / 26 : "")}";
        var plant  = plants[rng.Next(plants.Count)];
        var user   = users[rng.Next(users.Count)];
        var folder = Path.Combine(pdfBase, "Schedules", name);

        var schedule = new Schedule
        {
            Name               = name,
            PlantId            = plant.Id,
            ImportedByUserId   = user.Id,
            ImportDate         = date,
            LocalPdfFolder     = folder,
            ReadyForProduction = rng.NextDouble() > 0.3,
        };
        db.Set<Schedule>().Add(schedule);
        await db.SaveChangesAsync();

        Directory.CreateDirectory(folder);

        int orderCount = rng.Next(20, 31);
        for (int o = 0; o < orderCount; o++)
        {
            var order = new ScheduleOrder
            {
                ScheduleId  = schedule.Id,
                OrderNumber = $"{rng.Next(10000, 99999)}",
                Qty         = rng.Next(1, 6), // 1–5 inclusive
            };
            db.Set<ScheduleOrder>().Add(order);
            await db.SaveChangesAsync();

            int partCount = rng.Next(10, 41);
            var partPool  = SampleWithoutRepeat(realParts, partCount, rng);
            foreach (var template in partPool)
            {
                bool hasPdf = CopyPdf(template.PartNumber, folder, pdfLookup);
                if (hasPdf) pdfsCopied++;

                db.Set<PartLineItem>().Add(new PartLineItem
                {
                    ScheduleOrderId = order.Id,
                    PartNumber      = template.PartNumber,
                    Title           = template.Title,
                    Description     = template.Description,
                    Category        = template.Category,
                    Qty             = rng.Next(1, 5),
                    Material        = template.Material,
                    Thickness       = template.Thickness,
                    StructCode      = template.StructCode,
                    Operations      = template.Operations,
                    IsStock         = template.IsStock,
                    RequiresPdf     = template.RequiresPdf,
                    HasPdf          = hasPdf,
                    IsProcessed     = false,
                });
            }
            await db.SaveChangesAsync();
        }

        if ((i + 1) % 10 == 0)
            Console.WriteLine($"  {i + 1}/{numSchedules} schedules created...");
    }

    Console.WriteLine($"\nDone. PDFs copied: {pdfsCopied}");
    Console.WriteLine($"View in the app by typing \"[{ds}]\" in the grid search box.");
    return 0;
}

// ─────────────────────────────────────────────────────────────────────────────
// CLEAR
// ─────────────────────────────────────────────────────────────────────────────

async Task<int> ClearAsync(string ds)
{
    var prefix = Prefix(ds);
    Console.WriteLine($"Clearing dataset [{ds}] (prefix: \"{prefix}\")...");

    await using var db = new ApplicationDbContext(dbOpts);

    var batches = await db.Set<Batch>()
        .Include(b => b.BatchProducts).ThenInclude(bp => bp.Parts)
        .Where(b => b.Name.StartsWith(prefix))
        .ToListAsync();

    var schedules = await db.Set<Schedule>()
        .Include(s => s.ScheduleOrders).ThenInclude(so => so.Parts)
        .Where(s => s.Name.StartsWith(prefix))
        .ToListAsync();

    if (batches.Count == 0 && schedules.Count == 0)
    {
        Console.WriteLine("Nothing found to clear.");
        return 0;
    }

    // Delete PDF folders
    foreach (var b in batches)
        TryDeleteFolder(b.LocalPdfFolder);
    foreach (var s in schedules)
        TryDeleteFolder(s.LocalPdfFolder);

    // EF cascade should handle children, but remove explicitly to be safe
    foreach (var b in batches)
    {
        foreach (var bp in b.BatchProducts)
        {
            db.Set<PartLineItem>().RemoveRange(bp.Parts);
            db.Set<BatchProduct>().Remove(bp);
        }
        db.Set<Batch>().Remove(b);
    }
    foreach (var s in schedules)
    {
        foreach (var so in s.ScheduleOrders)
        {
            db.Set<PartLineItem>().RemoveRange(so.Parts);
            db.Set<ScheduleOrder>().Remove(so);
        }
        db.Set<Schedule>().Remove(s);
    }

    await db.SaveChangesAsync();
    Console.WriteLine($"Cleared {batches.Count} batches and {schedules.Count} schedules.");
    return 0;
}

// ─────────────────────────────────────────────────────────────────────────────
// LIST
// ─────────────────────────────────────────────────────────────────────────────

async Task<int> ListAsync()
{
    await using var db = new ApplicationDbContext(dbOpts);

    var batchNames    = await db.Set<Batch>().Select(b => b.Name).ToListAsync();
    var scheduleNames = await db.Set<Schedule>().Select(s => s.Name).ToListAsync();

    var datasets = new Dictionary<string, (int batches, int schedules)>(StringComparer.OrdinalIgnoreCase);

    foreach (var name in batchNames)
    {
        var ds = ExtractDataset(name);
        datasets.TryGetValue(ds, out var counts);
        datasets[ds] = (counts.batches + 1, counts.schedules);
    }
    foreach (var name in scheduleNames)
    {
        var ds = ExtractDataset(name);
        datasets.TryGetValue(ds, out var counts);
        datasets[ds] = (counts.batches, counts.schedules + 1);
    }

    if (datasets.Count == 0)
    {
        Console.WriteLine("Database is empty.");
        return 0;
    }

    Console.WriteLine($"{"Dataset",-20} {"Batches",10} {"Schedules",12}");
    Console.WriteLine(new string('-', 44));
    foreach (var (ds, counts) in datasets.OrderBy(k => k.Key))
        Console.WriteLine($"{ds,-20} {counts.batches,10} {counts.schedules,12}");

    return 0;
}

// ─────────────────────────────────────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────────────────────────────────────

static string Prefix(string dataset) => $"[{dataset}] ";

static string ExtractDataset(string name)
{
    if (name.StartsWith("[") && name.Contains("] "))
    {
        var end = name.IndexOf("] ");
        return name[1..end];
    }
    return "<production>";
}

static Dictionary<string, string> BuildPdfLookup(string pdfBase)
{
    var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    if (!Directory.Exists(pdfBase)) return lookup;

    foreach (var file in Directory.EnumerateFiles(pdfBase, "*.pdf", SearchOption.AllDirectories))
    {
        var partNum = Path.GetFileNameWithoutExtension(file);
        lookup.TryAdd(partNum, file); // first found wins
    }
    return lookup;
}

static bool CopyPdf(string partNumber, string destFolder, Dictionary<string, string> lookup)
{
    if (!lookup.TryGetValue(partNumber, out var src)) return false;
    try
    {
        var dest = Path.Combine(destFolder, partNumber + ".pdf");
        if (!File.Exists(dest))
            File.Copy(src, dest);
        return true;
    }
    catch
    {
        return false;
    }
}

static void TryDeleteFolder(string? folder)
{
    if (folder is null || !Directory.Exists(folder)) return;
    try { Directory.Delete(folder, recursive: true); }
    catch (Exception ex) { Console.WriteLine($"  Warning: could not delete {folder}: {ex.Message}"); }
}

static List<PartLineItem> SampleWithoutRepeat(List<PartLineItem> pool, int count, Random rng)
{
    // Sample up to `count` items. If pool is smaller, sample with repetition.
    if (pool.Count <= count)
    {
        // Shuffle and return all, or repeat if needed
        var result = pool.OrderBy(_ => rng.Next()).ToList();
        while (result.Count < count)
            result.Add(pool[rng.Next(pool.Count)]);
        return result;
    }
    return pool.OrderBy(_ => rng.Next()).Take(count).ToList();
}

static int GetIntArg(string[] args, string flag, int defaultVal)
{
    var idx = Array.IndexOf(args, flag);
    if (idx >= 0 && idx + 1 < args.Length && int.TryParse(args[idx + 1], out var val))
        return val;
    return defaultVal;
}

static int PrintUsage()
{
    Console.WriteLine("""
        Usage:
          dotnet run -- seed <dataset> [--batches N] [--schedules N] [--days D]
          dotnet run -- clear <dataset>
          dotnet run -- list

        Examples:
          dotnet run -- seed demo --batches 100 --schedules 60
          dotnet run -- seed perf --batches 500 --schedules 300 --days 730
          dotnet run -- clear demo
          dotnet run -- list

        Datasets are prefixed as "[dataset] " in batch/schedule names.
        Use the grid search box (type "[demo]") to view a specific dataset.
        """);
    return 1;
}
