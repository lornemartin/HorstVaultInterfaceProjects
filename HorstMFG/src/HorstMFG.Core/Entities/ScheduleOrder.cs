namespace HorstMFG.Core.Entities;

public class ScheduleOrder
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int Qty { get; set; } = 1;

    public Schedule Schedule { get; set; } = null!;
    public ICollection<PartLineItem> Parts { get; set; } = new List<PartLineItem>();
}
