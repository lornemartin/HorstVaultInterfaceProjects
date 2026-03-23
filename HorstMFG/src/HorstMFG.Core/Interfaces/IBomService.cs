using HorstMFG.Core.DTOs;
using HorstMFG.Core.Entities;
using HorstMFG.Core.Enums;

namespace HorstMFG.Core.Interfaces;

public interface IBomService
{
    /// <summary>
    /// Parse a Vault Item Export tab-delimited file into BomExportLines.
    /// </summary>
    List<BomExportLine> ParseExportFile(Stream fileStream, BomType bomType);

    /// <summary>
    /// Import parsed MakeToOrder BOM lines: groups by Level-1 assembly into BatchProducts,
    /// creates PartLineItems, and copies PDFs to the local folder.
    /// </summary>
    Task<Batch> ImportBatchAsync(string name, int batchQty, int plantId, int userId, List<BomExportLine> lines);

    /// <summary>
    /// Import parsed MakeToStock BOM lines: creates a single ScheduleOrder with all lines
    /// as PartLineItems, and copies PDFs to the local folder.
    /// </summary>
    Task<Schedule> ImportScheduleAsync(string name, string orderNumber, int orderQty, int plantId, int userId, List<BomExportLine> lines);

    /// <summary>
    /// Get Batch/BatchProduct/PartLineItem tree data for the Daily Schedule tab.
    /// </summary>
    Task<List<ExportTreeItem>> GetBatchTreeItemsAsync(int? plantId = null, bool includeProcessed = false, DateTime? fromDate = null, DateTime? toDate = null, string? searchTerm = null);
    Task<List<ExportTreeItem>> GetBatchChildrenAsync(string batchName, int parentTreeId, int nextTreeId, bool includeProcessed = false);

    /// <summary>
    /// Get children for a batch tree row by parent TreeId (used by CustomAdaptor load-on-demand).
    /// TreeId scheme: Batch = batch.Id, BatchProduct = product.Id + 1_000_000, Part = part.Id + 100_000_000.
    /// </summary>
    Task<List<ExportTreeItem>> GetBatchChildrenByParentTreeIdAsync(int parentTreeId, bool includeProcessed = false);

    /// <summary>
    /// Get Schedule/ScheduleOrder/PartLineItem tree data for the Batches tab.
    /// </summary>
    Task<List<ExportTreeItem>> GetScheduleTreeItemsAsync(int? plantId = null, bool includeProcessed = false, DateTime? fromDate = null, DateTime? toDate = null, string? searchTerm = null);
    Task<List<ExportTreeItem>> GetScheduleChildrenAsync(string scheduleName, int parentTreeId, int nextTreeId, bool includeProcessed = false);

    /// <summary>
    /// Get children for a schedule tree row by parent TreeId (used by Web API load-on-demand).
    /// TreeId scheme: Schedule = schedule.Id, Order = order.Id + 1_000_000, Part = part.Id + 100_000_000.
    /// </summary>
    Task<List<ExportTreeItem>> GetScheduleChildrenByParentTreeIdAsync(int parentTreeId, bool includeProcessed = false);

    /// <summary>
    /// Update the IsStock flag on a PartLineItem.
    /// </summary>
    Task UpdatePartIsStockAsync(int partLineItemId, bool isStock);

    /// <summary>
    /// Check if a PDF exists on the share for the given part number.
    /// </summary>
    bool PdfExistsOnShare(string partNumber);

    /// <summary>
    /// Trigger PowerJobs script to generate a PDF for the given part number.
    /// Returns true if the script completed successfully.
    /// </summary>
    Task<bool> GeneratePdfAsync(string partNumber);
}
