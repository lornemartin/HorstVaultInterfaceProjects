using PrintPDF;
using System;
using System.IO;

namespace HorstPdfPrinter;

/// <summary>
/// Thin wrapper around PrintPDF.PrintObject that runs with [STAThread] on Main().
/// ApprenticeServerComponent requires an STA apartment; when spawned as a child process
/// the apartment state is controlled by this attribute, which is more reliable than
/// setting it on a background thread inside the parent process.
///
/// Args: &lt;idwPath&gt; &lt;outputFolder&gt; &lt;printerName&gt; [&lt;ghostscriptPath&gt;]
/// Exit: 0 = success, 1 = failure (error written to stderr)
/// </summary>
internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Usage: HorstPdfPrinter <idwPath> <outputFolder> <printerName> [<ghostscriptPath>]");
            return 1;
        }

        string idwPath      = args[0];
        // Ensure trailing backslash — PrintObject uses string concatenation for the output path,
        // so it requires the folder to end with a separator. The caller strips it to avoid the
        // Windows \"  quoting problem (a trailing \ before " escapes the closing quote).
        string outputFolder = args[1].TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
        string printerName  = args[2];
        string ghostscript   = args.Length >= 4 ? args[3] : "";

        if (!File.Exists(idwPath))
        {
            Console.Error.WriteLine($"IDW file not found: {idwPath}");
            return 1;
        }

        if (!string.IsNullOrEmpty(ghostscript))
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(AppSettings.SettingsFilePath)!);
                AppSettings.Set("GhostScriptWorkingFolder", ghostscript);
            }
            catch (Exception ex)
            {
                // Non-fatal — printToPDF may still work if AppSettings.xml already exists
                Console.Error.WriteLine($"Warning: Could not write AppSettings: {ex.Message}");
            }
        }

        try
        {
            string errMsg = "";
            string logMsg = "";
            bool ok = new PrintObject().printToPDF(idwPath, outputFolder, printerName,
                                                    ref errMsg, ref logMsg);

            if (!string.IsNullOrWhiteSpace(logMsg))
                Console.WriteLine(logMsg);

            if (ok)
                return 0;

            Console.Error.WriteLine(string.IsNullOrWhiteSpace(errMsg) ? "printToPDF returned false" : errMsg);
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
    }
}
