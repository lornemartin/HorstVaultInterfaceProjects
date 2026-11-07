using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using HorstMFG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace HorstMFG.EnvTool;

/// <summary>
/// An environment is one HorstMFG database plus the files that belong to it: its local PDF folder
/// (pinned in its system_configuration as LocalPdfRoot) and, per station, its Radan projects folder
/// (nesting_stations.RadanProjectsRootPath). The web app always runs on LiveDatabase; making
/// another environment live renames databases, so no config file changes and no files move.
///
/// Only databases named LiveDatabase or LiveDatabase_* are ever touched.
/// </summary>
public class EnvironmentManager
{
    private const string LocalPdfRootKey = "LocalPdfRoot";   // matches HorstMFG.Web EnvironmentSettings
    private const string ManifestFile = "manifest.json";
    private const string DumpFile = "database.dump";
    private const string PdfZipFile = "pdfs.zip";

    // Kept by `clear`: what an environment needs to be usable, as opposed to the data in it.
    private static readonly HashSet<string> SetupTables = new(StringComparer.Ordinal)
    {
        "__EFMigrationsHistory", "plants", "users", "user_roles", "nesting_stations", "system_configuration",
    };

    private readonly Postgres _pg;
    private readonly string _live;
    private readonly string _pdfRoot;
    private readonly string _backupRoot;
    private readonly WebApp _webApp;

    public EnvironmentManager(Postgres pg, string liveDatabase, string pdfRoot, string backupRoot, WebApp webApp)
    {
        _pg = pg;
        _live = liveDatabase;
        _pdfRoot = pdfRoot;
        _backupRoot = backupRoot;
        _webApp = webApp;
    }

    // ── list ────────────────────────────────────────────────────────────────

