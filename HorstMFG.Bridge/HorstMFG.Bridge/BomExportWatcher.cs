using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HorstMFG.Bridge;

/// <summary>
/// Watches the ItemExport output file for changes and pushes the contents
/// to HorstMFG so the BOM Import page can be pre-populated automatically.
/// </summary>
public class BomExportWatcher : IDisposable
{
    private readonly ILogger<BomExportWatcher> _log;

    private FileSystemWatcher? _watcher;
    private HubConnection?     _hub;
    private int                _stationId;
    private string?            _filePath;
    private Timer?             _debounceTimer;
    private const int          DebounceMs = 2000;

    public BomExportWatcher(ILogger<BomExportWatcher> log)
    {
        _log = log;
    }

    public void Attach(HubConnection hub, int stationId, string filePath)
    {
        _hub       = hub;
        _stationId = stationId;
        _filePath  = filePath;

        _watcher?.Dispose();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            _log.LogDebug("BomExportWatcher: no export file path configured — not watching");
            return;
        }

        var dir  = Path.GetDirectoryName(filePath)!;
        var file = Path.GetFileName(filePath);

        if (!Directory.Exists(dir))
        {
            _log.LogWarning("BomExportWatcher: directory does not exist: {Dir}", dir);
            return;
        }

        _watcher = new FileSystemWatcher(dir, file)
        {
            NotifyFilter            = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents     = true,
        };

        _watcher.Changed += OnChanged;
        _log.LogInformation("BomExportWatcher: watching {Path}", filePath);
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
        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(_ => TriggerSend(), null, DebounceMs, Timeout.Infinite);
    }

    private void TriggerSend()
    {
        if (_hub == null || _filePath == null) return;

        if (_hub.State != HubConnectionState.Connected)
        {
            _log.LogWarning("BomExportWatcher: hub not connected (state={State}) — skipping send", _hub.State);
            return;
        }

        try
        {
            var contents = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(contents)) return;

            _log.LogInformation("BomExportWatcher: export file changed — sending to HorstMFG");
            _hub.InvokeAsync("BomExportDetected", _stationId, contents).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "BomExportWatcher: failed to send BOM export");
        }
    }

    public void Dispose() => Detach();
}
