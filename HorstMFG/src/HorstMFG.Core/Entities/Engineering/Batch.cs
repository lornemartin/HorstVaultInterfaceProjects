namespace HorstMFG.Core.Entities;

public class Batch : ImportRecord
{
    public ICollection<BatchProduct> BatchProducts { get; set; } = new List<BatchProduct>();
}