    public async Task ListAsync()
    {
        var names = (await _pg.ListDatabasesAsync()).Where(IsEnvironmentName).ToList();
        if (names.Count == 0) { Console.WriteLine($"No '{_live}' environments on this server."); return; }

        foreach (var name in names)
        {
            var size = await _pg.SizeAsync(name);
            var connections = await _pg.ConnectionCountAsync(name);
            Console.WriteLine($"{name}{(name == _live ? "   ◄ LIVE" : "")}");
            Console.WriteLine($"  Size:        {size / (1024.0 * 1024):N1} MB, {connections} open connection(s)");
            try
            {
                await using var conn = await _pg.OpenAsync(name);
                Console.WriteLine($"  Migration:   {await LastMigrationAsync(conn) ?? "(not a HorstMFG database)"}");
                var pinned = await GetConfigAsync(conn, LocalPdfRootKey);
                var pdfRoot = pinned ?? DefaultPdfRoot(name);
                Console.WriteLine($"  PDF folder:  {pdfRoot}{(pinned == null ? "  (not pinned yet — set when the app or this tool first opens it)" : "")}" +
                                  $"{(Directory.Exists(pdfRoot) ? $", {FolderSize(pdfRoot) / (1024.0 * 1024):N1} MB" : ", missing")}");
                foreach (var (station, root) in await StationRootsAsync(conn))
                    Console.WriteLine($"  Station:     {station} → {root ?? "(bridge's own setting)"}");
            }
            catch (PostgresException ex) { Console.WriteLine($"  (couldn't read: {ex.MessageText})"); }
        }

        var backups = Directory.Exists(_backupRoot) ? Directory.GetDirectories(_backupRoot).Length : 0;
        Console.WriteLine($"\n{backups} backup(s) in {_backupRoot}");
    }

    // ── create ──────────────────────────────────────────────────────────────

    /// <summary>A new, empty environment at the current migration, seeded the way the web app seeds
    /// a fresh database (plants, an admin login, default configuration).</summary>
    public async Task CreateAsync(string name)
    {
        RequireEnvironmentName(name);
        if (await _pg.ExistsAsync(name)) throw new UserError($"Database '{name}' already exists.");

        await _pg.ExecuteAdminAsync($"CREATE DATABASE \"{name}\"");
        Console.WriteLine($"Created database {name}. Applying migrations and seed data...");
        await using (var db = NewContext(name))
            await new DbInitializer(db, NullLogger<DbInitializer>.Instance).InitializeAsync();

        var pdfRoot = DefaultPdfRoot(name);
        await using (var conn = await _pg.OpenAsync(name))
            await SetConfigAsync(conn, LocalPdfRootKey, pdfRoot);
        Directory.CreateDirectory(pdfRoot);

        Console.WriteLine($"Done. PDF folder: {pdfRoot}");
        Console.WriteLine("It has the default admin login (admin / admin123) and no nesting stations.");
    }

    // ── backup ──────────────────────────────────────────────────────────────

    /// <returns>The backup folder.</returns>
    /// <param name="skipPdfs">Database only — the PDF folder can be tens of GB.</param>
    public async Task<string> BackupAsync(string name, string? label = null, bool skipPdfs = false)
    {
        RequireEnvironmentName(name);
        if (!await _pg.ExistsAsync(name)) throw new UserError($"Database '{name}' doesn't exist.");

        var folder = Path.Combine(_backupRoot, $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}{(label == null ? "" : "_" + label)}");
        Directory.CreateDirectory(folder);
        Console.WriteLine($"Backing up {name} to {folder}");

        string pdfRoot;
        string? migration;
        await using (var conn = await _pg.OpenAsync(name))
        {
            pdfRoot = await GetConfigAsync(conn, LocalPdfRootKey) ?? DefaultPdfRoot(name);
            migration = await LastMigrationAsync(conn);
        }

        Console.WriteLine($"  Dumping the database ({await _pg.SizeAsync(name) / (1024.0 * 1024):N0} MB)...");
        await _pg.DumpAsync(name, Path.Combine(folder, DumpFile));
        Console.WriteLine("  Database dumped.");

        var hasPdfs = !skipPdfs && Directory.Exists(pdfRoot);
        if (hasPdfs)
        {
            Console.WriteLine($"  Copying PDFs from {pdfRoot} into {PdfZipFile} (uncompressed — PDFs are already compressed)...");
            ZipFolder(pdfRoot, Path.Combine(folder, PdfZipFile));
            Console.WriteLine("  PDFs copied.");
        }
        else
            Console.WriteLine(skipPdfs ? "  PDFs skipped (--skip-pdfs)." : $"  No PDF folder at {pdfRoot}; skipped.");

        var manifest = new BackupManifest(name, DateTime.UtcNow, pdfRoot, migration, hasPdfs);
        await File.WriteAllTextAsync(Path.Combine(folder, ManifestFile),
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        return folder;
    }

    // ── restore / clone ─────────────────────────────────────────────────────

    /// <summary>
    /// Restores a backup into a new environment, never over an existing one. PDFs go to the new
    /// environment's own folder and the stored PDF paths are rewritten to match.
    /// </summary>
    /// <param name="detachStations">Clears each station's active project and Radan folder, and
    /// takes items out of Radan, so a copy of live can't touch live's Radan projects.</param>
    public async Task RestoreAsync(string backupFolder, string name, bool detachStations)
    {
        RequireEnvironmentName(name);
        if (await _pg.ExistsAsync(name)) throw new UserError($"Database '{name}' already exists. Restore into a new name.");
        var manifestPath = Path.Combine(backupFolder, ManifestFile);
        if (!File.Exists(manifestPath)) throw new UserError($"{backupFolder} isn't an EnvTool backup (no {ManifestFile}).");
        var manifest = JsonSerializer.Deserialize<BackupManifest>(await File.ReadAllTextAsync(manifestPath))!;
        var pdfRoot = DefaultPdfRoot(name);
        if (Directory.Exists(pdfRoot) && Directory.EnumerateFileSystemEntries(pdfRoot).Any())
            throw new UserError($"{pdfRoot} already has files in it. Move them out of the way and restore again.");

        Console.WriteLine($"Restoring {manifest.Database} ({manifest.CreatedUtc.ToLocalTime():yyyy-MM-dd HH:mm}) into {name}");
        await _pg.ExecuteAdminAsync($"CREATE DATABASE \"{name}\"");
        try
        {
            await _pg.RestoreAsync(name, Path.Combine(backupFolder, DumpFile));
        }
        catch
        {
            Console.WriteLine("  Restore failed; removing the half-restored database.");
            await _pg.ExecuteAdminAsync($"DROP DATABASE IF EXISTS \"{name}\"");
            throw;
        }
        Console.WriteLine("  Database restored.");

        // A backup from an older version gets the current schema, as the web app would give it on
        // startup — the steps below rely on today's columns.
        await using (var db = NewContext(name))
        {
            var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
            if (pending.Count > 0)
            {
                await db.Database.MigrateAsync();
                Console.WriteLine($"  Brought up to date: {string.Join(", ", pending)}.");
            }
        }

        Directory.CreateDirectory(pdfRoot);
        var zip = Path.Combine(backupFolder, PdfZipFile);
        if (File.Exists(zip))
        {
            Console.WriteLine($"  Extracting PDFs to {pdfRoot}...");
            UnzipFolder(zip, pdfRoot);
            Console.WriteLine($"  PDFs extracted to {pdfRoot}.");
        }

        await using var conn = await _pg.OpenAsync(name);
        await SetConfigAsync(conn, LocalPdfRootKey, pdfRoot);
        var moved = await RewritePdfFoldersAsync(conn, manifest.LocalPdfRoot, pdfRoot);
        Console.WriteLine($"  {moved} batch/schedule PDF folder path(s) pointed at the new folder.");

        if (detachStations)
        {
            await DetachStationsAsync(conn);
            Console.WriteLine("  Stations detached: no active project or Radan folder; nothing marked as in Radan.");
        }
        else
            Console.WriteLine("  Stations kept as they were — they still point at the original Radan folders.");
    }

    /// <summary>Copies an environment to a new one by backing it up and restoring the backup.</summary>
    public async Task CloneAsync(string source, string name, bool keepStations, bool skipPdfs)
    {
        RequireEnvironmentName(name);
        if (await _pg.ExistsAsync(name)) throw new UserError($"Database '{name}' already exists.");
        var backup = await BackupAsync(source, label: "for_" + name, skipPdfs);
        await RestoreAsync(backup, name, detachStations: !keepStations);
    }

    // ── clear ───────────────────────────────────────────────────────────────

    /// <summary>Empties an environment's data (orders, batches, parts, nests…) and its PDF folder,
    /// keeping plants, users, stations and configuration. Backs it up first. Clearing live stops
    /// the web app for the duration.</summary>
    public async Task ClearAsync(string name, bool backup)
    {
        RequireEnvironmentName(name);
        if (!await _pg.ExistsAsync(name)) throw new UserError($"Database '{name}' doesn't exist.");

        var restart = name == _live && await _webApp.StopAsync();
        try
        {
            await ClearDataAsync(name, backup);
        }
        finally
        {
            if (restart) await _webApp.StartAsync();
        }
    }

    private async Task ClearDataAsync(string name, bool backup)
    {
        if (backup) await BackupAsync(name, label: "before_clear");

        await using var conn = await _pg.OpenAsync(name);
        var tables = await TablesAsync(conn);
        var toClear = tables.Where(t => !SetupTables.Contains(t)).ToList();

        // Plain TRUNCATE (no CASCADE) fails rather than silently emptying a kept table; check up
        // front so the message says which link is in the way.
        var blocking = (await ForeignKeysAsync(conn))
            .Where(fk => SetupTables.Contains(fk.Child) && toClear.Contains(fk.Parent)).ToList();
        if (blocking.Count > 0)
            throw new UserError("Can't clear: kept tables refer to cleared ones: " +
                                string.Join(", ", blocking.Select(fk => $"{fk.Child} → {fk.Parent}")));

        await using (var tx = await conn.BeginTransactionAsync())
        {
            await ExecAsync(conn, $"TRUNCATE {string.Join(", ", toClear.Select(t => $"\"{t}\""))} RESTART IDENTITY", tx);
            // Their projects are gone, so stations start with no active project (Radan folders stay).
            await ExecAsync(conn, "UPDATE nesting_stations SET \"ProjectPath\" = NULL, \"ProjectName\" = NULL", tx);
            await tx.CommitAsync();
        }
        Console.WriteLine($"Cleared {toClear.Count} table(s) in {name}; kept {string.Join(", ", SetupTables.Where(tables.Contains))}.");

        var pdfRoot = await GetConfigAsync(conn, LocalPdfRootKey) ?? DefaultPdfRoot(name);
        if (Directory.Exists(pdfRoot) && IsUnderPdfRoot(pdfRoot))
        {
            foreach (var dir in Directory.GetDirectories(pdfRoot)) Directory.Delete(dir, recursive: true);
            foreach (var file in Directory.GetFiles(pdfRoot)) File.Delete(file);
            Console.WriteLine($"Emptied {pdfRoot}.");
        }
        else if (Directory.Exists(pdfRoot))
            Console.WriteLine($"Left {pdfRoot} alone: it isn't under {_pdfRoot}.");
    }

    // ── swap ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Makes an environment live by renaming databases: live → live_prev_&lt;timestamp&gt;, then
    /// name → live. With an app pool configured, the web app is stopped for the swap and started
    /// again afterwards (even if the swap fails); otherwise it must be stopped by hand (or
    /// disconnect=true to end its sessions).
    /// </summary>
    public async Task SwapAsync(string name, bool disconnect, bool backup)
    {
        RequireEnvironmentName(name);
        if (name == _live) throw new UserError($"'{name}' is already live.");
        if (!await _pg.ExistsAsync(name)) throw new UserError($"Database '{name}' doesn't exist.");

        var restart = await _webApp.StopAsync();
        try
        {
            await RenameIntoLiveAsync(name, disconnect, backup);
        }
        finally
        {
            if (restart) await _webApp.StartAsync();
        }
    }

    private async Task RenameIntoLiveAsync(string name, bool disconnect, bool backup)
    {
        var liveExists = await _pg.ExistsAsync(_live);

        foreach (var db in liveExists ? new[] { _live, name } : new[] { name })
        {
            var open = await _pg.ConnectionCountAsync(db);
            if (open == 0) continue;
            if (!disconnect)
                throw new UserError($"{db} has {open} open connection(s). Stop the web app first, " +
                                    "or pass --disconnect to end those sessions.");
            await _pg.DisconnectAllAsync(db);
            Console.WriteLine($"Ended {open} session(s) on {db}.");
        }

        if (liveExists && backup) await BackupAsync(_live, label: "before_swap");

        var previous = $"{_live}_prev_{DateTime.Now:yyyyMMdd_HHmmss}";
        if (liveExists)
            await _pg.ExecuteAdminAsync($"ALTER DATABASE \"{_live}\" RENAME TO \"{previous}\"");
        try
        {
            await _pg.ExecuteAdminAsync($"ALTER DATABASE \"{name}\" RENAME TO \"{_live}\"");
        }
        catch
        {
            if (liveExists)
            {
                Console.WriteLine("Rename failed; putting the old live database back.");
                await _pg.ExecuteAdminAsync($"ALTER DATABASE \"{previous}\" RENAME TO \"{_live}\"");
            }
            throw;
        }

        Console.WriteLine($"{name} is now live as {_live}." + (liveExists ? $" The old live database is {previous}." : ""));
        Console.WriteLine((_webApp.IsManaged ? "" : "Start the web app. ") +
                          "Bridges pick up their stations' Radan folders from the new database when they reconnect.");
    }

    // ── migrate / drop ──────────────────────────────────────────────────────

    public async Task MigrateAsync(string name)
    {
        RequireEnvironmentName(name);
        if (!await _pg.ExistsAsync(name)) throw new UserError($"Database '{name}' doesn't exist.");
        await using var db = NewContext(name);
        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Count == 0) { Console.WriteLine($"{name} is up to date."); return; }
        await db.Database.MigrateAsync();
        Console.WriteLine($"Applied {pending.Count} migration(s) to {name}: {string.Join(", ", pending)}");
    }

