namespace HorstMFG.Core.Entities;

public class PdfDocument
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int PartId { get; set; }
    public int BatchId { get; set; }
    public string? Department { get; set; }
    public DateTime GeneratedDate { get; set; } = DateTime.UtcNow;

    public Part Part { get; set; } = null!;
    public BomImportBatch Batch { get; set; } = null!;
}
