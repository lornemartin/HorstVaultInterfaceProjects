using Microsoft.Extensions.Logging;

namespace HorstMFG.Web.Services;

/// <summary>
/// Field-level transformations applied to BOM rows during import. Shared
/// between the legacy TSV path (<see cref="BomService"/>) and the Vault
/// callback path (<see cref="VaultBomIngestService"/>) so the two stay
/// behaviorally identical.
/// </summary>
internal static class BomFieldMappers
{
    /// <summary>
    /// Thickness lookup from structural code, matching the legacy
    /// VaultItemProcessor logic.
    /// </summary>
    private static readonly Dictionary<string, string> StructCodeThickness = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SH-062"]  = "0.062",
        ["SH-075"]  = "0.075",
        ["SH-125"]  = "0.120",
        ["SH-188"]  = "0.188",
        ["SH-250"]  = "0.250",
        ["SH-312"]  = "0.312",
        ["SH-375"]  = "0.375",
        ["SH-500"]  = "0.500",
        ["SH-625"]  = "0.625",
        ["SH-750"]  = "0.750",
        ["SH-875"]  = "0.875",
        ["SH-1000"] = "1.000",
        ["SH-1250"] = "1.250",
        ["SH-1500"] = "1.500",
    };

    /// <summary>
    /// For Laser operations with no thickness and a known structural code,
    /// returns the derived thickness; otherwise returns the input thickness unchanged.
    /// </summary>
    public static string DeriveThicknessForLaser(string operations, string thickness, string structCode)
    {
        if (!string.IsNullOrEmpty(thickness)) return thickness;
        if (string.IsNullOrEmpty(structCode)) return thickness;
        if (!operations.Equals("Laser", StringComparison.OrdinalIgnoreCase)) return thickness;

        var codePrefix = structCode.Split(' ')[0];
        return StructCodeThickness.TryGetValue(codePrefix, out var derived) ? derived : thickness;
    }

    /// <summary>
    /// Vault's raw "Plant ID" custom-property text, mapped to our own Plant.Code. Vault can also
    /// send "Plant 1&amp;2" (a part shared between both plants) or blank — both intentionally
    /// return null here, since a single PartLineItem.PlantId FK can't represent "both plants".
    /// Callers should still store the raw string (PartLineItem.PlantIdRaw) so that case stays
    /// distinguishable from a genuine blank.
    /// </summary>
    private static readonly Dictionary<string, string> VaultPlantCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Plant 1"] = "P1",
        ["Plant 2"] = "P2",
        ["Plant 3"] = "P3",
    };

    public static string? MapVaultPlantCode(string? raw)
        => !string.IsNullOrWhiteSpace(raw) && VaultPlantCodes.TryGetValue(raw.Trim(), out var code)
            ? code
            : null;

    /// <summary>
    /// Strip <c>.ipt</c> or <c>.iam</c> extension if present (case-insensitive).
    /// </summary>
    public static string StripCadExtension(string number)
    {
        if (string.IsNullOrEmpty(number)) return number;
        if (number.EndsWith(".ipt", StringComparison.OrdinalIgnoreCase) ||
            number.EndsWith(".iam", StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetFileNameWithoutExtension(number);
        }
        return number;
    }

    /// <summary>
    /// File.Exists over the PDF share can spuriously return false: it swallows every
    /// exception internally, and a batch of concurrent first-time UNC connections from
    /// the service account (no persistent share mapping) has been observed to trigger
    /// transient auth/connection failures that look identical to "file not found".
    /// Falls back to an explicit open (which does throw) so real errors get logged
    /// instead of silently counting as missing.
    /// </summary>
    public static bool FileExistsWithRetry(string path, ILogger log, int maxAttempts = 3, int delayMs = 150)
    {
        if (File.Exists(path)) return true;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            Thread.Sleep(delayMs);
            try
            {
                using var stream = File.OpenRead(path);
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            catch (DirectoryNotFoundException)
            {
                return false;
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Transient error checking {Path} (attempt {Attempt}/{Max})",
                    path, attempt, maxAttempts);
            }
        }
        return false;
    }
}
