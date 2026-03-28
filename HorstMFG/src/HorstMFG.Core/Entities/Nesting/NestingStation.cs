namespace HorstMFG.Core.Entities;

public class NestingStation
{
    public int Id { get; set; }
    public int PlantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
    public string? ProjectPath { get; set; }
    public DateTime? BridgeLastSeen { get; set; }
    public string? BridgeVersion { get; set; }

    public Plant Plant { get; set; } = null!;
}
