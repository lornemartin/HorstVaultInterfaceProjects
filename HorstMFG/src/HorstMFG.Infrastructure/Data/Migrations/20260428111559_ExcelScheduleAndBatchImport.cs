using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HorstMFG.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExcelScheduleAndBatchImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "schedule_orders",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductNumber",
                table: "schedule_orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "VaultBomImported",
                table: "schedule_orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "batch_products",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "VaultBomImported",
                table: "batch_products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"
                UPDATE schedule_orders so
                SET ""ProductNumber"" = (
                    SELECT pli.""PartNumber""
                    FROM part_line_items pli
                    WHERE pli.""ScheduleOrderId"" = so.""Id""
                      AND LOWER(pli.""Category"") = 'product'
                    LIMIT 1
                );
            ");

            migrationBuilder.Sql(@"
                UPDATE schedule_orders
                SET ""VaultBomImported"" = true
                WHERE ""ProductNumber"" IS NOT NULL;
            ");

            migrationBuilder.Sql(@"
                UPDATE batch_products
                SET ""VaultBomImported"" = true;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "schedule_orders");

            migrationBuilder.DropColumn(
                name: "ProductNumber",
                table: "schedule_orders");

            migrationBuilder.DropColumn(
                name: "VaultBomImported",
                table: "schedule_orders");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "batch_products");

            migrationBuilder.DropColumn(
                name: "VaultBomImported",
                table: "batch_products");
        }
    }
}
