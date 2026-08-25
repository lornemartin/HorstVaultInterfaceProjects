using RadanInterface2;
using RadProject;
using System;
using System.IO;

namespace HorstMFG.Bridge.Nesting.Radan;

/// <summary>Implements INestingProjectService using the Radan RPD file format.</summary>
public class RadanProjectService : INestingProjectService
{
    public void FlushCurrentState()
    {
        try
        {
            var ri     = new RadanInterface();
            var errMsg = "";
            ri.SaveNest(ref errMsg);
            if (ri.isProjectOpen(ref errMsg))
                ri.SaveProject();
        }
        catch
        {
            // Radan may not be running — not fatal
        }
    }

    public NestingProjectData LoadProject(string path)
    {
        // Every caller (Send, Retrieve, Sync/AutoSync, Finalize) funnels through here, so
        // enforcing the live-Radan check in this one place covers all of them without
        // duplicating it at each call site. Refuse to proceed if Radan's actual open project
        // doesn't match — otherwise we'd read/write whatever's on disk at `path` regardless of
        // what the operator actually has open, which can silently produce stale or misdirected
        // data (see SendToNestingHandler's original guard for the incident this was built for).
        // Note: CreateNewProject's own use of RpdService.Load (a template-copy step that runs
        // AFTER this check has already passed for the project being closed) bypasses this
        // intentionally — it doesn't call this method.
        var openProject = GetOpenProjectPath();
        if (openProject == null || !string.Equals(openProject, path, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Radan's currently open project ('{openProject ?? "none"}') does not match " +
                $"the requested project ('{path}'). Open the correct project in Radan, or update " +
                "the active project in HorstMFG, before continuing.");

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

    public void AdjustPartsQtyToMade(NestingProjectData project, long[] nestingIds)
        => RpdService.AdjustPartQtyToMade(Native(project), nestingIds);

    public bool UpdatePartQty(NestingProjectData project, long nestingId, int qty, string expectedSymPath)
        => RpdService.UpdatePartQty(Native(project), nestingId, qty, expectedSymPath);

    public SyncPayload ReadSyncData(NestingProjectData project)
    {
        var prj        = Native(project);
        var nestFolder = RpdService.GetNestFolder(prj);
        var payload    = RpdService.BuildSyncPayload(prj, nestFolder);
        payload.ProjectPath = project.Path;
        return payload;
    }

    public string? GetOpenProjectPath()
    {
        try
        {
            var ri     = new RadanInterface();
            var errMsg = "";
            if (!ri.IsActive()) return null;
            var path = ri.getOpenProjectName(ref errMsg);
            return string.IsNullOrEmpty(path) ? null : path;
        }
        catch
        {
            return null;
        }
    }

    public void NotifyProjectChanged(string path)
    {
        try
        {
            var ri    = new RadanInterface();
            var errMsg = "";

            // Save the open nest first so quantities update immediately
            ri.SaveNest(ref errMsg);

            // Reload the project in Radan so new/removed parts become visible
            ri.LoadProject(path);
        }
        catch
        {
            // Radan may not be running — not fatal, the RPD file is already saved correctly
        }
    }

    public void SetPartAttributes(string symPath, string? material, decimal thickness,
                                  string? description, string? orderNumber,
                                  string? scheduleName, string? batchName, bool hasBends)
    {
        var ri      = new RadanInterface();
        var errMsg  = "";
        ri.InsertAdditionalAttributes(
            symPath,
            material     ?? "",
            thickness.ToString(),
            "in",
            description  ?? "",
            orderNumber  ?? "",
            scheduleName ?? "",
            batchName    ?? "",
            hasBends,
            ref errMsg);
    }

    public (string? Description, string? Material, decimal? Thickness) ReadPartAttributes(string symPath)
    {
        try
        {
            var ri = new RadanInterface();

            string? desc = null, mat = null, thkStr = null;
            try { desc   = ri.GetDescriptionFromSym(symPath); } catch { }
            try { mat    = ri.GetMaterialTypeFromSym(symPath); } catch { }
            try { thkStr = ri.GetThicknessFromSym(symPath);    } catch { }

            var cleanDesc = !string.IsNullOrWhiteSpace(desc) ? desc.Trim() : null;
            var cleanMat  = !string.IsNullOrWhiteSpace(mat) && !mat.StartsWith("Error") && !mat.StartsWith("No ")
                            ? mat.Trim() : null;
            decimal? thk  = decimal.TryParse(thkStr, out var t) && t > 0 ? t : null;

            return (cleanDesc, cleanMat, thk);
        }
        catch
        {
            return (null, null, null);
        }
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
