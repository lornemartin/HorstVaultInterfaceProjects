using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;

namespace VaultItemProcessor
{
    public partial class XtraReport11 : DevExpress.XtraReports.UI.XtraReport
    {
        public XtraReport11()
        {
            InitializeComponent();
        }

        private void xrPictureBox1_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            //xrPictureBox1.ImageUrl = @"M:\PDF Drawing Files\HDO-08.pdf";
            
        }

        private void xrPdfContent1_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            xrPdfContent1.SourceUrl = @"M:\PDF Drawing Files\HDO-08.pdf";
        }
    }
}
