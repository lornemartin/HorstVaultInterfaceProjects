namespace HorstMFG.Core.Entities;

/// <summary>
/// Which order or batch item a Radan part stands for within one project. Written when Radan assigns
/// the part its ID (SendToNesting) and kept after Finalize, so a finalized project can still be
/// re-synced. Exactly one of OrderItemId / BatchItemId is set.
/// </summary>
public class RadanPlacement
{
    public int Id { get; set; }
    public int RadanProjectId { get; set; }
    public int RadanIdNumber { get; set; }
    public int? OrderItemId { get; set; }
    public int? BatchItemId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public RadanProject RadanProject { get; set; } = null!;
    public OrderItem? OrderItem { get; set; }
    public BatchItem? BatchItem { get; set; }
}
