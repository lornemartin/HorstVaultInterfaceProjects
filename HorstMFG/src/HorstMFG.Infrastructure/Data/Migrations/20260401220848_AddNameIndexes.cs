using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HorstMFG.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNameIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_schedules_Name",
                table: "schedules",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_batches_Name",
                table: "batches",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_schedules_Name",
                table: "schedules");

            migrationBuilder.DropIndex(
                name: "IX_batches_Name",
                table: "batches");
        }
    }
}
