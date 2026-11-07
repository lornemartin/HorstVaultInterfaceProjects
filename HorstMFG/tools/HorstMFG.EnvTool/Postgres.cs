using System.Diagnostics;
using Npgsql;

namespace HorstMFG.EnvTool;

/// <summary>Server-level PostgreSQL operations, and running pg_dump / pg_restore.</summary>
public class Postgres
{
    private readonly NpgsqlConnectionStringBuilder _base;
    private readonly string _pgBinPath;

    public Postgres(string connectionString, string pgBinPath)
    {
        _base = new NpgsqlConnectionStringBuilder(connectionString);
        _pgBinPath = pgBinPath;
    }

    public string ConnectionStringFor(string database) =>
        new NpgsqlConnectionStringBuilder(_base.ConnectionString) { Database = database, Pooling = false }.ConnectionString;

    public async Task<NpgsqlConnection> OpenAsync(string database)
    {
        var conn = new NpgsqlConnection(ConnectionStringFor(database));
        await conn.OpenAsync();
        return conn;
    }

    public async Task<List<string>> ListDatabasesAsync()
    {
        await using var conn = await OpenAsync("postgres");
        await using var cmd = new NpgsqlCommand("SELECT datname FROM pg_database WHERE NOT datistemplate ORDER BY datname", conn);
        var names = new List<string>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) names.Add(r.GetString(0));
        return names;
    }

    public async Task<bool> ExistsAsync(string database) =>
        (await ListDatabasesAsync()).Contains(database);

    public async Task<long> SizeAsync(string database)
    {
        await using var conn = await OpenAsync("postgres");
        await using var cmd = new NpgsqlCommand("SELECT pg_database_size(@n)", conn);
        cmd.Parameters.AddWithValue("n", database);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    /// <summary>Sessions connected to the database, other than this tool's own.</summary>
    public async Task<int> ConnectionCountAsync(string database)
    {
        await using var conn = await OpenAsync("postgres");
        await using var cmd = new NpgsqlCommand(
            "SELECT count(*) FROM pg_stat_activity WHERE datname = @n AND pid <> pg_backend_pid()", conn);
        cmd.Parameters.AddWithValue("n", database);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task DisconnectAllAsync(string database)
    {
        await using var conn = await OpenAsync("postgres");
        await using var cmd = new NpgsqlCommand(
            "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @n AND pid <> pg_backend_pid()", conn);
        cmd.Parameters.AddWithValue("n", database);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>Runs server-level SQL (CREATE/ALTER/DROP DATABASE). Callers only pass names that
    /// passed <see cref="EnvironmentManager"/>'s name check, so quoting them is enough.</summary>
    public async Task ExecuteAdminAsync(string sql)
    {
        await using var conn = await OpenAsync("postgres");
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    public Task DumpAsync(string database, string file) =>
        RunToolAsync("pg_dump", $"{ConnectionArgs(database)} --format=custom --file=\"{file}\"");

    public Task RestoreAsync(string database, string file) =>
        RunToolAsync("pg_restore", $"{ConnectionArgs(database)} --no-owner --no-privileges \"{file}\"");

    private string ConnectionArgs(string database) =>
        $"--host={_base.Host} --port={_base.Port} --username={_base.Username} --dbname={database} --no-password";

    private async Task RunToolAsync(string tool, string arguments)
    {
        var exe = Path.Combine(_pgBinPath, tool + ".exe");
        if (!File.Exists(exe)) throw new FileNotFoundException($"{tool} not found in PgBinPath ({_pgBinPath}).", exe);

        var psi = new ProcessStartInfo(exe, arguments)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        // Password goes through the environment, never the command line.
        if (!string.IsNullOrEmpty(_base.Password)) psi.Environment["PGPASSWORD"] = _base.Password;

        using var process = Process.Start(psi)!;
        var stderr = process.StandardError.ReadToEndAsync();
        var stdout = process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"{tool} failed (exit {process.ExitCode}):\n{(await stderr).Trim()}\n{(await stdout).Trim()}".Trim());
    }
}
