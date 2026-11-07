namespace HorstMFG.DataTransfer;

// Rows as read from the legacy RadanMaster SQL Server database.

public record SrcOrder(
    int Id, string? OrderNumber, string? ScheduleName, string? BatchName,
    DateTime EntryDate, DateTime? DueDate, bool IsComplete, bool IsBatch, DateTime? DateCompleted);

public record SrcOrderItem(
    int Id, int OrderId, bool IsComplete, int QtyRequired, int QtyNested,
    bool IsInProject, string? Notes, int PartId, int RadanIdNumber);

public record SrcPart(
    int Id, string? FileName, string? Description, string? Material,
    double? Thickness, byte[]? Thumbnail, bool HasBends);

/// <summary>NestPath is the Radan project folder; NestName is the nest's file name (e.g. "2234.drg").</summary>
public record SrcNest(int Id, string? NestName, string? NestPath, byte[]? Thumbnail);

public record SrcNestedPart(int Id, int Qty, int NestId, int OrderItemId);

public record SourceData(
    List<SrcPart> Parts,
    List<SrcNest> Nests,
    List<SrcOrder> Orders,
    List<SrcOrderItem> OrderItems,
    List<SrcNestedPart> NestedParts);
