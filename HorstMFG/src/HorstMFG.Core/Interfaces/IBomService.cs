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
    /// Update the Qty on a BatchProduct (product row in the Batches grid).
    /// </summary>
    Task UpdateBatchProductQtyAsync(int batchProductId, int qty);

    /// <summary>
    /// Update the Qty on a ScheduleOrder (order row in the Orders grid).
    /// </summary>
    Task UpdateScheduleOrderQtyAsync(int scheduleOrderId, int qty);

    /// <summary>
    /// Update the IsStock flag on a PartLineItem.
    /// </summary>
    Task UpdatePartIsStockAsync(int partLineItemId, bool isStock);

    /// <summary>
    /// Count how many PartLineItems share the same PartNumber within the same Batch or Schedule
    /// as the given partLineItemId.
    /// </summary>
    Task<int> CountSiblingsByPartNumberAsync(int partLineItemId);

    /// <summary>
    /// Update IsStock for every PartLineItem that shares the same PartNumber within the same
    /// Batch or Schedule as the given partLineItemId.
    /// </summary>
    Task UpdatePartIsStockForAllSiblingsAsync(int partLineItemId, bool isStock);

    /// <summary>
    /// Permanently delete a PartLineItem from the database.
    /// </summary>
    Task RemovePartLineItemAsync(int partLineItemId);

    /// <summary>
    /// Delete a Batch and all its BatchProducts and PartLineItems.
    /// </summary>
    Task DeleteBatchAsync(int batchId);

    /// <summary>
    /// Delete a BatchProduct and all its PartLineItems.
    /// </summary>
    Task DeleteBatchProductAsync(int batchProductId);

    /// <summary>
    /// Delete a Schedule and all its ScheduleOrders and PartLineItems.
    /// </summary>
    Task DeleteScheduleAsync(int scheduleId);

    /// <summary>
    /// Delete a ScheduleOrder and all its PartLineItems.
    /// </summary>
    Task DeleteScheduleOrderAsync(int scheduleOrderId);

    /// <summary>
    /// Release a Batch to production: populates NestBatch and BatchItems.
    /// Throws InvalidOperationException if already released.
    /// </summary>
    Task ReleaseBatchToProductionAsync(int batchId);

    /// <summary>
    /// Release a Schedule to production: populates NestOrders and OrderItems.
    /// Throws InvalidOperationException if already released.
    /// </summary>
    Task ReleaseScheduleToProductionAsync(int scheduleId);

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
