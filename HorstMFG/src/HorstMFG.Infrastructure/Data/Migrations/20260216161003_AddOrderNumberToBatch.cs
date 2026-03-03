using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HorstMFG.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderNumberToBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrderNumber",
                table: "bom_import_batches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderNumber",
                table: "bom_import_batches");
        }
    }
}
