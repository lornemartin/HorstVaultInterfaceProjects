using System.Globalization;
using HorstMFG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Parsing;

namespace HorstMFG.Web.Services;

public class ReportService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ReportService> _log;
    private readonly string _localPdfPath;

    public ReportService(ApplicationDbContext db, ILogger<ReportService> log, IConfiguration config)
    {
        _db = db;
        _log = log;
        _localPdfPath = config["FileSystemPaths:LocalPdfPath"] ?? @"C:\HorstMFG\PDFs\";
    }

    // ── Schedule (Schedule → ScheduleOrder → PartLineItem) ───────────────────

    public async Task<byte[]?> GenerateScheduleOperationReportAsync(string scheduleName, string operation)
    {
        var schedules = await _db.Schedules
            .Where(s => s.Name == scheduleName)
            .Include(s => s.ScheduleOrders)
                .ThenInclude(so => so.Parts)
            .ToListAsync();

        if (schedules.Count == 0) return null;

        var pdfFolder = schedules[0].LocalPdfFolder
            ?? Path.Combine(_localPdfPath, "Schedules", scheduleName);

        var groups = schedules
            .SelectMany(s => s.ScheduleOrders)
            .SelectMany(so => so.Parts
                .Where(p => !p.IsStock &&
                            p.Operations != null &&
                            p.Operations.Contains(operation, StringComparison.OrdinalIgnoreCase))
                .Select(p => new { Part = p, Label = so.OrderNumber, ParentQty = so.Qty }))
            .GroupBy(x => x.Part.PartNumber)
            .Select(g => BuildGroup(g.Key,
                g.First().Part.Title,
                g.First().Part.Thickness,
                g.First().Part.Material,
                g.GroupBy(x => x.Label)
                 .Select(og => (og.Key, og.Sum(x => x.Part.Qty * x.ParentQty)))
                 .OrderBy(o => o.Key)
                 .ToList(),
                Path.Combine(pdfFolder, g.Key + ".pdf")))
            .Where(g => File.Exists(g.PdfPath))
            .ToList();

        groups = SortGroups(groups, operation);

        if (groups.Count == 0)
        {
            _log.LogWarning("No '{Op}' parts with local PDFs found for schedule '{Name}'", operation, scheduleName);
            return null;
        }

        _log.LogInformation("Generating '{Op}' report for schedule '{Name}': {Count} parts", operation, scheduleName, groups.Count);
        return BuildReport(groups, scheduleName, isSchedule: true, operation, _log);
    }

    // ── Batches (Batch → BatchProduct → PartLineItem) ────────────────────────

    public async Task<byte[]?> GenerateBatchOperationReportAsync(string batchName, string operation)
    {
        var batches = await _db.Batches
            .Where(b => b.Name == batchName)
            .Include(b => b.BatchProducts)
                .ThenInclude(bp => bp.Parts)
            .ToListAsync();

        if (batches.Count == 0) return null;

        var pdfFolder = batches[0].LocalPdfFolder
            ?? Path.Combine(_localPdfPath, "Batches", batchName);

        var groups = batches
            .SelectMany(b => b.BatchProducts)
            .SelectMany(bp => bp.Parts
                .Where(p => p.IsStock &&
                            p.Operations != null &&
                            p.Operations.Contains(operation, StringComparison.OrdinalIgnoreCase))
                .Select(p => new { Part = p, Label = bp.ProductName, ParentQty = bp.Qty }))
            .GroupBy(x => x.Part.PartNumber)
            .Select(g => BuildGroup(g.Key,
                g.First().Part.Title,
                g.First().Part.Thickness,
                g.First().Part.Material,
                g.GroupBy(x => x.Label)
                 .Select(og => (og.Key, og.Sum(x => x.Part.Qty * x.ParentQty)))
                 .OrderBy(o => o.Key)
                 .ToList(),
                Path.Combine(pdfFolder, g.Key + ".pdf")))
            .Where(g => File.Exists(g.PdfPath))
            .ToList();

        groups = SortGroups(groups, operation);

        if (groups.Count == 0)
        {
            _log.LogWarning("No '{Op}' parts with local PDFs found for batch '{Name}'", operation, batchName);
            return null;
        }

        _log.LogInformation("Generating '{Op}' report for batch '{Name}': {Count} parts", operation, batchName, groups.Count);
        return BuildReport(groups, batchName, isSchedule: false, operation, _log);
    }

    // ── Per-operation sort ────────────────────────────────────────────────────

    private static List<LaserPartGroup> SortGroups(List<LaserPartGroup> groups, string operation)
        => operation.ToLowerInvariant() switch
        {
            "laser" => [.. groups.OrderBy(g => ParseThickness(g.Thickness)).ThenBy(g => g.Material)],
            _       => [.. groups.OrderBy(g => g.PartNumber)],
        };

    // ── Per-operation report builder ──────────────────────────────────────────

    private static byte[] BuildReport(List<LaserPartGroup> groups, string sourceName,
        bool isSchedule, string operation, ILogger log)
        => operation.ToLowerInvariant() switch
        {
            // Add cases here for operations that need a different layout, e.g.:
            // "purchased" => BuildTablePdf(groups, sourceName, isSchedule, log),
            _ => BuildLaserPdf(groups, sourceName, isSchedule, operation, log),
        };

    // ── PDF assembly ─────────────────────────────────────────────────────────

    private static byte[] BuildLaserPdf(List<LaserPartGroup> parts, string sourceName, bool isSchedule, string operation, ILogger log)
    {
        var output = new PdfDocument();
        output.PageSettings.Size = PdfPageSize.Letter;
        output.PageSettings.Margins.All = 0;

        foreach (var part in parts)
        {
            byte[] srcBytes = File.ReadAllBytes(part.PdfPath);
            var srcDoc = new PdfLoadedDocument(srcBytes);
            try
            {
                if (srcDoc.Pages.Count == 0) continue;

                var srcPage = srcDoc.Pages[0] as PdfLoadedPage;
                if (srcPage == null) continue;

                // Build template before importing so it can be drawn onto the cover page.
                var template = srcPage.CreateTemplate();

                // /Rotate is NOT incorporated into the template dimensions, so we must
                // swap width/height when the page is stored rotated 90° or 270°.
                bool rotated = srcPage.Rotation == PdfPageRotateAngle.RotateAngle90 ||
                               srcPage.Rotation == PdfPageRotateAngle.RotateAngle270;
                bool isLandscape = rotated ? template.Height > template.Width
                                           : template.Width  > template.Height;

                // Page 1: full-size drawing (front of sheet when duplex printing)
                for (int i = 0; i < srcDoc.Pages.Count; i++)
                    output.ImportPage(srcDoc, i);

                // Page 2: cover page with thumbnail + qty breakdown (back of sheet)
                var cover = output.Pages.Add();
                DrawCoverPage(cover, template, rotated, isLandscape, part, sourceName, isSchedule, operation);
            }
            finally
            {
                srcDoc.Close();
            }
        }

        using var ms = new MemoryStream();
        output.Save(ms);
        output.Close();
        return ms.ToArray();
    }

    private static void DrawCoverPage(PdfPage cover, PdfTemplate template,
        bool rotated, bool isLandscape, LaserPartGroup part, string sourceName, bool isSchedule, string operation)
    {
        if (isLandscape)
            DrawCoverPageHorizontal(cover, template, rotated, part, sourceName, isSchedule, operation);
        else
            DrawCoverPageVertical(cover, template, rotated, part, sourceName, isSchedule, operation);
    }

    /// <summary>
    /// Landscape source PDF: thumbnail on top, details below.
    /// </summary>
    private static void DrawCoverPageHorizontal(PdfPage cover, PdfTemplate template, bool rotated, LaserPartGroup part, string sourceName, bool isSchedule, string operation)
    {
        var g  = cover.Graphics;
        float pw = cover.GetClientSize().Width;
        float ph = cover.GetClientSize().Height;

        float margin = 20f;
        float divY   = ph * 0.55f;

        // Visual dimensions — swap stored W/H when the page carries a 90°/270° /Rotate.
        float vW = rotated ? template.Height : template.Width;
        float vH = rotated ? template.Width  : template.Height;

        float thumbMaxW = pw - margin * 2f;
        float thumbMaxH = divY - margin * 2f;
        float scale  = Math.Min(thumbMaxW / vW, thumbMaxH / vH);
        float thumbW = vW * scale;
        float thumbH = vH * scale;
        float thumbX = (pw - thumbW) / 2f;
        float thumbY = margin + (thumbMaxH - thumbH) / 2f;

        DrawTemplate(g, template, rotated, thumbX, thumbY, thumbW, thumbH);
        g.DrawRectangle(new PdfPen(new PdfColor(180, 180, 180), 0.5f),
            new RectangleF(thumbX, thumbY, thumbW, thumbH));

        // Horizontal divider
        g.DrawLine(new PdfPen(new PdfColor(210, 210, 210), 1f),
            new PointF(margin, divY), new PointF(pw - margin, divY));

        // Details — bottom section
        DrawDetails(g, part, margin + 10f, divY + 18f, pw, margin, sourceName, isSchedule, operation);
    }

    /// <summary>
    /// Portrait source PDF: thumbnail on left, details on right.
    /// </summary>
    private static void DrawCoverPageVertical(PdfPage cover, PdfTemplate template, bool rotated, LaserPartGroup part, string sourceName, bool isSchedule, string operation)
    {
        var g  = cover.Graphics;
        float pw = cover.GetClientSize().Width;
        float ph = cover.GetClientSize().Height;

        float margin = 20f;
        float divX   = pw * 0.50f;

        // Visual dimensions — swap stored W/H when the page carries a 90°/270° /Rotate.
        float vW = rotated ? template.Height : template.Width;
        float vH = rotated ? template.Width  : template.Height;

        float thumbMaxW = divX - margin * 2f;
        float thumbMaxH = ph - margin * 2f;
        float scale  = Math.Min(thumbMaxW / vW, thumbMaxH / vH);
        float thumbW = vW * scale;
        float thumbH = vH * scale;
        float thumbX = margin + (thumbMaxW - thumbW) / 2f;
        float thumbY = (ph - thumbH) / 2f;

        DrawTemplate(g, template, rotated, thumbX, thumbY, thumbW, thumbH);
        g.DrawRectangle(new PdfPen(new PdfColor(180, 180, 180), 0.5f),
            new RectangleF(thumbX, thumbY, thumbW, thumbH));

        // Vertical divider
        g.DrawLine(new PdfPen(new PdfColor(210, 210, 210), 1f),
            new PointF(divX, margin), new PointF(divX, ph - margin));

        // Details — right section
        DrawDetails(g, part, divX + 18f, 50f, pw, margin, sourceName, isSchedule, operation);
    }

    /// <summary>
    /// Draws a template into the rectangle (tx, ty, tw, th), applying a 90° CW rotation
    /// transform when the source page carries a /Rotate 90 attribute.
    /// </summary>
    private static void DrawTemplate(PdfGraphics g, PdfTemplate template, bool rotated,
        float tx, float ty, float tw, float th)
    {
        if (!rotated)
        {
            g.DrawPdfTemplate(template, new PointF(tx, ty), new SizeF(tw, th));
            return;
        }

        // For /Rotate=90: content is stored portrait (template.Width × template.Height)
        // but must render landscape.  Apply a 90° CW rotation around the top-right corner
        // of the target box so the rendered result fills (tx, ty, tw, th).
        //
        //   After TranslateTransform(tx+tw, ty) + RotateTransform(90):
        //     local +x → screen down,  local +y → screen left
        //   DrawPdfTemplate at (0,0) with size (th, tw) fills the box correctly because:
        //     th units down  = tw units right in visual  (= the landscape width)  — wait,
        //   Actually stored W maps along local X (down), stored H along local Y (left):
        //     stored W (template.Width)  * scale = th  → renders as the visual height
        //     stored H (template.Height) * scale = tw  → renders as the visual width
        g.Save();
        g.TranslateTransform(tx + tw, ty);
        g.RotateTransform(90);
        g.DrawPdfTemplate(template, new PointF(0, 0), new SizeF(th, tw));
        g.Restore();
    }

    private static void DrawDetails(PdfGraphics g, LaserPartGroup part,
        float bx, float by, float pw, float margin, string sourceName, bool isSchedule, string operation)
    {
        var fontTitle  = new PdfStandardFont(PdfFontFamily.Helvetica, 16, PdfFontStyle.Bold);
        var fontBold11 = new PdfStandardFont(PdfFontFamily.Helvetica, 11, PdfFontStyle.Bold);
        var fontReg10  = new PdfStandardFont(PdfFontFamily.Helvetica, 10);
        var fontReg9   = new PdfStandardFont(PdfFontFamily.Helvetica, 9);
        var black      = PdfBrushes.Black;
        var grayBrush  = new PdfSolidBrush(new PdfColor(110, 110, 110));
        var grayPen    = new PdfPen(new PdfColor(200, 200, 200), 0.5f);

        // Part number
        g.DrawString(part.PartNumber, fontTitle, black, new PointF(bx, by));
        by += 26f;

        // Title
        if (!string.IsNullOrWhiteSpace(part.Title))
        {
            g.DrawString(part.Title, fontReg10, black, new PointF(bx, by));
            by += 18f;
        }
        by += 10f;

        // Thickness, Material, and source name
        if (!string.IsNullOrWhiteSpace(part.Thickness))
        {
            g.DrawString("Thickness", fontReg9, grayBrush, new PointF(bx, by));
            g.DrawString(part.Thickness, fontReg10, black, new PointF(bx + 62f, by));
            by += 17f;
        }
        if (!string.IsNullOrWhiteSpace(part.Material))
        {
            g.DrawString("Material", fontReg9, grayBrush, new PointF(bx, by));
            g.DrawString(part.Material, fontReg10, black, new PointF(bx + 62f, by));
            by += 17f;
        }
        g.DrawString("Operation", fontReg9, grayBrush, new PointF(bx, by));
        g.DrawString(operation,   fontReg10, black,    new PointF(bx + 62f, by));
        by += 17f;
        string sourceLabel = isSchedule ? "Schedule" : "Batch";
        g.DrawString(sourceLabel, fontReg9, grayBrush, new PointF(bx, by));
        g.DrawString(sourceName,  fontReg10, black,    new PointF(bx + 62f, by));
        by += 17f;

        by += 16f;

        // Total qty
        g.DrawString($"Total Qty:  {part.TotalQty}", fontBold11, black, new PointF(bx, by));
        by += 20f;

        // Separator
        g.DrawLine(grayPen, new PointF(bx, by), new PointF(pw - margin, by));
        by += 10f;

        // Per-order / per-product rows
        float qtyColX = pw - margin - 45f;
        foreach (var (label, qty) in part.Orders)
        {
            g.DrawString(label, fontReg9, black, new PointF(bx, by));
            g.DrawString($"{qty} pcs", fontReg9, black, new PointF(qtyColX, by));
            by += 15f;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static LaserPartGroup BuildGroup(
        string partNumber,
        string? title,
        string? thickness,
        string? material,
        List<(string Label, int Qty)> orders,
        string pdfPath) => new()
    {
        PartNumber = partNumber,
        Title      = title,
        Thickness  = thickness,
        Material   = material,
        TotalQty   = orders.Sum(o => o.Qty),
        Orders     = orders,
        PdfPath    = pdfPath,
    };

    private static double ParseThickness(string? thickness)
    {
        if (string.IsNullOrWhiteSpace(thickness)) return double.MaxValue;
        var numeric = thickness.Split(' ')[0];
        if (double.TryParse(numeric, NumberStyles.Number, CultureInfo.InvariantCulture, out double d))
            return d;
        return double.MaxValue;
    }

    private sealed class LaserPartGroup
    {
        public string PartNumber { get; init; } = "";
        public string? Title     { get; init; }
        public string? Thickness { get; init; }
        public string? Material  { get; init; }
        public int TotalQty      { get; init; }
        public List<(string Label, int Qty)> Orders { get; init; } = new();
        public string PdfPath    { get; init; } = "";
    }
}