    // ── set-radan-root ──────────────────────────────────────────────────────

    /// <summary>
    /// Sets the Radan projects folder of one station (by ID or name) or all of them, so an
    /// environment can work in Radan projects separate from another's. Null puts the station back
    /// on its bridge's own setting. A station whose folder changes loses its active project, since
    /// that project is in the old folder.
    /// </summary>
    public async Task SetRadanRootAsync(string name, string station, string? rootPath)
    {
        RequireEnvironmentName(name);
        if (!await _pg.ExistsAsync(name)) throw new UserError($"Database '{name}' doesn't exist.");
        rootPath = rootPath?.Trim().TrimEnd('\\', '/');
        if (rootPath == "") throw new UserError("Give a folder, or 'default' for the bridge's own setting.");

        await using var conn = await _pg.OpenAsync(name);
        var stations = new List<(int Id, string Name, string? Root)>();
        await using (var cmd = new NpgsqlCommand(
            "SELECT \"Id\", \"Name\", \"RadanProjectsRootPath\" FROM nesting_stations ORDER BY \"Id\"", conn))
        await using (var r = await cmd.ExecuteReaderAsync())
            while (await r.ReadAsync()) stations.Add((r.GetInt32(0), r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2)));
        if (stations.Count == 0) throw new UserError($"{name} has no nesting stations.");

