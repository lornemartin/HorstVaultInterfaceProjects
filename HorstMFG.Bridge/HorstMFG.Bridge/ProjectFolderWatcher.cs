using HorstMFG.Bridge.Nesting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace HorstMFG.Bridge;

/// <summary>
/// Watches every .rpd under the Radan projects folder and, when one other than the active project
/// is saved, reads it and sends it to HorstMFG as a ProjectResync. That keeps finalized projects
/// edited in Radan after the fact up to date without anyone having to remember to re-sync them.
/// The active project is left to FileWatcher. HorstMFG ignores projects it never nested in.
///
/// One recursive watcher is a single OS handle however many project folders there are. Radan
/// writes many other files while nesting, so the buffer is enlarged and an overflow is logged;
/// a missed save can still be caught with the manual Re-sync in the Browse dialog.
/// </summary>
public class ProjectFolderWatcher : IDisposable
{
    private readonly INestingProjectService _nesting;
    private readonly ILogger<ProjectFolderWatcher> _log;
    private readonly object _lock = new();
    private readonly Dictionary<string, Timer> _pending = new(StringComparer.OrdinalIgnoreCase);

    private FileSystemWatcher? _watcher;
    private HubConnection? _hub;
    private int _stationId;
    private Func<string?> _activeProjectPath = () => null;
    private volatile bool _suspended;
    private const int DebounceMs = 3000;

    public ProjectFolderWatcher(INestingProjectService nesting, ILogger<ProjectFolderWatcher> log)
    {
        _nesting = nesting;
        _log     = log;
    }

    /// <summary>(Re)starts watching rootPath. Safe to call again, from any thread — the server can
    /// send a new projects folder while the worker is attaching on connect.</summary>
    public void Attach(HubConnection hub, int stationId, string rootPath, Func<string?> activeProjectPath)
    {
        lock (_lock)
        {
            Detach();
            _hub               = hub;
            _stationId         = stationId;
            _activeProjectPath = activeProjectPath;

            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            {
                _log.LogWarning("ProjectFolderWatcher: projects folder '{Root}' not found — finalized projects won't re-sync automatically", rootPath);
                return;
            }

            _watcher = new FileSystemWatcher(rootPath, "*.rpd")
            {
                IncludeSubdirectories = true,
                NotifyFilter          = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                InternalBufferSize    = 64 * 1024,
            };
            _watcher.Changed += OnChanged;
            _watcher.Renamed += OnChanged;   // some savers write a temp file and rename it over the .rpd
            _watcher.Error   += (_, e) =>
                _log.LogWarning(e.GetException(), "ProjectFolderWatcher: watcher error (changes may have been missed)");
            _watcher.EnableRaisingEvents = true;
            _log.LogInformation("ProjectFolderWatcher: watching {Root} for saved projects", rootPath);
        }
    }

    /// <summary>Ignore saves while a bridge command is writing project files.</summary>
    public void Suspend() => _suspended = true;

    public void Resume() => _suspended = false;

    public void Detach()
    {
        lock (_lock)
        {
            _watcher?.Dispose();
            _watcher = null;
            foreach (var timer in _pending.Values) timer.Dispose();
            _pending.Clear();
        }
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        if (_suspended) return;
        if (!e.FullPath.EndsWith(".rpd", StringComparison.OrdinalIgnoreCase)) return;
        if (string.Equals(e.FullPath, _activeProjectPath(), StringComparison.OrdinalIgnoreCase)) return;

        // Debounce per file: Radan can write the same .rpd several times in one save.
        lock (_lock)
        {
            if (_pending.TryGetValue(e.FullPath, out var existing)) existing.Dispose();
            var path = e.FullPath;
            _pending[path] = new Timer(_ => Resync(path), null, DebounceMs, Timeout.Infinite);
        }
    }

    private void Resync(string path)
    {
        lock (_lock)
        {
            if (_pending.TryGetValue(path, out var timer)) { timer.Dispose(); _pending.Remove(path); }
        }
        if (_suspended || _hub == null) return;
        if (_hub.State != HubConnectionState.Connected)
        {
            _log.LogWarning("ProjectFolderWatcher: hub not connected — skipping re-sync of {Path}", path);
            return;
        }

        try
        {
            _log.LogInformation("ProjectFolderWatcher: {Path} saved — re-syncing", path);
            var payload = _nesting.ReadSyncDataFromFile(path);
            _hub.InvokeAsync("ProjectResync", _stationId, JsonSerializer.Serialize(payload)).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "ProjectFolderWatcher: re-sync of {Path} failed", path);
        }
    }

    public void Dispose() => Detach();
}
