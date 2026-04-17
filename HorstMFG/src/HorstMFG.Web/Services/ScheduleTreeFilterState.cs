namespace HorstMFG.Web.Services;

/// <summary>
/// Scoped state shared between DailySchedule.razor and ScheduleTreeAdaptor
/// so the adaptor knows which filters to apply when fetching tree data.
/// </summary>
public class ScheduleTreeFilterState
{
    public int? PlantId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SearchTerm { get; set; }
}
