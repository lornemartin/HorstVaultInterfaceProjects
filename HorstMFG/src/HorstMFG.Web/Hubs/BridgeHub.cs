using HorstMFG.Infrastructure.Data;
using HorstMFG.Web.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
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

    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly BridgeNotificationService _notifications;
    private readonly string _expectedApiKey;
    private readonly ILogger<BridgeHub> _log;

    public BridgeHub(IDbContextFactory<ApplicationDbContext> dbFactory,
                     BridgeNotificationService notifications,
                     IConfiguration config,
                     ILogger<BridgeHub> log)
    {
        _dbFactory      = dbFactory;
        _notifications  = notifications;
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

        if (string.IsNullOrEmpty(_expectedApiKey) ||
            string.Equals(apiKey, _expectedApiKey, StringComparison.Ordinal))
        {
            _authorized[Context.ConnectionId] = true;
        }
        else
        {
            _log.LogWarning("Bridge connection rejected — invalid API key (connId={ConnId})",
                            Context.ConnectionId);
            Context.Abort();
        }

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
    public Task CommandResult(int stationId, string commandId, bool success, string payload)
    {
        _log.LogDebug("CommandResult station={StationId} cmd={CommandId} ok={Success}",
                      stationId, commandId, success);
        _notifications.OnCommandCompleted(stationId, commandId, success, payload);
        return Task.CompletedTask;
    }

    /// <summary>Called by the bridge to report command progress.</summary>
    public Task Progress(int stationId, string commandId, string message, int percent)
    {
        _notifications.OnProgress(stationId, commandId, message, percent);
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

        _ = hubContext.Clients.Client(connId)
                      .SendAsync("ExecuteCommand", commandId, commandType, payload);
        return true;
    }

    /// <summary>Returns true if a bridge for this station is currently connected.</summary>
    public static bool IsStationOnline(int stationId)
        => _stationToConn.ContainsKey(stationId);

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
