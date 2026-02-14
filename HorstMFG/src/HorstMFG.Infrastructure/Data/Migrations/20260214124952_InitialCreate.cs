using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HorstMFG.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "parts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Material = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Thickness = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    StructCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Operations = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HasBends = table.Column<bool>(type: "boolean", nullable: false),
                    IsStock = table.Column<bool>(type: "boolean", nullable: false),
                    Keywords = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LifecycleState = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Thumbnail = table.Column<byte[]>(type: "bytea", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "plants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "system_configuration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_configuration", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "nests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NestName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NestPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Thumbnail = table.Column<byte[]>(type: "bytea", nullable: true),
                    PlantId = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_nests_plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PlantId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_users_plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bom_import_batches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ImportDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BomType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PlantId = table.Column<int>(type: "integer", nullable: false),
                    IsFinalized = table.Column<bool>(type: "boolean", nullable: false),
                    FinalizedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ImportedByUserId = table.Column<int>(type: "integer", nullable: false)
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
                name: "user_roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    RoleName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_roles_users_UserId",
                        column: x => x.UserId,
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
                    Number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ParentNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UnitQty = table.Column<int>(type: "integer", nullable: false),
                    RequiresPdf = table.Column<bool>(type: "boolean", nullable: false),
                    HasPdf = table.Column<bool>(type: "boolean", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BatchId = table.Column<int>(type: "integer", nullable: true),
                    PlantId = table.Column<int>(type: "integer", nullable: false),
                    EntryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsComplete = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProductNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_orders_bom_import_batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "bom_import_batches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_orders_plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pdf_documents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PartId = table.Column<int>(type: "integer", nullable: false),
                    BatchId = table.Column<int>(type: "integer", nullable: false),
                    Department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    GeneratedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pdf_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pdf_documents_bom_import_batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "bom_import_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_pdf_documents_parts_PartId",
                        column: x => x.PartId,
                        principalTable: "parts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    PartId = table.Column<int>(type: "integer", nullable: false),
                    QtyRequired = table.Column<int>(type: "integer", nullable: false),
                    QtyNested = table.Column<int>(type: "integer", nullable: false),
                    IsComplete = table.Column<bool>(type: "boolean", nullable: false),
                    IsInRadanProject = table.Column<bool>(type: "boolean", nullable: false),
                    RadanIdNumber = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_items_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_items_parts_PartId",
                        column: x => x.PartId,
                        principalTable: "parts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "nested_parts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NestId = table.Column<int>(type: "integer", nullable: false),
                    OrderItemId = table.Column<int>(type: "integer", nullable: false),
                    Qty = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nested_parts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_nested_parts_nests_NestId",
                        column: x => x.NestId,
                        principalTable: "nests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_nested_parts_order_items_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "order_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "radan_id_assignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RadanIdNumber = table.Column<int>(type: "integer", nullable: false),
                    OrderItemId = table.Column<int>(type: "integer", nullable: false),
                    PlantId = table.Column<int>(type: "integer", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsReleased = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_radan_id_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_radan_id_assignments_order_items_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "order_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_radan_id_assignments_plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_nested_parts_NestId",
                table: "nested_parts",
                column: "NestId");

            migrationBuilder.CreateIndex(
                name: "IX_nested_parts_OrderItemId",
                table: "nested_parts",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_nests_PlantId",
                table: "nests",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_OrderId",
                table: "order_items",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_PartId",
                table: "order_items",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_BatchId",
                table: "orders",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_OrderNumber",
                table: "orders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_PlantId",
                table: "orders",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_parts_Number",
                table: "parts",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pdf_documents_BatchId",
                table: "pdf_documents",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_pdf_documents_PartId",
                table: "pdf_documents",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "IX_plants_Code",
                table: "plants",
                column: "Code",
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

            migrationBuilder.CreateIndex(
                name: "IX_system_configuration_Key",
                table: "system_configuration",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_UserId_RoleName",
                table: "user_roles",
                columns: new[] { "UserId", "RoleName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_PlantId",
                table: "users",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_users_UserName",
                table: "users",
                column: "UserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bom_line_items");

            migrationBuilder.DropTable(
                name: "nested_parts");

            migrationBuilder.DropTable(
                name: "pdf_documents");

            migrationBuilder.DropTable(
                name: "radan_id_assignments");

            migrationBuilder.DropTable(
                name: "system_configuration");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "nests");

            migrationBuilder.DropTable(
                name: "order_items");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "parts");

            migrationBuilder.DropTable(
                name: "bom_import_batches");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "plants");
        }
    }
}
