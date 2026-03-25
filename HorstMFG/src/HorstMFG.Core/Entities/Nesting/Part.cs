using HorstMFG.Core.Enums;

namespace HorstMFG.Core.Entities;

public class Part
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public PartCategory Category { get; set; }
    public string? Material { get; set; }
    public decimal? Thickness { get; set; }
    public string? StructCode { get; set; }
    public string? Operations { get; set; }
    public bool HasBends { get; set; }
    public bool IsStock { get; set; }
    public string? Keywords { get; set; }
    public string? LifecycleState { get; set; }
    public byte[]? Thumbnail { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
