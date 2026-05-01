using HorstMFG.Core.Entities;
using HorstMFG.Core.Enums;
using HorstMFG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HorstMFG.Web.Services;

public record ExcelScheduleImportResult(
    bool Success,
    int? ScheduleId,
    string? JobId,
    string? GatewayError,
    int OrdersCreated,
    int OrdersWithoutProduct,
    int OrdersDispatched,
    List<string> Warnings,
    List<string> Errors);

/// <summary>
/// Orchestrates: parse schedule Excel, create Schedule + ScheduleOrders (with
/// suffix logic for multi-product orders and notes for LA- continuations),
/// commit, then dispatch to the VaultGateway and register the job.
///
/// Schema additions are committed even if the Gateway dispatch fails — the
/// caller can offer a Retry that reuses the existing Schedule.
/// </summary>
public class ExcelScheduleImportService
{
    private readonly ExcelScheduleParser _parser;
    private readonly VaultGatewayClient _gateway;
    private readonly BomImportJobTracker _jobs;
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly string _localPdfPath;
    private readonly ILogger<ExcelScheduleImportService> _log;

    public ExcelScheduleImportService(
        ExcelScheduleParser parser,
        VaultGatewayClient gateway,
        BomImportJobTracker jobs,
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IConfiguration config,
        ILogger<ExcelScheduleImportService> log)
    {
        _parser = parser;
        _gateway = gateway;
        _jobs = jobs;
        _dbFactory = dbFactory;
        _localPdfPath = config["FileSystemPaths:LocalPdfPath"] ?? @"C:\HorstMFG\PDFs\";
        _log = log;
    }

    public async Task<ExcelScheduleImportResult> ImportAsync(
        Stream xlsx, int plantId, int userId, CancellationToken ct = default)
    {
        var parsed = _parser.Parse(xlsx);
        if (parsed.Errors.Count > 0)
            return new ExcelScheduleImportResult(false, null, null, null, 0, 0, 0,
                new List<string>(), parsed.Errors);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var existing = await db.Schedules.FirstOrDefaultAsync(
            s => s.Name == parsed.Name && s.PlantId == plantId, ct);
        if (existing is not null)
            return new ExcelScheduleImportResult(false, null, null, null, 0, 0, 0,
                new(), new() { $"A schedule named '{parsed.Name}' already exists in this plant." });

        var schedule = new Schedule
        {
            Name = parsed.Name,
            PlantId = plantId,
            ImportedByUserId = userId,
            ImportDate = DateTime.UtcNow,
            LocalPdfFolder = Path.Combine(_localPdfPath, "Schedules", parsed.Name),
        };
        db.Schedules.Add(schedule);
        await db.SaveChangesAsync(ct);

        var (orders, warnings) = BuildOrders(parsed.Rows, schedule.Id);
        db.ScheduleOrders.AddRange(orders);
        await db.SaveChangesAsync(ct);

        var dispatched = orders.Where(o => !string.IsNullOrEmpty(o.ProductNumber)).ToList();
        var withoutProduct = orders.Count - dispatched.Count;

        if (dispatched.Count == 0)
        {
            return new ExcelScheduleImportResult(true, schedule.Id, null, null,
                orders.Count, withoutProduct, 0, warnings, new List<string>());
        }

        try
        {
            var items = dispatched
                .Select(o => new VaultGatewayClient.GatewayBatchItem(o.Id, o.ProductNumber!))
                .ToList();
            var jobId = await _gateway.SubmitBatchAsync(items, ct);
            _jobs.Register(jobId, BomType.MakeToStock);
            _log.LogInformation(
                "Schedule '{Name}' imported — schedule={ScheduleId}, orders={Orders}, dispatched={Dispatched}, jobId={JobId}",
                parsed.Name, schedule.Id, orders.Count, dispatched.Count, jobId);
            return new ExcelScheduleImportResult(true, schedule.Id, jobId, null,
                orders.Count, withoutProduct, dispatched.Count, warnings, new List<string>());
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Schedule '{Name}' created but Gateway dispatch failed", parsed.Name);
            return new ExcelScheduleImportResult(true, schedule.Id, null, ex.Message,
                orders.Count, withoutProduct, 0, warnings, new List<string>());
        }
    }

    /// <summary>
    /// Re-issues a Vault BOM query for orders in the schedule that aren't yet imported,
    /// using each order's current ProductNumber. Used by both retry-after-gateway-failure
    /// and edit-and-reimport flows.
    /// </summary>
    public async Task<ExcelScheduleImportResult> RetrySchedulePendingAsync(
        int scheduleId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var schedule = await db.Schedules.FirstOrDefaultAsync(s => s.Id == scheduleId, ct);
        if (schedule is null)
            return new ExcelScheduleImportResult(false, null, null, null, 0, 0, 0,
                new(), new() { $"Schedule {scheduleId} not found." });

        var pending = await db.ScheduleOrders
            .Where(so => so.ScheduleId == scheduleId
                      && so.VaultBomImported == false
                      && so.ProductNumber != null && so.ProductNumber != "")
            .ToListAsync(ct);

        if (pending.Count == 0)
            return new ExcelScheduleImportResult(true, scheduleId, null, null,
                0, 0, 0, new() { "Nothing to retry." }, new());

        try
        {
            var items = pending
                .Select(o => new VaultGatewayClient.GatewayBatchItem(o.Id, o.ProductNumber!))
                .ToList();
            var jobId = await _gateway.SubmitBatchAsync(items, ct);
            _jobs.Register(jobId, BomType.MakeToStock);
            return new ExcelScheduleImportResult(true, scheduleId, jobId, null,
                0, 0, pending.Count, new(), new());
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Retry for schedule {Id} failed", scheduleId);
            return new ExcelScheduleImportResult(true, scheduleId, null, ex.Message,
                0, 0, 0, new(), new());
        }
    }

