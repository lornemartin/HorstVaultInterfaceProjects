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

        

        private void xrPdfContent1_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            ProductionListProduct curRow = (ProductionListProduct)this.GetCurrentRow();
            xrPdfContent1.SourceUrl = @"M:\PDF Drawing Files\" + this.tableCell36.Value + ".pdf";
        }
    }
}
