using HorstMFG.Bridge.Nesting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HorstMFG.Bridge.Handlers;

public class FinalizeHandler
{
    private readonly INestingProjectService _nesting;
    private readonly ILogger<FinalizeHandler> _log;

    public FinalizeHandler(INestingProjectService nesting, ILogger<FinalizeHandler> log)
    {
        _nesting = nesting;
        _log     = log;
    }

    public FinalizeResult Execute(string projectPath, Action<string, int> reportProgress)
    {
        reportProgress("Syncing current project state…", 10);
        var project = _nesting.LoadProject(projectPath);
        var sync    = _nesting.ReadSyncData(project);

        // Same partition as RetrieveFromNesting:
        // - QtyNested == 0 → fully cleared (RadanIdNumber wiped)
        // - QtyNested  > 0 → adjusted (RadanIdNumber kept for future re-send)
        var clearedIds  = sync.Parts.Where(p => p.QtyNested == 0).Select(p => (int)p.RadanIdNumber).ToList();
        var adjustedIds = sync.Parts.Where(p => p.QtyNested  > 0).Select(p => (int)p.RadanIdNumber).ToList();

        reportProgress("Creating new project…", 60);
        var newProjectPath = _nesting.CreateNewProject(projectPath, DateTime.Today);
        var newProjectName = System.IO.Path.GetFileNameWithoutExtension(newProjectPath);

        reportProgress("Opening new project in Radan…", 85);
        _nesting.NotifyProjectChanged(newProjectPath);

        _log.LogInformation("Finalized. New project: {Name} — {Cleared} cleared, {Adjusted} adjusted",
            newProjectName, clearedIds.Count, adjustedIds.Count);
        reportProgress("Done.", 100);

        return new FinalizeResult
        {
            Sync            = sync,
            ClearedItemIds  = clearedIds,
            AdjustedItemIds = adjustedIds,
            NewProjectName  = newProjectName,
            NewProjectPath  = newProjectPath,
        };
    }
}
