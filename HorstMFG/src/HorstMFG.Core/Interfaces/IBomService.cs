using HorstMFG.Core.Entities;
using HorstMFG.Core.Enums;

namespace HorstMFG.Core.Interfaces;

public interface IBomService
{
    Task<BomImportBatch> ImportBatchAsync(string name, BomType bomType, int plantId, int userId);
    Task<IEnumerable<BomImportBatch>> GetBatchesAsync(int? plantId = null);
    Task<BomImportBatch?> GetBatchByIdAsync(int id);
    Task<IEnumerable<BomLineItem>> GetBomTreeAsync(int batchId);
    Task FinalizeBatchAsync(int batchId);
}
