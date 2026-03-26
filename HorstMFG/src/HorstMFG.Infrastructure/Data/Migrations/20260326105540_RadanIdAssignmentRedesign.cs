using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HorstMFG.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RadanIdAssignmentRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_batch_items_radan_id_assignments_RadanIdAssignmentId",
                table: "batch_items");

            migrationBuilder.DropForeignKey(
                name: "FK_radan_id_assignments_order_items_OrderItemId",
                table: "radan_id_assignments");

            migrationBuilder.DropIndex(
                name: "IX_batch_items_RadanIdAssignmentId",
                table: "batch_items");

            migrationBuilder.DropColumn(
                name: "RadanIdAssignmentId",
                table: "batch_items");

            migrationBuilder.AlterColumn<int>(
                name: "OrderItemId",
                table: "radan_id_assignments",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "BatchItemId",
                table: "radan_id_assignments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_radan_id_assignments_BatchItemId",
                table: "radan_id_assignments",
                column: "BatchItemId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_radan_id_assignments_batch_items_BatchItemId",
                table: "radan_id_assignments",
                column: "BatchItemId",
                principalTable: "batch_items",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_radan_id_assignments_order_items_OrderItemId",
                table: "radan_id_assignments",
                column: "OrderItemId",
                principalTable: "order_items",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_radan_id_assignments_batch_items_BatchItemId",
                table: "radan_id_assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_radan_id_assignments_order_items_OrderItemId",
                table: "radan_id_assignments");

            migrationBuilder.DropIndex(
                name: "IX_radan_id_assignments_BatchItemId",
                table: "radan_id_assignments");

            migrationBuilder.DropColumn(
                name: "BatchItemId",
                table: "radan_id_assignments");

            migrationBuilder.AlterColumn<int>(
                name: "OrderItemId",
                table: "radan_id_assignments",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RadanIdAssignmentId",
                table: "batch_items",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_batch_items_RadanIdAssignmentId",
                table: "batch_items",
                column: "RadanIdAssignmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_batch_items_radan_id_assignments_RadanIdAssignmentId",
                table: "batch_items",
                column: "RadanIdAssignmentId",
                principalTable: "radan_id_assignments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_radan_id_assignments_order_items_OrderItemId",
                table: "radan_id_assignments",
                column: "OrderItemId",
                principalTable: "order_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
