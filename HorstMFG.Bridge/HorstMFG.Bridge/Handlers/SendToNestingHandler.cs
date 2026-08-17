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

        // One DB item = one Radan part. Even when several items share a FileName
        // (same part number needed by different orders), each gets its own Radan entry
        // so sync/retrieve/finalize never have to split a combined qty back across items.
        for (int idx = 0; idx < items.Count; idx++)
        {
            var item = items[idx];

            reportProgress($"Sending {item.FileName} ({idx + 1}/{items.Count})",
                           (idx + 1) * 100 / items.Count);

            var symSharePath   = Path.Combine(_config.SymNetworkSharePath, item.FileName + ".sym");
            var missingSymFile = !File.Exists(symSharePath);

            _log.LogInformation(
                "Item {Index}/{Total}: FileName={FileName} qty={Qty} symExists={Exists}",
                idx + 1, items.Count, item.FileName, item.QtyRequired, !missingSymFile);

            var destDir = Path.Combine(projectFolder, "Symbols",
                                       item.OrderNumber ?? "Unknown",
                                       projectName,
                                       item.FileName);
            var destSym = Path.Combine(destDir, item.FileName + ".sym");

            if (!missingSymFile)
            {
                Directory.CreateDirectory(destDir);
                File.Copy(symSharePath, destSym, overwrite: true);
                var orderNumber = item.ItemType == "Order" ? item.OrderNumber : null;
                _nesting.SetPartAttributes(destSym, item.Material, item.Thickness,
                    item.Description, orderNumber, item.ScheduleName, item.BatchName, item.HasBends);
                _log.LogInformation("Copied {Sym} to project", item.FileName);
            }
            else
            {
                _log.LogWarning("Symbol file not found for {FileName} — adding without sym", item.FileName);
            }

            // Reuse this item's own existing RadanIdNumber if it's still present in the project.
            // The ID may not exist (e.g. carried over from a previous project), in which case we
            // fall through and add the part as new.
            long partId;
            if (item.RadanIdNumber.HasValue &&
                _nesting.UpdatePartQty(project, item.RadanIdNumber.Value, item.QtyRequired))
            {
                partId = item.RadanIdNumber.Value;
                _log.LogInformation("Updated qty for existing Radan ID {Id} to {Qty}", partId, item.QtyRequired);
            }
            else
            {
                partId = _nesting.GetNextId(project);
                _nesting.AddPart(project, destSym, partId, item.QtyRequired, item.Material, item.Thickness);
                _log.LogInformation("Added new Radan part ID {Id} qty {Qty} (existingId={ExistingId} not in project)",
                    partId, item.QtyRequired, item.RadanIdNumber?.ToString() ?? "none");
            }

            results.Add(new SendToNestingResult
            {
                ItemId         = item.ItemId,
                RadanIdNumber  = partId,
                MissingSymFile = missingSymFile,
            });
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
