namespace HorstMFG.Core.Entities;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int PartId { get; set; }
    public int QtyRequired { get; set; }
    public int QtyNested { get; set; }
    public bool IsComplete { get; set; }
    public bool IsInRadanProject { get; set; }
    public int? RadanIdNumber { get; set; }
    public string? Notes { get; set; }

    public Order Order { get; set; } = null!;
    public Part Part { get; set; } = null!;
    public ICollection<NestedPart> NestedParts { get; set; } = new List<NestedPart>();
    public RadanIdAssignment? RadanIdAssignment { get; set; }
}
