using ClosedXML.Excel;

namespace HorstMFG.Web.Services;

public record ParsedScheduleRow(int RowIndex, string OrderNumber, int Qty, string ProductNumber, bool IsContinuation);
public record ParsedSchedule(string Name, List<ParsedScheduleRow> Rows, List<string> Errors);

/// <summary>
/// Reads a schedule Excel workbook into <see cref="ParsedSchedule"/>.
/// Schedule name is in cell A1; data rows start at row 2.
/// Column A = OrderNumber (trim trailing whitespace),
/// Column D = Qty,
/// Column E = ProductNumber. A product cell that is exactly "LA-" with nothing
/// after the dash is a continuation row that supplies extra-info Notes for the
/// preceding real product line in the same order.
/// </summary>
public class ExcelScheduleParser
{
    public ParsedSchedule Parse(Stream xlsx)
    {
        using var workbook = new XLWorkbook(xlsx);
        var sheet = workbook.Worksheets.FirstOrDefault()
                    ?? throw new InvalidOperationException("Workbook contains no worksheets.");

        var name = sheet.Cell("A1").GetString().Trim();

        var rows = new List<ParsedScheduleRow>();
        var errors = new List<string>();

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (int r = 2; r <= lastRow; r++)
        {
            string orderNumber = sheet.Cell(r, 1).GetString().Trim();
            string qtyRaw = sheet.Cell(r, 4).GetString().Trim();
            string productNumber = sheet.Cell(r, 5).GetString().Trim();

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
                                           isContinuation));
        }

        if (string.IsNullOrEmpty(name))
            errors.Add("Schedule name (cell A1) is empty.");

        return new ParsedSchedule(name, rows, errors);
    }
}
