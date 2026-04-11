using Ghostscript.NET;
using Ghostscript.NET.Processor;

/// <summary>
/// Runs Ghostscript via Ghostscript.NET against gsdll64.dll placed alongside the host executable.
/// No Ghostscript installation required — bundle gsdll64.dll in the output directory.
/// </summary>
internal static class GhostscriptRunner
{
    // Ghostscript is not re-entrant within a single process.
    private static readonly object _lock = new();

    private static GhostscriptVersionInfo GetVersion() =>
        new(Path.Combine(AppContext.BaseDirectory, "gsdll64.dll"));

    public static void Run(string[] args)
    {
        lock (_lock)
        {
            using var processor = new GhostscriptProcessor(GetVersion(), false);
            processor.Process(args, null);
        }
    }
}
