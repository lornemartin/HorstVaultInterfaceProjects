using HorstMFG.Bridge.Nesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;

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
                _log.LogInformation("Copied {Sym} to project", item.FileName);
            }
            else
            {
                _log.LogWarning("Symbol file not found for {FileName} — adding to project without sym", item.FileName);
            }

            var nextId = _nesting.GetNextId(project);
            _nesting.AddPart(project, destSym, nextId, item.QtyRequired, item.Material, item.Thickness);

            results.Add(new SendToNestingResult
            {
                ItemId         = item.ItemId,
                RadanIdNumber  = nextId,
                MissingSymFile = missingSymFile,
            });
        }

        _nesting.SaveProject(project, projectPath);
        _nesting.NotifyProjectChanged(projectPath);
        return results;
    }
}