        var targets = station.Equals("all", StringComparison.OrdinalIgnoreCase)
            ? stations
            : stations.Where(s => s.Id.ToString() == station || s.Name.Equals(station, StringComparison.OrdinalIgnoreCase)).ToList();
        if (targets.Count != 1 && !station.Equals("all", StringComparison.OrdinalIgnoreCase))
            throw new UserError((targets.Count == 0 ? $"No station '{station}' in {name}." : $"More than one station is named '{station}'; use its ID.") +
                                " Stations: " + string.Join(", ", stations.Select(s => $"{s.Id}: {s.Name}")));

        foreach (var s in targets)
        {
            if (string.Equals(s.Root, rootPath, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"  {s.Id} {s.Name}: already {rootPath ?? "(bridge's own setting)"}");
                continue;
            }
            await using var cmd = new NpgsqlCommand(
                "UPDATE nesting_stations SET \"RadanProjectsRootPath\" = @root, \"ProjectPath\" = NULL, \"ProjectName\" = NULL WHERE \"Id\" = @id", conn);
            cmd.Parameters.AddWithValue("root", (object?)rootPath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("id", s.Id);
            await cmd.ExecuteNonQueryAsync();
            Console.WriteLine($"  {s.Id} {s.Name}: {s.Root ?? "(bridge's own setting)"} → {rootPath ?? "(bridge's own setting)"}; active project cleared");
        }

        Console.WriteLine("The folder is on each station's PC (or a share it can reach); it isn't checked or created here.");
        if (name == _live)
            Console.WriteLine("This is the live database: connected bridges switch when they next connect. " +
                              "The Nesting Stations page switches them straight away.");
    }

    /// <summary>Deletes a non-live environment and its PDF folder, backing it up first.</summary>
    public async Task DropAsync(string name, bool backup)
    {
        RequireEnvironmentName(name);
        if (name == _live) throw new UserError("The live database can't be dropped. Swap another environment in first.");
        if (!await _pg.ExistsAsync(name)) throw new UserError($"Database '{name}' doesn't exist.");

        string pdfRoot;
        await using (var conn = await _pg.OpenAsync(name))
            pdfRoot = await GetConfigAsync(conn, LocalPdfRootKey) ?? DefaultPdfRoot(name);
        if (backup) await BackupAsync(name, label: "before_drop");

        if (await _pg.ConnectionCountAsync(name) > 0) await _pg.DisconnectAllAsync(name);
        await _pg.ExecuteAdminAsync($"DROP DATABASE \"{name}\"");
        Console.WriteLine($"Dropped {name}.");

        if (Directory.Exists(pdfRoot) && IsUnderPdfRoot(pdfRoot) && !await IsPdfRootOfAnotherEnvironment(pdfRoot, name))
        {
            Directory.Delete(pdfRoot, recursive: true);
            Console.WriteLine($"Deleted {pdfRoot}.");
        }
        else if (Directory.Exists(pdfRoot))
            Console.WriteLine($"Left {pdfRoot} in place (outside {_pdfRoot}, or another environment uses it).");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    public bool IsEnvironmentName(string name) =>
        name == _live || (name.StartsWith(_live + "_", StringComparison.Ordinal) && Regex.IsMatch(name, "^[a-z0-9_]+$"));

    private void RequireEnvironmentName(string name)
    {
        if (!Regex.IsMatch(name, "^[a-z][a-z0-9_]{0,62}$") || !IsEnvironmentName(name))
            throw new UserError($"'{name}' isn't an environment name. Use {_live} or {_live}_<lowercase letters, digits, _>.");
    }

    private string DefaultPdfRoot(string name) => Path.GetFullPath(Path.Combine(_pdfRoot, name));

    private bool IsUnderPdfRoot(string path)
    {
        var root = Path.GetFullPath(_pdfRoot).TrimEnd('\\') + "\\";
        var full = Path.GetFullPath(path).TrimEnd('\\') + "\\";
        return full.StartsWith(root, StringComparison.OrdinalIgnoreCase) && full.Length > root.Length;
    }

    private async Task<bool> IsPdfRootOfAnotherEnvironment(string pdfRoot, string except)
    {
        foreach (var other in (await _pg.ListDatabasesAsync()).Where(n => n != except && IsEnvironmentName(n)))
        {
            try
            {
                await using var conn = await _pg.OpenAsync(other);
                var root = await GetConfigAsync(conn, LocalPdfRootKey) ?? DefaultPdfRoot(other);
                if (string.Equals(Path.GetFullPath(root).TrimEnd('\\'), Path.GetFullPath(pdfRoot).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            catch (PostgresException) { }
        }
        return false;
    }

    private ApplicationDbContext NewContext(string name) => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_pg.ConnectionStringFor(name)).Options);

    private static async Task<int> RewritePdfFoldersAsync(NpgsqlConnection conn, string oldRoot, string newRoot)
    {
        oldRoot = oldRoot.TrimEnd('\\');
        newRoot = newRoot.TrimEnd('\\');
        var total = 0;
        foreach (var table in new[] { "batches", "schedules" })
        {
            await using var cmd = new NpgsqlCommand(
                $"UPDATE {table} SET \"LocalPdfFolder\" = @new || substr(\"LocalPdfFolder\", length(@old) + 1) " +
                "WHERE lower(left(\"LocalPdfFolder\", length(@old) + 1)) = lower(@old || '\\')", conn);
            cmd.Parameters.AddWithValue("old", oldRoot);
            cmd.Parameters.AddWithValue("new", newRoot);
            total += await cmd.ExecuteNonQueryAsync();
        }
        return total;
    }

    private static async Task DetachStationsAsync(NpgsqlConnection conn)
    {
        await ExecAsync(conn, "UPDATE nesting_stations SET \"ProjectPath\" = NULL, \"ProjectName\" = NULL, \"RadanProjectsRootPath\" = NULL");
        foreach (var table in new[] { "order_items", "batch_items" })
            await ExecAsync(conn, $"UPDATE {table} SET \"IsInRadanProject\" = false, \"NestingStationId\" = NULL, \"RadanIdNumber\" = NULL " +
                                  "WHERE \"IsInRadanProject\" OR \"NestingStationId\" IS NOT NULL");
    }

    private static async Task<string?> GetConfigAsync(NpgsqlConnection conn, string key)
    {
        if (!(await TablesAsync(conn)).Contains("system_configuration")) return null;
        await using var cmd = new NpgsqlCommand("SELECT \"Value\" FROM system_configuration WHERE \"Key\" = @k", conn);
        cmd.Parameters.AddWithValue("k", key);
        return await cmd.ExecuteScalarAsync() as string;
    }

    private static async Task SetConfigAsync(NpgsqlConnection conn, string key, string value)
    {
        await using var update = new NpgsqlCommand("UPDATE system_configuration SET \"Value\" = @v WHERE \"Key\" = @k", conn);
        update.Parameters.AddWithValue("k", key);
        update.Parameters.AddWithValue("v", value);
        if (await update.ExecuteNonQueryAsync() > 0) return;

        await using var insert = new NpgsqlCommand(
            "INSERT INTO system_configuration (\"Key\", \"Value\", \"Description\") VALUES (@k, @v, @d)", conn);
        insert.Parameters.AddWithValue("k", key);
        insert.Parameters.AddWithValue("v", value);
        insert.Parameters.AddWithValue("d", "Folder this database's local PDF copies are kept in");
        await insert.ExecuteNonQueryAsync();
    }

    private static async Task<string?> LastMigrationAsync(NpgsqlConnection conn)
    {
        if (!(await TablesAsync(conn)).Contains("__EFMigrationsHistory")) return null;
        await using var cmd = new NpgsqlCommand(
            "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\" DESC LIMIT 1", conn);
        return await cmd.ExecuteScalarAsync() as string;
    }

    private static async Task<List<(string Station, string? Root)>> StationRootsAsync(NpgsqlConnection conn)
    {
        var result = new List<(string, string?)>();
        if (!(await TablesAsync(conn)).Contains("nesting_stations")) return result;
        await using var cmd = new NpgsqlCommand(
            "SELECT \"Name\", \"RadanProjectsRootPath\" FROM nesting_stations ORDER BY \"Id\"", conn);
        try
        {
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) result.Add((r.GetString(0), r.IsDBNull(1) ? null : r.GetString(1)));
        }
        catch (PostgresException) { }   // a database from before stations had a Radan folder
        return result;
    }

    private static async Task<List<string>> TablesAsync(NpgsqlConnection conn)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE'", conn);
        var list = new List<string>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(r.GetString(0));
        return list;
    }

    private static async Task<List<(string Child, string Parent)>> ForeignKeysAsync(NpgsqlConnection conn)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT c.relname, p.relname FROM pg_constraint k " +
            "JOIN pg_class c ON c.oid = k.conrelid JOIN pg_class p ON p.oid = k.confrelid " +
            "JOIN pg_namespace n ON n.oid = c.relnamespace WHERE k.contype = 'f' AND n.nspname = 'public'", conn);
        var list = new List<(string, string)>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add((r.GetString(0), r.GetString(1)));
        return list;
    }

