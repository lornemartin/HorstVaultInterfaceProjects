using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VaultItemProcessor.Reports2
{
    public class BandsawReportCutListItem2
    {
        public int Qty { get; set; }
        public string ItemNumber { get; set; }
        public string ItemDescription { get; set; }
        public string Material { get; set; }
        public string Operations { get; set; }

        public List<BandsawReportOrder2> ReportOrders { get; set; }
    }
}
