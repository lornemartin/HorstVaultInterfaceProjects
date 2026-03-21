namespace HorstMFG.Web.Services;

public class BatchTreeFilterState
{
    public int? PlantId { get; set; }
    public bool IncludeProcessed { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    // Set by the adaptor on every ReadAsync call so the component can distinguish
    // root loads (no parent filter) from child loads (parent filter present).
    public bool LastReadWasRootLoad { get; set; }
}