    private static async Task ExecAsync(NpgsqlConnection conn, string sql, NpgsqlTransaction? tx = null)
    {
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        await cmd.ExecuteNonQueryAsync();
    }

    private static long FolderSize(string path) =>
        Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length);

    /// <summary>Zips a folder with progress. Stored, not compressed: PDFs are already compressed,
    /// so deflating them again costs time and saves almost nothing.</summary>
    private static void ZipFolder(string source, string zipPath)
    {
        var files = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
        var progress = new CopyProgress("PDFs", files.Length, files.Sum(f => new FileInfo(f).Length));

        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            if (!Directory.EnumerateFileSystemEntries(dir).Any())
                zip.CreateEntry(EntryName(source, dir) + "/");

        foreach (var file in files)
        {
            var entry = zip.CreateEntry(EntryName(source, file), CompressionLevel.NoCompression);
            try { entry.LastWriteTime = File.GetLastWriteTime(file); }
            catch (ArgumentOutOfRangeException) { }   // zip dates run 1980–2107; keep the default
            // ReadWrite: the web app may be copying a PDF in while the backup runs.
            using (var input = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var output = entry.Open())
                progress.Copy(input, output);
            progress.FileDone();
        }
        progress.Finish();
    }

    /// <summary>Extracts a backup's PDF zip with progress. Reads compressed (older) backups too.</summary>
    private static void UnzipFolder(string zipPath, string target)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        var files = zip.Entries.Where(e => !e.FullName.EndsWith('/')).ToList();
        var progress = new CopyProgress("PDFs", files.Count, files.Sum(e => e.Length));

        var root = Path.GetFullPath(target) + Path.DirectorySeparatorChar;
        foreach (var entry in zip.Entries)
        {
            var path = Path.GetFullPath(Path.Combine(target, entry.FullName));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new UserError($"{zipPath} has an entry outside its folder: {entry.FullName}");
            if (entry.FullName.EndsWith('/')) { Directory.CreateDirectory(path); continue; }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using (var input = entry.Open())
            using (var output = File.Create(path))
                progress.Copy(input, output);
            File.SetLastWriteTime(path, entry.LastWriteTime.LocalDateTime);
            progress.FileDone();
        }
        progress.Finish();
    }

    private static string EntryName(string root, string path) =>
        Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');

    private record BackupManifest(string Database, DateTime CreatedUtc, string LocalPdfRoot, string? LastMigration, bool HasPdfs);
}

/// <summary>A problem to report to the person running the tool, without a stack trace.</summary>
public class UserError(string message) : Exception(message);
