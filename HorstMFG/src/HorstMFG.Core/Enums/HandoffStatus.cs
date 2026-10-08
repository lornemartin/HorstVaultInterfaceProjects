namespace HorstMFG.Core.Enums;

/// <summary>
/// Cross-plant handoff state for a BatchProductHandoff/ScheduleOrderHandoff row.
/// A row only ever exists for an actual cross-plant assignment, so existence of a
/// row is itself the signal; it starts Pending and moves to Accepted or Rejected.
/// </summary>
public enum HandoffStatus
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2
}
