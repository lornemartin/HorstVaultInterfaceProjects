using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace VaultItemProcessor
{
    [XmlRoot("ProductionListProduct")]
    public class ProductionListProduct
    {
        [XmlElement("ID")]
        public int ID { get; set; }
        [XmlElement("Qty")]
        public int Qty { get; set; }
        [XmlElement("Number")]
        public string Number { get; set; }
        [XmlElement("ItemDescription")]
        public string ItemDescription { get; set; }
        [XmlElement("Category")]
        public string Category { get; set; }
        [XmlElement("PlantID")]
        public string PlantID { get; set; }
        [XmlElement("IsStock")]
        public bool IsStock { get; set; }
        [XmlElement("Keywords")]
        public string Keywords { get; set; }
        [XmlElement("Notes")]
        public string Notes { get; set; }
        [XmlElement("OrderNumber")]
        public string OrderNumber { get; set; }
        [System.Xml.Serialization.XmlArrayItemAttribute("ProductionListLineItem", IsNullable = false)]
        public List<ProductionListLineItem> SubItems { get; set; }

       public ProductionListProduct()
        {
            ID = 0;
            Number = "";
            ItemDescription = "";
            Category = "";
            PlantID = "";
            IsStock = false;
            Keywords = "";
            Notes = "";
            SubItems = new List<ProductionListLineItem>();
        }
    }

    
}
