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
    Task<List<ExportTreeItem>> GetBatchTreeItemsAsync(int? plantId = null, bool includeProcessed = false, DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Get Schedule/ScheduleOrder/PartLineItem tree data for the Batches tab.
    /// </summary>
    Task<List<ExportTreeItem>> GetScheduleTreeItemsAsync(int? plantId = null, bool includeProcessed = false, DateTime? fromDate = null, DateTime? toDate = null);

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
