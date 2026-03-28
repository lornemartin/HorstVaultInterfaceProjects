using RadProject;
using System;
using System.IO;
using System.Linq;

namespace HorstMFG.Bridge.Nesting.Radan;

/// <summary>Low-level RPD file operations wrapping the RadanProject library.</summary>
internal static class RpdService
{
    public static RadanProject Load(string path)
    {
        var prj = new RadanProject();
        return prj.LoadData(path)
            ?? throw new InvalidOperationException($"Failed to load RPD: {path}");
    }

    public static void Save(RadanProject project, string path)
    {
        if (!project.SaveData(path))
            throw new InvalidOperationException($"Failed to save RPD: {path}");
    }

    public static void AddPart(RadanProject project, string symPath, int qty,
                               string? material, decimal thickness)
    {
        var part = new RadanPart(
            symFile:    symPath,
            id:         project.Parts.NextID,
            mat:        material ?? "",
            thick:      (double)thickness,
            thickUnits: "in",
            qty:        qty);

        project.AddPart(part);
    }

    public static void RemoveParts(RadanProject project, long[] nestingIds)
    {
        var toRemove = project.Parts.Part
            .Where(p => nestingIds.Contains(p.ID))
            .ToList();
        foreach (var p in toRemove)
            project.Parts.Part.Remove(p);
    }

    public static SyncPayload BuildSyncPayload(RadanProject project, string nestFolder)
    {
        var payload = new SyncPayload();

        // Parts
        foreach (var part in project.Parts.Values())
        {
            payload.Parts.Add(new SyncPart
            {
                RadanIdNumber = part.ID,
                QtyNested     = part.Made,
            });

            // Nests this part appears in
            if (part.UsedInNests == null) continue;
            foreach (var usedIn in part.UsedInNests)
            {
                var nest = payload.Nests.FirstOrDefault(n => n.NestName == usedIn.ID.ToString());
                if (nest == null)
                {
                    nest = new SyncNest
                    {
                        NestName = usedIn.ID.ToString(),
                        NestPath = ResolveNestPath(nestFolder, usedIn.ID),
                    };
                    payload.Nests.Add(nest);
                }
                nest.Parts.Add(new NestEntry
                {
                    RadanIdNumber = part.ID,
                    Qty           = usedIn.Made,
                });
            }
        }

        return payload;
    }

    public static string GetNestFolder(RadanProject project)
        => project.RadanSchedule?[0]?.JobDetails?[0]?.NestFolder ?? "";

    public static string CreateNewProject(string currentPath, DateTime date)
    {
        var currentDir  = Path.GetDirectoryName(currentPath)!;
        var projectsRoot = Path.GetDirectoryName(currentDir)!;
        var newName     = UniqueFolderName(projectsRoot, date.ToString("yyyy-MM-dd"));
        var newDir      = Path.Combine(projectsRoot, newName);

        Directory.CreateDirectory(newDir);
        Directory.CreateDirectory(Path.Combine(newDir, "nests"));
        Directory.CreateDirectory(Path.Combine(newDir, "Symbols"));
        Directory.CreateDirectory(Path.Combine(newDir, "remnants"));

        // Copy RPD as template, then clear project-specific data
        var newRpdPath = Path.Combine(newDir, newName + ".rpd");
        var project    = Load(currentPath);

        project.Parts.Part    = new System.Collections.Generic.List<RadanPart>();
        project.Parts.NextID  = 1;
        project.Nests         = new System.Collections.Generic.List<RadanNest>();

        if (project.RadanSchedule?.Length > 0 && project.RadanSchedule[0].JobDetails?.Length > 0)
        {
            var jd = project.RadanSchedule[0].JobDetails[0];
            jd.NestFolder         = Path.Combine(newDir, "nests");
            jd.RemnantSaveFolder  = Path.Combine(newDir, "remnants");
            jd.NextNestNum        = 1;
        }

        Save(project, newRpdPath);
        return newRpdPath;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string? ResolveNestPath(string nestFolder, long nestId)
    {
        if (string.IsNullOrWhiteSpace(nestFolder) || !Directory.Exists(nestFolder))
            return null;
        var candidates = Directory.GetFiles(nestFolder, $"{nestId}.drg");
        return candidates.Length > 0 ? candidates[0] : null;
    }

    private static string UniqueFolderName(string root, string baseName)
    {
        if (!Directory.Exists(Path.Combine(root, baseName)))
            return baseName;
        int i = 1;
        while (Directory.Exists(Path.Combine(root, $"{baseName}({i})")))
            i++;
        return $"{baseName}({i})";
    }
}
