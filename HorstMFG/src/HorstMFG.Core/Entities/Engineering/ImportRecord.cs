namespace HorstMFG.Core.Entities;

public abstract class ImportRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime ImportDate { get; set; } = DateTime.UtcNow;
    public int PlantId { get; set; }
    public int ImportedByUserId { get; set; }
    public bool ReadyForProduction { get; set; }
    public string? LocalPdfFolder { get; set; }

    public Plant Plant { get; set; } = null!;
    public ApplicationUser ImportedByUser { get; set; } = null!;
}
