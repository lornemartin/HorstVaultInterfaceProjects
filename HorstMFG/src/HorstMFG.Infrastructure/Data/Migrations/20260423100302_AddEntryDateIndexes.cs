using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HorstMFG.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEntryDateIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_nest_orders_entry_date ON nest_orders (\"EntryDate\")");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_nest_batches_entry_date ON nest_batches (\"EntryDate\")");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_nest_orders_entry_date");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_nest_batches_entry_date");

        }
    }
}
