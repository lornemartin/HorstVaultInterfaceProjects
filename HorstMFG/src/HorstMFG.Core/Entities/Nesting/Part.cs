namespace HorstMFG.Core.Entities;

public class Part
{
    public int Id { get; set; }
    public string? FileName { get; set; }
    public string? Description { get; set; }
    public string? Material { get; set; }
    public decimal? Thickness { get; set; }
    public byte[]? Thumbnail { get; set; }
    public bool HasBends { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
