using System.Collections.Concurrent;
using HorstMFG.Core.Enums;

namespace HorstMFG.Web.Services;

/// <summary>
/// In-memory registry of in-flight Vault BOM import jobs. The Excel import
/// services (Phase 6/7) call <see cref="Register"/> when they POST a batch to
/// the VaultGateway; the callback handler calls <see cref="GetImportType"/>
/// to know whether the callback's <c>trackingId</c> refers to a
/// <c>ScheduleOrder</c> (MakeToStock) or a <c>BatchProduct</c> (MakeToOrder).
/// State is in-memory by design — restart-on-failure means the user retries,
/// which creates a new jobId.
/// </summary>
public class BomImportJobTracker
{
    private readonly ConcurrentDictionary<string, BomType> _jobs = new();

    public void Register(string jobId, BomType importType)
        => _jobs[jobId] = importType;

    public BomType? GetImportType(string jobId)
        => _jobs.TryGetValue(jobId, out var t) ? t : null;

    public void Forget(string jobId)
        => _jobs.TryRemove(jobId, out _);
}
