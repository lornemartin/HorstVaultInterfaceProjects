namespace HorstMFG.Bridge.Vault;

public interface IVaultService
{
    /// <summary>Downloads the named part file from Vault and returns the local path of the downloaded file.</summary>
    string DownloadPart(string fileName, string targetFolder);
}
