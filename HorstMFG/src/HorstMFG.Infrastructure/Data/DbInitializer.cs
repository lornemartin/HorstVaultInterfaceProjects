using HorstMFG.Core.Entities;
using HorstMFG.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HorstMFG.Infrastructure.Data;

public class DbInitializer
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(ApplicationDbContext context, ILogger<DbInitializer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        await _context.Database.MigrateAsync();

        if (await _context.Plants.AnyAsync())
        {
            _logger.LogInformation("Database already seeded.");
            return;
        }

        _logger.LogInformation("Seeding database...");

        // Plants
        var plant1 = new Plant { Name = "Plant 1 - Main", Code = "P1" };
        var plant2 = new Plant { Name = "Plant 2", Code = "P2" };
        _context.Plants.AddRange(plant1, plant2);
        await _context.SaveChangesAsync();

        // Admin user (password: admin123)
        var admin = new ApplicationUser
        {
            UserName = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            FullName = "System Administrator",
            PlantId = plant1.Id
        };
        _context.Users.Add(admin);
        await _context.SaveChangesAsync();

        // Admin role
        _context.UserRoles.Add(new UserRole
        {
            UserId = admin.Id,
            RoleName = UserRoleType.Admin
        });
        await _context.SaveChangesAsync();

        // Default configuration values
        _context.SystemConfigurations.AddRange(
            new SystemConfiguration { Key = "GhostscriptPath", Value = @"C:\Program Files\gs\gs9.21\bin\", Description = "Path to Ghostscript binaries" },
            new SystemConfiguration { Key = "PdfOutputPath", Value = @"C:\HorstMFG\PDFs\", Description = "Default PDF output directory" },
            new SystemConfiguration { Key = "VaultServer", Value = "localhost", Description = "Autodesk Vault server address" }
        );
        await _context.SaveChangesAsync();

        _logger.LogInformation("Database seeded successfully.");
    }
}
