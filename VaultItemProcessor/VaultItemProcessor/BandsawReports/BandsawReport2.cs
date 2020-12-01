using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VaultItemProcessor.Reports2
{
    public class BandsawReport2
    {
        public List<BandsawReportProduct2> ReportProducts { get; set; }
        //public void AddProduct(ProductionListProduct prod)
        //{
        //    string itemNumber = prod.Number;
        //    string orderNumber = prod.OrderNumber;

        //    BandsawReportProduct searchProduct = ReportProducts.Where(r => r.ProductName == itemNumber).FirstOrDefault();
        //    if (searchProduct == null)
        //    {
        //        searchProduct = new BandsawReportProduct();
        //        searchProduct.ProductName = itemNumber;
        //        searchProduct.ProductDescription = prod.ItemDescription;

        //        BandsawReportOrder order = new BandsawReportOrder();
        //        order.OrderNumber = orderNumber;
        //        order.Qty = prod.Qty;

        //        searchProduct.ReportOrders.Add(order);
        //        ReportProducts.Add(searchProduct);
        //    }
        //    else
        //    {
        //        BandsawReportOrder order = new BandsawReportOrder();
        //        order.OrderNumber = orderNumber;
        //        order.Qty = prod.Qty;

        //        searchProduct.ReportOrders.Add(order);
        //    }
        //}

    }
}
