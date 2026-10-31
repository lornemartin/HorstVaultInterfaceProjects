using HorstMFG.Core.DTOs;
using HorstMFG.Core.Entities;
using HorstMFG.Core.Enums;
using HorstMFG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HorstMFG.Web.Services;

/// <summary>
/// Receives a single VaultGateway callback (one ScheduleOrder or BatchProduct's BOM),
/// creates the corresponding <see cref="PartLineItem"/> rows, and copies referenced
/// PDFs to the local folder. Idempotent — re-imports replace existing PartLineItems
/// for the parent.
/// </summary>
public class VaultBomIngestService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly BomImportJobTracker _jobs;
    private readonly BomPdfCopyService _pdfCopy;
    private readonly ILogger<VaultBomIngestService> _log;

    public VaultBomIngestService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        BomImportJobTracker jobs,
        BomPdfCopyService pdfCopy,
        ILogger<VaultBomIngestService> log)
    {
        _dbFactory = dbFactory;
        _jobs = jobs;
        _pdfCopy = pdfCopy;
        _log = log;
    }

    public async Task IngestSingleAsync(VaultBomCallbackPayload payload, CancellationToken ct = default)
    {
        var importType = _jobs.GetImportType(payload.JobId);
        if (importType is null)
        {
            _log.LogWarning("Vault callback for unknown jobId {JobId} — ignoring", payload.JobId);
            return;
        }

        if (importType == BomType.MakeToStock)
            await IngestForScheduleOrderAsync(payload, ct);
        else
            await IngestForBatchProductAsync(payload, ct);
    }

    private async Task IngestForScheduleOrderAsync(VaultBomCallbackPayload payload, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var order = await db.ScheduleOrders
            .Include(so => so.Schedule).ThenInclude(s => s.Plant)
            .Include(so => so.Parts)
            .FirstOrDefaultAsync(so => so.Id == payload.TrackingId, ct);

        if (order is null)
        {
            _log.LogWarning("Vault callback trackingId={TrackingId} did not match any ScheduleOrder", payload.TrackingId);
            return;
        }

        if (!payload.Found)
        {
            order.Notes = AppendNote(order.Notes, $"Vault import failed: {payload.ErrorMessage}");
            order.VaultBomImported = false;
            order.LastImportError = payload.ErrorMessage ?? "Item not found in Vault.";
            await db.SaveChangesAsync(ct);
            _log.LogInformation("ScheduleOrder {Id} ({Order}/{Product}) — Vault item not found: {Err}",
                order.Id, order.OrderNumber, order.ProductNumber, payload.ErrorMessage);
            return;
        }

        // Idempotent re-import: replace any existing children.
        if (order.Parts.Count > 0)
            db.PartLineItems.RemoveRange(order.Parts);

        var newItems = BomLineBuilder.Build(payload.Lines, order.Schedule.Plant, order.ProductNumber).ToList();
        BomLineBuilder.ResolvePlantIds(newItems, await PlantCodeToIdAsync(db, ct));
        var partNumbers = newItems.Select(p => p.PartNumber).Distinct().ToList();
        var pdfsOnShare = await _pdfCopy.WhichExistOnShareAsync(partNumbers, ct);
        foreach (var line in newItems)
        {
            line.ScheduleOrderId = order.Id;
            line.HasPdf = pdfsOnShare.Contains(line.PartNumber);
            db.PartLineItems.Add(line);
        }
        order.VaultBomImported = true;
        order.LastImportError = null;
        await db.SaveChangesAsync(ct);

        var (copied, total) = await _pdfCopy.CopyForScheduleAsync(order.Schedule.Plant.Name, order.Schedule.Name, partNumbers, ct);
        _log.LogInformation(
            "Ingested ScheduleOrder {Id} ({Order}/{Product}) — {Lines} parts, copied {Copied}/{Total} PDFs",
            order.Id, order.OrderNumber, order.ProductNumber, partNumbers.Count, copied, total);
    }

    private async Task IngestForBatchProductAsync(VaultBomCallbackPayload payload, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var product = await db.BatchProducts
            .Include(bp => bp.Batch).ThenInclude(b => b.Plant)
            .Include(bp => bp.Parts)
            .FirstOrDefaultAsync(bp => bp.Id == payload.TrackingId, ct);

        if (product is null)
        {
            _log.LogWarning("Vault callback trackingId={TrackingId} did not match any BatchProduct", payload.TrackingId);
            return;
        }

        if (!payload.Found)
        {
            product.Notes = AppendNote(product.Notes, $"Vault import failed: {payload.ErrorMessage}");
            product.VaultBomImported = false;
            product.LastImportError = payload.ErrorMessage ?? "Item not found in Vault.";
            await db.SaveChangesAsync(ct);
            _log.LogInformation("BatchProduct {Id} ({Product}) — Vault item not found: {Err}",
                product.Id, product.ProductName, payload.ErrorMessage);
            return;
        }

        if (product.Parts.Count > 0)
            db.PartLineItems.RemoveRange(product.Parts);

        var newItems = BomLineBuilder.Build(payload.Lines, product.Batch.Plant, product.ProductName).ToList();
        BomLineBuilder.ResolvePlantIds(newItems, await PlantCodeToIdAsync(db, ct));
        var partNumbers = newItems.Select(p => p.PartNumber).Distinct().ToList();
        var pdfsOnShare = await _pdfCopy.WhichExistOnShareAsync(partNumbers, ct);
        foreach (var line in newItems)
        {
            line.BatchProductId = product.Id;
            line.HasPdf = pdfsOnShare.Contains(line.PartNumber);
            db.PartLineItems.Add(line);
        }
        product.VaultBomImported = true;
        product.LastImportError = null;
        await db.SaveChangesAsync(ct);

        var (copied, total) = await _pdfCopy.CopyForBatchAsync(product.Batch.Plant.Name, product.Batch.Name, partNumbers, ct);
        _log.LogInformation(
            "Ingested BatchProduct {Id} ({Product}) — {Lines} parts, copied {Copied}/{Total} PDFs",
            product.Id, product.ProductName, partNumbers.Count, copied, total);
    }

    private static Task<Dictionary<string, int>> PlantCodeToIdAsync(ApplicationDbContext db, CancellationToken ct) =>
        db.Plants.AsNoTracking().ToDictionaryAsync(p => p.Code, p => p.Id, StringComparer.OrdinalIgnoreCase, ct);

    private static string AppendNote(string? existing, string addition)
        => string.IsNullOrEmpty(existing) ? addition : existing + "\n" + addition;
}
