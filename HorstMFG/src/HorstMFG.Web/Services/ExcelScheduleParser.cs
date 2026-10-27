using ClosedXML.Excel;

namespace HorstMFG.Web.Services;

public record ParsedScheduleRow(int RowIndex, string OrderNumber, int Qty, string ProductNumber, bool IsContinuation,
    string Description);
public record ParsedSchedule(string Name, List<ParsedScheduleRow> Rows, List<string> Errors);

/// <summary>
/// Reads a schedule Excel workbook into <see cref="ParsedSchedule"/>.
/// Schedule name is the workbook's file name without extension; data rows start at row 1.
/// Column E = Qty,
/// Column F = OrderNumber (trim trailing whitespace),
/// Column G = ProductNumber,
/// Column H = Description. A product cell that is exactly "LA-" with nothing
/// after the dash is a continuation row whose Description is an extra-info Note for the
/// preceding real product line in the same order.
/// </summary>
public class ExcelScheduleParser
{
    public ParsedSchedule Parse(Stream xlsx, string fileName)
    {
        using var workbook = new XLWorkbook(xlsx);
        var sheet = workbook.Worksheets.FirstOrDefault()
                    ?? throw new InvalidOperationException("Workbook contains no worksheets.");

        var name = Path.GetFileNameWithoutExtension(fileName).Trim();

        var rows = new List<ParsedScheduleRow>();
        var errors = new List<string>();

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (int r = 1; r <= lastRow; r++)
        {
            string qtyRaw = sheet.Cell(r, 5).GetString().Trim();
            string orderNumber = sheet.Cell(r, 6).GetString().Trim();
            string productNumber = sheet.Cell(r, 7).GetString().Trim();
            string description = sheet.Cell(r, 8).GetString().Trim();

            if (string.IsNullOrEmpty(orderNumber) && string.IsNullOrEmpty(productNumber))
                continue;

            if (string.IsNullOrEmpty(orderNumber) != string.IsNullOrEmpty(productNumber))
            {
                errors.Add($"Row {r}: order number and product number must both be set or both blank.");
                continue;
            }

            int qty = 0;
            if (!string.IsNullOrEmpty(qtyRaw) && !int.TryParse(qtyRaw, out qty))
            {
                errors.Add($"Row {r}: qty '{qtyRaw}' is not a valid integer.");
                continue;
            }
            if (qty <= 0) qty = 1;

            bool isContinuation = string.Equals(productNumber, "LA-", StringComparison.OrdinalIgnoreCase);

            rows.Add(new ParsedScheduleRow(r, orderNumber, qty,
                                           isContinuation ? "" : productNumber,
                                           isContinuation, description));
        }

        if (string.IsNullOrEmpty(name))
            errors.Add("Schedule name (file name) is empty.");

        return new ParsedSchedule(name, rows, errors);
    }
}
