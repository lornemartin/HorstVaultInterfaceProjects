using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using System.IO;
using PdfSharp;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using PdfSharp.Pdf.IO;
using DevExpress.XtraPdfViewer;

namespace VaultItemProcessor
{
    public partial class XtraReportParts : DevExpress.XtraReports.UI.XtraReport
    {
        public XtraReportParts()
        {
            InitializeComponent();
        }

        private void xrPictureBox2_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            
            var number = Report.GetCurrentColumnValue("Number");

            string filename = (AppSettings.Get("ExportFilePath").ToString() + "Pdfs\\") + (string)number + ".pdf";
            if (File.Exists(filename))
            {
                PdfViewer pdfViewer = new PdfViewer();
                byte[] bytes = System.IO.File.ReadAllBytes(filename);

                Stream stream = new MemoryStream(bytes);

                pdfViewer.LoadDocument(stream);

                int numPages = pdfViewer.PageCount;

                PdfDocument doc = new PdfDocument();
                doc = PdfReader.Open(filename, PdfDocumentOpenMode.InformationOnly);

                for (int p = 1; p <= numPages; p++)
                {
                    PdfPage page = doc.Pages[p - 1];
                    Bitmap bitmap = pdfViewer.CreateBitmap(p, 950);

                    float h = (float)page.Height;
                    float w = (float)page.Width;
                    var angle = page.Rotate;
                    var orient = page.Orientation;
                    //if ((page.Orientation == PdfSharp.PageOrientation.Portrait && angle == 90) || (page.Orientation == PdfSharp.PageOrientation.Portrait && h < w))
                    //    bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);

                    xrPictureBox2.ImageSource = new DevExpress.XtraPrinting.Drawing.ImageSource(bitmap);
                }
            }
            else
            {
                xrPictureBox2.ImageSource = null;
            }
        }
    }
}
