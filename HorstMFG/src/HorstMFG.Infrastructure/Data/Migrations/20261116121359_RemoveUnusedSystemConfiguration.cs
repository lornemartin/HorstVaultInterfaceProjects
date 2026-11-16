using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HorstMFG.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedSystemConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seeded at project start but never read; the real values live in appsettings.
            migrationBuilder.Sql("""
                DELETE FROM system_configuration
                WHERE "Key" IN ('GhostscriptPath', 'PdfOutputPath', 'VaultServer');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO system_configuration ("Key", "Value", "Description") VALUES
                    ('GhostscriptPath', 'C:\Program Files\gs\gs9.21\bin\', 'Path to Ghostscript binaries'),
                    ('PdfOutputPath',   'C:\HorstMFG\PDFs\',               'Default PDF output directory'),
                    ('VaultServer',     'localhost',                       'Autodesk Vault server address')
                ON CONFLICT ("Key") DO NOTHING;
                """);
        }
    }
}
