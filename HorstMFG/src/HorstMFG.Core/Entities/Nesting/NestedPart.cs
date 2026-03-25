namespace HorstMFG.Core.Entities;

public class NestedPart
{
    public int Id { get; set; }
    public int NestId { get; set; }
    public int OrderItemId { get; set; }
    public int Qty { get; set; }

    public Nest Nest { get; set; } = null!;
    public OrderItem OrderItem { get; set; } = null!;
}
