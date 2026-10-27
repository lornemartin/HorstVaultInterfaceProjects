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
    /// Orders page SfGrid grouped by ScheduleId → ScheduleOrderId. When <paramref name="releasedOnly"/>
    /// is true, Schedules not yet released to production (ReadyForProduction == false) are excluded
    /// entirely — used to hide unreleased work from ShopFloor-only users.
    /// </summary>
    Task<List<FlatSchedulePartRow>> GetFlatSchedulePartsAsync(
        int? plantId = null, DateTime? fromDate = null, DateTime? toDate = null, string? searchTerm = null,
        bool releasedOnly = false);

    /// <summary>
    /// Get a flat list of all PartLineItems (with denormalized Batch/Product context) for the
    /// Batches page SfGrid grouped by BatchId → BatchProductId. When <paramref name="releasedOnly"/>
    /// is true, Batches not yet released to production (ReadyForProduction == false) are excluded
    /// entirely — used to hide unreleased work from ShopFloor-only users.
    /// </summary>
    Task<List<FlatBatchPartRow>> GetFlatBatchPartsAsync(
        int? plantId = null, DateTime? fromDate = null, DateTime? toDate = null, string? searchTerm = null,
        bool releasedOnly = false);

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
    /// Update PlantId on a BatchProduct and cascade the same value to every PartLineItem
    /// belonging to it, overwriting their individual (Vault-derived) PlantId.
    /// </summary>
    Task UpdateBatchProductPlantAsync(int batchProductId, int plantId);

    /// <summary>
    /// Update PlantId on a ScheduleOrder and cascade the same value to every PartLineItem
    /// belonging to it, overwriting their individual (Vault-derived) PlantId.
    /// </summary>
    Task UpdateScheduleOrderPlantAsync(int scheduleOrderId, int plantId);

    /// <summary>
    /// Update PlantId and/or PlantIdRaw on a single PartLineItem, independent of its parent
    /// BatchProduct/ScheduleOrder. plantIdRaw preserves free-typed text that doesn't match a
    /// known Plant; it is shown in the UI whenever plantId is null.
    /// </summary>
    Task UpdatePartPlantAsync(int partLineItemId, int? plantId, string? plantIdRaw);

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
    /// Update PlantId/PlantIdRaw for every PartLineItem that shares the same PartNumber within
    /// the same Batch or Schedule as the given partLineItemId.
    /// </summary>
    Task UpdatePartPlantForAllSiblingsAsync(int partLineItemId, int? plantId, string? plantIdRaw);

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
    /// Release a Batch to production: populates NestBatch and BatchItems for the batch's own
    /// home-plant parts only. Cross-plant accepted parts are released independently by the
    /// destination plant via ReleaseBatchProductHandoffAsync. Throws InvalidOperationException
    /// if already released, or if <paramref name="releasingPlantId"/> isn't the batch's own plant.
    /// </summary>
    Task ReleaseBatchToProductionAsync(int batchId, int releasingPlantId);

    /// <summary>
    /// Release a Schedule to production: populates NestOrders and OrderItems for the schedule's
    /// own home-plant parts only. Cross-plant accepted parts are released independently by the
    /// destination plant via ReleaseScheduleOrderHandoffAsync. Throws InvalidOperationException
    /// if already released, or if <paramref name="releasingPlantId"/> isn't the schedule's own plant.
    /// </summary>
    Task ReleaseScheduleToProductionAsync(int scheduleId, int releasingPlantId);

    /// <summary>
    /// Flips every Draft BatchProductHandoff under the given Batch to Pending, making them
    /// visible/actionable to the destination plant. Returns the number of rows flipped.
    /// </summary>
    Task<int> NotifyBatchHandoffsAsync(int batchId);

    /// <summary>
    /// Flips every Draft ScheduleOrderHandoff under the given Schedule to Pending, making them
    /// visible/actionable to the destination plant. Returns the number of rows flipped.
    /// </summary>
    Task<int> NotifyScheduleHandoffsAsync(int scheduleId);

    /// <summary>
    /// Accept a cross-plant BatchProductHandoff: sets Status to Accepted.
    /// </summary>
    Task AcceptBatchProductHandoffAsync(int handoffId, int userId);

    /// <summary>
    /// Reject a cross-plant BatchProductHandoff: sets Status to Rejected with a reason.
    /// The row is left in place as a permanent record.
    /// </summary>
    Task RejectBatchProductHandoffAsync(int handoffId, int userId, string? reason);

    /// <summary>
    /// Creates a fresh Pending BatchProductHandoff for (batchProductId, destinationPlantId) if
    /// the most recent row for that pair is Rejected and at least one live part still resolves
    /// to that destination plant. Returns 1 if a new row was created, 0 otherwise.
    /// </summary>
    Task<int> ResubmitBatchProductHandoffAsync(int batchProductId, int destinationPlantId);

    /// <summary>
    /// Independently releases every Accepted, not-yet-released BatchProductHandoff for
    /// (batchId, destinationPlantId) in one action — the destination plant's equivalent of
    /// ReleaseBatchToProductionAsync, producing a single NestBatch. Sets ReleasedDate/
    /// ReleasedByUser on each handoff released. Returns the new NestBatch's Id. Throws if
    /// nothing qualifies.
    /// </summary>
    Task<int> ReleaseBatchHandoffsAsync(int batchId, int destinationPlantId, int userId);

    /// <summary>
    /// Accept a cross-plant ScheduleOrderHandoff: sets Status to Accepted.
    /// </summary>
    Task AcceptScheduleOrderHandoffAsync(int handoffId, int userId);

    /// <summary>
    /// Reject a cross-plant ScheduleOrderHandoff: sets Status to Rejected with a reason.
    /// The row is left in place as a permanent record.
    /// </summary>
    Task RejectScheduleOrderHandoffAsync(int handoffId, int userId, string? reason);

    /// <summary>
    /// Creates a fresh Pending ScheduleOrderHandoff for (scheduleOrderId, destinationPlantId) if
    /// the most recent row for that pair is Rejected and at least one live part still resolves
    /// to that destination plant. Returns 1 if a new row was created, 0 otherwise.
    /// </summary>
    Task<int> ResubmitScheduleOrderHandoffAsync(int scheduleOrderId, int destinationPlantId);

    /// <summary>
    /// Independently releases every Accepted, not-yet-released ScheduleOrderHandoff for
    /// (scheduleId, destinationPlantId) in one action — the destination plant's equivalent of
    /// ReleaseScheduleToProductionAsync, producing one NestOrder per ScheduleOrder. Sets
    /// ReleasedDate/ReleasedByUser on each handoff released. Returns the new NestOrder ids.
    /// Throws if nothing qualifies.
    /// </summary>
    Task<List<int>> ReleaseScheduleHandoffsAsync(int scheduleId, int destinationPlantId, int userId);

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
