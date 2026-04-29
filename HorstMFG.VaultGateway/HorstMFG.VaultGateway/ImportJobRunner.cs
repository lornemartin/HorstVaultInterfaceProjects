using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HorstMFG.VaultGateway.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VA = VaultAccess;

namespace HorstMFG.VaultGateway;

public class ImportJobRunner
{
    private readonly IOptions<GatewayConfig> _config;
    private readonly IHttpClientFactory _httpFactory;
    private readonly VaultClient _vault;
    private readonly ILogger<ImportJobRunner> _log;
    private readonly ConcurrentDictionary<string, JobStatus> _jobs = new();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
    };

    public ImportJobRunner(IOptions<GatewayConfig> config,
                           IHttpClientFactory httpFactory,
                           VaultClient vault,
                           ILogger<ImportJobRunner> log)
    {
        _config = config;
        _httpFactory = httpFactory;
        _vault = vault;
        _log = log;
    }

    public string Submit(ImportBomBatchRequest request)
    {
        var jobId = Guid.NewGuid().ToString();
        var status = new JobStatus
        {
            JobId = jobId,
            Status = "running",
            TotalCount = request.Items.Count,
        };
        _jobs[jobId] = status;

        // Fire and forget — runs in background, posts callbacks as items complete.
        _ = Task.Run(() => RunAsync(jobId, request, status));

        return jobId;
    }

    public JobStatus? Get(string jobId)
        => _jobs.TryGetValue(jobId, out var s) ? s : null;

    private async Task RunAsync(string jobId, ImportBomBatchRequest request, JobStatus status)
    {
        _log.LogInformation("Job {JobId} starting with {Count} item(s); callback={Url}",
            jobId, request.Items.Count, request.CallbackUrl);

        using var http = _httpFactory.CreateClient();

        foreach (var item in request.Items)
        {
            VaultBomCallbackPayload payload;
            try
            {
                payload = await Task.Run(() => BuildPayloadFromVault(jobId, item));
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Job {JobId} item {TrackingId} ({Product}) — Vault query failed",
                    jobId, item.TrackingId, item.ProductNumber);
                payload = new VaultBomCallbackPayload
                {
                    JobId = jobId,
                    TrackingId = item.TrackingId,
                    Found = false,
                    ErrorMessage = ex.Message,
                };
                status.Errors.Add($"trackingId={item.TrackingId}: {ex.Message}");
            }

            try
            {
                await PostCallbackAsync(http, request.CallbackUrl, payload);
                _log.LogInformation("Job {JobId} item {TrackingId} ({Product}) — callback sent (found={Found})",
                    jobId, item.TrackingId, item.ProductNumber, payload.Found);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Job {JobId} item {TrackingId} — callback POST failed", jobId, item.TrackingId);
                status.Errors.Add($"trackingId={item.TrackingId} callback: {ex.Message}");
            }

            status.ProcessedCount++;
        }

        status.Status = status.Errors.Count == 0 ? "completed" : "completed_with_errors";
        _log.LogInformation("Job {JobId} done — status={Status}, errors={Errors}",
            jobId, status.Status, status.Errors.Count);
    }

    private async Task PostCallbackAsync(HttpClient http, string callbackUrl, VaultBomCallbackPayload payload)
    {
        var json = JsonSerializer.Serialize(payload, JsonOpts);
        using var req = new HttpRequestMessage(HttpMethod.Post, callbackUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        req.Headers.Add("X-Gateway-Key", _config.Value.CallbackApiKey);

        using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Callback to {callbackUrl} returned {(int)resp.StatusCode}: {body}");
        }
    }

    private VaultBomCallbackPayload BuildPayloadFromVault(string jobId, ImportItem item)
    {
        VA.VaultBomResult? result = _vault.GetItemBom(item.ProductNumber, refreshFromSource: true);

        if (result is null)
        {
            return new VaultBomCallbackPayload
            {
                JobId = jobId,
                TrackingId = item.TrackingId,
                Found = false,
                ErrorMessage = $"Item '{item.ProductNumber}' not found in Vault",
            };
        }

        return new VaultBomCallbackPayload
        {
            JobId = jobId,
            TrackingId = item.TrackingId,
            Found = true,
            Lines = result.Lines.Select(MapLine).ToList(),
        };
    }

    private static VaultBomLine MapLine(VA.VaultBomItem src) => new()
    {
        Level           = src.Level,
        Number          = src.Number,
        Title           = src.Title,
        ItemDescription = src.ItemDescription,
        Category        = src.Category,
        Thickness       = src.Thickness,
        Material        = src.Material,
        Operations      = src.Operations,
        Qty             = src.Qty,
        StructCode      = src.StructCode,
        PlantId         = src.PlantId,
        IsStock         = src.IsStock,
        RequiresPdf     = src.RequiresPdf,
        Comment         = src.Comment,
        DateModified    = src.DateModified,
        LifecycleState  = src.LifecycleState,
        StockName       = src.StockName,
        Keywords        = src.Keywords,
        Notes           = src.Notes,
        Revision        = src.Revision,
    };
}
