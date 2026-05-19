using HorstMFG.Bridge.Nesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HorstMFG.Bridge.Handlers;

public class SendToNestingHandler
{
    private readonly INestingProjectService _nesting;
    private readonly BridgeConfig _config;
    private readonly ILogger<SendToNestingHandler> _log;

    public SendToNestingHandler(INestingProjectService nesting, IOptions<BridgeConfig> config,
                                ILogger<SendToNestingHandler> log)
    {
        _nesting = nesting;
        _config  = config.Value;
        _log     = log;
    }

    public List<SendToNestingResult> Execute(string projectPath,
                                             List<SendToNestingItem> items,
                                             Action<string, int> reportProgress)
    {
        _nesting.FlushCurrentState();
        var project = _nesting.LoadProject(projectPath);
        var results = new List<SendToNestingResult>();
        var projectFolder = Path.GetDirectoryName(projectPath)!;
        var projectName   = Path.GetFileNameWithoutExtension(projectPath);

        // Group by FileName — identical parts become one Radan entry with combined qty
        var groups = items
            .GroupBy(i => i.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        for (int g = 0; g < groups.Count; g++)
        {
            var group    = groups[g].ToList();
            var rep      = group[0];  // representative for sym copy and attribute writing
            var totalQty = group.Sum(i => i.QtyRequired);

            reportProgress($"Sending {rep.FileName} ({g + 1}/{groups.Count})",
                           (g + 1) * 100 / groups.Count);

            var symSharePath   = Path.Combine(_config.SymNetworkSharePath, rep.FileName + ".sym");
            var missingSymFile = !File.Exists(symSharePath);

            _log.LogInformation(
                "Group {Index}/{Total}: FileName={FileName} count={Count} totalQty={Qty} symExists={Exists}",
                g + 1, groups.Count, rep.FileName, group.Count, totalQty, !missingSymFile);

            var destDir = Path.Combine(projectFolder, "Symbols",
                                       rep.OrderNumber ?? "Unknown",
                                       projectName,
                                       rep.FileName);
            var destSym = Path.Combine(destDir, rep.FileName + ".sym");

            if (!missingSymFile)
            {
                Directory.CreateDirectory(destDir);
                File.Copy(symSharePath, destSym, overwrite: true);
                var orderNumber = rep.ItemType == "Order"
                    ? string.Join(", ", group.Select(i => i.OrderNumber)
                                            .Where(o => !string.IsNullOrEmpty(o))
                                            .Distinct())
                    : null;
                _nesting.SetPartAttributes(destSym, rep.Material, rep.Thickness,
                    rep.Description, orderNumber, rep.ScheduleName, rep.BatchName, rep.HasBends);
                _log.LogInformation("Copied {Sym} to project", rep.FileName);
            }
            else
            {
                _log.LogWarning("Symbol file not found for {FileName} — adding without sym", rep.FileName);
            }

            // If any item in the group already has a RadanIdNumber, reuse it (re-send)
            var existingId = group.Select(i => i.RadanIdNumber).FirstOrDefault(id => id.HasValue);

            long partId;
            if (existingId.HasValue)
            {
                _nesting.UpdatePartQty(project, existingId.Value, totalQty);
                partId = existingId.Value;
                _log.LogInformation("Updated qty for existing Radan ID {Id} to {Qty}", partId, totalQty);
            }
            else
            {
                partId = _nesting.GetNextId(project);
                _nesting.AddPart(project, destSym, partId, totalQty, rep.Material, rep.Thickness);
                _log.LogInformation("Added new Radan part ID {Id} qty {Qty}", partId, totalQty);
            }

            // All items in the group share the same RadanIdNumber
            foreach (var item in group)
            {
                results.Add(new SendToNestingResult
                {
                    ItemId         = item.ItemId,
                    RadanIdNumber  = partId,
                    MissingSymFile = missingSymFile,
                });
            }
        }

        _nesting.SaveProject(project, projectPath);

        // Read the saved RPD back from disk and verify each unique part is present
        var savedProject = _nesting.LoadProject(projectPath);
        var savedSync    = _nesting.ReadSyncData(savedProject);
        var savedPartIds = new System.Collections.Generic.HashSet<long>(
                              savedSync.Parts.Select(p => p.RadanIdNumber));
        foreach (var r in results)
        {
            r.VerifiedInProject = savedPartIds.Contains(r.RadanIdNumber);
            _log.LogInformation("ItemId={ItemId} FileName={FileName} RadanID={Id} verified={Verified}",
                r.ItemId,
                items.FirstOrDefault(i => i.ItemId == r.ItemId)?.FileName ?? "?",
                r.RadanIdNumber, r.VerifiedInProject);
        }

        _nesting.NotifyProjectChanged(projectPath);
        return results;
    }
}
