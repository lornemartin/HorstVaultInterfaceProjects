using HorstMFG.Bridge.Nesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HorstMFG.Bridge.Handlers;

public class UpdateThumbnailHandler
{
    private readonly INestingProjectService _nesting;
    private readonly BridgeConfig _config;
    private readonly ILogger<UpdateThumbnailHandler> _log;

    public UpdateThumbnailHandler(INestingProjectService nesting, IOptions<BridgeConfig> config,
                                  ILogger<UpdateThumbnailHandler> log)
    {
        _nesting = nesting;
        _config  = config.Value;
        _log     = log;
    }

    public List<UpdateThumbnailResult> Execute(List<UpdateThumbnailItem> items,
                                               Action<string, int> reportProgress)
    {
        _log.LogInformation("UpdateThumbnail: received {Count} item(s)", items.Count);

        var results = new List<UpdateThumbnailResult>();

        for (int i = 0; i < items.Count; i++)
        {
            var item     = items[i];
            var baseName = Path.GetFileNameWithoutExtension(item.FileName);
            var symPath  = Path.Combine(_config.SymNetworkSharePath, baseName + ".sym");
            bool exists  = File.Exists(symPath);

            _log.LogInformation("Item {Index}/{Total}: FileName={FileName} → symPath={SymPath} exists={Exists}",
                i + 1, items.Count, item.FileName, symPath, exists);

            reportProgress($"Extracting thumbnail for {item.FileName} ({i + 1}/{items.Count})",
                           (i + 1) * 100 / items.Count);

            if (!exists)
            {
                _log.LogWarning("Sym file not found: {SymPath}", symPath);
                results.Add(new UpdateThumbnailResult { PartId = item.PartId, Success = false });
                continue;
            }

            try
            {
                var bytes = _nesting.ExtractThumbnail(symPath);
                _log.LogInformation("ExtractThumbnail for {FileName}: {Bytes}",
                    item.FileName, bytes != null ? $"{bytes.Length} bytes" : "null");
                results.Add(new UpdateThumbnailResult
                {
                    PartId         = item.PartId,
                    ThumbnailBytes = bytes,
                    Success        = bytes != null,
                });
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to extract thumbnail for {FileName}", item.FileName);
                results.Add(new UpdateThumbnailResult { PartId = item.PartId, Success = false });
            }
        }

        _log.LogInformation("UpdateThumbnail complete: {Success}/{Total} part(s) had thumbnail data",
            results.Count(r => r.Success), results.Count);
        return results;
    }
}
