using HorstMFG.Core.Entities;
using HorstMFG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HorstMFG.Web.Services;

/// <summary>
/// Settings that belong to the database (the environment) rather than to the web server's
/// appsettings, so swapping which database is live — by renaming databases, with config untouched —
/// brings the right files along. Loaded once at startup; changing one needs an app restart.
/// </summary>
public class EnvironmentSettings
{
    /// <summary>system_configuration key holding this database's local PDF folder.</summary>
    public const string LocalPdfRootKey = "LocalPdfRoot";

    private readonly IConfiguration _config;

    public EnvironmentSettings(IConfiguration config)
    {
        _config = config;
        LocalPdfRoot = DefaultLocalPdfRoot();
    }

    /// <summary>
    /// Folder this database's PDF copies go in. Batches and schedules store their folder as an
    /// absolute path under it, so it must not follow the database's name once rows point into it.
    /// </summary>
    public string LocalPdfRoot { get; private set; }

    /// <summary>
    /// Reads this database's settings, first pinning any that are missing to the value the app has
    /// been deriving so far ({LocalPdfPath}\{DatabaseName}), so nothing moves for existing data.
    /// </summary>
    public async Task LoadAsync(ApplicationDbContext db)
    {
        var row = await db.SystemConfigurations.FirstOrDefaultAsync(c => c.Key == LocalPdfRootKey);
        if (row == null)
        {
            row = new SystemConfiguration
            {
                Key = LocalPdfRootKey,
                Value = DefaultLocalPdfRoot(),
                Description = "Folder this database's local PDF copies are kept in",
            };
            db.SystemConfigurations.Add(row);
            await db.SaveChangesAsync();
        }
        LocalPdfRoot = row.Value;
    }

    private string DefaultLocalPdfRoot() => Path.Combine(
        _config["FileSystemPaths:LocalPdfPath"] ?? @"C:\HorstMFG\PDFs\",
        BomPdfCopyService.ResolveDatabaseName(_config));
}
