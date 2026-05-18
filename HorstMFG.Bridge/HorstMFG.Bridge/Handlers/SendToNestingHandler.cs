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

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            reportProgress($"Sending {item.FileName} ({i + 1}/{items.Count})",
                           (i + 1) * 100 / items.Count);

            var symSharePath = Path.Combine(_config.SymNetworkSharePath, item.FileName + ".sym");
            var missingSymFile = !File.Exists(symSharePath);

            _log.LogInformation("Item {Index}/{Total}: FileName={FileName} → symPath={SymPath} exists={Exists}",
                i + 1, items.Count, item.FileName, symSharePath, !missingSymFile);

            // Determine destination sym path in project Symbols folder
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
                _log.LogWarning("Symbol file not found for {FileName} — adding to project without sym", item.FileName);
            }

            long partId;
            if (item.RadanIdNumber.HasValue)
            {
                // Part already exists in the project (re-send of a partially-nested item) — update qty only
                _nesting.UpdatePartQty(project, item.RadanIdNumber.Value, item.QtyRequired);
                partId = item.RadanIdNumber.Value;
                _log.LogInformation("Updated qty for existing part {Id} to {Qty}", partId, item.QtyRequired);
            }
            else
            {
                partId = _nesting.GetNextId(project);
                _nesting.AddPart(project, destSym, partId, item.QtyRequired, item.Material, item.Thickness);
            }

            results.Add(new SendToNestingResult
            {
                ItemId         = item.ItemId,
                RadanIdNumber  = partId,
                MissingSymFile = missingSymFile,
            });
        }

        _nesting.SaveProject(project, projectPath);

        // Read the saved RPD back from disk and verify each part is present
        var savedProject  = _nesting.LoadProject(projectPath);
        var savedSync     = _nesting.ReadSyncData(savedProject);
        var savedPartIds  = new System.Collections.Generic.HashSet<long>(
                               savedSync.Parts.Select(p => p.RadanIdNumber));
        foreach (var r in results)
        {
            r.VerifiedInProject = savedPartIds.Contains(r.RadanIdNumber);
            _log.LogInformation("Part {FileName} Radan ID={Id} verified={Verified}",
                items.FirstOrDefault(i => i.ItemId == r.ItemId)?.FileName ?? "?",
                r.RadanIdNumber, r.VerifiedInProject);
        }

        _nesting.NotifyProjectChanged(projectPath);
        return results;
    }
}
