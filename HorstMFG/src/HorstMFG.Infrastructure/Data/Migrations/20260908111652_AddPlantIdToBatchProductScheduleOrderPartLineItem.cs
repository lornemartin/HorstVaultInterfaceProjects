using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HorstMFG.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlantIdToBatchProductScheduleOrderPartLineItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlantId",
                table: "schedule_orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlantId",
                table: "part_line_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlantIdRaw",
                table: "part_line_items",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlantId",
                table: "batch_products",
                type: "integer",
                nullable: true);

            // Backfill existing rows from their parent Batch/Schedule — a simple, always-correct
            // copy (unlike part_line_items.PlantId, which needs Vault's per-line data that can't
            // be recovered for already-imported rows, so it's intentionally left null here).
            migrationBuilder.Sql(
                "UPDATE batch_products bp SET \"PlantId\" = b.\"PlantId\" FROM batches b WHERE b.\"Id\" = bp.\"BatchId\";");
            migrationBuilder.Sql(
                "UPDATE schedule_orders so SET \"PlantId\" = s.\"PlantId\" FROM schedules s WHERE s.\"Id\" = so.\"ScheduleId\";");

            migrationBuilder.AlterColumn<int>(
                name: "PlantId",
                table: "batch_products",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PlantId",
                table: "schedule_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_schedule_orders_PlantId",
                table: "schedule_orders",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_part_line_items_PlantId",
                table: "part_line_items",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_batch_products_PlantId",
                table: "batch_products",
                column: "PlantId");

            migrationBuilder.AddForeignKey(
                name: "FK_batch_products_plants_PlantId",
                table: "batch_products",
                column: "PlantId",
                principalTable: "plants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_part_line_items_plants_PlantId",
                table: "part_line_items",
                column: "PlantId",
                principalTable: "plants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_schedule_orders_plants_PlantId",
                table: "schedule_orders",
                column: "PlantId",
                principalTable: "plants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_batch_products_plants_PlantId",
                table: "batch_products");

            migrationBuilder.DropForeignKey(
                name: "FK_part_line_items_plants_PlantId",
                table: "part_line_items");

            migrationBuilder.DropForeignKey(
                name: "FK_schedule_orders_plants_PlantId",
                table: "schedule_orders");

            migrationBuilder.DropIndex(
                name: "IX_schedule_orders_PlantId",
                table: "schedule_orders");

            migrationBuilder.DropIndex(
                name: "IX_part_line_items_PlantId",
                table: "part_line_items");

            migrationBuilder.DropIndex(
                name: "IX_batch_products_PlantId",
                table: "batch_products");

            migrationBuilder.DropColumn(
                name: "PlantId",
                table: "schedule_orders");

            migrationBuilder.DropColumn(
                name: "PlantId",
                table: "part_line_items");

            migrationBuilder.DropColumn(
                name: "PlantIdRaw",
                table: "part_line_items");

            migrationBuilder.DropColumn(
                name: "PlantId",
                table: "batch_products");
        }
    }
}
