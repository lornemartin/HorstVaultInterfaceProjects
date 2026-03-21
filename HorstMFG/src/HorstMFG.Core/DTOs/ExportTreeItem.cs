namespace HorstMFG.Core.DTOs;

/// <summary>
/// Flat DTO for Syncfusion TreeGrid with self-referencing 3-level hierarchy.
/// Level 1: Batch/Schedule row (IsBatchRow = true)
/// Level 2: BatchProduct/ScheduleOrder row (IsProductRow = true)
/// Level 3: PartLineItem leaf rows
/// </summary>
public class ExportTreeItem
{
    public int TreeId { get; set; }
    public int? TreeParentId { get; set; }
    public bool IsBatchRow { get; set; }
    public bool IsProductRow { get; set; }
    public bool IsExpanded { get; set; } = true;

    // Batch/Schedule-level fields (IsBatchRow = true)
    public string? BatchName { get; set; }
    public DateTime? ImportDate { get; set; }
    public string? PlantName { get; set; }
    public string? ImportedBy { get; set; }
    public bool ReadyForProduction { get; set; }
    public int ItemCount { get; set; }

    public bool HasChildren { get; set; }

    // Product/Order-level fields (IsProductRow = true)
    public string? ProductName { get; set; }
    public string? OrderNumber { get; set; }
    public int? ParentQty { get; set; }

    // Line-item fields (leaf rows)
    public string? Number { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int CategoryOrder { get; set; } = int.MaxValue;
    public string? Material { get; set; }
    public string? Thickness { get; set; }
    public string? Operations { get; set; }
    public int? Qty { get; set; }
    public bool IsStock { get; set; }
    public bool HasPdf { get; set; }
    public string? Notes { get; set; }
    public bool IsProcessed { get; set; }
}
