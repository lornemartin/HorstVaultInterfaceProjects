using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using System.IO;
using DevExpress.XtraPdfViewer;

namespace VaultItemProcessor
{
    public partial class XtraReport11 : DevExpress.XtraReports.UI.XtraReport
    {
        public XtraReport11()
        {
            InitializeComponent();
        }

        

        private void xrPdfContent1_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            ProductionListProduct curRow = (ProductionListProduct)this.GetCurrentRow();
            xrPdfContent1.SourceUrl = @"M:\PDF Drawing Files\" + this.xrLabelNumber.Value + ".pdf";
        }

        private void xrPictureBox1_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            string filename = (AppSettings.Get("ExportFilePath").ToString() + "Pdfs\\") + this.xrLabelNumber.Value + ".pdf";
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


        }
    }
}
