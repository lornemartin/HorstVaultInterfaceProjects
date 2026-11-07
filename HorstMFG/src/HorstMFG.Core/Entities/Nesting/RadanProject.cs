using HorstMFG.Core.Enums;

namespace HorstMFG.Core.Entities;

/// <summary>
/// One Radan project folder. Keyed by folder rather than .rpd path because legacy RadanMaster data
/// only ever recorded the folder. Radan numbers parts and nests per project, so a part ID or nest
/// name only means something together with the project it belongs to.
/// </summary>
public class RadanProject
{
    public int Id { get; set; }
    public int PlantId { get; set; }
    public string ProjectDir { get; set; } = string.Empty;
    /// <summary>The .rpd file. Null for legacy projects.</summary>
    public string? RpdPath { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public RadanProjectStatus Status { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? FinalizedDate { get; set; }

    public Plant Plant { get; set; } = null!;
    public ICollection<Nest> Nests { get; set; } = new List<Nest>();
    public ICollection<RadanPlacement> Placements { get; set; } = new List<RadanPlacement>();
}
