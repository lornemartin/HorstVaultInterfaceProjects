using System;
using System.Linq;
using ACW = Autodesk.Connectivity.WebServices;
using VA = VaultAccess;

namespace VaultPropertyLister;

/// <summary>
/// Lists Vault ITEM property definitions (display name → system name) so the system names
/// can be pasted into VaultBomQueryService.BuildBomMappings. Read-only login — no license used.
///
/// Usage: VaultPropertyLister &lt;server&gt; &lt;vault&gt; &lt;user&gt; [password] [filter]
///   filter  optional case-insensitive substring of the display name
/// </summary>
internal static class Program
{
    // Display names of the properties VaultBomQueryService still has TODO placeholders for,
    // paired with the constant each system name belongs in.
    private static readonly (string DisplayName, string Constant)[] Wanted =
    {
        ("P2 Plant ID",        "P2PlantIdSysName"),
        ("P2 Is Stock",        "P2IsStockSysName"),
        ("P1 Stock Exception", "P1StockExceptionSysName"),
        ("P2 Stock Exception", "P2StockExceptionSysName"),
        ("2nd Operation",      "SecondOperationSysName"),
        ("FinishOps",          "FinishOpsSysName"),
    };

    private static int Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: VaultPropertyLister <server> <vault> <user> [password] [filter]");
            return 1;
        }

        string server = args[0], vault = args[1], user = args[2];
        string password = args.Length > 3 ? args[3] : Prompt($"Password for {user}: ");
        string? filter = args.Length > 4 ? args[4] : null;

        var va = new VA.VaultAccess();
        var error = va.LoginHeadless(user, password, server, vault);
        if (!string.IsNullOrEmpty(error))
        {
            Console.Error.WriteLine($"Vault login failed: {error}");
            return 2;
        }

        try
        {
            ACW.PropDef[] defs = va.Connection.WebServiceManager.PropertyService
                .GetPropertyDefinitionsByEntityClassId("ITEM");

            var userDefs = defs
                .Where(d => !d.IsSys)
                .Where(d => filter == null || d.DispName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(d => d.DispName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            int width = Math.Max(12, userDefs.Select(d => d.DispName.Length).DefaultIfEmpty(0).Max());
            Console.WriteLine($"{"Display Name".PadRight(width)}  {"Type",-10}  System Name");
            Console.WriteLine(new string('-', width + 2 + 10 + 2 + 36));
            foreach (var d in userDefs)
                Console.WriteLine($"{d.DispName.PadRight(width)}  {d.Typ,-10}  {d.SysName}");

            Console.WriteLine();
            Console.WriteLine("// Paste into VaultAccess/VaultBomQueryService.cs:");
            foreach (var (displayName, constant) in Wanted)
            {
                var match = defs.FirstOrDefault(d => d.DispName.Equals(displayName, StringComparison.OrdinalIgnoreCase));
                Console.WriteLine(match != null
                    ? $"private const string {constant} = \"{match.SysName}\";"
                    : $"// {constant}: no ITEM property named \"{displayName}\" found");
            }
            return 0;
        }
        finally
        {
            va.CloseVaultConnection();
        }
    }

    private static string Prompt(string label)
    {
        Console.Write(label);
        var sb = new System.Text.StringBuilder();
        ConsoleKeyInfo key;
        while ((key = Console.ReadKey(intercept: true)).Key != ConsoleKey.Enter)
        {
            if (key.Key == ConsoleKey.Backspace) { if (sb.Length > 0) sb.Length--; }
            else sb.Append(key.KeyChar);
        }
        Console.WriteLine();
        return sb.ToString();
    }
}
