using HorstMFG.Core.Entities;

namespace HorstMFG.Core.Interfaces;

public interface IPdfService
{
    Task<IEnumerable<PdfDocument>> GenerateBatchPdfsAsync(int batchId);
    Task<IEnumerable<PdfDocument>> GetPdfsByBatchAsync(int batchId);
    Task<PdfDocument?> GetPdfByIdAsync(int id);
}
