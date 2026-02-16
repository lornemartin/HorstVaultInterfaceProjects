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
    /// Import parsed BOM lines into the database, creating the batch, parts, and line items.
    /// </summary>
    Task<BomImportBatch> ImportBatchAsync(string name, BomType bomType, int plantId, int userId, List<BomExportLine> lines);

    Task<IEnumerable<BomImportBatch>> GetBatchesAsync(int? plantId = null);
    Task<BomImportBatch?> GetBatchByIdAsync(int id);
    Task<IEnumerable<BomLineItem>> GetBomTreeAsync(int batchId);
    Task FinalizeBatchAsync(int batchId);
}
