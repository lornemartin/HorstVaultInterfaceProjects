namespace HorstMFG.Core.Entities;

public class BatchItem
{
    public int Id { get; set; }
    public int NestBatchId { get; set; }
    public int PartId { get; set; }
    public int QtyRequired { get; set; }
    public int QtyNested { get; set; }
    public bool IsComplete { get; set; }
    public bool IsInRadanProject { get; set; }
    public int? RadanIdNumber { get; set; }
    public string? Notes { get; set; }

    public int? NestingStationId { get; set; }

    public NestBatch NestBatch { get; set; } = null!;
    public Part Part { get; set; } = null!;
    public NestingStation? NestingStation { get; set; }
    public ICollection<NestedPart> NestedParts { get; set; } = new List<NestedPart>();
}
