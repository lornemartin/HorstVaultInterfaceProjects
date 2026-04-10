using System;

namespace HorstMFG.Bridge.Vault;

public interface IVaultService
{
    /// <summary>Logs into Vault and keeps the connection alive for subsequent calls.</summary>
    void Connect();

    /// <summary>Downloads the named part file from Vault and returns the local path of the downloaded file.</summary>
    string DownloadPart(string fileName, string targetFolder);

    /// <summary>
    /// Finds the IDW file(s) in Vault that contain drawing sheets for the given model file
    /// (e.g. "PART-001(Description).ipt"), downloads the first one to <paramref name="targetFolder"/>,
    /// and returns the local path of the downloaded IDW.
    /// </summary>
    string DownloadIDWForModel(string modelFileName, string targetFolder,
                               Action<string>? progress = null);
}
