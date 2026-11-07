namespace HorstMFG.Core.Entities;

public class Nest
{
    public int Id { get; set; }
    public string NestName { get; set; } = string.Empty;
    public string? NestPath { get; set; }
    public byte[]? Thumbnail { get; set; }
    public int PlantId { get; set; }
    /// <summary>Null only for rows that predate projects and couldn't be matched to one.</summary>
    public int? RadanProjectId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public Plant Plant { get; set; } = null!;
    public RadanProject? RadanProject { get; set; }
    public ICollection<NestedPart> NestedParts { get; set; } = new List<NestedPart>();
}
