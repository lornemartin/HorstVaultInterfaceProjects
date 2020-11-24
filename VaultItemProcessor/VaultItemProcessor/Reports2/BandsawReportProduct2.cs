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
        }

        //public void AddAssembly(ProductionListProduct prod)
        //{
        //    BandsawReportAssembly reportAssembly = new BandsawReportAssembly();
        //    reportAssembly.AssemblyName = prod.Number;
        //    reportAssembly.AssemblyDesc = prod.ItemDescription;
        //    reportAssembly.Pages = new List<byte[]>();

        //    BandsawReportOrder order = new BandsawReportOrder();
        //    order.OrderNumber = prod.OrderNumber;
        //    order.Qty = prod.Qty;
        //    reportAssembly.Orders.Add(order);

        //    ReportAssemblies.Add(reportAssembly);
        //}

        //public void AddProduct(ProductionListProduct prod)
        //{
        //    BandsawReportAssembly reportProduct = new BandsawReportAssembly();
        //    reportProduct.AssemblyName = prod.Number;
        //    reportProduct.AssemblyDesc = prod.ItemDescription;

        //    BandsawReportOrder order = new BandsawReportOrder();
        //    order.OrderNumber = prod.OrderNumber;
        //    order.Qty = prod.Qty;
        //    reportProduct.Orders.Add(order);

        //    ReportPro

        //}

    }
}
