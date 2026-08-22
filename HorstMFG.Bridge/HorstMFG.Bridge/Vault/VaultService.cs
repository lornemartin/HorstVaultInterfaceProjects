using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;

namespace HorstMFG.Bridge.Vault;

public class VaultService : IVaultService
{
    private readonly VaultConfig _config;
    private readonly ILogger<VaultService> _log;

    private VaultAccess.VaultAccess? _va;

    public VaultService(IOptions<VaultConfig> config, ILogger<VaultService> log)
    {
        _config = config.Value;
        _log    = log;
    }

    public void Connect()
    {
        try
        {
            // Release the old session before replacing it — otherwise the server-side
            // connection/license seat from a dead connection is never freed, and repeated
            // reconnects can exhaust the account's concurrent-session limit over time.
            try { _va?.CloseVaultConnection(); } catch { /* best-effort */ }

            _va = new VaultAccess.VaultAccess();
            var error = _va.LoginHeadless(_config.Username, _config.Password, _config.Server, _config.Vault);
            if (string.IsNullOrEmpty(error))
                _log.LogInformation("Vault connection established ({Server}/{Vault})",
                                    _config.Server, _config.Vault);
            else
            {
                _log.LogWarning("Vault login failed: {Error}", error);
                _va = null;
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Vault login failed");
            _va = null;
        }
    }

    public string DownloadPart(string fileName, string targetFolder)
    {
        EnsureConnected();

        _log.LogInformation("Retrieving {FileName} from Vault", fileName);

        bool ok = _va!.searrchAndDownloadFile(fileName, targetFolder);
        if (!ok)
            throw new FileNotFoundException($"Vault could not find {fileName}");

        var localPath = Path.Combine(targetFolder,
            Path.GetFileNameWithoutExtension(fileName) + ".ipt");
        if (!File.Exists(localPath))
            throw new FileNotFoundException($"Expected downloaded file not found: {localPath}");

        return localPath;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void EnsureConnected()
    {
        if (_va != null && _va.IsConnectionActive()) return;

        _log.LogWarning("Vault connection lost — reconnecting");
        Connect();

        if (_va == null || !_va.IsConnectionActive())
            throw new InvalidOperationException("Vault is not connected.");
    }
}
