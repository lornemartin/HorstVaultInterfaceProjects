using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HorstMFG.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropIsProcessedPascal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Column was created with PascalCase name by InitialCreate before snake_case naming was configured.
            migrationBuilder.Sql("""ALTER TABLE part_line_items DROP COLUMN IF EXISTS "IsProcessed";""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""ALTER TABLE part_line_items ADD COLUMN IF NOT EXISTS "IsProcessed" boolean NOT NULL DEFAULT false;""");
        }
    }
}
