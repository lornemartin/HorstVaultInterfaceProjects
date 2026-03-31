namespace HorstMFG.Bridge.Vault;

public interface IVaultService
{
    /// <summary>Logs into Vault and keeps the connection alive for subsequent calls.</summary>
    void Connect();

    /// <summary>Downloads the named part file from Vault and returns the local path of the downloaded file.</summary>
    string DownloadPart(string fileName, string targetFolder);
}
