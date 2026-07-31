using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HorstMFG.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLastImportErrorToBatchProductAndScheduleOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastImportError",
                table: "schedule_orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastImportError",
                table: "batch_products",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastImportError",
                table: "schedule_orders");

            migrationBuilder.DropColumn(
                name: "LastImportError",
                table: "batch_products");
        }
    }
}