    /// <summary>
    /// Re-issues a Vault BOM query for a single ScheduleOrder. Used when the user
    /// re-imports after editing ProductNumber or after a per-order gateway failure.
    /// </summary>
    public async Task<ExcelScheduleImportResult> RetrySingleScheduleOrderAsync(
        int orderId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var order = await db.ScheduleOrders.FirstOrDefaultAsync(so => so.Id == orderId, ct);

        if (order is null)
            return new ExcelScheduleImportResult(false, null, null, null, 0, 0, 0,
                new(), new() { $"Order {orderId} not found." });

        if (string.IsNullOrEmpty(order.ProductNumber))
            return new ExcelScheduleImportResult(false, order.ScheduleId, null, null, 0, 1, 0,
                new(), new() { "Order has no product number — set it before retrying." });

        try
        {
            var items = new List<VaultGatewayClient.GatewayBatchItem>
            {
                new(order.Id, order.ProductNumber)
            };
            var jobId = await _gateway.SubmitBatchAsync(items, ct);
            _jobs.Register(jobId, BomType.MakeToStock);
            _log.LogInformation("Single retry submitted for ScheduleOrder {Id} ({Product}), jobId={JobId}",
                order.Id, order.ProductNumber, jobId);
            return new ExcelScheduleImportResult(true, order.ScheduleId, jobId, null,
                0, 0, 1, new(), new());
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Single retry for order {Id} failed", orderId);
            return new ExcelScheduleImportResult(true, order.ScheduleId, null, ex.Message,
                0, 0, 0, new(), new());
        }
    }

    /// <summary>
    /// Walks parsed rows in order. Real product rows produce ScheduleOrders, suffixed
    /// (a/b/c…) when an OrderNumber repeats. LA- continuation rows are appended to
    /// the most recent real order in the same OrderNumber as a Notes line. If an
    /// OrderNumber has only LA- rows, a single empty ScheduleOrder is created with
    /// ProductNumber=null and the LA- info aggregated in Notes.
    /// </summary>
    private static (List<ScheduleOrder> Orders, List<string> Warnings) BuildOrders(
        IEnumerable<ParsedScheduleRow> rows, int scheduleId)
    {
        var orders = new List<ScheduleOrder>();
        var warnings = new List<string>();

        // Per-OrderNumber bookkeeping
        var realCount = new Dictionary<string, int>();      // # of real products seen so far per OrderNumber
        var mostRecentReal = new Dictionary<string, ScheduleOrder>();
        var allLaPlaceholder = new Dictionary<string, ScheduleOrder>();

        foreach (var row in rows)
        {
            if (row.IsContinuation)
            {
                var note = $"LA- {row.Qty}";
                if (mostRecentReal.TryGetValue(row.OrderNumber, out var attachTo))
                {
                    attachTo.Notes = AppendNote(attachTo.Notes, note);
                }
                else
                {
                    // No real product yet for this order — accumulate on a placeholder
                    if (!allLaPlaceholder.TryGetValue(row.OrderNumber, out var placeholder))
                    {
                        placeholder = new ScheduleOrder
                        {
                            ScheduleId = scheduleId,
                            OrderNumber = row.OrderNumber,
                            Qty = 1,
                            ProductNumber = null,
                            VaultBomImported = false,
                            Notes = note,
                        };
                        allLaPlaceholder[row.OrderNumber] = placeholder;
                        orders.Add(placeholder);
                        warnings.Add($"Order {row.OrderNumber}: no product number found — created empty order, edit ProductNumber and re-import to populate.");
                    }
                    else
                    {
                        placeholder.Notes = AppendNote(placeholder.Notes, note);
                    }
                }
                continue;
            }

            // Real product row.
            int seen = realCount.GetValueOrDefault(row.OrderNumber);
            string suffixedNumber = row.OrderNumber;
            if (seen >= 1)
            {
                if (seen >= 26)
                {
                    warnings.Add($"Order {row.OrderNumber}: more than 26 products on a single order — skipped row {row.RowIndex}.");
                    continue;
                }
                // First duplicate becomes 'b', second 'c', etc. (a is the original.)
                // Tweak: rename the first occurrence to "Xa" once we know there are duplicates.
                if (seen == 1 && mostRecentReal.TryGetValue(row.OrderNumber, out var first))
                    first.OrderNumber = first.OrderNumber + "a";
                suffixedNumber = row.OrderNumber + (char)('a' + seen);
            }

            var order = new ScheduleOrder
            {
                ScheduleId = scheduleId,
                OrderNumber = suffixedNumber,
                Qty = row.Qty,
                ProductNumber = row.ProductNumber,
                VaultBomImported = false,
                Notes = null,
            };
            // If this was the first real product for an order that previously had only
            // LA- rows, fold the LA- notes into this order and remove the placeholder.
            if (allLaPlaceholder.TryGetValue(row.OrderNumber, out var placeholder2))
            {
                order.Notes = placeholder2.Notes;
                orders.Remove(placeholder2);
                allLaPlaceholder.Remove(row.OrderNumber);
            }
            orders.Add(order);
            mostRecentReal[row.OrderNumber] = order;
            realCount[row.OrderNumber] = seen + 1;
        }

        return (orders, warnings);
    }

    private static string AppendNote(string? existing, string addition)
        => string.IsNullOrEmpty(existing) ? addition : existing + "\n" + addition;
}
