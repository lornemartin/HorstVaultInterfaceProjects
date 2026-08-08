using ExcelDataReader;
using System.Data;

namespace HorstMFG.Web.Services;

/// <summary>
/// Reads a Purchase Order batch workbook (.xls or .xlsx).
/// Reads the "Purchase Order" sheet: batch name from E15,
/// data rows from row 18 onward with Qty in col D (4) and product in col F (6).
/// Some vendor PO templates (e.g. outsourced-work orders) repurpose E12:E15 for
/// vendor Name/Address/City/Phone instead, leaving E15 blank — those put the batch
/// label in F18 instead, so we fall back there when E15 is empty.
/// </summary>
public class PoBatchParser
{
    static PoBatchParser()
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }

    public ParsedBatch Parse(Stream stream)
    {
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var ds = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false }
        });

        var table = ds.Tables["Purchase Order"]
            ?? throw new InvalidOperationException("Workbook has no 'Purchase Order' sheet.");

        var name = GetCell(table, 14, 4); // E15
        if (string.IsNullOrEmpty(name))
            name = GetCell(table, 17, 5); // fallback: F18

        var rows = new List<ParsedBatchRow>();
        var errors = new List<string>();

        for (int r = 17; r < table.Rows.Count; r++) // data starts at row 18 (0-based 17)
        {
            if (table.Columns.Count < 6) continue;

            string qtyRaw     = GetCell(table.Rows[r], 3); // col D
            string units      = GetCell(table.Rows[r], 4); // col E
            string productRaw = GetCell(table.Rows[r], 5); // col F

            if (string.IsNullOrEmpty(qtyRaw) && string.IsNullOrEmpty(productRaw))
                continue;

            // Skip section headers and notes that have no qty
            if (string.IsNullOrEmpty(qtyRaw))
                continue;

            // Skip the trailing summary row (e.g. "121   UNITS")
            if (units.Equals("UNITS", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!int.TryParse(qtyRaw, out var qty))
            {
                errors.Add($"Row {r + 1}: qty '{qtyRaw}' is not a valid integer.");
                continue;
            }
            if (qty <= 0) qty = 1;

            if (string.IsNullOrEmpty(productRaw))
            {
                errors.Add($"Row {r + 1}: qty {qty} has no product description.");
                continue;
            }

            rows.Add(new ParsedBatchRow(r + 1, qty, productRaw));
        }

        if (string.IsNullOrEmpty(name))
            errors.Add("Batch name is empty (checked cell E15 and fallback cell F18).");

        return new ParsedBatch(name, rows, errors);
    }

    private static string GetCell(DataTable table, int row, int col) =>
        table.Rows.Count > row && table.Columns.Count > col
            ? GetCell(table.Rows[row], col)
            : "";

    private static string GetCell(DataRow row, int col)
    {
        var val = row[col];
        return val is null || val == DBNull.Value ? "" : val.ToString()?.Trim() ?? "";
    }
}
