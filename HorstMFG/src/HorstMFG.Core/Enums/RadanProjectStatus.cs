namespace HorstMFG.Core.Enums;

/// <summary>
/// Lifecycle of a RadanProject. Open projects are being nested (a station's active project, or one
/// it was switched away from without finalizing); Finalized ones were rotated out by Finalize but
/// can still be re-synced after edits in Radan; Legacy ones were imported from RadanMaster with no
/// part links, so they're history only and never synced. Stored as text.
/// </summary>
public enum RadanProjectStatus
{
    Open,
    Finalized,
    Legacy
}
