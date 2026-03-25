using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HorstMFG.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class NestingRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_nested_parts_order_items_OrderItemId",
                table: "nested_parts");

            migrationBuilder.DropForeignKey(
                name: "FK_order_items_orders_OrderId",
                table: "order_items");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropIndex(
                name: "IX_parts_Number",
                table: "parts");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "parts");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "parts");

            migrationBuilder.DropColumn(
                name: "IsStock",
                table: "parts");

            migrationBuilder.DropColumn(
                name: "Keywords",
                table: "parts");

            migrationBuilder.DropColumn(
                name: "LifecycleState",
                table: "parts");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "parts");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "parts");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "parts");

            migrationBuilder.DropColumn(
                name: "Operations",
                table: "parts");

            migrationBuilder.DropColumn(
                name: "StructCode",
                table: "parts");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "parts");

            migrationBuilder.RenameColumn(
                name: "OrderId",
                table: "order_items",
                newName: "NestOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_order_items_OrderId",
                table: "order_items",
                newName: "IX_order_items_NestOrderId");

            migrationBuilder.AlterColumn<int>(
                name: "OrderItemId",
                table: "nested_parts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "BatchItemId",
                table: "nested_parts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "nest_batches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchId = table.Column<int>(type: "integer", nullable: false),
                    PlantId = table.Column<int>(type: "integer", nullable: false),
                    EntryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsComplete = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nest_batches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_nest_batches_batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_nest_batches_plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "nest_orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ScheduleOrderId = table.Column<int>(type: "integer", nullable: false),
                    PlantId = table.Column<int>(type: "integer", nullable: false),
                    EntryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsComplete = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nest_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_nest_orders_plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_nest_orders_schedule_orders_ScheduleOrderId",
                        column: x => x.ScheduleOrderId,
                        principalTable: "schedule_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "batch_items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NestBatchId = table.Column<int>(type: "integer", nullable: false),
                    PartId = table.Column<int>(type: "integer", nullable: false),
                    QtyRequired = table.Column<int>(type: "integer", nullable: false),
                    QtyNested = table.Column<int>(type: "integer", nullable: false),
                    IsComplete = table.Column<bool>(type: "boolean", nullable: false),
                    IsInRadanProject = table.Column<bool>(type: "boolean", nullable: false),
                    RadanIdNumber = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RadanIdAssignmentId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_batch_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_batch_items_nest_batches_NestBatchId",
                        column: x => x.NestBatchId,
                        principalTable: "nest_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_batch_items_parts_PartId",
                        column: x => x.PartId,
                        principalTable: "parts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_batch_items_radan_id_assignments_RadanIdAssignmentId",
                        column: x => x.RadanIdAssignmentId,
                        principalTable: "radan_id_assignments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_nested_parts_BatchItemId",
                table: "nested_parts",
                column: "BatchItemId");

            migrationBuilder.CreateIndex(
                name: "IX_batch_items_NestBatchId",
                table: "batch_items",
                column: "NestBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_batch_items_PartId",
                table: "batch_items",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "IX_batch_items_RadanIdAssignmentId",
                table: "batch_items",
                column: "RadanIdAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_nest_batches_BatchId",
                table: "nest_batches",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_nest_batches_PlantId",
                table: "nest_batches",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_nest_orders_PlantId",
                table: "nest_orders",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_nest_orders_ScheduleOrderId",
                table: "nest_orders",
                column: "ScheduleOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_nested_parts_batch_items_BatchItemId",
                table: "nested_parts",
                column: "BatchItemId",
                principalTable: "batch_items",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_nested_parts_order_items_OrderItemId",
                table: "nested_parts",
                column: "OrderItemId",
                principalTable: "order_items",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_order_items_nest_orders_NestOrderId",
                table: "order_items",
                column: "NestOrderId",
                principalTable: "nest_orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_nested_parts_batch_items_BatchItemId",
                table: "nested_parts");

            migrationBuilder.DropForeignKey(
                name: "FK_nested_parts_order_items_OrderItemId",
                table: "nested_parts");

            migrationBuilder.DropForeignKey(
                name: "FK_order_items_nest_orders_NestOrderId",
                table: "order_items");

            migrationBuilder.DropTable(
                name: "batch_items");

            migrationBuilder.DropTable(
                name: "nest_orders");

            migrationBuilder.DropTable(
                name: "nest_batches");

            migrationBuilder.DropIndex(
                name: "IX_nested_parts_BatchItemId",
                table: "nested_parts");

            migrationBuilder.DropColumn(
                name: "BatchItemId",
                table: "nested_parts");

            migrationBuilder.RenameColumn(
                name: "NestOrderId",
                table: "order_items",
                newName: "OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_order_items_NestOrderId",
                table: "order_items",
                newName: "IX_order_items_OrderId");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "parts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "parts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsStock",
                table: "parts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Keywords",
                table: "parts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LifecycleState",
                table: "parts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "parts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "parts",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Number",
                table: "parts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Operations",
                table: "parts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StructCode",
                table: "parts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "parts",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "OrderItemId",
                table: "nested_parts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchId = table.Column<int>(type: "integer", nullable: true),
                    PlantId = table.Column<int>(type: "integer", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EntryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsComplete = table.Column<bool>(type: "boolean", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProductNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_orders_batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "batches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_orders_plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_parts_Number",
                table: "parts",
                column: "Number",
                unique: true);

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

            migrationBuilder.AddForeignKey(
                name: "FK_nested_parts_order_items_OrderItemId",
                table: "nested_parts",
                column: "OrderItemId",
                principalTable: "order_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_order_items_orders_OrderId",
                table: "order_items",
                column: "OrderId",
                principalTable: "orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
