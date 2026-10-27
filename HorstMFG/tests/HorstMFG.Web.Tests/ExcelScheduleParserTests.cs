using ClosedXML.Excel;
using HorstMFG.Web.Services;

namespace HorstMFG.Web.Tests;

public class ExcelScheduleParserTests
{
    // Rows laid out like S:\Shop Schedule Drawings\2026\11 November\2026-11-0405\2026-11-0405.xlsx:
    // A = line #, B = date, C = PO / note, E = qty, F = order #, G = product #, H = description, I = customer.
    private static MemoryStream Workbook(params object[][] rows)
    {
        using var wb = new XLWorkbook();
        var sheet = wb.AddWorksheet("Sheet1");
        for (int r = 0; r < rows.Length; r++)
            for (int c = 0; c < rows[r].Length; c++)
                if (rows[r][c] is { } v) sheet.Cell(r + 1, c + 1).Value = XLCellValue.FromObject(v);
        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void Parse_ReadsNewLayoutAndNamesScheduleFromFile()
    {
        using var xlsx = Workbook(
            [1, 20261021, "RUSH!", null!, 1, "A656150", "LA-HDN5548MAQT", "5500 LB POWER TINE PALLET FORK", "HORST STOCK"],
            [2, 20261021, "RUSH!", null!, 1, "A656150", "LA-", "RUSH! above for new Manitou arriving next week", "HORST STOCK"],
            [2, 20260107, null!, null!, 1, "A628117                ", "LA-WOKJDH480", "JD H480 WELD ON KIT", "HORST STOCK"]);

        var parsed = new ExcelScheduleParser().Parse(xlsx, "2026-11-0405.xlsx");

        Assert.Empty(parsed.Errors);
        Assert.Equal("2026-11-0405", parsed.Name);
        Assert.Collection(parsed.Rows,
            r => { Assert.Equal(1, r.RowIndex); Assert.Equal("A656150", r.OrderNumber); Assert.Equal("LA-HDN5548MAQT", r.ProductNumber); Assert.False(r.IsContinuation); },
            r => { Assert.True(r.IsContinuation); Assert.Equal("", r.ProductNumber); Assert.Equal("RUSH! above for new Manitou arriving next week", r.Description); },
            r => { Assert.Equal("A628117", r.OrderNumber); Assert.Equal(1, r.Qty); });
    }
}
