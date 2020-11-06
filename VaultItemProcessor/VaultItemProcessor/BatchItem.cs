using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VaultItemProcessor
{
    public class BatchItem
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

        public BatchItem()
        {
            Qty = 0;
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
        }
    }

    
}
