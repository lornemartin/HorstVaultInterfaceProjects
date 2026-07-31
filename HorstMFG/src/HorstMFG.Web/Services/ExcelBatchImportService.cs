using HorstMFG.Core.Entities;
using HorstMFG.Core.Enums;
using HorstMFG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HorstMFG.Web.Services;

public record ExcelBatchImportResult(
    bool Success,
    int? BatchId,
    string? JobId,
    string? GatewayError,
    int ProductsCreated,
    int ProductsDispatched,
    List<string> Warnings,
    List<string> Errors);

/// <summary>
/// Orchestrates batch Excel imports: parse, create Batch + BatchProducts
/// (merging duplicates with summed qty), commit, then dispatch to the VaultGateway.
/// </summary>
public class ExcelBatchImportService
{
    private readonly ExcelBatchParser _parser;
    private readonly PoBatchParser _poParser;
    private readonly VaultGatewayClient _gateway;
    private readonly BomImportJobTracker _jobs;
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly string _localPdfPath;
    private readonly ILogger<ExcelBatchImportService> _log;

    public ExcelBatchImportService(
        ExcelBatchParser parser,
        PoBatchParser poParser,
        VaultGatewayClient gateway,
        BomImportJobTracker jobs,
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IConfiguration config,
        ILogger<ExcelBatchImportService> log)
    {
        _parser = parser;
        _poParser = poParser;
        _gateway = gateway;
        _jobs = jobs;
        _dbFactory = dbFactory;
        _localPdfPath = config["FileSystemPaths:LocalPdfPath"] ?? @"C:\HorstMFG\PDFs\";
        _log = log;
    }

    public async Task<ExcelBatchImportResult> ImportAsync(
        Stream xlsx, int plantId, int userId, CancellationToken ct = default)
    {
        var parsed = _parser.Parse(xlsx);
        if (parsed.Errors.Count > 0)
            return Failure(parsed.Errors);
        return await ImportParsedAsync(parsed, plantId, userId, ct);
    }

    public async Task<ExcelBatchImportResult> ImportFromPoAsync(
        Stream stream, int plantId, int userId, CancellationToken ct = default)
    {
        ParsedBatch parsed;
        try { parsed = _poParser.Parse(stream); }
        catch (Exception ex) { return Failure(new() { $"Could not read file: {ex.Message}" }); }
        if (parsed.Errors.Count > 0)
            return Failure(parsed.Errors);
        return await ImportParsedAsync(parsed, plantId, userId, ct);
    }

    private async Task<ExcelBatchImportResult> ImportParsedAsync(
        ParsedBatch parsed, int plantId, int userId, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var existing = await db.Batches.FirstOrDefaultAsync(
            b => b.Name == parsed.Name && b.PlantId == plantId, ct);
        if (existing is not null)
            return Failure(new() { $"A batch named '{parsed.Name}' already exists in this plant." });

        var batch = new Batch
        {
            Name = parsed.Name,
            PlantId = plantId,
            ImportedByUserId = userId,
            ImportDate = DateTime.UtcNow,
            LocalPdfFolder = Path.Combine(_localPdfPath, "Batches", parsed.Name),
        };
        db.Batches.Add(batch);
        await db.SaveChangesAsync(ct);

        var (products, warnings) = BuildProducts(parsed.Rows, batch.Id);
        db.BatchProducts.AddRange(products);
        await db.SaveChangesAsync(ct);

        try
        {
            var items = products
                .Select(p => new VaultGatewayClient.GatewayBatchItem(p.Id, p.ProductName))
                .ToList();
            var jobId = await _gateway.SubmitBatchAsync(items, ct);
            _jobs.Register(jobId, BomType.MakeToOrder);
            _log.LogInformation(
                "Batch '{Name}' imported — batch={BatchId}, products={Products}, jobId={JobId}",
                parsed.Name, batch.Id, products.Count, jobId);
            return new ExcelBatchImportResult(true, batch.Id, jobId, null,
                products.Count, products.Count, warnings, new List<string>());
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Batch '{Name}' created but Gateway dispatch failed", parsed.Name);
            return new ExcelBatchImportResult(true, batch.Id, null, ex.Message,
                products.Count, 0, warnings, new List<string>());
        }
    }

    private static ExcelBatchImportResult Failure(List<string> errors) =>
        new(false, null, null, null, 0, 0, new(), errors);

