namespace HorstMFG.Core.Enums;

/// <summary>
/// Cross-plant handoff state for a BatchProductHandoff/ScheduleOrderHandoff row.
/// A row only ever exists for an actual cross-plant assignment. It starts Draft
/// (detected, not yet sent — invisible to the destination plant), moves to Pending
/// once the sender explicitly notifies the destination plant, then to Accepted or
/// Rejected. Appended rather than renumbered so existing stored values keep their
/// meaning.
/// </summary>
public enum HandoffStatus
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2,
    Draft = 3
}
