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

        // Sync first to capture latest nested quantities before modifying the project
        var sync = _nesting.ReadSyncData(project);

        // Partition: parts with nothing nested get removed; parts with nesting get
        // their required qty adjusted down to match what's already nested.
        var nestedQty = sync.Parts.ToDictionary(p => p.RadanIdNumber, p => p.QtyNested);

        var toRemove  = items.Where(i => !nestedQty.TryGetValue(i.RadanIdNumber, out var q) || q == 0).ToList();
        var toAdjust  = items.Where(i =>  nestedQty.TryGetValue(i.RadanIdNumber, out var q) && q >  0).ToList();

        if (toRemove.Count > 0)
            _nesting.RemoveParts(project, toRemove.Select(i => i.RadanIdNumber).ToArray());

        if (toAdjust.Count > 0)
            _nesting.AdjustPartsQtyToMade(project, toAdjust.Select(i => i.RadanIdNumber).ToArray());

        _nesting.SaveProject(project, projectPath);
        _nesting.NotifyProjectChanged(projectPath);

        _log.LogInformation(
            "Retrieved {RemoveCount} part(s) from nesting project; adjusted qty for {AdjustCount} nested part(s)",
            toRemove.Count, toAdjust.Count);

        return new RetrieveFromNestingResult
        {
            Sync            = sync,
            ClearedItemIds  = toRemove.Select(i => i.ItemId).ToList(),
            AdjustedItemIds = toAdjust.Select(i => i.ItemId).ToList(),
        };
    }
}
