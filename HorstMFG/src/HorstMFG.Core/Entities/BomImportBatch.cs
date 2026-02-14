using HorstMFG.Core.Enums;

namespace HorstMFG.Core.Entities;

public class BomImportBatch
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime ImportDate { get; set; } = DateTime.UtcNow;
    public BomType BomType { get; set; }
    public int PlantId { get; set; }
    public bool IsFinalized { get; set; }
    public DateTime? FinalizedDate { get; set; }
    public int ImportedByUserId { get; set; }

    public Plant Plant { get; set; } = null!;
    public ApplicationUser ImportedByUser { get; set; } = null!;
    public ICollection<BomLineItem> LineItems { get; set; } = new List<BomLineItem>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<PdfDocument> PdfDocuments { get; set; } = new List<PdfDocument>();
}
