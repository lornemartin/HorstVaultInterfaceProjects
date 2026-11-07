namespace HorstMFG.Core.Entities;

public class NestingStation
{
    public int Id { get; set; }
    public int PlantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
    public string? ProjectPath { get; set; }
    /// <summary>
    /// Folder this station's Radan projects live in. Kept per database so each environment can use
    /// its own folder, and per station because stations can each have a local one (or share one).
    /// Sent to the station's bridge when it connects; null leaves the bridge on its own setting.
    /// </summary>
    public string? RadanProjectsRootPath { get; set; }
    public DateTime? BridgeLastSeen { get; set; }
    public string? BridgeVersion { get; set; }

    public Plant Plant { get; set; } = null!;
}
