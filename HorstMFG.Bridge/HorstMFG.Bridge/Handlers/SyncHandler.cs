using HorstMFG.Bridge.Nesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HorstMFG.Bridge.Handlers;

public class SyncHandler
{
    private readonly INestingProjectService _nesting;
    private readonly BridgeConfig _config;
    private readonly ILogger<SyncHandler> _log;

    public SyncHandler(INestingProjectService nesting, IOptions<BridgeConfig> config,
                       ILogger<SyncHandler> log)
    {
        _nesting = nesting;
        _config  = config.Value;
        _log     = log;
    }

    public SyncPayload Execute(string projectPath)
    {
        _log.LogInformation("Sync: loading project {Path}", projectPath);
        _nesting.FlushCurrentState();
        var project = _nesting.LoadProject(projectPath);
        return _nesting.ReadSyncData(project);
    }
}
