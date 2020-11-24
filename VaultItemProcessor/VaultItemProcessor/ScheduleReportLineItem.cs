using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VaultItemProcessor
{
    public class ScheduleReportLineItem
    {
        public int Qty { get; set; }
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
        public string OrderNumber { get; set; }
        public string Type { get; set; }
        public ScheduleReportLineItem()
        {
            Qty = 0;
            Number = "";
            Title = "";
            ItemDescription = "";
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
            OrderNumber = "";
            Type = "";
        }

        public ScheduleReportLineItem(ProductionListProduct prod)
        {
            Qty = prod.Qty;
            Number = prod.Number;
            Title = "";
            ItemDescription = prod.ItemDescription;
            Category = prod.Category;
            Material = "";
            MaterialThickness = "";
            StructCode = "";
            Operations = "";
            PlantID = prod.PlantID;
            IsStock = prod.IsStock;
            HasPdf = false;
            Keywords = prod.Keywords;
            Notes = prod.Notes;
            OrderNumber = prod.OrderNumber;
            Type = "1-Product";
        }

        public ScheduleReportLineItem(ProductionListLineItem lItem, ProductionListProduct parent)
        {
            Qty = lItem.Qty;
            Number = lItem.Number;
            Title = lItem.Title;
            ItemDescription = lItem.ItemDescription;
            Category = lItem.Category;
            Material = lItem.Material;
            MaterialThickness = lItem.MaterialThickness;
            StructCode = lItem.StructCode;
            Operations = lItem.Operations;
            PlantID = lItem.PlantID;
            IsStock = lItem.IsStock;
            HasPdf = lItem.HasPdf;
            Keywords = lItem.Keywords;
            Notes = lItem.Notes;
            OrderNumber = parent.OrderNumber;
            Type = "2-Part";
        }
    }
}
