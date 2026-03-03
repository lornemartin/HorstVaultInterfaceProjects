namespace HorstMFG.Core.Entities;

public class BatchProduct
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public string ProductName { get; set; } = string.Empty;

    public Batch Batch { get; set; } = null!;
    public ICollection<PartLineItem> Parts { get; set; } = new List<PartLineItem>();
}
