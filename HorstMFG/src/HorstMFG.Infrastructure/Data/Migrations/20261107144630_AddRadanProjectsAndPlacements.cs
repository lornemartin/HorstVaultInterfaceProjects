using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HorstMFG.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRadanProjectsAndPlacements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RadanProjectId",
                table: "nests",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "radan_projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlantId = table.Column<int>(type: "integer", nullable: false),
                    ProjectDir = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RpdPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProjectName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinalizedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_radan_projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_radan_projects_plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "radan_placements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RadanProjectId = table.Column<int>(type: "integer", nullable: false),
                    RadanIdNumber = table.Column<int>(type: "integer", nullable: false),
                    OrderItemId = table.Column<int>(type: "integer", nullable: true),
                    BatchItemId = table.Column<int>(type: "integer", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_radan_placements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_radan_placements_batch_items_BatchItemId",
                        column: x => x.BatchItemId,
                        principalTable: "batch_items",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_radan_placements_order_items_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "order_items",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_radan_placements_radan_projects_RadanProjectId",
                        column: x => x.RadanProjectId,
                        principalTable: "radan_projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_nests_RadanProjectId_NestName",
                table: "nests",
                columns: new[] { "RadanProjectId", "NestName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_radan_placements_BatchItemId",
                table: "radan_placements",
                column: "BatchItemId");

            migrationBuilder.CreateIndex(
                name: "IX_radan_placements_OrderItemId",
                table: "radan_placements",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_radan_placements_RadanProjectId_RadanIdNumber",
                table: "radan_placements",
                columns: new[] { "RadanProjectId", "RadanIdNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_radan_projects_PlantId_ProjectDir",
                table: "radan_projects",
                columns: new[] { "PlantId", "ProjectDir" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_nests_radan_projects_RadanProjectId",
                table: "nests",
                column: "RadanProjectId",
                principalTable: "radan_projects",
                principalColumn: "Id");

            // ── Backfill from existing data ──────────────────────────────────────
            // HorstMFG nests store <project>\nests\<n>.drg; nests imported from RadanMaster store the
            // project folder itself with a "<n>.drg" name. Every project found that way starts out
            // Finalized (or Legacy), then each station's current project is marked Open.
            migrationBuilder.Sql(@"
INSERT INTO radan_projects (""PlantId"", ""ProjectDir"", ""RpdPath"", ""ProjectName"", ""Status"", ""CreatedDate"")
SELECT DISTINCT n.""PlantId"", d.dir, d.dir || '\' || d.name || '.rpd', d.name, 'Finalized', now()
FROM nests n
CROSS JOIN LATERAL (SELECT regexp_replace(n.""NestPath"", '\\nests\\[^\\]+$', '', 'i') AS dir) x
CROSS JOIN LATERAL (SELECT x.dir AS dir, regexp_replace(x.dir, '^.*\\', '') AS name) d
WHERE n.""NestPath"" ~* '\\nests\\[^\\]+\.drg$'
ON CONFLICT (""PlantId"", ""ProjectDir"") DO NOTHING;

INSERT INTO radan_projects (""PlantId"", ""ProjectDir"", ""RpdPath"", ""ProjectName"", ""Status"", ""CreatedDate"")
SELECT DISTINCT n.""PlantId"", n.""NestPath"", NULL, regexp_replace(n.""NestPath"", '^.*\\', ''), 'Legacy', now()
FROM nests n
WHERE n.""NestPath"" IS NOT NULL AND n.""NestPath"" !~* '\.drg$'
ON CONFLICT (""PlantId"", ""ProjectDir"") DO NOTHING;

INSERT INTO radan_projects (""PlantId"", ""ProjectDir"", ""RpdPath"", ""ProjectName"", ""Status"", ""CreatedDate"")
SELECT s.""PlantId"", regexp_replace(s.""ProjectPath"", '\\[^\\]+$', ''), s.""ProjectPath"",
       regexp_replace(regexp_replace(s.""ProjectPath"", '^.*\\', ''), '\.rpd$', '', 'i'), 'Open', now()
FROM nesting_stations s
WHERE COALESCE(s.""ProjectPath"", '') <> ''
ON CONFLICT (""PlantId"", ""ProjectDir"") DO UPDATE SET ""Status"" = 'Open', ""RpdPath"" = EXCLUDED.""RpdPath"";

UPDATE nests n SET ""RadanProjectId"" = p.""Id""
FROM radan_projects p
WHERE p.""PlantId"" = n.""PlantId""
  AND p.""ProjectDir"" = CASE WHEN n.""NestPath"" ~* '\\nests\\[^\\]+\.drg$'
                             THEN regexp_replace(n.""NestPath"", '\\nests\\[^\\]+$', '', 'i')
                             ELSE n.""NestPath"" END;
");

            // Placements: items currently in a station's project, then finalized items whose Radan ID
            // can only belong to one project (they kept it, and every nest they're on is in that project).
            foreach (var (table, column) in new[] { ("order_items", "OrderItemId"), ("batch_items", "BatchItemId") })
            {
                migrationBuilder.Sql($@"
INSERT INTO radan_placements (""RadanProjectId"", ""RadanIdNumber"", ""{column}"", ""CreatedDate"")
SELECT p.""Id"", i.""RadanIdNumber"", i.""Id"", now()
FROM {table} i
JOIN nesting_stations s ON s.""Id"" = i.""NestingStationId""
JOIN radan_projects p ON p.""PlantId"" = s.""PlantId"" AND p.""ProjectDir"" = regexp_replace(s.""ProjectPath"", '\\[^\\]+$', '')
WHERE i.""IsInRadanProject"" AND i.""RadanIdNumber"" IS NOT NULL
ON CONFLICT (""RadanProjectId"", ""RadanIdNumber"") DO NOTHING;

INSERT INTO radan_placements (""RadanProjectId"", ""RadanIdNumber"", ""{column}"", ""CreatedDate"")
SELECT MIN(n.""RadanProjectId""), i.""RadanIdNumber"", i.""Id"", now()
FROM {table} i
JOIN nested_parts np ON np.""{column}"" = i.""Id""
JOIN nests n ON n.""Id"" = np.""NestId""
JOIN radan_projects p ON p.""Id"" = n.""RadanProjectId"" AND p.""Status"" <> 'Legacy'
WHERE NOT i.""IsInRadanProject"" AND i.""RadanIdNumber"" IS NOT NULL
GROUP BY i.""Id"", i.""RadanIdNumber""
HAVING COUNT(DISTINCT n.""RadanProjectId"") = 1
ON CONFLICT (""RadanProjectId"", ""RadanIdNumber"") DO NOTHING;
");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_nests_radan_projects_RadanProjectId",
                table: "nests");

            migrationBuilder.DropTable(
                name: "radan_placements");

            migrationBuilder.DropTable(
                name: "radan_projects");

            migrationBuilder.DropIndex(
                name: "IX_nests_RadanProjectId_NestName",
                table: "nests");

            migrationBuilder.DropColumn(
                name: "RadanProjectId",
                table: "nests");
        }
    }
}
