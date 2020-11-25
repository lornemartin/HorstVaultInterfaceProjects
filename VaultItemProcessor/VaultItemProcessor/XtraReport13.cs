using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using System.IO;
using DevExpress.XtraPdfViewer;

namespace VaultItemProcessor
{
    public partial class XtraReport13 : DevExpress.XtraReports.UI.XtraReport
    {
        public XtraReport13()
        {
            InitializeComponent();
        }

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
    }
}
