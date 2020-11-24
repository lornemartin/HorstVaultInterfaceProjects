using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VaultItemProcessor.Reports2
{
    public class BandsawReportProduct2
    {
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }
        public List<BandsawReportOrder2> ReportOrders { get; set; }
        public List<BandsawReportCutListItem2> ReportCutListItems { get; set; }
        public List<BandsawReportAssembly2> ReportAssemblies { get; set; }
        public List<BandsawReportPart2> ReportParts { get; set; }

        public BandsawReportProduct2(ProductionListProduct prod)
        {
            ProductName = prod.Number;
            ProductDescription = prod.ItemDescription;
            ReportOrders = new List<BandsawReportOrder2>();
            ReportCutListItems = new List<BandsawReportCutListItem2>();
            ReportAssemblies = new List<BandsawReportAssembly2>();
            ReportParts = new List<BandsawReportPart2>();

            BandsawReportOrder2 order = new BandsawReportOrder2();
            order.OrderNumber = prod.OrderNumber;
            order.Qty = prod.Qty;
        }

        public void AddReportOrder(BandsawReportOrder2 order)
        {
            this.ReportOrders.Add(order);

            foreach (Reports2.BandsawReportAssembly2 assy in this.ReportAssemblies)
                assy.ReportOrders.Add(order);
            foreach (Reports2.BandsawReportCutListItem2 cutListItem in this.ReportCutListItems)
                cutListItem.ReportOrders.Add(order);
            foreach (Reports2.BandsawReportPart2 prt in this.ReportParts)
                prt.ReportOrders.Add(order);
        }
    }
}
