namespace HorstMFG.Web.Services;

public class BatchTreeFilterState
{
    public int? PlantId { get; set; }
    public bool IncludeProcessed { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
