using HorstMFG.Bridge.Nesting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace HorstMFG.Bridge.Handlers;

public class RetrieveFromNestingHandler
{
    private readonly INestingProjectService _nesting;
    private readonly ILogger<RetrieveFromNestingHandler> _log;

    public RetrieveFromNestingHandler(INestingProjectService nesting,
                                      ILogger<RetrieveFromNestingHandler> log)
    {
        _nesting = nesting;
        _log     = log;
    }

    public RetrieveFromNestingResult Execute(
        string projectPath, List<RetrieveFromNestingItem> items)
    {
        var project = _nesting.LoadProject(projectPath);

        // Sync first to capture latest state before removing
        var sync = _nesting.ReadSyncData(project);

        var nestingIds = items.Select(i => i.RadanIdNumber).ToArray();
        _nesting.RemoveParts(project, nestingIds);
        _nesting.SaveProject(project, projectPath);
        _nesting.NotifyProjectChanged(projectPath);

        _log.LogInformation("Removed {Count} part(s) from nesting project", items.Count);

        return new RetrieveFromNestingResult
        {
            Sync           = sync,
            ClearedItemIds = items.Select(i => i.ItemId).ToList(),
        };
    }
}
