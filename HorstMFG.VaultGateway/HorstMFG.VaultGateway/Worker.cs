using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HorstMFG.VaultGateway.Dtos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HorstMFG.VaultGateway;

public class Worker : BackgroundService
{
    private readonly GatewayConfig _config;
    private readonly ImportJobRunner _runner;
    private readonly ILogger<Worker> _log;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public Worker(IOptions<GatewayConfig> config,
                  ImportJobRunner runner,
                  ILogger<Worker> log)
    {
        _config = config.Value;
        _runner = runner;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new HttpListener();
        var prefix = _config.ListenUrl.EndsWith("/") ? _config.ListenUrl : _config.ListenUrl + "/";
        listener.Prefixes.Add(prefix);
        listener.Start();
        _log.LogInformation("VaultGateway listening on {Prefix}", prefix);

        using var registration = stoppingToken.Register(() =>
        {
            try { listener.Stop(); } catch { }
        });

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                HttpListenerContext ctx;
                try
                {
                    ctx = await listener.GetContextAsync();
                }
                catch (HttpListenerException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException) { break; }

                _ = Task.Run(() => HandleAsync(ctx));
            }
        }
        finally
        {
            try { listener.Close(); } catch { }
        }
    }

    private async Task HandleAsync(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var resp = ctx.Response;

        try
        {
            var path = req.Url?.AbsolutePath ?? "";
            _log.LogDebug("{Method} {Path}", req.HttpMethod, path);

            if (req.HttpMethod == "POST" && path.Equals("/api/import-bom-batch", StringComparison.OrdinalIgnoreCase))
            {
                await HandleSubmitAsync(req, resp);
            }
            else if (req.HttpMethod == "GET" && path.StartsWith("/api/jobs/", StringComparison.OrdinalIgnoreCase))
            {
                var jobId = path.Substring("/api/jobs/".Length).Trim('/');
                HandleGetJob(jobId, resp);
            }
            else
            {
                resp.StatusCode = (int)HttpStatusCode.NotFound;
                await WriteJsonAsync(resp, new { error = "not found" });
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Request handler crashed");
            try
            {
                resp.StatusCode = (int)HttpStatusCode.InternalServerError;
                await WriteJsonAsync(resp, new { error = ex.Message });
            }
            catch { }
        }
        finally
        {
            try { resp.Close(); } catch { }
        }
    }

    private async Task HandleSubmitAsync(HttpListenerRequest req, HttpListenerResponse resp)
    {
        ImportBomBatchRequest? body;
        using (var reader = new StreamReader(req.InputStream, req.ContentEncoding ?? Encoding.UTF8))
        {
            var raw = await reader.ReadToEndAsync();
            body = JsonSerializer.Deserialize<ImportBomBatchRequest>(raw, JsonOpts);
        }

        if (body is null || string.IsNullOrWhiteSpace(body.CallbackUrl) || body.Items.Count == 0)
        {
            resp.StatusCode = (int)HttpStatusCode.BadRequest;
            await WriteJsonAsync(resp, new { error = "callbackUrl and items[] are required" });
            return;
        }

        var jobId = _runner.Submit(body);
        resp.StatusCode = (int)HttpStatusCode.Accepted;
        await WriteJsonAsync(resp, new ImportBomBatchResponse { JobId = jobId });
    }

    private void HandleGetJob(string jobId, HttpListenerResponse resp)
    {
        var status = _runner.Get(jobId);
        if (status is null)
        {
            resp.StatusCode = (int)HttpStatusCode.NotFound;
            WriteJsonAsync(resp, new { error = "job not found" }).GetAwaiter().GetResult();
            return;
        }
        resp.StatusCode = (int)HttpStatusCode.OK;
        WriteJsonAsync(resp, status).GetAwaiter().GetResult();
    }

    private static async Task WriteJsonAsync(HttpListenerResponse resp, object payload)
    {
        var json = JsonSerializer.Serialize(payload, JsonOpts);
        var bytes = Encoding.UTF8.GetBytes(json);
        resp.ContentType = "application/json; charset=utf-8";
        resp.ContentLength64 = bytes.Length;
        await resp.OutputStream.WriteAsync(bytes, 0, bytes.Length);
    }
}
