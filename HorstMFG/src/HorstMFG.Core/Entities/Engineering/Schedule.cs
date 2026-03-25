namespace HorstMFG.Core.Entities;

public class Schedule : ImportRecord
{
    public ICollection<ScheduleOrder> ScheduleOrders { get; set; } = new List<ScheduleOrder>();
}
