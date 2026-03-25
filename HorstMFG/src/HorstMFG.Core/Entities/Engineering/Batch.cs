namespace HorstMFG.Core.Entities;

public class Batch : ImportRecord
{
    public ICollection<BatchProduct> BatchProducts { get; set; } = new List<BatchProduct>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
