using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VaultItemProcessor
{
    public class AggregateLineItem
    {
        public string Parent { get; set; }
        public string Number { get; set; }
        public string Title { get; set; }
        public string ItemDescription { get; set; }
        public string Category { get; set; }
        public string Material { get; set; }
        public string MaterialThickness { get; set; }
        public string StructCode { get; set; }
        public string Operations { get; set; }
        public bool HasPdf { get; set; }
        public string PlantID { get; set; }
        public bool IsStock { get; set; }
        public string Keywords { get; set; }
        public string Notes { get; set; }
        public List<OrderData> AssociatedOrders { get; set; }

        public AggregateLineItem()
        {
            Parent = "";
            Number = "";
            Title = "";
            ItemDescription ="";
            Category = "";
            Material = "";
            MaterialThickness = "";
            StructCode = "";
            Operations = "";
            PlantID = "";
            IsStock = false;
            HasPdf = false;
            Keywords = "";
            Notes = "";


            AssociatedOrders = new List<OrderData>();
        }

        public AggregateLineItem(ExportLineItem exportedLineItem, string orderNumber, int orderQty)
        {
            Parent = exportedLineItem.Parent;
            Number = exportedLineItem.Number;
            Title = exportedLineItem.Title;
            ItemDescription = exportedLineItem.ItemDescription;
            Category = exportedLineItem.Category;
            Material = exportedLineItem.Material;
            MaterialThickness = exportedLineItem.MaterialThickness;
            StructCode = exportedLineItem.StructCode;
            Operations = exportedLineItem.Operations;
            HasPdf = exportedLineItem.HasPdf;
            PlantID = exportedLineItem.PlantID;
            IsStock = exportedLineItem.IsStock;
            Keywords = exportedLineItem.Keywords;
            Notes = exportedLineItem.Notes;
            OrderData order = new OrderData(orderNumber, exportedLineItem.Qty, orderQty);
            AssociatedOrders = new List<OrderData>();
            AssociatedOrders.Add(order);
        }
    }

    public class OrderData
    {
        public string OrderNumber { get; set; }
        public int UnitQty { get; set; }
        public int OrderQty { get; set; }

        public OrderData()
        {
            OrderNumber = "";
            UnitQty = 0;
            OrderQty = 0;
        }

        public OrderData(string orderNumber, int unitQty, int orderQty)
        {
            OrderNumber = orderNumber;
            UnitQty = unitQty;
            OrderQty = orderQty;
        }
    }

    
}
