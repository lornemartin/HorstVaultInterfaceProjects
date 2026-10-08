using HorstMFG.Core.Enums;

namespace HorstMFG.Core.Entities;

/// <summary>
/// Tracks acceptance of a ScheduleOrder's parts assigned (via PartLineItem.PlantId) to a
/// plant other than the Schedule's home plant. Created pre-release as soon as such an
/// assignment appears; the destination plant must Accept before those parts become
/// visible/editable there, and before Release-to-Production will route them.
/// </summary>
public class ScheduleOrderHandoff
{
    public int Id { get; set; }
    public int ScheduleOrderId { get; set; }
    public int DestinationPlantId { get; set; }
    public HandoffStatus Status { get; set; } = HandoffStatus.Pending;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedDate { get; set; }
    public int? AcceptedByUserId { get; set; }
    public DateTime? RejectedDate { get; set; }
    public int? RejectedByUserId { get; set; }
    public string? RejectionReason { get; set; }

    public ScheduleOrder ScheduleOrder { get; set; } = null!;
    public Plant DestinationPlant { get; set; } = null!;
    public ApplicationUser? AcceptedByUser { get; set; }
    public ApplicationUser? RejectedByUser { get; set; }
}
