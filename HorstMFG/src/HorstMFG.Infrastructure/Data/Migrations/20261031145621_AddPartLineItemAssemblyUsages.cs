using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HorstMFG.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPartLineItemAssemblyUsages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "part_line_item_assembly_usages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PartLineItemId = table.Column<int>(type: "integer", nullable: false),
                    AssemblyNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Qty = table.Column<int>(type: "integer", nullable: false),
                    DirectQty = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_part_line_item_assembly_usages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_part_line_item_assembly_usages_part_line_items_PartLineItem~",
                        column: x => x.PartLineItemId,
                        principalTable: "part_line_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_part_line_item_assembly_usages_PartLineItemId",
                table: "part_line_item_assembly_usages",
                column: "PartLineItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "part_line_item_assembly_usages");
        }
    }
}
