namespace HorstMFG.Core.Entities;

public class NestBatch
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public int PlantId { get; set; }
    public DateTime EntryDate { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public bool IsComplete { get; set; }
    public DateTime? CompletedDate { get; set; }

    public Batch Batch { get; set; } = null!;
    public Plant Plant { get; set; } = null!;
    public ICollection<BatchItem> Items { get; set; } = new List<BatchItem>();
}