    /// <summary>
    /// Re-issues a Vault BOM query for batch products that aren't yet imported.
    /// </summary>
    public async Task<ExcelBatchImportResult> RetryBatchPendingAsync(
        int batchId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var batch = await db.Batches.FirstOrDefaultAsync(b => b.Id == batchId, ct);
        if (batch is null)
            return new ExcelBatchImportResult(false, null, null, null, 0, 0,
                new(), new() { $"Batch {batchId} not found." });

        var pending = await db.BatchProducts
            .Where(bp => bp.BatchId == batchId && bp.VaultBomImported == false)
            .ToListAsync(ct);

        if (pending.Count == 0)
            return new ExcelBatchImportResult(true, batchId, null, null,
                0, 0, new() { "Nothing to retry." }, new());

        foreach (var p in pending)
            p.LastImportError = null;
        await db.SaveChangesAsync(ct);

        try
        {
            var items = pending
                .Select(p => new VaultGatewayClient.GatewayBatchItem(p.Id, p.ProductName))
                .ToList();
            var jobId = await _gateway.SubmitBatchAsync(items, ct);
            _jobs.Register(jobId, BomType.MakeToOrder);
            return new ExcelBatchImportResult(true, batchId, jobId, null,
                0, pending.Count, new(), new());
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Retry for batch {Id} failed", batchId);
            return new ExcelBatchImportResult(true, batchId, null, ex.Message,
                0, 0, new(), new());
        }
    }

    /// <summary>
    /// Re-issues a Vault BOM query for a single BatchProduct.
    /// </summary>
    public async Task<ExcelBatchImportResult> RetrySingleBatchProductAsync(
        int productId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var product = await db.BatchProducts.FirstOrDefaultAsync(bp => bp.Id == productId, ct);

        if (product is null)
            return new ExcelBatchImportResult(false, null, null, null, 0, 0,
                new(), new() { $"Product {productId} not found." });

        // Reset so the poll loop waits for the new ingest rather than seeing the
        // still-true flag from a prior import and reporting done immediately.
        product.VaultBomImported = false;
        product.LastImportError = null;
        await db.SaveChangesAsync(ct);

        try
        {
            var items = new List<VaultGatewayClient.GatewayBatchItem>
            {
                new(product.Id, product.ProductName)
            };
            var jobId = await _gateway.SubmitBatchAsync(items, ct);
            _jobs.Register(jobId, BomType.MakeToOrder);
            _log.LogInformation("Single retry submitted for BatchProduct {Id} ({Product}), jobId={JobId}",
                product.Id, product.ProductName, jobId);
            return new ExcelBatchImportResult(true, product.BatchId, jobId, null,
                0, 1, new(), new());
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Single retry for batch product {Id} failed", productId);
            return new ExcelBatchImportResult(true, product.BatchId, null, ex.Message,
                0, 0, new(), new());
        }
    }

    /// <summary>
    /// Walks parsed rows, merging duplicates by ProductNumber. The first occurrence
    /// becomes the BatchProduct; later duplicates get summed into its Qty and the
    /// merge is recorded in Notes + warnings (per "merge with a warning" decision).
    /// </summary>
    private static (List<BatchProduct> Products, List<string> Warnings) BuildProducts(
        IEnumerable<ParsedBatchRow> rows, int batchId)
    {
        var byProduct = new Dictionary<string, BatchProduct>(StringComparer.OrdinalIgnoreCase);
        var firstRowIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var warnings = new List<string>();

        foreach (var row in rows)
        {
            if (byProduct.TryGetValue(row.ProductNumber, out var existing))
            {
                existing.Qty += row.Qty;
                existing.Notes = AppendNote(existing.Notes,
                    $"Merged from row {row.RowIndex} (+qty {row.Qty})");
                warnings.Add($"Product '{row.ProductNumber}' appeared on row {firstRowIndex[row.ProductNumber]} (qty {existing.Qty - row.Qty}) and row {row.RowIndex} (qty {row.Qty}) — merged to qty {existing.Qty}.");
            }
            else
            {
                var product = new BatchProduct
                {
                    BatchId = batchId,
                    ProductName = row.ProductNumber,
                    Qty = row.Qty,
                    VaultBomImported = false,
                    Notes = null,
                };
                byProduct[row.ProductNumber] = product;
                firstRowIndex[row.ProductNumber] = row.RowIndex;
            }
        }

        return (byProduct.Values.ToList(), warnings);
    }

    private static string AppendNote(string? existing, string addition)
        => string.IsNullOrEmpty(existing) ? addition : existing + "\n" + addition;
}
