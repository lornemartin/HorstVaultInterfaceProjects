using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace VaultItemProcessor
{
    [XmlRoot("ProductionListLineItem")]
    public class ProductionListLineItem
    {
        [XmlElement("Qty")]
        public int Qty { get; set; }
        [XmlElement("Number")]
        public string Number { get; set; }
        [XmlElement("Title")]
        public string Title { get; set; }
        [XmlElement("ItemDescription")]
        public string ItemDescription { get; set; }
        [XmlElement("Category")]
        public string Category { get; set; }
        [XmlElement("Material")]
        public string Material { get; set; }
        [XmlElement("MaterialThickness")]
        public string MaterialThickness { get; set; }
        [XmlElement("StructCode")]
        public string StructCode { get; set; }
        [XmlElement("Operations")]
        public string Operations { get; set; }
        [XmlElement("HasPdf")]
        public bool HasPdf { get; set; }
        [XmlElement("PlantID")]
        public string PlantID { get; set; }
        [XmlElement("IsStock")]
        public bool IsStock { get; set; }
        [XmlElement("Keywords")]
        public string Keywords { get; set; }
        [XmlElement("Notes")]
        public string Notes { get; set; }

        public ProductionListLineItem()
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
