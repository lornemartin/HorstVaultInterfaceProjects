using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HorstMFG.Web.Services;

/// <summary>
/// Typed client for the local <c>HorstMFG.VaultGateway</c> Windows service.
/// </summary>
public class VaultGatewayClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _callbackUrl;
    private readonly ILogger<VaultGatewayClient> _log;

    public VaultGatewayClient(HttpClient http, IConfiguration config, ILogger<VaultGatewayClient> log)
    {
        _http = http;
        _baseUrl = (config["Gateway:BaseUrl"] ?? "http://127.0.0.1:5050").TrimEnd('/');
        _callbackUrl = config["Gateway:CallbackUrl"] ?? "http://localhost:5150/api/internal/vault-bom-result";
        _log = log;
    }

    public async Task<string> SubmitBatchAsync(
        IReadOnlyList<GatewayBatchItem> items, CancellationToken ct = default)
    {
        var req = new GatewayBatchRequest(_callbackUrl, items);
        using var resp = await _http.PostAsJsonAsync($"{_baseUrl}/api/import-bom-batch", req, ct);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<GatewayBatchResponse>(cancellationToken: ct)
                   ?? throw new InvalidOperationException("Gateway returned an empty response.");
        _log.LogInformation("Gateway accepted batch — jobId={JobId}, items={Count}", body.JobId, items.Count);
        return body.JobId;
    }

    public record GatewayBatchItem(int TrackingId, string ProductNumber);
    private record GatewayBatchRequest(string CallbackUrl, IReadOnlyList<GatewayBatchItem> Items);
    private record GatewayBatchResponse(string JobId);
}
