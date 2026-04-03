using HorstMFG.Bridge.Handlers;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HorstMFG.Bridge;

/// <summary>
/// Watches the active .rpd file for changes made by the nesting software and
/// automatically pushes a Sync to HorstMFG when the file settles.
/// </summary>
public class FileWatcher : IDisposable
{
    private readonly SyncHandler _sync;
    private readonly ILogger<FileWatcher> _log;

    private FileSystemWatcher? _watcher;
    private HubConnection? _hub;
    private int _stationId;
    private string? _projectPath;
    private Timer? _debounceTimer;
    private const int DebounceMs = 2000;

    public FileWatcher(SyncHandler sync, ILogger<FileWatcher> log)
    {
        _sync = sync;
        _log  = log;
    }

    public void Attach(HubConnection hub, int stationId, string projectPath)
    {
        _hub        = hub;
        _stationId  = stationId;
        _projectPath = projectPath;

        _watcher?.Dispose();
        if (string.IsNullOrWhiteSpace(projectPath) || !File.Exists(projectPath))
        {
            _log.LogWarning("FileWatcher: project path not set or file does not exist — not watching");
            return;
        }

        _watcher = new FileSystemWatcher(
            Path.GetDirectoryName(projectPath)!,
            Path.GetFileName(projectPath))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };

        _watcher.Changed += OnChanged;
        _log.LogInformation("FileWatcher: watching {Path}", projectPath);
    }

    public void Detach()
    {
        _watcher?.Dispose();
        _watcher = null;
        _debounceTimer?.Dispose();
        _debounceTimer = null;
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        // Reset debounce timer on every change event
        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(_ => TriggerSync(), null, DebounceMs, Timeout.Infinite);
    }

    private void TriggerSync()
    {
        if (_hub == null || _projectPath == null) return;

        if (_hub.State != HubConnectionState.Connected)
        {
            _log.LogWarning("FileWatcher: hub not connected (state={State}) — skipping auto-sync", _hub.State);
            return;
        }

        try
        {
            _log.LogInformation("FileWatcher: RPD changed — auto-syncing");
            var payload = _sync.Execute(_projectPath);
            var json    = JsonSerializer.Serialize(payload);
            _hub.InvokeAsync("AutoSync", _stationId, json).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "FileWatcher: auto-sync failed");
        }
    }

    public void Dispose()
    {
        Detach();
    }
}
