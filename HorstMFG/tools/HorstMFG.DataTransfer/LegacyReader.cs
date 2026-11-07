using System.Data;
using Microsoft.Data.SqlClient;

namespace HorstMFG.DataTransfer;

/// <summary>Reads the legacy RadanMaster SQL Server database. Read-only.</summary>
public class LegacyReader(string connectionString)
{
    public SqlConnection Open() => new(connectionString);

    /// <summary>The selected orders (see <see cref="LegacyTransform.SelectOrders"/>) and everything
    /// under them: their items and parts, the nested-part rows for those items and their nests.</summary>
    public async Task<SourceData> ReadAsync(DateTime? since)
    {
        await using var sql = Open();
        await sql.OpenAsync();

        var orders = LegacyTransform.SelectOrders(await ReadOrdersAsync(sql), since);
        var orderItems  = await ReadOrderItemsAsync(sql, orders.Select(o => o.Id).ToHashSet());
        var parts       = await ReadPartsAsync(sql, orderItems.Select(oi => oi.PartId).ToHashSet());
        var nestedParts = await ReadNestedPartsAsync(sql, orderItems.Select(oi => oi.Id).ToHashSet());
        var nests       = await ReadNestsAsync(sql, nestedParts.Select(np => np.NestId).ToHashSet());

        return new SourceData(parts, nests, orders, orderItems, nestedParts);
    }

    /// <summary>NestedParts rows whose order item no longer exists. Never transferred; reported so
    /// the totals don't look short.</summary>
    public async Task<int> CountOrphanNestedPartsAsync()
    {
        await using var sql = Open();
        await sql.OpenAsync();
        return await ScalarAsync(sql,
            "SELECT COUNT(*) FROM NestedParts np LEFT JOIN OrderItems oi ON oi.ID = np.OrderItem_ID WHERE oi.ID IS NULL");
    }

