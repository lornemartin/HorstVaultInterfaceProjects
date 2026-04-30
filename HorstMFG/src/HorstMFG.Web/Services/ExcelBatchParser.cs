using ClosedXML.Excel;

namespace HorstMFG.Web.Services;

public record ParsedBatchRow(int RowIndex, int Qty, string ProductNumber);
public record ParsedBatch(string Name, List<ParsedBatchRow> Rows, List<string> Errors);

/// <summary>
/// Reads a batch Excel workbook. A1 is the batch name; rows 2..N have
/// Column A = Qty, Column B = ProductNumber.
/// </summary>
public class ExcelBatchParser
{
    public ParsedBatch Parse(Stream xlsx)
    {
        using var workbook = new XLWorkbook(xlsx);
        var sheet = workbook.Worksheets.FirstOrDefault()
                    ?? throw new InvalidOperationException("Workbook contains no worksheets.");

        var name = sheet.Cell("A1").GetString().Trim();

        var rows = new List<ParsedBatchRow>();
        var errors = new List<string>();

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (int r = 2; r <= lastRow; r++)
        {
            string qtyRaw = sheet.Cell(r, 1).GetString().Trim();
            string productNumber = sheet.Cell(r, 2).GetString().Trim();

            if (string.IsNullOrEmpty(qtyRaw) && string.IsNullOrEmpty(productNumber))
                continue;

            if (!string.IsNullOrEmpty(qtyRaw) && string.IsNullOrEmpty(productNumber))
            {
                errors.Add($"Row {r}: qty '{qtyRaw}' has no product number.");
                continue;
            }

            int qty = 0;
            if (!string.IsNullOrEmpty(qtyRaw) && !int.TryParse(qtyRaw, out qty))
            {
                errors.Add($"Row {r}: qty '{qtyRaw}' is not a valid integer.");
                continue;
            }
            if (qty <= 0) qty = 1;

            rows.Add(new ParsedBatchRow(r, qty, productNumber));
        }

        if (string.IsNullOrEmpty(name))
            errors.Add("Batch name (cell A1) is empty.");

        return new ParsedBatch(name, rows, errors);
    }
}
