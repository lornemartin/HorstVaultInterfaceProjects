using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VA = VaultAccess;

namespace HorstMFG.VaultGateway;

public class VaultClient
{
    private readonly VaultConfig _config;
    private readonly ILogger<VaultClient> _log;

    // ImportJobRunner fires one job per submitted import batch with no queueing — two users
    // importing at the same time means two threads calling GetItemBom concurrently on this one
    // shared connection. That's unsafe: UpdateItem's promote-components sequence
    // (UpdatePromoteComponents -> GetPromoteComponentOrder -> PromoteComponents ->
    // GetPromoteComponentsResults) is scoped to the connection/session on Vault's server side,
    // not to an individual call, so interleaving it across threads can mix up two different
    // items' promoted data instead of just failing loudly. This also protects EnsureConnected's
    // reconnect-on-demand path, which otherwise has its own race if two threads find the
    // connection dead at the same time.
    private readonly SemaphoreSlim _lock = new(1, 1);

    private VA.VaultAccess? _va;
    private VA.VaultBomQueryService? _bomService;

    public VaultClient(IOptions<VaultConfig> config, ILogger<VaultClient> log)
    {
        _config = config.Value;
        _log = log;
    }

    public void Connect()
    {
        try
        {
            _va = new VA.VaultAccess();
            // Standard auth (not ReadOnly) — promote-components routine needs edit rights.
            var error = _va.LoginHeadlessForItems(_config.Username, _config.Password, _config.Server, _config.Vault);
            if (string.IsNullOrEmpty(error))
            {
                _bomService = new VA.VaultBomQueryService(_va);
                _log.LogInformation("Vault connection established ({Server}/{Vault})",
                                    _config.Server, _config.Vault);
            }
            else
            {
                _log.LogWarning("Vault login failed: {Error}", error);
                _va = null;
                _bomService = null;
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Vault login failed");
            _va = null;
            _bomService = null;
        }
    }

    public VA.VaultBomResult? GetItemBom(string itemNumber, bool refreshFromSource)
    {
        _lock.Wait();
        try
        {
            EnsureConnected();
            return _bomService!.GetItemBom(itemNumber, refreshFromSource);
        }
        finally
        {
            _lock.Release();
        }
    }

    private void EnsureConnected()
    {
        if (_va != null && _va.IsConnectionActive() && _bomService != null) return;

        _log.LogWarning("Vault connection lost — reconnecting");
        Connect();

        if (_va == null || !_va.IsConnectionActive() || _bomService == null)
            throw new InvalidOperationException("Vault is not connected.");
    }
}
