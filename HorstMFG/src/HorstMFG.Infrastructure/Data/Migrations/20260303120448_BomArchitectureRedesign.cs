using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HorstMFG.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class BomArchitectureRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_orders_bom_import_batches_BatchId",
                table: "orders");

            migrationBuilder.DropForeignKey(
                name: "FK_pdf_documents_bom_import_batches_BatchId",
                table: "pdf_documents");

            migrationBuilder.DropTable(
                name: "bom_line_items");

            migrationBuilder.DropTable(
                name: "bom_import_batches");

            migrationBuilder.DropIndex(
                name: "IX_pdf_documents_BatchId",
                table: "pdf_documents");

            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "pdf_documents");

            migrationBuilder.CreateTable(
                name: "batches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ImportDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlantId = table.Column<int>(type: "integer", nullable: false),
                    ImportedByUserId = table.Column<int>(type: "integer", nullable: false),
                    ReadyForProduction = table.Column<bool>(type: "boolean", nullable: false),
                    LocalPdfFolder = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_batches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_batches_plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_batches_users_ImportedByUserId",
                        column: x => x.ImportedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "schedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ImportDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlantId = table.Column<int>(type: "integer", nullable: false),
                    ImportedByUserId = table.Column<int>(type: "integer", nullable: false),
                    ReadyForProduction = table.Column<bool>(type: "boolean", nullable: false),
                    LocalPdfFolder = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_schedules_plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_schedules_users_ImportedByUserId",
                        column: x => x.ImportedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "batch_products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchId = table.Column<int>(type: "integer", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_batch_products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_batch_products_batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "schedule_orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ScheduleId = table.Column<int>(type: "integer", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_schedule_orders_schedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "part_line_items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PartNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Qty = table.Column<int>(type: "integer", nullable: false),
                    Material = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Thickness = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StructCode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Operations = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsStock = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresPdf = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    HasPdf = table.Column<bool>(type: "boolean", nullable: false),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    BatchProductId = table.Column<int>(type: "integer", nullable: true),
                    ScheduleOrderId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_part_line_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_part_line_items_batch_products_BatchProductId",
                        column: x => x.BatchProductId,
                        principalTable: "batch_products",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_part_line_items_schedule_orders_ScheduleOrderId",
                        column: x => x.ScheduleOrderId,
                        principalTable: "schedule_orders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_batch_products_BatchId",
                table: "batch_products",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_batches_ImportedByUserId",
                table: "batches",
                column: "ImportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_batches_PlantId",
                table: "batches",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_part_line_items_BatchProductId",
                table: "part_line_items",
                column: "BatchProductId");

            migrationBuilder.CreateIndex(
                name: "IX_part_line_items_ScheduleOrderId",
                table: "part_line_items",
                column: "ScheduleOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_orders_ScheduleId",
                table: "schedule_orders",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_schedules_ImportedByUserId",
                table: "schedules",
                column: "ImportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_schedules_PlantId",
                table: "schedules",
                column: "PlantId");

            migrationBuilder.AddForeignKey(
                name: "FK_orders_batches_BatchId",
                table: "orders",
                column: "BatchId",
                principalTable: "batches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_orders_batches_BatchId",
                table: "orders");

            migrationBuilder.DropTable(
                name: "part_line_items");

            migrationBuilder.DropTable(
                name: "batch_products");

            migrationBuilder.DropTable(
                name: "schedule_orders");

            migrationBuilder.DropTable(
                name: "batches");

            migrationBuilder.DropTable(
                name: "schedules");

            migrationBuilder.AddColumn<int>(
                name: "BatchId",
                table: "pdf_documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "bom_import_batches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ImportedByUserId = table.Column<int>(type: "integer", nullable: false),
                    PlantId = table.Column<int>(type: "integer", nullable: false),
                    BomType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FinalizedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ImportDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsFinalized = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_import_batches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bom_import_batches_plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bom_import_batches_users_ImportedByUserId",
                        column: x => x.ImportedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bom_line_items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchId = table.Column<int>(type: "integer", nullable: false),
                    ParentId = table.Column<int>(type: "integer", nullable: true),
                    PartId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    HasPdf = table.Column<bool>(type: "boolean", nullable: false),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ParentNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RequiresPdf = table.Column<bool>(type: "boolean", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    UnitQty = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bom_line_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bom_line_items_bom_import_batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "bom_import_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bom_line_items_bom_line_items_ParentId",
                        column: x => x.ParentId,
                        principalTable: "bom_line_items",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_bom_line_items_parts_PartId",
                        column: x => x.PartId,
                        principalTable: "parts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pdf_documents_BatchId",
                table: "pdf_documents",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_bom_import_batches_ImportedByUserId",
                table: "bom_import_batches",
                column: "ImportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_bom_import_batches_PlantId",
                table: "bom_import_batches",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_bom_line_items_BatchId",
                table: "bom_line_items",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_bom_line_items_ParentId",
                table: "bom_line_items",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_bom_line_items_PartId",
                table: "bom_line_items",
                column: "PartId");

            migrationBuilder.AddForeignKey(
                name: "FK_orders_bom_import_batches_BatchId",
                table: "orders",
                column: "BatchId",
                principalTable: "bom_import_batches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_pdf_documents_bom_import_batches_BatchId",
                table: "pdf_documents",
                column: "BatchId",
                principalTable: "bom_import_batches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
