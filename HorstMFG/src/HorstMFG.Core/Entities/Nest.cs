namespace HorstMFG.Core.Entities;

public class Nest
{
    public int Id { get; set; }
    public string NestName { get; set; } = string.Empty;
    public string? NestPath { get; set; }
    public byte[]? Thumbnail { get; set; }
    public int PlantId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public Plant Plant { get; set; } = null!;
    public ICollection<NestedPart> NestedParts { get; set; } = new List<NestedPart>();
}
