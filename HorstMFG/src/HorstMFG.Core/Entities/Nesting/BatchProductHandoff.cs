using HorstMFG.Core.Enums;

namespace HorstMFG.Core.Entities;

/// <summary>
/// Tracks acceptance of a BatchProduct's parts assigned (via PartLineItem.PlantId) to a
/// plant other than the Batch's home plant. Created pre-release as soon as such an
/// assignment appears, starting Draft (detected, not yet sent) until the sender notifies
/// the destination plant (Pending). The destination plant must Accept before those parts
/// become visible/editable there; once Accepted, the destination plant independently
/// releases its own portion to production (ReleasedDate/ReleasedByUser).
/// </summary>
public class BatchProductHandoff
{
    public int Id { get; set; }
    public int BatchProductId { get; set; }
    public int DestinationPlantId { get; set; }
    public HandoffStatus Status { get; set; } = HandoffStatus.Draft;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedDate { get; set; }
    public int? AcceptedByUserId { get; set; }
    public DateTime? RejectedDate { get; set; }
    public int? RejectedByUserId { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? ReleasedDate { get; set; }
    public int? ReleasedByUserId { get; set; }

    public BatchProduct BatchProduct { get; set; } = null!;
    public Plant DestinationPlant { get; set; } = null!;
    public ApplicationUser? AcceptedByUser { get; set; }
    public ApplicationUser? RejectedByUser { get; set; }
    public ApplicationUser? ReleasedByUser { get; set; }
}
