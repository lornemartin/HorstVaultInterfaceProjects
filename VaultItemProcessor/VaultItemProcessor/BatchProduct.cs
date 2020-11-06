using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VaultItemProcessor
{
    public class BatchProduct
    {
        public int Qty { get; set; }
        public string Number { get; set; }
        public string ItemDescription { get; set; }
        public string Category { get; set; }
        public string PlantID { get; set; }
        public bool IsStock { get; set; }
        public string Keywords { get; set; }
        public string Notes { get; set; }
        public List<BatchItem> SubItems { get; set; }

       public BatchProduct()
        {
            Number = "";
            ItemDescription = "";
            Category = "";
            PlantID = "";
            IsStock = false;
            Keywords = "";
            Notes = "";
            SubItems = new List<BatchItem>();
        }
    }

    
}
