using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VA = VaultAccess;

namespace HorstMFG.VaultGateway;

public class VaultClient
{
    private readonly VaultConfig _config;
    private readonly ILogger<VaultClient> _log;

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
        EnsureConnected();
        return _bomService!.GetItemBom(itemNumber, refreshFromSource);
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
