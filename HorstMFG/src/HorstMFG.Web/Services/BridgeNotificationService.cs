using System;

namespace HorstMFG.Web.Services;

/// <summary>
/// Singleton that relays bridge events (progress, results) to Blazor pages.
/// Pages subscribe/unsubscribe in OnAfterRenderAsync / Dispose.
/// </summary>
public class BridgeNotificationService
{
    /// <summary>stationId, commandId, message, percent</summary>
    public event Action<int, string, string, int>? ProgressReceived;

    /// <summary>stationId, commandId, success, jsonPayload</summary>
    public event Action<int, string, bool, string>? CommandCompleted;

    /// <summary>stationId, syncPayloadJson — fired by the FileWatcher auto-sync</summary>
    public event Action<int, string>? AutoSyncReceived;

    /// <summary>stationId, fileContents — fired when ItemExport writes a new BOM export file</summary>
    public event Action<int, string>? BomExportReceived;

    /// <summary>
    /// Most recent BOM export file contents waiting to be processed.
    /// Set when the export is detected; cleared by BomImport after processing.
    /// </summary>
    public string? PendingBomExport { get; private set; }

    public void ClearPendingBomExport() => PendingBomExport = null;

    /// <summary>stationId, projectName, projectPath</summary>
    public event Action<int, string?, string?>? StationProjectChanged;

    internal void OnProgress(int stationId, string commandId, string message, int percent)
        => ProgressReceived?.Invoke(stationId, commandId, message, percent);

    internal void OnCommandCompleted(int stationId, string commandId, bool success, string payload)
        => CommandCompleted?.Invoke(stationId, commandId, success, payload);

    internal void OnAutoSync(int stationId, string syncPayloadJson)
        => AutoSyncReceived?.Invoke(stationId, syncPayloadJson);

    internal void OnBomExportDetected(int stationId, string fileContents)
    {
        PendingBomExport = fileContents;
        BomExportReceived?.Invoke(stationId, fileContents);
    }

    internal void OnStationProjectChanged(int stationId, string? projectName, string? projectPath)
        => StationProjectChanged?.Invoke(stationId, projectName, projectPath);
}
