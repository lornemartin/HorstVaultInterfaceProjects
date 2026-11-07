/*
 * HorstMFG Environment Tool
 *
 * Manages HorstMFG environments on the server: a database plus the files that belong to it
 * (its local PDF folder; its stations' Radan projects folders are recorded in it). The web app
 * always runs on the live database; another environment is made live by renaming databases,
 * so config files never change and no files move.
 *
 * Usage (run from tools/HorstMFG.EnvTool/, or wherever its appsettings.json is):
 *
 *   dotnet run -- list
 *   dotnet run -- create  <name>
 *   dotnet run -- backup  <name> [--skip-pdfs]
 *   dotnet run -- restore <backup-folder> <name> [--detach-stations]
 *   dotnet run -- clone   <source> <name> [--keep-stations] [--skip-pdfs]
 *   dotnet run -- clear   <name> [--no-backup]
 *   dotnet run -- swap    <name> [--disconnect] [--no-backup]
 *   dotnet run -- migrate <name>
 *   dotnet run -- set-radan-root <name> <station|all> <folder|default>
 *   dotnet run -- drop    <name> [--no-backup]
 *
 *   Names are the live database's name (horstmfg) or that name + "_something" (horstmfg_staging).
 *   clear, swap and drop ask you to type the name to confirm; --confirm <name> does it up front.
 *   --config <file> adds a settings file on top of appsettings.json.
 */

using HorstMFG.EnvTool;
using Microsoft.Extensions.Configuration;
using Npgsql;

// ── Settings ────────────────────────────────────────────────────────────────

var configFile = GetArg("--config");
var settingsDir = File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"))
    ? Directory.GetCurrentDirectory()
    : AppContext.BaseDirectory;
var builder = new ConfigurationBuilder().AddJsonFile(Path.Combine(settingsDir, "appsettings.json"), optional: true);
if (configFile != null) builder.AddJsonFile(Path.GetFullPath(configFile), optional: false);
var settings = builder.Build().GetSection("EnvTool");

IConfiguration? webSettings = null;
var webSettingsPath = settings["WebAppSettingsPath"];
if (!string.IsNullOrWhiteSpace(webSettingsPath))
{
    var full = Path.GetFullPath(Path.Combine(settingsDir, webSettingsPath));
    // On the server the real settings are in appsettings.Production.json beside it (see scripts/deploy.ps1).
    if (File.Exists(full))
        webSettings = new ConfigurationBuilder()
            .AddJsonFile(full)
            .AddJsonFile(Path.Combine(Path.GetDirectoryName(full)!, "appsettings.Production.json"), optional: true)
            .Build();
}

var connectionString = NonBlank(settings["ConnectionString"]) ?? webSettings?.GetConnectionString("DefaultConnection");
var pdfRoot = NonBlank(settings["PdfRoot"]) ?? NonBlank(webSettings?["FileSystemPaths:LocalPdfPath"]);
var live = NonBlank(settings["LiveDatabase"]) ?? "horstmfg";
var backupRoot = NonBlank(settings["BackupRoot"]) ?? @"C:\HorstMFG\Backups";
var pgBin = NonBlank(settings["PgBinPath"]) ?? @"C:\Program Files\PostgreSQL\18\bin";
var webApp = new WebApp(NonBlank(settings["AppPool"]));

if (args.Length == 0 || args[0] is "-h" or "--help" or "help") { PrintUsage(); return 0; }
if (connectionString == null) return Fail("No connection string: set EnvTool:ConnectionString, or WebAppSettingsPath to the web app's appsettings.json.");
if (pdfRoot == null) return Fail("No PDF root: set EnvTool:PdfRoot, or WebAppSettingsPath to the web app's appsettings.json.");

var manager = new EnvironmentManager(new Postgres(connectionString, pgBin), live, pdfRoot, backupRoot, webApp);

// ── Dispatch ────────────────────────────────────────────────────────────────

var command = args[0].ToLowerInvariant();
var positional = args.Skip(1).Where((a, i) => !a.StartsWith("--") && (i == 0 || !TakesValue(args[i]))).ToList();