    public static async Task<int> ScalarAsync(SqlConnection sql, string query)
    {
        var cmd = sql.CreateCommand();
        cmd.CommandText = query;
        cmd.CommandTimeout = 120;
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    private static async Task<List<SrcOrder>> ReadOrdersAsync(SqlConnection sql)
    {
        var cmd = sql.CreateCommand();
        cmd.CommandText = "SELECT ID, OrderNumber, ScheduleName, BatchName, EntryDate, DueDate, IsComplete, IsBatch, DateCompleted FROM Orders";
        cmd.CommandTimeout = 120;

        var list = new List<SrcOrder>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new SrcOrder(
                Id:            r.GetInt32("ID"),
                OrderNumber:   r.IsDBNull("OrderNumber")   ? null : r.GetString("OrderNumber"),
                ScheduleName:  r.IsDBNull("ScheduleName")  ? null : r.GetString("ScheduleName"),
                BatchName:     r.IsDBNull("BatchName")     ? null : r.GetString("BatchName"),
                EntryDate:     r.GetDateTime("EntryDate"),
                DueDate:       r.IsDBNull("DueDate")       ? null : r.GetDateTime("DueDate"),
                IsComplete:    r.GetBoolean("IsComplete"),
                IsBatch:       r.GetBoolean("IsBatch"),
                DateCompleted: r.IsDBNull("DateCompleted") ? null : r.GetDateTime("DateCompleted")
            ));
        }
        return list;
    }

    private static async Task<List<SrcOrderItem>> ReadOrderItemsAsync(SqlConnection sql, HashSet<int> orderIds)
    {
        if (orderIds.Count == 0) return [];

        var cmd = sql.CreateCommand();
        cmd.CommandText = """
            SELECT ID, OrderID, IsComplete, QtyRequired, QtyNested, IsInProject, Notes, PartID, RadanIDNumber
            FROM OrderItems
            WHERE OrderID IN (SELECT value FROM STRING_SPLIT(@ids, ','))
            """;
        cmd.Parameters.AddWithValue("@ids", string.Join(',', orderIds));
        cmd.CommandTimeout = 120;

        var list = new List<SrcOrderItem>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new SrcOrderItem(
                Id:            r.GetInt32("ID"),
                OrderId:       r.GetInt32("OrderID"),
                IsComplete:    r.GetBoolean("IsComplete"),
                QtyRequired:   r.GetInt32("QtyRequired"),
                QtyNested:     r.GetInt32("QtyNested"),
                IsInProject:   r.GetBoolean("IsInProject"),
                Notes:         r.IsDBNull("Notes") ? null : r.GetString("Notes"),
                PartId:        r.GetInt32("PartID"),
                RadanIdNumber: r.GetInt32("RadanIDNumber")
            ));
        }
        return list;
    }

    private static async Task<List<SrcPart>> ReadPartsAsync(SqlConnection sql, HashSet<int> partIds)
    {
        if (partIds.Count == 0) return [];

        var cmd = sql.CreateCommand();
        cmd.CommandText = """
            SELECT ID, FileName, Description, Material, Thickness, Thumbnail, HasBends
            FROM Parts
            WHERE ID IN (SELECT value FROM STRING_SPLIT(@ids, ','))
            """;
        cmd.Parameters.AddWithValue("@ids", string.Join(',', partIds));
        cmd.CommandTimeout = 120;

        var list = new List<SrcPart>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new SrcPart(
                Id:          r.GetInt32("ID"),
                FileName:    r.IsDBNull("FileName")    ? null : r.GetString("FileName"),
                Description: r.IsDBNull("Description") ? null : r.GetString("Description"),
                Material:    r.IsDBNull("Material")    ? null : r.GetString("Material"),
                Thickness:   r.IsDBNull("Thickness")   ? null : r.GetDouble("Thickness"),
                Thumbnail:   r.IsDBNull("Thumbnail")   ? null : (byte[])r["Thumbnail"],
                HasBends:    r.GetBoolean("HasBends")
            ));
        }
        return list;
    }

    private static async Task<List<SrcNestedPart>> ReadNestedPartsAsync(SqlConnection sql, HashSet<int> orderItemIds)
    {
        if (orderItemIds.Count == 0) return [];

        var cmd = sql.CreateCommand();
        cmd.CommandText = """
            SELECT ID, Qty, Nest_ID, OrderItem_ID
            FROM NestedParts
            WHERE OrderItem_ID IN (SELECT value FROM STRING_SPLIT(@ids, ','))
            """;
        cmd.Parameters.AddWithValue("@ids", string.Join(',', orderItemIds));
        cmd.CommandTimeout = 120;

        var list = new List<SrcNestedPart>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new SrcNestedPart(
                Id:          r.GetInt32("ID"),
                Qty:         r.GetInt32("Qty"),
                NestId:      r.IsDBNull("Nest_ID")      ? 0 : r.GetInt32("Nest_ID"),
                OrderItemId: r.IsDBNull("OrderItem_ID") ? 0 : r.GetInt32("OrderItem_ID")
            ));
        }
        return list.Where(np => np.NestId > 0 && np.OrderItemId > 0).ToList();
    }

    private static async Task<List<SrcNest>> ReadNestsAsync(SqlConnection sql, HashSet<int> nestIds)
    {
        if (nestIds.Count == 0) return [];

        var cmd = sql.CreateCommand();
        cmd.CommandText = """
            SELECT ID, nestName, nestPath, Thumbnail
            FROM Nests
            WHERE ID IN (SELECT value FROM STRING_SPLIT(@ids, ','))
            """;
        cmd.Parameters.AddWithValue("@ids", string.Join(',', nestIds));
        cmd.CommandTimeout = 120;

        var list = new List<SrcNest>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new SrcNest(
                Id:        r.GetInt32("ID"),
                NestName:  r.IsDBNull("nestName")  ? null : r.GetString("nestName"),
                NestPath:  r.IsDBNull("nestPath")  ? null : r.GetString("nestPath"),
                Thumbnail: r.IsDBNull("Thumbnail") ? null : (byte[])r["Thumbnail"]
            ));
        }
        return list;
    }
}
