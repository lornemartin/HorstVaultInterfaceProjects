namespace HorstMFG.Core.Entities;

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int? BatchId { get; set; }
    public int PlantId { get; set; }
    public DateTime EntryDate { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public bool IsComplete { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? ProductNumber { get; set; }

    public Batch? Batch { get; set; }
    public Plant Plant { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
