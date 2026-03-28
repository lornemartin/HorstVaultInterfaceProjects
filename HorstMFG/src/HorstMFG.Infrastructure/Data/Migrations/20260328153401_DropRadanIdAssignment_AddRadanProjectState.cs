using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HorstMFG.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropRadanIdAssignment_AddRadanProjectState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "radan_id_assignments");

            migrationBuilder.CreateTable(
                name: "radan_project_states",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlantId = table.Column<int>(type: "integer", nullable: false),
                    ProjectName = table.Column<string>(type: "text", nullable: true),
                    ProjectPath = table.Column<string>(type: "text", nullable: true),
                    BridgeLastSeen = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BridgeVersion = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_radan_project_states", x => x.Id);
                    table.ForeignKey(
                        name: "FK_radan_project_states_plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_radan_project_states_PlantId",
                table: "radan_project_states",
                column: "PlantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "radan_project_states");

            migrationBuilder.CreateTable(
                name: "radan_id_assignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchItemId = table.Column<int>(type: "integer", nullable: true),
                    OrderItemId = table.Column<int>(type: "integer", nullable: true),
                    PlantId = table.Column<int>(type: "integer", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsReleased = table.Column<bool>(type: "boolean", nullable: false),
                    RadanIdNumber = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_radan_id_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_radan_id_assignments_batch_items_BatchItemId",
                        column: x => x.BatchItemId,
                        principalTable: "batch_items",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_radan_id_assignments_order_items_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "order_items",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_radan_id_assignments_plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_radan_id_assignments_BatchItemId",
                table: "radan_id_assignments",
                column: "BatchItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_radan_id_assignments_OrderItemId",
                table: "radan_id_assignments",
                column: "OrderItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_radan_id_assignments_PlantId",
                table: "radan_id_assignments",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_radan_id_assignments_RadanIdNumber_PlantId",
                table: "radan_id_assignments",
                columns: new[] { "RadanIdNumber", "PlantId" },
                unique: true);
        }
    }
}
