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
    private readonly ProjectFolderWatcher _folderWatcher;
    private readonly INestingProjectService _nesting;
    private readonly BomExportWatcher _bomExportWatcher;
    private readonly ILogger<Worker> _log;

    private HubConnection? _hub;
    private string? _activeProjectPath;
    // The station's projects folder as recorded in HorstMFG's database, if it has one.
    private string? _projectsRootOverride;
    private string ProjectsRoot => _projectsRootOverride ?? _config.RadanProjectsRootPath;

    public Worker(IOptions<BridgeConfig> config,
                  IVaultService vault,
                  SendToNestingHandler sendToNesting,
                  RetrieveFromNestingHandler retrieveFromNesting,
                  RetrieveFromVaultHandler retrieveFromVault,
                  SyncHandler sync,
                  FinalizeHandler finalize,
                  UpdateThumbnailHandler updateThumbnail,
                  FileWatcher fileWatcher,
                  ProjectFolderWatcher folderWatcher,
                  INestingProjectService nesting,
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
        _folderWatcher       = folderWatcher;
        _nesting             = nesting;
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
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _log.LogError(ex, "Connection lost — reconnecting in 10s");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _fileWatcher.Detach();
        _folderWatcher.Detach();
        _bomExportWatcher.Detach();
        if (_hub != null) await _hub.DisposeAsync();
    }

    private async Task ConnectAsync(CancellationToken ct)
    {
        if (_hub != null)
            await _hub.DisposeAsync();

        using var hubCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        _log.LogInformation("Connecting to {Url}/hubs/bridge ...", _config.HorstMfgUrl);

        _hub = new HubConnectionBuilder()
            .WithUrl($"{_config.HorstMfgUrl}/hubs/bridge", opts =>
            {
                opts.Headers.Add("X-Api-Key", _config.ApiKey);
            })
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

        // Hub method: HorstMFG sends this station's projects folder (kept per database) on connect
        // and when an admin changes it. Blank means fall back to RadanProjectsRootPath in appsettings.
        _hub.On<string>("SetProjectsRoot", (path) =>
        {
            _projectsRootOverride = string.IsNullOrWhiteSpace(path) ? null : path;
            _folderWatcher.Attach(_hub, _config.StationId, ProjectsRoot, () => _activeProjectPath);
            _log.LogInformation("Projects folder set to {Path}", ProjectsRoot);
        });

        // When the server closes the connection, cancel the idle wait so the
        // outer retry loop in ExecuteAsync can reconnect cleanly.
        _hub.Closed += ex =>
        {
            if (!ct.IsCancellationRequested)
            {
                _log.LogWarning("Hub connection closed: {Reason}", ex?.Message ?? "server closed connection");
                hubCts.Cancel();
            }
            return Task.CompletedTask;
        };

        await _hub.StartAsync(ct);
        await RegisterAsync();

        _folderWatcher.Attach(_hub, _config.StationId, ProjectsRoot, () => _activeProjectPath);

        if (!string.IsNullOrWhiteSpace(_config.BomExportFilePath))
            _bomExportWatcher.Attach(_hub, _config.StationId, _config.BomExportFilePath);

        _log.LogInformation("Connected to HorstMFG as station {Id}", _config.StationId);

        try
        {
            await Task.Delay(Timeout.Infinite, hubCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // Hub closed (not a service stop) — propagate so ExecuteAsync retries.
            throw new Exception("Hub connection closed unexpectedly");
        }
    }

    private Task RegisterAsync()
        => _hub!.InvokeAsync("Register", _config.StationId,
                             System.Reflection.Assembly.GetExecutingAssembly()
                                   .GetName().Version?.ToString() ?? "unknown");

    // ── Command dispatch ─────────────────────────────────────────────────────

    private async Task ExecuteCommandAsync(string commandId, string commandType, string payload)
    {
        // Read-only and doesn't touch the active project, so no project check or watcher suspend.
        // ReadProject reads any saved .rpd from disk, for a re-sync of a finalized project.
        if (commandType is "ListProjects" or "ReadProject")
        {
            try
            {
                object result = commandType == "ListProjects"
                    ? ListProjects()
                    : _nesting.ReadSyncDataFromFile(
                        JsonSerializer.Deserialize<ReadProjectRequest>(payload)?.Path
                        ?? throw new ArgumentException("ReadProject needs a Path."));
                await SendResultAsync(commandId, true, JsonSerializer.Serialize(result));
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Command {Type} failed", commandType);
                await SendResultAsync(commandId, false, ex.Message);
            }
            return;
        }

        if (string.IsNullOrWhiteSpace(_activeProjectPath) &&
            commandType is not "RetrieveFromVault" and not "UpdateThumbnail")
        {
            await SendResultAsync(commandId, false, "No active project configured.");
            return;
        }

        _fileWatcher.Suspend();
        _folderWatcher.Suspend();
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
        finally
        {
            // Cooldown absorbs any async RPD write Radan makes after processing NotifyProjectChanged
            _fileWatcher.Resume(cooldownMs: 4000);
            _folderWatcher.Resume();
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

    /// <summary>
    /// The .rpd files under the projects folder, newest first: ones directly in it, plus one per
    /// project folder (Finalize creates root\yyyy-MM-dd\yyyy-MM-dd.rpd). The folder is
    /// ProjectsRoot, or — when that's blank or doesn't exist — the one the active project's
    /// folder sits in.
    /// </summary>
    private System.Collections.Generic.List<ProjectFileEntry> ListProjects()
    {
        var root = ProjectsRoot;
        if (!System.IO.Directory.Exists(root) && !string.IsNullOrWhiteSpace(_activeProjectPath))
        {
            var fallback = System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(_activeProjectPath)) ?? "";
            _log.LogWarning("RadanProjectsRootPath '{Root}' not found; listing projects from '{Fallback}'", root, fallback);
            root = fallback;
        }
        if (string.IsNullOrWhiteSpace(root) || !System.IO.Directory.Exists(root))
            throw new InvalidOperationException($"Radan projects folder not found: '{root}'.");

        var files = new System.Collections.Generic.List<string>(
            System.IO.Directory.EnumerateFiles(root, "*.rpd"));
        foreach (var dir in System.IO.Directory.EnumerateDirectories(root))
        {
            try   { files.AddRange(System.IO.Directory.EnumerateFiles(dir, "*.rpd")); }
            catch (UnauthorizedAccessException) { }
        }

        var result = files.ConvertAll(f => new ProjectFileEntry
        {
            Path         = f,
            LastModified = System.IO.File.GetLastWriteTime(f),
        });
        result.Sort((a, b) => b.LastModified.CompareTo(a.LastModified));
        return result;
    }

    private Task SendResultAsync(string commandId, bool success, string payload)
        => _hub!.InvokeAsync("CommandResult", _config.StationId, commandId, success, payload);
}
