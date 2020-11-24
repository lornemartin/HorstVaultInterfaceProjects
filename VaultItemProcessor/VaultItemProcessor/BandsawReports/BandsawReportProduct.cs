using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VaultItemProcessor.Reports1
{
    public class BandsawReportProduct
    {
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }
        public List<BandsawReportOrder> ReportOrders { get; set; }
        public List<BandsawReportCutListItem> ReportCutListItems { get; set; }
        public List<BandsawReportAssembly> ReportAssemblies { get; set; }
        public List<BandsawReportPart> ReportParts { get; set; }

        public BandsawReportProduct(ProductionListProduct prod)
        {
            ProductName = prod.Number;
            ProductDescription = prod.ItemDescription;
            ReportOrders = new List<BandsawReportOrder>();
            ReportCutListItems = new List<BandsawReportCutListItem>();
            ReportAssemblies = new List<BandsawReportAssembly>();
            ReportParts = new List<BandsawReportPart>();

            BandsawReportOrder order = new BandsawReportOrder();
            order.OrderNumber = prod.OrderNumber;
            order.Qty = prod.Qty;
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
