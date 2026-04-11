using HorstMFG.Bridge.Nesting;
using HorstMFG.Bridge.Vault;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace HorstMFG.Bridge.Handlers;

public class GeneratePdfHandler
{
    private readonly IVaultService _vault;
    private readonly BridgeConfig  _config;
    private readonly ILogger<GeneratePdfHandler> _log;

    public GeneratePdfHandler(IVaultService vault, IOptions<BridgeConfig> config,
                               ILogger<GeneratePdfHandler> log)
    {
        _vault  = vault;
        _config = config.Value;
        _log    = log;
    }

    public List<GeneratePdfResult> Execute(List<GeneratePdfItem> items,
                                           Action<string, int> reportProgress)
    {
        var results = new List<GeneratePdfResult>();
        var tempDir = Path.Combine(Path.GetTempPath(), "HorstBridgePdf_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        // HorstPdfPrinter.exe runs with [STAThread] on Main() — required for the COM
        // ApprenticeServerComponent used inside PrintPDF. It is built to the same output
        // directory as this executable.
        var printerExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HorstPdfPrinter.exe");

        try
        {
            var outDir  = _config.PdfSharePath.TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
            var printer = _config.PdfPrinterName;

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                int pct = (i * 100) / items.Count;
                reportProgress($"Generating PDF for {item.Number}…", pct);
                _log.LogInformation("Generating PDF for {Number}", item.Number);

                string? idwPath = null;
                try
                {
                    // Build the Vault model filename: number + extension based on category
                    var ext = item.Category.Equals("Part", StringComparison.OrdinalIgnoreCase)
                              ? ".ipt" : ".iam";
                    var modelFileName = item.Number + ext;

                    idwPath = _vault.DownloadIDWForModel(modelFileName, tempDir,
                        vaultMsg => reportProgress(vaultMsg, pct));

                    var args = $"\"{idwPath}\" \"{outDir.TrimEnd(Path.DirectorySeparatorChar)}\" \"{printer}\"";
                    _log.LogDebug("Launching HorstPdfPrinter: {Args}", args);

                    var psi = new ProcessStartInfo(printerExe, args)
                    {
                        UseShellExecute        = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError  = true,
                    };

                    using var proc = Process.Start(psi)!;
                    var stdout = proc.StandardOutput.ReadToEnd();
                    var stderr = proc.StandardError.ReadToEnd();
                    proc.WaitForExit();

                    if (!string.IsNullOrWhiteSpace(stdout))
                        _log.LogDebug("PrintPDF log for {Number}: {Log}", item.Number, stdout);

                    if (proc.ExitCode == 0)
                    {
                        _log.LogInformation("PDF generated for {Number}", item.Number);
                        results.Add(new GeneratePdfResult { Number = item.Number, Success = true });
                    }
                    else
                    {
                        var errMsg = string.IsNullOrWhiteSpace(stderr) ? $"HorstPdfPrinter exited {proc.ExitCode}" : stderr;
                        _log.LogWarning("PDF generation failed for {Number}: {Error}", item.Number, errMsg);
                        results.Add(new GeneratePdfResult { Number = item.Number, Success = false,
                                                             Error = errMsg });
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "PDF generation threw for {Number}", item.Number);
                    results.Add(new GeneratePdfResult { Number = item.Number, Success = false,
                                                         Error = ex.Message });
                }
                finally
                {
                    if (idwPath != null && File.Exists(idwPath))
                        try { File.Delete(idwPath); } catch { }
                }
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        reportProgress("PDF generation complete.", 100);
        return results;
    }
}
