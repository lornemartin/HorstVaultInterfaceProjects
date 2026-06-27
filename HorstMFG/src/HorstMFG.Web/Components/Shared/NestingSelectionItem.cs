namespace HorstMFG.Web.Components.Shared;

/// <summary>
/// Represents one grid row that the user has selected for a nesting command.
/// Populated by the production pages and passed to NestingPanel.
/// </summary>
public record NestingSelectionItem(
    int     ItemId,
    int     PartId,
    string? FileName,
    int     QtyRequired,
    int     QtyNested,
    string? Material,
    decimal Thickness,
    /// <summary>Order number for Order items; Batch name for Batch items. Used as the Symbols sub-folder.</summary>
    string? GroupName,
    long?   RadanIdNumber,
    bool    IsInRadanProject,
    string? Description,
    string? ScheduleName,
    string? BatchName,
    bool    HasBends
);
