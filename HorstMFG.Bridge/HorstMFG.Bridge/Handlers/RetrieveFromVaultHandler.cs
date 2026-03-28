using HorstMFG.Bridge.Nesting;
using HorstMFG.Bridge.Vault;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RadanInterface2;
using System;
using System.Collections.Generic;
using System.IO;

namespace HorstMFG.Bridge.Handlers;

public class RetrieveFromVaultHandler
{
    private readonly IVaultService _vault;
    private readonly INestingProjectService _nesting;
    private readonly BridgeConfig _config;
    private readonly ILogger<RetrieveFromVaultHandler> _log;

    public RetrieveFromVaultHandler(IVaultService vault, INestingProjectService nesting,
                                    IOptions<BridgeConfig> config,
                                    ILogger<RetrieveFromVaultHandler> log)
    {
        _vault   = vault;
        _nesting = nesting;
        _config  = config.Value;
        _log     = log;
    }

    public List<RetrieveFromVaultResult> Execute(List<RetrieveFromVaultItem> items,
                                                 Action<string, int> reportProgress)
    {
        var results  = new List<RetrieveFromVaultResult>();
        var tempDir  = Path.Combine(Path.GetTempPath(), "HorstMFGBridge");
        Directory.CreateDirectory(tempDir);

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            reportProgress($"Retrieving {item.FileName} from Vault ({i + 1}/{items.Count})",
                           (i + 1) * 100 / items.Count);
            try
            {
                // Download .ipt from Vault
                var iptPath = _vault.DownloadPart(item.FileName, tempDir);

                // Unfold via RadanInterface2 and save as .sym to network share
                var ri       = new RadanInterface();
                if (!ri.Initialize())
                    throw new InvalidOperationException("Radan is not running or could not be reached.");

                var errMsg     = "";
                var partName   = "";
                var material   = "";
                var thickness  = "";
                var topPattern = "";

                if (!ri.Open3DFileInRadan(iptPath, material, ref errMsg))
                    throw new InvalidOperationException($"Open3DFileInRadan failed: {errMsg}");

                if (!ri.UnfoldActive3DFile(ref partName, ref material, ref thickness, ref topPattern, ref errMsg))
                    throw new InvalidOperationException($"UnfoldActive3DFile failed: {errMsg}");

                var baseName = Path.GetFileNameWithoutExtension(item.FileName);
                var symPath  = Path.Combine(_config.SymNetworkSharePath, baseName + ".sym");

                if (!ri.SavePart(topPattern, symPath, ref errMsg))
                    throw new InvalidOperationException($"SavePart failed: {errMsg}");

                _log.LogInformation("Retrieved and converted {FileName} to sym", item.FileName);
                results.Add(new RetrieveFromVaultResult { ItemId = item.ItemId, Success = true });
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to retrieve {FileName} from Vault", item.FileName);
                results.Add(new RetrieveFromVaultResult
                {
                    ItemId  = item.ItemId,
                    Success = false,
                    Error   = ex.Message,
                });
            }
        }

        return results;
    }
}
