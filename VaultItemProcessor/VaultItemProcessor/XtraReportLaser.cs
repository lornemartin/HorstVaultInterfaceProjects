using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;

namespace VaultItemProcessor
{
    public partial class XtraReportLaser : DevExpress.XtraReports.UI.XtraReport
    {
        public XtraReportLaser()
        {
            InitializeComponent();
        }

        private void xrPictureBox2_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {

        }
    }
}
