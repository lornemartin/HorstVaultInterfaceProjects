using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HorstMFG.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCrossPlantHandoffTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "batch_product_handoffs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchProductId = table.Column<int>(type: "integer", nullable: false),
                    DestinationPlantId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcceptedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcceptedByUserId = table.Column<int>(type: "integer", nullable: true),
                    RejectedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedByUserId = table.Column<int>(type: "integer", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_batch_product_handoffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_batch_product_handoffs_batch_products_BatchProductId",
                        column: x => x.BatchProductId,
                        principalTable: "batch_products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_batch_product_handoffs_plants_DestinationPlantId",
                        column: x => x.DestinationPlantId,
                        principalTable: "plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_batch_product_handoffs_users_AcceptedByUserId",
                        column: x => x.AcceptedByUserId,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_batch_product_handoffs_users_RejectedByUserId",
                        column: x => x.RejectedByUserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "schedule_order_handoffs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ScheduleOrderId = table.Column<int>(type: "integer", nullable: false),
                    DestinationPlantId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcceptedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcceptedByUserId = table.Column<int>(type: "integer", nullable: true),
                    RejectedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedByUserId = table.Column<int>(type: "integer", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule_order_handoffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_schedule_order_handoffs_plants_DestinationPlantId",
                        column: x => x.DestinationPlantId,
                        principalTable: "plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_schedule_order_handoffs_schedule_orders_ScheduleOrderId",
                        column: x => x.ScheduleOrderId,
                        principalTable: "schedule_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_schedule_order_handoffs_users_AcceptedByUserId",
                        column: x => x.AcceptedByUserId,
                        principalTable: "users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_schedule_order_handoffs_users_RejectedByUserId",
                        column: x => x.RejectedByUserId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_batch_product_handoffs_AcceptedByUserId",
                table: "batch_product_handoffs",
                column: "AcceptedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_batch_product_handoffs_BatchProductId",
                table: "batch_product_handoffs",
                column: "BatchProductId");

            migrationBuilder.CreateIndex(
                name: "IX_batch_product_handoffs_DestinationPlantId",
                table: "batch_product_handoffs",
                column: "DestinationPlantId");

            migrationBuilder.CreateIndex(
                name: "IX_batch_product_handoffs_RejectedByUserId",
                table: "batch_product_handoffs",
                column: "RejectedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_order_handoffs_AcceptedByUserId",
                table: "schedule_order_handoffs",
                column: "AcceptedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_order_handoffs_DestinationPlantId",
                table: "schedule_order_handoffs",
                column: "DestinationPlantId");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_order_handoffs_RejectedByUserId",
                table: "schedule_order_handoffs",
                column: "RejectedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_order_handoffs_ScheduleOrderId",
                table: "schedule_order_handoffs",
                column: "ScheduleOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "batch_product_handoffs");

            migrationBuilder.DropTable(
                name: "schedule_order_handoffs");
        }
    }
}
