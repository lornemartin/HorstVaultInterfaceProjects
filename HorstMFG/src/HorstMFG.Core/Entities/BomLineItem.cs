namespace HorstMFG.Core.Entities;

public class BomLineItem
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public int? ParentId { get; set; }
    public int PartId { get; set; }
    public string Number { get; set; } = string.Empty;
    public string? ParentNumber { get; set; }
    public int UnitQty { get; set; }
    public bool RequiresPdf { get; set; }
    public bool HasPdf { get; set; }
    public int Level { get; set; }
    public bool IsProcessed { get; set; }

    public BomImportBatch Batch { get; set; } = null!;
    public BomLineItem? Parent { get; set; }
    public Part Part { get; set; } = null!;
    public ICollection<BomLineItem> Children { get; set; } = new List<BomLineItem>();
}
