using HorstMFG.Bridge.Handlers;
using HorstMFG.Bridge.Nesting;
using HorstMFG.Bridge.Vault;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HorstMFG.Bridge;

public class Worker : BackgroundService
{
    private readonly BridgeConfig _config;
    private readonly IVaultService _vault;
    private readonly SendToNestingHandler _sendToNesting;
    private readonly RetrieveFromNestingHandler _retrieveFromNesting;
    private readonly RetrieveFromVaultHandler _retrieveFromVault;
    private readonly SyncHandler _sync;
    private readonly FinalizeHandler _finalize;
    private readonly UpdateThumbnailHandler _updateThumbnail;
    private readonly FileWatcher _fileWatcher;
    private readonly BomExportWatcher _bomExportWatcher;
    private readonly ILogger<Worker> _log;

    private HubConnection? _hub;
    private string? _activeProjectPath;

    public Worker(IOptions<BridgeConfig> config,
                  IVaultService vault,
                  SendToNestingHandler sendToNesting,
                  RetrieveFromNestingHandler retrieveFromNesting,
                  RetrieveFromVaultHandler retrieveFromVault,
                  SyncHandler sync,
                  FinalizeHandler finalize,
                  UpdateThumbnailHandler updateThumbnail,
                  FileWatcher fileWatcher,
                  BomExportWatcher bomExportWatcher,
                  ILogger<Worker> log)
    {
        _config              = config.Value;
        _vault               = vault;
        _sendToNesting       = sendToNesting;
        _retrieveFromNesting = retrieveFromNesting;
        _retrieveFromVault   = retrieveFromVault;
        _sync                = sync;
        _finalize            = finalize;
        _updateThumbnail     = updateThumbnail;
        _fileWatcher         = fileWatcher;
        _bomExportWatcher    = bomExportWatcher;
        _log                 = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Connect to Vault once at startup (VaultService will reconnect on demand if needed)
        _vault.Connect();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAsync(stoppingToken);
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _log.LogError(ex, "Connection lost — reconnecting in 10s");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _fileWatcher.Detach();
        _bomExportWatcher.Detach();
        if (_hub != null) await _hub.DisposeAsync();
    }

    private async Task ConnectAsync(CancellationToken ct)
    {
        _hub = new HubConnectionBuilder()
            .WithUrl($"{_config.HorstMfgUrl}/hubs/bridge", opts =>
            {
                opts.Headers.Add("X-Api-Key", _config.ApiKey);
            })
            .WithAutomaticReconnect()
            .Build();

        // Hub method: HorstMFG sends a command to this bridge
        _hub.On<string, string, string>("ExecuteCommand", async (commandId, commandType, payload) =>
        {
            _log.LogInformation("Received command {Type} [{Id}]", commandType, commandId);
            await ExecuteCommandAsync(commandId, commandType, payload);
        });

        // Hub method: HorstMFG sends the current active project path on connect
        _hub.On<string>("SetActiveProject", (path) =>
        {
            _activeProjectPath = path;
            _fileWatcher.Attach(_hub, _config.StationId, path);
            _log.LogInformation("Active project set to {Path}", path);
        });

        _hub.Reconnected += async _ =>
        {
            _log.LogInformation("Reconnected — re-registering station {Id}", _config.StationId);
            await RegisterAsync();
        };

        await _hub.StartAsync(ct);
        await RegisterAsync();

        if (!string.IsNullOrWhiteSpace(_config.BomExportFilePath))
            _bomExportWatcher.Attach(_hub, _config.StationId, _config.BomExportFilePath);

        _log.LogInformation("Connected to HorstMFG as station {Id}", _config.StationId);
    }

    private Task RegisterAsync()
        => _hub!.InvokeAsync("Register", _config.StationId,
                             System.Reflection.Assembly.GetExecutingAssembly()
                                   .GetName().Version?.ToString() ?? "unknown");

    // ── Command dispatch ─────────────────────────────────────────────────────

    private async Task ExecuteCommandAsync(string commandId, string commandType, string payload)
    {
        if (string.IsNullOrWhiteSpace(_activeProjectPath) &&
            commandType is not "RetrieveFromVault" and not "UpdateThumbnail")
        {
            await SendResultAsync(commandId, false, "No active project configured.");
            return;
        }

        try
        {
            void Progress(string msg, int pct) =>
                _hub!.InvokeAsync("Progress", _config.StationId, commandId, msg, pct)
                     .GetAwaiter().GetResult();

            object result = commandType switch
            {
                "SendToNesting" => _sendToNesting.Execute(
                    _activeProjectPath!,
                    JsonSerializer.Deserialize<System.Collections.Generic.List<SendToNestingItem>>(payload)!,
                    Progress),

                "RetrieveFromNesting" => _retrieveFromNesting.Execute(
                    _activeProjectPath!,
                    JsonSerializer.Deserialize<System.Collections.Generic.List<RetrieveFromNestingItem>>(payload)!),

                "RetrieveFromVault" => _retrieveFromVault.Execute(
                    JsonSerializer.Deserialize<System.Collections.Generic.List<RetrieveFromVaultItem>>(payload)!,
                    Progress),

                "Sync" => _sync.Execute(_activeProjectPath!),

                "Finalize" => HandleFinalize(Progress),

                "UpdateThumbnail" => _updateThumbnail.Execute(
                    JsonSerializer.Deserialize<System.Collections.Generic.List<UpdateThumbnailItem>>(payload)!,
                    Progress),

                _ => throw new NotSupportedException($"Unknown command: {commandType}"),
            };

            await SendResultAsync(commandId, true, JsonSerializer.Serialize(result));
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Command {Type} failed", commandType);
            await SendResultAsync(commandId, false, ex.Message);
        }
    }

    private FinalizeResult HandleFinalize(Action<string, int> progress)
    {
        var result = _finalize.Execute(_activeProjectPath!, progress);
        // Update file watcher to new project
        _activeProjectPath = result.NewProjectPath;
        _fileWatcher.Attach(_hub!, _config.StationId, result.NewProjectPath);
        return result;
    }

    private Task SendResultAsync(string commandId, bool success, string payload)
        => _hub!.InvokeAsync("CommandResult", _config.StationId, commandId, success, payload);
}
