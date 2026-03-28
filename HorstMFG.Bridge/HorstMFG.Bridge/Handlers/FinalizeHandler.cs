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

        // Items with QtyNested = 0 are un-nested — cleared back to HorstMFG
        var clearedIds = sync.Parts
            .Where(p => p.QtyNested == 0)
            .Select(p => (int)p.RadanIdNumber)
            .ToList();

        reportProgress("Creating new project…", 60);
        var newProjectPath = _nesting.CreateNewProject(projectPath, DateTime.Today);
        var newProjectName = System.IO.Path.GetFileNameWithoutExtension(newProjectPath);

        _log.LogInformation("Finalized. New project: {Name}", newProjectName);
        reportProgress("Done.", 100);

        return new FinalizeResult
        {
            Sync           = sync,
            ClearedItemIds = clearedIds,
            NewProjectName = newProjectName,
            NewProjectPath = newProjectPath,
        };
    }
}
