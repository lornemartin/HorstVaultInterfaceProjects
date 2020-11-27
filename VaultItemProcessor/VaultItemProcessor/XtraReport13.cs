using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using System.IO;
using DevExpress.XtraPdfViewer;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace VaultItemProcessor
{
    public partial class XtraReport13 : DevExpress.XtraReports.UI.XtraReport
    {
        public XtraReport13()
        {
            InitializeComponent();
        }

        //assembly 1 half page
        private void xrPictureBox2_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            //string filename = (AppSettings.Get("ExportFilePath").ToString() + "Pdfs\\") + this.tableCell27.Value + ".pdf";
            string s = ReportAssemblies.GetCurrentColumnValue("AssemblyName").ToString();
            string filename = (AppSettings.Get("ExportFilePath").ToString() + "Pdfs\\") + s + ".pdf";

            if (File.Exists(filename))
            {
                PdfViewer pdfViewer = new PdfViewer();
                byte[] bytes = System.IO.File.ReadAllBytes(filename);

                Stream stream = new MemoryStream(bytes);

                pdfViewer.LoadDocument(stream);
                Bitmap bitmap = pdfViewer.CreateBitmap(1, 950);

                PdfDocument doc = new PdfDocument();
                doc = PdfReader.Open(filename, PdfDocumentOpenMode.InformationOnly);

                PdfPage page = doc.Pages[0];

                float h = (float)page.Height;
                float w = (float)page.Width;
                //if ((page.Rotate == 90 && page.Orientation == PdfSharp.PageOrientation.Portrait))
                    //if ((page.Rotate == 90 && page.Orientation == PdfSharp.PageOrientation.Portrait) || h < 800)      // flip page if height is less than 800
                    //bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);

                pdfViewer.CloseDocument();
                pdfViewer.Dispose();

                xrPictureBox2.ImageSource = new DevExpress.XtraPrinting.Drawing.ImageSource(bitmap);
                xrPictureBox2.BackColor = Color.AliceBlue;
            }
            else
            {
                xrPictureBox2.ImageSource = null;
            }
        }

        // assembly 1 full page
        private void xrPictureBox5_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            string s = ReportAssemblies.GetCurrentColumnValue("AssemblyName").ToString();
            string filename = (AppSettings.Get("ExportFilePath").ToString() + "Pdfs\\") + s + ".pdf";

            if (File.Exists(filename))
            {
                PdfViewer pdfViewer = new PdfViewer();
                byte[] bytes = System.IO.File.ReadAllBytes(filename);

                Stream stream = new MemoryStream(bytes);

                pdfViewer.LoadDocument(stream);
                Bitmap bitmap = pdfViewer.CreateBitmap(1, 950);

                PdfDocument doc = new PdfDocument();
                doc = PdfReader.Open(filename, PdfDocumentOpenMode.InformationOnly);

                PdfPage page = doc.Pages[0];

                float h = (float)page.Height;
                float w = (float)page.Width;
                var angle = page.Rotate;
                var orient = page.Orientation;
                if ((page.Orientation == PdfSharp.PageOrientation.Portrait && angle == 90) || 
                     (page.Orientation == PdfSharp.PageOrientation.Portrait && h < w))
                    //if ((page.Rotate == 90 && page.Orientation == PdfSharp.PageOrientation.Portrait) || h < 800)      // flip page if height is less than 800
                    bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);

                pdfViewer.CloseDocument();
                pdfViewer.Dispose();

                xrPictureBox5.ImageSource = new DevExpress.XtraPrinting.Drawing.ImageSource(bitmap);
                xrPictureBox5.BackColor = Color.AliceBlue;
            }
            else
            {
                xrPictureBox5.ImageSource = null;
            }
        }

        // assembly two half page
        private void xrPictureBox4_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            string s = ReportAssemblies.GetCurrentColumnValue("AssemblyName").ToString();
            string filename = (AppSettings.Get("ExportFilePath").ToString() + "Pdfs\\") + s + ".pdf";

            if (File.Exists(filename))
            {
                PdfViewer pdfViewer = new PdfViewer();
                byte[] bytes = System.IO.File.ReadAllBytes(filename);

                Stream stream = new MemoryStream(bytes);
                Bitmap bitmap = null;
                pdfViewer.LoadDocument(stream);
                if (pdfViewer.PageCount == 2)
                {
                    bitmap = pdfViewer.CreateBitmap(2, 950);  // changed from 2 to 1 for testing
                }

                PdfDocument doc = new PdfDocument();
                doc = PdfReader.Open(filename, PdfDocumentOpenMode.InformationOnly);

                PdfPage page = doc.Pages[0];

                var orient = page.Orientation;
                var rotate = page.Rotate;

                float h = (float)page.Height;
                float w = (float)page.Width;

                if (pdfViewer.PageCount == 2)
                {
                    if ((page.Rotate == 90 && page.Orientation == PdfSharp.PageOrientation.Portrait))       // flip page if height is less than 800
                        bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
                }


                pdfViewer.CloseDocument();
                pdfViewer.Dispose();

                xrPictureBox4.ImageSource = new DevExpress.XtraPrinting.Drawing.ImageSource(bitmap);
                xrPictureBox4.BackColor = Color.AliceBlue;
            }
            else
            {
                xrPictureBox4.ImageSource = null;
            }
        }

        // assembly 2 full page
        private void xrPictureBox6_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            string s = ReportAssemblies.GetCurrentColumnValue("AssemblyName").ToString();
            string filename = (AppSettings.Get("ExportFilePath").ToString() + "Pdfs\\") + s + ".pdf";

            if (File.Exists(filename))
            {
                PdfViewer pdfViewer = new PdfViewer();
                byte[] bytes = System.IO.File.ReadAllBytes(filename);

                Stream stream = new MemoryStream(bytes);
                Bitmap bitmap = null;

                pdfViewer.LoadDocument(stream);
                if (pdfViewer.PageCount == 2)
                {
                    bitmap = pdfViewer.CreateBitmap(2, 950);     // changed from 2 to 1 for testing
                    PdfDocument doc = new PdfDocument();
                    doc = PdfReader.Open(filename, PdfDocumentOpenMode.InformationOnly);

                    PdfPage page = doc.Pages[0];

                    float h = (float)page.Height;
                    float w = (float)page.Width;
                    var angle = page.Rotate;
                    //if (h < 800 || h < w)       // flip page if height is less than 800
                    if ((page.Orientation == PdfSharp.PageOrientation.Portrait && angle == 90) ||
                      (page.Orientation == PdfSharp.PageOrientation.Portrait && h < w))
                        bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);

                }

                pdfViewer.CloseDocument();
                pdfViewer.Dispose();

                xrPictureBox6.ImageSource = new DevExpress.XtraPrinting.Drawing.ImageSource(bitmap);
                xrPictureBox6.BackColor = Color.AliceBlue;
            }
            else
            {
                xrPictureBox6.ImageSource = null;
            }
        }

        

        // parts half page
        private void xrPictureBox1_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            string s = ReportParts.GetCurrentColumnValue("PartName").ToString();
            string filename = (AppSettings.Get("ExportFilePath").ToString() + "Pdfs\\") + s + ".pdf";
            if (File.Exists(filename))
            {
                PdfViewer pdfViewer = new PdfViewer();
                byte[] bytes = System.IO.File.ReadAllBytes(filename);

                Stream stream = new MemoryStream(bytes);

                pdfViewer.LoadDocument(stream);
                Bitmap bitmap = pdfViewer.CreateBitmap(1, 950);

                pdfViewer.CloseDocument();
                pdfViewer.Dispose();

                xrPictureBox1.ImageSource = new DevExpress.XtraPrinting.Drawing.ImageSource(bitmap);
                xrPictureBox1.BackColor = Color.AliceBlue;
            }
            else
            {
                xrPictureBox1.ImageSource = null;
            }
        }

        // parts full page
        private void xrPictureBox7_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            string s = ReportParts.GetCurrentColumnValue("PartName").ToString();
            string filename = (AppSettings.Get("ExportFilePath").ToString() + "Pdfs\\") + s + ".pdf";
            if (File.Exists(filename))
            {
                PdfViewer pdfViewer = new PdfViewer();
                byte[] bytes = System.IO.File.ReadAllBytes(filename);

                Stream stream = new MemoryStream(bytes);

                pdfViewer.LoadDocument(stream);
                Bitmap bitmap = pdfViewer.CreateBitmap(1, 950);


                PdfDocument doc = new PdfDocument();
                doc = PdfReader.Open(filename, PdfDocumentOpenMode.InformationOnly);

                PdfPage page = doc.Pages[0];

                var orient = page.Orientation;
                var angle = page.Rotate;

                float h = (float)page.Height;
                float w = (float)page.Width;

                if ((page.Orientation == PdfSharp.PageOrientation.Portrait && angle == 90) ||
                     (page.Orientation == PdfSharp.PageOrientation.Portrait && h < w))       // flip page if height is less than 800
                    //if ((page.Rotate == 90 && page.Orientation == PdfSharp.PageOrientation.Portrait) || h < 800)       // flip page if height is less than 800
                    bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);

                pdfViewer.CloseDocument();
                pdfViewer.Dispose();

                xrPictureBox7.ImageSource = new DevExpress.XtraPrinting.Drawing.ImageSource(bitmap);
                xrPictureBox7.BackColor = Color.AliceBlue;
            }
            else
            {
                xrPictureBox7.ImageSource = null;
            }
        }

        private void ReportAssemblies2_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            string s = ReportAssemblies.GetCurrentColumnValue("AssemblyName").ToString();
            string filename = (AppSettings.Get("ExportFilePath").ToString() + "Pdfs\\") + s + ".pdf";

            if (File.Exists(filename))
            {
                PdfViewer pdfViewer = new PdfViewer();
                byte[] bytes = System.IO.File.ReadAllBytes(filename);

                Stream stream = new MemoryStream(bytes);

                pdfViewer.LoadDocument(stream);

                if (pdfViewer.PageCount == 1)
                    e.Cancel = true;
            }
            else
            {
                e.Cancel = true;
            }
        }

        
    }
}
