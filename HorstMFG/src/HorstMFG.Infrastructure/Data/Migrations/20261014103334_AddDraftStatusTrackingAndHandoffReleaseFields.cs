using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HorstMFG.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDraftStatusTrackingAndHandoffReleaseFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReleasedByUserId",
                table: "schedule_order_handoffs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReleasedDate",
                table: "schedule_order_handoffs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReleasedByUserId",
                table: "batch_product_handoffs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReleasedDate",
                table: "batch_product_handoffs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_schedule_order_handoffs_ReleasedByUserId",
                table: "schedule_order_handoffs",
                column: "ReleasedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_batch_product_handoffs_ReleasedByUserId",
                table: "batch_product_handoffs",
                column: "ReleasedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_batch_product_handoffs_users_ReleasedByUserId",
                table: "batch_product_handoffs",
                column: "ReleasedByUserId",
                principalTable: "users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_schedule_order_handoffs_users_ReleasedByUserId",
                table: "schedule_order_handoffs",
                column: "ReleasedByUserId",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_batch_product_handoffs_users_ReleasedByUserId",
                table: "batch_product_handoffs");

            migrationBuilder.DropForeignKey(
                name: "FK_schedule_order_handoffs_users_ReleasedByUserId",
                table: "schedule_order_handoffs");

            migrationBuilder.DropIndex(
                name: "IX_schedule_order_handoffs_ReleasedByUserId",
                table: "schedule_order_handoffs");

            migrationBuilder.DropIndex(
                name: "IX_batch_product_handoffs_ReleasedByUserId",
                table: "batch_product_handoffs");

            migrationBuilder.DropColumn(
                name: "ReleasedByUserId",
                table: "schedule_order_handoffs");

            migrationBuilder.DropColumn(
                name: "ReleasedDate",
                table: "schedule_order_handoffs");

            migrationBuilder.DropColumn(
                name: "ReleasedByUserId",
                table: "batch_product_handoffs");

            migrationBuilder.DropColumn(
                name: "ReleasedDate",
                table: "batch_product_handoffs");
        }
    }
}
