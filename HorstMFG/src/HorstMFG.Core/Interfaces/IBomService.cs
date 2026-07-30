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
    /// Get a flat list of all PartLineItems (with denormalized Schedule/Order context) for the
    /// Orders page SfGrid grouped by ScheduleId → ScheduleOrderId.
    /// </summary>
    Task<List<FlatSchedulePartRow>> GetFlatSchedulePartsAsync(
        int? plantId = null, DateTime? fromDate = null, DateTime? toDate = null, string? searchTerm = null);

    /// <summary>
    /// Get a flat list of all PartLineItems (with denormalized Batch/Product context) for the
    /// Batches page SfGrid grouped by BatchId → BatchProductId.
    /// </summary>
    Task<List<FlatBatchPartRow>> GetFlatBatchPartsAsync(
        int? plantId = null, DateTime? fromDate = null, DateTime? toDate = null, string? searchTerm = null);

    /// <summary>
    /// Rename a Batch (top-level group in the Batches grid).
    /// </summary>
    Task UpdateBatchNameAsync(int batchId, string name);

    /// <summary>
    /// Update the Qty on a BatchProduct (product row in the Batches grid).
    /// </summary>
    Task UpdateBatchProductQtyAsync(int batchProductId, int qty);

    /// <summary>
    /// Update the ProductName (Vault item number) on a BatchProduct and reset VaultBomImported
    /// so the re-import button becomes available.
    /// </summary>
    Task UpdateBatchProductNameAsync(int batchProductId, string productName);

    /// <summary>
    /// Update the Qty on a ScheduleOrder (order row in the Orders grid).
    /// </summary>
    Task UpdateScheduleOrderQtyAsync(int scheduleOrderId, int qty);

    /// <summary>
    /// Update the ProductNumber on a ScheduleOrder and reset VaultBomImported so the
    /// re-import button becomes available.
    /// </summary>
    Task UpdateScheduleOrderProductNumberAsync(int scheduleOrderId, string? productNumber);

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
    /// Set IsStock = true for every PartLineItem belonging to the given BatchProduct.
    /// </summary>
    Task MarkAllPartsAsStockForBatchProductAsync(int batchProductId);

    /// <summary>
    /// Set IsStock = true for every PartLineItem belonging to any BatchProduct in the given Batch.
    /// </summary>
    Task MarkAllPartsAsStockForBatchAsync(int batchId);

    /// <summary>
    /// Set IsStock = true for all PartLineItems in the batch that share a PartNumber with any
    /// part in the given BatchProduct.
    /// </summary>
    Task MarkMatchingPartsAsStockAcrossBatchAsync(int batchProductId);

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

    /// <summary>
    /// Re-checks the network share for a PartLineItem's PDF and, if now present, copies it
    /// into the batch/schedule's local folder and marks HasPdf = true for every PartLineItem
    /// sharing that part number within the same Batch or Schedule. Returns false if the PDF
    /// still isn't on the share.
    /// </summary>
    Task<bool> RefreshPartPdfAsync(int partLineItemId);
}
