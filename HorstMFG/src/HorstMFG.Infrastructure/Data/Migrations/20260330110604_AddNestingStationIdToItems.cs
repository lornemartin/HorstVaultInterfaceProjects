using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HorstMFG.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNestingStationIdToItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NestingStationId",
                table: "order_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NestingStationId",
                table: "batch_items",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_items_NestingStationId",
                table: "order_items",
                column: "NestingStationId");

            migrationBuilder.CreateIndex(
                name: "IX_batch_items_NestingStationId",
                table: "batch_items",
                column: "NestingStationId");

            migrationBuilder.AddForeignKey(
                name: "FK_batch_items_nesting_stations_NestingStationId",
                table: "batch_items",
                column: "NestingStationId",
                principalTable: "nesting_stations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_order_items_nesting_stations_NestingStationId",
                table: "order_items",
                column: "NestingStationId",
                principalTable: "nesting_stations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_batch_items_nesting_stations_NestingStationId",
                table: "batch_items");

            migrationBuilder.DropForeignKey(
                name: "FK_order_items_nesting_stations_NestingStationId",
                table: "order_items");

            migrationBuilder.DropIndex(
                name: "IX_order_items_NestingStationId",
                table: "order_items");

            migrationBuilder.DropIndex(
                name: "IX_batch_items_NestingStationId",
                table: "batch_items");

            migrationBuilder.DropColumn(
                name: "NestingStationId",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "NestingStationId",
                table: "batch_items");
        }
    }
}
