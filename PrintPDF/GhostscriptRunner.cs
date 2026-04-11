using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PrintPDF
{
    /// <summary>
    /// Runs Ghostscript via direct P/Invoke against gsdll64.dll placed alongside the host executable.
    /// Used instead of Ghostscript.NET NuGet because PrintPDF.dll is strong-named and cannot
    /// load unsigned assemblies in .NET Framework.
    /// </summary>
    internal static class GhostscriptRunner
    {
        // Ghostscript is not re-entrant within a single process.
        private static readonly object _lock = new object();

        [DllImport("gsdll64.dll", EntryPoint = "gsapi_new_instance",
                   CallingConvention = CallingConvention.Cdecl)]
        private static extern int gsapi_new_instance(out IntPtr pinstance, IntPtr caller_handle);

        [DllImport("gsdll64.dll", EntryPoint = "gsapi_init_with_args",
                   CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int gsapi_init_with_args(IntPtr instance, int argc, string[] argv);

        [DllImport("gsdll64.dll", EntryPoint = "gsapi_exit",
                   CallingConvention = CallingConvention.Cdecl)]
        private static extern int gsapi_exit(IntPtr instance);

        [DllImport("gsdll64.dll", EntryPoint = "gsapi_delete_instance",
                   CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsapi_delete_instance(IntPtr instance);

        /// <summary>Converts a PostScript file to PDF using the pdfwrite device.</summary>
        public static void PsToPdf(string psPath, string pdfPath)
        {
            lock (_lock)
            {
                int rc = gsapi_new_instance(out IntPtr instance, IntPtr.Zero);
                if (rc < 0)
                    throw new InvalidOperationException("gsapi_new_instance failed with code " + rc);
                try
                {
                    rc = gsapi_init_with_args(instance, 7, new[]
                    {
                        "gs",
                        "-dBATCH", "-dNOPAUSE", "-dQUIET",
                        "-sDEVICE=pdfwrite",
                        "-sOutputFile=" + pdfPath,
                        psPath
                    });
                    // e_Quit (-101) is the normal clean exit triggered by -dBATCH
                    if (rc < 0 && rc != -101)
                        throw new InvalidOperationException("Ghostscript exited with code " + rc);
                }
                finally
                {
                    gsapi_exit(instance);
                    gsapi_delete_instance(instance);
                }
            }
        }
    }
}
