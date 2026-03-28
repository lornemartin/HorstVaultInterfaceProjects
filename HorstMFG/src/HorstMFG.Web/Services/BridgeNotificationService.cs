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

    /// <summary>stationId, projectName, projectPath</summary>
    public event Action<int, string?, string?>? StationProjectChanged;

    internal void OnProgress(int stationId, string commandId, string message, int percent)
        => ProgressReceived?.Invoke(stationId, commandId, message, percent);

    internal void OnCommandCompleted(int stationId, string commandId, bool success, string payload)
        => CommandCompleted?.Invoke(stationId, commandId, success, payload);

    internal void OnStationProjectChanged(int stationId, string? projectName, string? projectPath)
        => StationProjectChanged?.Invoke(stationId, projectName, projectPath);
}
