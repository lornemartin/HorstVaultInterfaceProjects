using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HorstMFG.Web.Services;

/// <summary>
/// Copies referenced PDFs from the network share into the local
/// <c>{LocalPdfPath}\Schedules\{name}\</c> or <c>\Batches\{name}\</c> folder.
/// Extracted from <see cref="BomService"/> so the Vault-callback ingest path
/// can reuse the same copy logic as the legacy TSV import.
/// </summary>
public class BomPdfCopyService
{
    private readonly string _pdfSharePath;
    private readonly string _localPdfPath;
    private readonly ILogger<BomPdfCopyService> _log;

    public BomPdfCopyService(IConfiguration config, ILogger<BomPdfCopyService> log)
    {
        _pdfSharePath = config["FileSystemPaths:PdfSharePath"] ?? @"S:\PDF Drawing Files\";
        _localPdfPath = config["FileSystemPaths:LocalPdfPath"] ?? @"C:\HorstMFG\PDFs\";
        _log = log;
    }

    public Task<(int Copied, int Total)> CopyForScheduleAsync(
        string scheduleName, IEnumerable<string> partNumbers, CancellationToken ct = default)
        => CopyAsync(Path.Combine(_localPdfPath, "Schedules", scheduleName), partNumbers, ct);

    public Task<(int Copied, int Total)> CopyForBatchAsync(
        string batchName, IEnumerable<string> partNumbers, CancellationToken ct = default)
        => CopyAsync(Path.Combine(_localPdfPath, "Batches", batchName), partNumbers, ct);

    /// <summary>
    /// Parallel <c>File.Exists</c> probe on the share — returns the subset of
    /// <paramref name="partNumbers"/> that have a corresponding <c>.pdf</c>.
    /// Mirrors the existing TSV parser's HasPdf check (BomService.ParseExportFile).
    /// </summary>
    public async Task<HashSet<string>> WhichExistOnShareAsync(
        IEnumerable<string> partNumbers, CancellationToken ct = default)
    {
        var found = new System.Collections.Concurrent.ConcurrentBag<string>();
        await Parallel.ForEachAsync(partNumbers.Distinct(),
            new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = ct },
            async (partNumber, c) =>
            {
                if (await Task.Run(() => File.Exists(Path.Combine(_pdfSharePath, partNumber + ".pdf")), c))
                    found.Add(partNumber);
            });
        return new HashSet<string>(found, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<(int Copied, int Total)> CopyAsync(
        string destinationFolder, IEnumerable<string> partNumbers, CancellationToken ct)
    {
        var partList = partNumbers.Distinct().ToList();

        try
        {
            Directory.CreateDirectory(destinationFolder);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not create local PDF folder: {Folder}", destinationFolder);
            return (0, partList.Count);
        }

        int copied = 0;
        await Parallel.ForEachAsync(partList,
            new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = ct },
            async (partNumber, c) =>
            {
                var sourcePath = Path.Combine(_pdfSharePath, partNumber + ".pdf");
                var destPath = Path.Combine(destinationFolder, partNumber + ".pdf");
                try
                {
                    if (!await Task.Run(() => File.Exists(sourcePath), c)) return;
                    await Task.Run(() => File.Copy(sourcePath, destPath, overwrite: true), c);
                    Interlocked.Increment(ref copied);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Failed to copy PDF for {PartNumber}", partNumber);
                }
            });

        return (copied, partList.Count);
    }
}
