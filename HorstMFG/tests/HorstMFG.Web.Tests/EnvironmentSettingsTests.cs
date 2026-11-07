using HorstMFG.Core.Entities;
using HorstMFG.Infrastructure.Data;
using HorstMFG.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HorstMFG.Web.Tests;

/// <summary>
/// The local PDF folder is pinned in the database so it doesn't follow the database's name when
/// databases are renamed to swap the live one.
/// </summary>
public class EnvironmentSettingsTests
{
    private static IConfiguration Config(string database) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = $"Host=localhost;Database={database};Username=x",
            ["FileSystemPaths:LocalPdfPath"] = @"C:\HorstMFG\PDFs\",
        })
        .Build();

    private static ApplicationDbContext NewDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task MissingSetting_IsPinnedToTheFolderUsedSoFar()
    {
        await using var db = NewDb();
        var settings = new EnvironmentSettings(Config("horstmfg"));

        await settings.LoadAsync(db);

        Assert.Equal(@"C:\HorstMFG\PDFs\horstmfg", settings.LocalPdfRoot);
        Assert.Equal(@"C:\HorstMFG\PDFs\horstmfg",
            (await db.SystemConfigurations.SingleAsync(c => c.Key == EnvironmentSettings.LocalPdfRootKey)).Value);
    }

    [Fact]
    public async Task PinnedSetting_SurvivesTheDatabaseBeingRenamed()
    {
        await using var db = NewDb();
        await new EnvironmentSettings(Config("horstmfg_staging")).LoadAsync(db);

        // The same database, now live under the name horstmfg.
        var afterSwap = new EnvironmentSettings(Config("horstmfg"));
        await afterSwap.LoadAsync(db);

        Assert.Equal(@"C:\HorstMFG\PDFs\horstmfg_staging", afterSwap.LocalPdfRoot);
    }

    [Fact]
    public async Task ExistingSetting_IsUsedAsIs()
    {
        await using var db = NewDb();
        db.SystemConfigurations.Add(new SystemConfiguration { Key = EnvironmentSettings.LocalPdfRootKey, Value = @"D:\Pdfs\Test" });
        await db.SaveChangesAsync();
        var settings = new EnvironmentSettings(Config("horstmfg"));

        await settings.LoadAsync(db);

        Assert.Equal(@"D:\Pdfs\Test", settings.LocalPdfRoot);
    }
}
