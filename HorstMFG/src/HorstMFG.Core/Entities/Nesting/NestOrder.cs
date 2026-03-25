namespace HorstMFG.Core.Entities;

public class NestOrder
{
    public int Id { get; set; }
    public int ScheduleOrderId { get; set; }
    public int PlantId { get; set; }
    public DateTime EntryDate { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public bool IsComplete { get; set; }
    public DateTime? CompletedDate { get; set; }

    public ScheduleOrder ScheduleOrder { get; set; } = null!;
    public Plant Plant { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
