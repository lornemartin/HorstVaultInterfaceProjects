using RadanInterface2;
using RadProject;
using System;
using System.IO;

namespace HorstMFG.Bridge.Nesting.Radan;

/// <summary>Implements INestingProjectService using the Radan RPD file format.</summary>
public class RadanProjectService : INestingProjectService
{
    public NestingProjectData LoadProject(string path)
    {
        var prj = RpdService.Load(path);
        return new NestingProjectData { Path = path, Native = prj };
    }

    public void SaveProject(NestingProjectData project, string path)
    {
        RpdService.Save(Native(project), path);
        project.Path = path;
    }

    public long GetNextId(NestingProjectData project)
        => Native(project).Parts.NextID;

    public void AddPart(NestingProjectData project, string symPath, long nestingId,
                        int qty, string? material, decimal thickness)
    {
        // nestingId is ignored — AddPart reads NextID from the project and auto-increments
        RpdService.AddPart(Native(project), symPath, qty, material, thickness);
    }

    public void RemoveParts(NestingProjectData project, long[] nestingIds)
        => RpdService.RemoveParts(Native(project), nestingIds);

    public SyncPayload ReadSyncData(NestingProjectData project)
    {
        var prj        = Native(project);
        var nestFolder = RpdService.GetNestFolder(prj);
        return RpdService.BuildSyncPayload(prj, nestFolder);
    }

    public string CreateNewProject(string currentPath, DateTime date)
        => RpdService.CreateNewProject(currentPath, date);

    public byte[]? ExtractThumbnail(string symPath)
    {
        if (!File.Exists(symPath)) return null;
        try
        {
            var ri   = new RadanInterface();
            var chars = ri.GetThumbnailDataFromSym(symPath);
            if (chars == null || chars.Length == 0) return null;
            // The thumbnail is stored as a base64-encoded string inside the sym XML
            return Convert.FromBase64CharArray(chars, 0, chars.Length);
        }
        catch
        {
            return null;
        }
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private static RadanProject Native(NestingProjectData project)
        => project.Native as RadanProject
           ?? throw new InvalidOperationException("Project data is not a Radan project.");
}
