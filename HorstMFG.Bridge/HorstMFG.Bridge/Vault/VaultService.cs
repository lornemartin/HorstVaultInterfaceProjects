using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;

namespace HorstMFG.Bridge.Vault;

public class VaultService : IVaultService
{
    private readonly VaultConfig _config;
    private readonly ILogger<VaultService> _log;

    public VaultService(IOptions<VaultConfig> config, ILogger<VaultService> log)
    {
        _config = config.Value;
        _log    = log;
    }

    public string DownloadPart(string fileName, string targetFolder)
    {
        _log.LogInformation("Retrieving {FileName} from Vault", fileName);

        var va = new VaultAccess.VaultAccess();
        va.Login(_config.Username, _config.Password, _config.Server, _config.Vault);

        try
        {
            bool ok = va.searrchAndDownloadFile(fileName, targetFolder);
            if (!ok)
                throw new FileNotFoundException($"Vault could not find {fileName}");

            // searrchAndDownloadFile downloads to targetFolder using the original filename
            var localPath = Path.Combine(targetFolder,
                Path.GetFileNameWithoutExtension(fileName) + ".ipt");
            if (!File.Exists(localPath))
                throw new FileNotFoundException($"Expected downloaded file not found: {localPath}");
            return localPath;
        }
        finally
        {
            va.CloseVaultConnection();
        }
    }
}