try
{
    switch (command)
    {
        case "list":
            await manager.ListAsync();
            break;
        case "create":
            await manager.CreateAsync(Positional(0, "name"));
            break;
        case "backup":
            Console.WriteLine($"Backup written to {await manager.BackupAsync(Positional(0, "name"), skipPdfs: HasFlag("--skip-pdfs"))}");
            break;
        case "restore":
            await manager.RestoreAsync(Positional(0, "backup folder"), Positional(1, "name"), HasFlag("--detach-stations"));
            break;
        case "clone":
            await manager.CloneAsync(Positional(0, "source"), Positional(1, "name"), HasFlag("--keep-stations"), HasFlag("--skip-pdfs"));
            break;
        case "clear":
        {
            var name = Positional(0, "name");
            if (!Confirm(name, $"This empties {name}'s data and PDF folder" +
                               (name == live ? " — and it is the LIVE database." + (webApp.IsManaged ? " The web app is stopped meanwhile." : "") : ".")))
                return 1;
            await manager.ClearAsync(name, backup: !HasFlag("--no-backup"));
            break;
        }
        case "swap":
        {
            var name = Positional(0, "name");
            if (!Confirm(name, $"This makes {name} the live database ({live}). " +
                               (webApp.IsManaged ? "The web app is stopped meanwhile (including the backup of live)." : "The web app should be stopped.")))
                return 1;
            await manager.SwapAsync(name, HasFlag("--disconnect"), backup: !HasFlag("--no-backup"));
            break;
        }
        case "migrate":
            await manager.MigrateAsync(Positional(0, "name"));
            break;
        case "set-radan-root":
        {
            var root = Positional(2, "folder (or 'default')");
            await manager.SetRadanRootAsync(Positional(0, "name"), Positional(1, "station ID, name or 'all'"),
                root.Equals("default", StringComparison.OrdinalIgnoreCase) ? null : root);
            break;
        }
        case "drop":
        {
            var name = Positional(0, "name");
            if (!Confirm(name, $"This deletes the {name} database and its PDF folder."))
                return 1;
            await manager.DropAsync(name, backup: !HasFlag("--no-backup"));
            break;
        }
        default:
            PrintUsage();
            return 1;
    }
    return 0;
}
catch (UserError ex) { return Fail(ex.Message); }
catch (NpgsqlException ex) { return Fail($"PostgreSQL: {ex.Message}"); }

// ── Helpers ─────────────────────────────────────────────────────────────────

string Positional(int index, string what) =>
    index < positional.Count ? positional[index] : throw new UserError($"Missing {what}. Run with --help for usage.");

bool HasFlag(string flag) => args.Contains(flag, StringComparer.OrdinalIgnoreCase);

string? GetArg(string name)
{
    var i = Array.FindIndex(args, a => a.Equals(name, StringComparison.OrdinalIgnoreCase));
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

static bool TakesValue(string arg) => arg is "--config" or "--confirm";

static string? NonBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

bool Confirm(string name, string warning)
{
    if (GetArg("--confirm") == name) return true;
    Console.WriteLine(warning);
    Console.Write($"Type {name} to continue: ");
    if (Console.ReadLine()?.Trim() == name) return true;
    Console.WriteLine("Cancelled.");
    return false;
}

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    return 1;
}

static void PrintUsage() => Console.WriteLine("""
    HorstMFG Environment Tool

      list                                     Environments: size, migration, PDF folder, stations' Radan folders
      create  <name>                           New empty environment, seeded like a fresh install
      backup  <name> [--skip-pdfs]             Database dump + zip of its PDF folder, under BackupRoot
      restore <backup-folder> <name>           Into a new environment (never over an existing one)
              [--detach-stations]              Clear stations' active projects and Radan folders
      clone   <source> <name>                  Backup + restore; stations are detached unless
              [--keep-stations] [--skip-pdfs]   (--skip-pdfs: database only; the PDF folder can be tens of GB)
      clear   <name> [--no-backup]             Empty its data and PDF folder; keeps plants, users,
                                               stations and configuration. Backs up first.
      swap    <name> [--disconnect]            Make it live by renaming databases. Backs up live first.
              [--no-backup]                    With AppPool set, stops and restarts the web app; otherwise
                                               stop it first, or --disconnect to end its sessions.
      migrate <name>                           Apply pending migrations
      set-radan-root <name> <station|all>      Set stations' Radan projects folder in that environment
              <folder|default>                 (station by ID or name; default = the bridge's own setting)
      drop    <name> [--no-backup]             Delete a non-live environment and its PDF folder

    Names: the live database's name, or that name + "_something". clear, swap and drop ask you to
    type the name; --confirm <name> answers that up front. --config <file> adds a settings file.
    """);
