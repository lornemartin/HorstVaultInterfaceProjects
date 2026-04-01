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
                // Always call Initialize() — Marshal.GetActiveObject queries the Windows ROT,
                // so it picks up a freshly restarted Radan automatically. IsActive() only checks
                // for a non-null reference and cannot detect a stale COM object after restart.
                var ri = new RadanInterface();
                bool ok = ri.Initialize();

                if (ok && !_wasConnected)
                {
                    _log.LogInformation("Connected to Radan");
                    _wasConnected = true;
                }
                else if (!ok && _wasConnected)
                {
                    _log.LogWarning("Radan disconnected — will retry every 5s");
                    _wasConnected = false;
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
