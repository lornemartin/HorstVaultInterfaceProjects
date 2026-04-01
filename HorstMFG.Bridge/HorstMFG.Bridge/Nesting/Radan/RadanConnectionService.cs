using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RadanInterface2;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HorstMFG.Bridge.Nesting.Radan;

/// <summary>
/// Background service that keeps a live connection to the Radan COM server.
/// Polls every 5 seconds and calls Initialize() whenever IsActive() returns false,
/// so handlers never need to initialize on their own.
/// </summary>
public class RadanConnectionService : BackgroundService
{
    private readonly ILogger<RadanConnectionService> _log;
    private bool _wasConnected = false;

    public RadanConnectionService(ILogger<RadanConnectionService> log)
    {
        _log = log;
    }

    public bool IsConnected => new RadanInterface().IsActive();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("Radan connection monitor started — waiting for Radan to launch");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var ri = new RadanInterface();
                if (!ri.IsActive())
                {
                    bool ok = ri.Initialize();
                    if (ok)
                    {
                        _log.LogInformation("Connected to Radan");
                        _wasConnected = true;
                    }
                    else if (_wasConnected)
                    {
                        _log.LogWarning("Radan disconnected — will retry");
                        _wasConnected = false;
                    }
                }
                else if (!_wasConnected)
                {
                    // Was already active when we first checked (e.g. reconnect after app restart)
                    _log.LogInformation("Connected to Radan");
                    _wasConnected = true;
                }
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Radan probe failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
