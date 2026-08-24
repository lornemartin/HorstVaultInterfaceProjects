using HorstMFG.Infrastructure.Data;
using HorstMFG.Web.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace HorstMFG.Web.Hubs;

/// <summary>
/// SignalR hub that bridge instances connect to.
/// Each bridge authenticates with an API key (X-Api-Key header) and registers its StationId.
/// HorstMFG pages send commands via IHubContext&lt;BridgeHub&gt;.
/// </summary>
public class BridgeHub : Hub
{
    // connectionId → stationId  (bridge connections only)
    private static readonly ConcurrentDictionary<string, int> _connToStation = new();
    // stationId → connectionId  (latest connection wins)
    private static readonly ConcurrentDictionary<int, string> _stationToConn = new();
    // connections that passed API key validation
    private static readonly ConcurrentDictionary<string, bool> _authorized = new();
    // commandId → commandType — populated on send, removed on result
    private static readonly ConcurrentDictionary<string, string> _commandTypes = new();

    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly BridgeNotificationService _notifications;
    private readonly NestingSyncService _syncService;
    private readonly string _expectedApiKey;
    private readonly ILogger<BridgeHub> _log;

    public BridgeHub(IDbContextFactory<ApplicationDbContext> dbFactory,
                     BridgeNotificationService notifications,
                     NestingSyncService syncService,
                     IConfiguration config,
                     ILogger<BridgeHub> log)
    {
        _dbFactory      = dbFactory;
        _notifications  = notifications;
        _syncService    = syncService;
        _expectedApiKey = config["Bridge:ApiKey"] ?? "";
        _log            = log;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override Task OnConnectedAsync()
    {
        var httpCtx = Context.GetHttpContext();
        // Accept key from either custom header or SignalR's standard access_token query param
        var apiKey = httpCtx?.Request.Headers["X-Api-Key"].FirstOrDefault()
                  ?? httpCtx?.Request.Query["access_token"].FirstOrDefault()
                  ?? "";

        if (!string.IsNullOrEmpty(_expectedApiKey) &&
            !string.Equals(apiKey, _expectedApiKey, StringComparison.Ordinal))
        {
            // Middleware should have blocked this already; abort as defence-in-depth.
            _log.LogWarning("Bridge connection rejected by hub — invalid API key (connId={ConnId})",
                            Context.ConnectionId);
            Context.Abort();
            return Task.CompletedTask;  // don't call base after Abort
        }

        _authorized[Context.ConnectionId] = true;
        return base.OnConnectedAsync();
    }

    // ── Bridge → Server ───────────────────────────────────────────────────────

    /// <summary>Called by the bridge on connect to identify itself.</summary>
    public async Task Register(int stationId, string version)
    {
        var connId = Context.ConnectionId;

        if (!_authorized.ContainsKey(connId))
        {
            _log.LogWarning("Register called on unauthorized connection {ConnId}", connId);
            Context.Abort();
            return;
        }

        _connToStation[connId]    = stationId;
        _stationToConn[stationId] = connId;

        _log.LogInformation("Bridge station {StationId} connected (v{Version})", stationId, version);

        // Update DB heartbeat
        await using var db   = await _dbFactory.CreateDbContextAsync();
        var station = await db.NestingStations.FirstOrDefaultAsync(s => s.Id == stationId);
        if (station != null)
        {
            station.BridgeLastSeen = DateTime.UtcNow;
            station.BridgeVersion  = version;
            await db.SaveChangesAsync();

            // Send current active project to bridge so it can start watching the file
            if (!string.IsNullOrEmpty(station.ProjectPath))
                await Clients.Caller.SendAsync("SetActiveProject", station.ProjectPath);
        }
    }

    /// <summary>Called by the bridge when a command finishes.</summary>
    public async Task CommandResult(int stationId, string commandId, bool success, string payload)
    {
        _commandTypes.TryRemove(commandId, out var commandType);
        _log.LogInformation("CommandResult station={StationId} cmd={CommandId} type={Type} ok={Success}",
                            stationId, commandId, commandType ?? "(null)", success);
        if (commandType == "UpdateThumbnail" && success)
            await HandleThumbnailResultAsync(payload);
        if (commandType == "Finalize" && success)
            await HandleFinalizeResultAsync(stationId, payload);
        _notifications.OnCommandCompleted(stationId, commandId, success, payload);
    }

    /// <summary>
    /// Applies the Finalize result here, server-side, rather than relying on whichever browser
    /// panel dispatched the command to still be connected when this arrives. Finalize's real-world
    /// effect (Radan rotating to a new project) is irreversible; the DB bookkeeping — new project
    /// path, clearing tracking, final NestedPart rows — must not silently no-op just because a
    /// browser tab closed/refreshed/disconnected in the meantime. NestingPanel.razor still applies
    /// the same result if it's connected to receive it (via OnCommandCompleted below), which is a
    /// harmless no-op re-application in that case.
    /// </summary>
    private async Task HandleFinalizeResultAsync(int stationId, string payload)
    {
        try
        {
            var result = JsonSerializer.Deserialize<FinalizeResultDto>(payload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (result == null) { _log.LogWarning("HandleFinalizeResult: payload deserialized to null"); return; }

            var rejection = await _syncService.ApplyFinalizeAsync(stationId, 0, result);
            if (rejection != null)
                _log.LogWarning("Finalize rejected for station {StationId}: {Reason}", stationId, rejection);
            else
                _log.LogInformation("Finalize applied server-side for station {StationId} — new project {Project}",
                    stationId, result.NewProjectName);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to apply Finalize result server-side for station {StationId}", stationId);
        }
    }

    private async Task HandleThumbnailResultAsync(string payload)
    {
        try
        {
            var results = JsonSerializer.Deserialize<List<ThumbnailResultDto>>(payload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (results == null) { _log.LogWarning("HandleThumbnailResult: payload deserialized to null"); return; }

            _log.LogInformation("HandleThumbnailResult: {Total} result(s), {Ok} successful",
                results.Count, results.Count(r => r.Success));

            await using var db = await _dbFactory.CreateDbContextAsync();
            int stored = 0, cleared = 0;
            foreach (var r in results)
            {
                var part = await db.Parts.FindAsync(r.PartId);
                if (part == null) { _log.LogWarning("PartId={PartId} not found in database", r.PartId); continue; }

                if (r.Success && r.ThumbnailBytes != null)
                {
                    part.Thumbnail = r.ThumbnailBytes;
                    stored++;
                    _log.LogInformation("Stored {Bytes} bytes for PartId={PartId}", r.ThumbnailBytes.Length, r.PartId);
                }
                else
                {
                    part.Thumbnail = null;
                    cleared++;
                    _log.LogInformation("Cleared thumbnail for PartId={PartId} (no sym file)", r.PartId);
                }

                if (part.Description == null && r.Description != null) part.Description = r.Description;
                if (part.Material    == null) part.Material = r.Material ?? "Steel, Mild";
                if (part.Thickness   == null && r.Thickness   != null) part.Thickness   = r.Thickness;
            }
            await db.SaveChangesAsync();
            _log.LogInformation("HandleThumbnailResult: stored={Stored} cleared={Cleared}", stored, cleared);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to store thumbnail results");
        }
    }

    private record ThumbnailResultDto(int PartId, byte[]? ThumbnailBytes, bool Success,
                                      string? Description, string? Material, decimal? Thickness);

    /// <summary>Called by the bridge to report command progress.</summary>
    public Task Progress(int stationId, string commandId, string message, int percent)
    {
        _notifications.OnProgress(stationId, commandId, message, percent);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called by the FileWatcher when the RPD file changes on disk.
    /// Treated as a spontaneous Sync result so the UI updates automatically.
    /// </summary>
    public async Task AutoSync(int stationId, string syncPayloadJson)
    {
        // Applied here, server-side, so it lands in the database whether or not any browser
        // panel happens to be open to catch the broadcast below (which is UI-refresh only now).
        try
        {
            var sync = JsonSerializer.Deserialize<SyncPayloadDto>(syncPayloadJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (sync != null)
            {
                var rejection = await _syncService.ApplySyncPayloadAsync(stationId, 0, sync);
                if (rejection != null)
                    _log.LogWarning("AutoSync rejected for station {StationId}: {Reason}", stationId, rejection);
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to apply AutoSync payload server-side for station {StationId}", stationId);
        }

        _notifications.OnAutoSync(stationId, syncPayloadJson);
    }

    /// <summary>
    /// Called by the BomExportWatcher when ItemExport writes a new export file.
    /// Forwards the raw file contents to any open BOM Import pages for pre-population.
    /// </summary>
    public Task BomExportDetected(int stationId, string fileContents)
    {
        _log.LogInformation("BOM export detected from station {StationId}", stationId);
        _notifications.OnBomExportDetected(stationId, fileContents);
        return Task.CompletedTask;
    }

    // ── Server → Bridge (called via IHubContext from Blazor pages) ────────────

    /// <summary>
    /// Send a command to a specific nesting station.
    /// Returns false if the station is not connected.
    /// </summary>
    public static bool TrySendCommand(IHubContext<BridgeHub> hubContext,
                                      int stationId,
                                      string commandId,
                                      string commandType,
                                      string payload)
    {
        if (!_stationToConn.TryGetValue(stationId, out var connId))
            return false;

        _commandTypes[commandId] = commandType;
        _ = hubContext.Clients.Client(connId)
                      .SendAsync("ExecuteCommand", commandId, commandType, payload);
        return true;
    }

    /// <summary>Returns true if a bridge for this station is currently connected.</summary>
    public static bool IsStationOnline(int stationId)
        => _stationToConn.ContainsKey(stationId);

    /// <summary>Returns any connected station ID, or null if no bridge is online.</summary>
    public static int? GetAnyOnlineStationId()
        => _stationToConn.IsEmpty ? null : _stationToConn.Keys.First();

    /// <summary>
    /// Tells the bridge for a given station to switch to a new active project.
    /// Returns false if the station is not connected.
    /// </summary>
    public static bool TrySendSetActiveProject(IHubContext<BridgeHub> hubContext,
                                               int stationId, string projectPath)
    {
        if (!_stationToConn.TryGetValue(stationId, out var connId)) return false;
        _ = hubContext.Clients.Client(connId).SendAsync("SetActiveProject", projectPath);
        return true;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var connId = Context.ConnectionId;
        _authorized.TryRemove(connId, out _);
        if (_connToStation.TryRemove(connId, out var stationId))
        {
            _stationToConn.TryRemove(stationId, out _);
            _log.LogInformation("Bridge station {StationId} disconnected", stationId);
        }
        return base.OnDisconnectedAsync(exception);
    }
}
