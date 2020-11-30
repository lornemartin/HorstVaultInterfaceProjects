using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace VaultItemProcessor
{
    [XmlRoot("ProductionListLineItem")]
    public class ProductionListLineItem : IComparable<ProductionListLineItem>
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

        public int CompareTo(ProductionListLineItem other)
        {
            //this sorts the underlying data that the treelist is displaying.
            //it controls the sort order of the pdfs in the root level pdf folder.
            try
            {
                if (this.IsStock == other.IsStock)
                {
                    if (this.Category == other.Category)
                    {
                        if (this.Operations == other.Operations)
                        {
                            if (this.MaterialThickness == other.MaterialThickness)
                            {
                                if (this.StructCode == other.StructCode)
                                {
                                    return this.StructCode.CompareTo(other.StructCode);
                                }
                                else
                                    return 0;
                            }
                            else
                            {
                                return this.MaterialThickness.CompareTo(other.MaterialThickness);
                            }
                        }
                        else
                        {
                            return this.Operations.CompareTo(other.Operations);
                        }
                    }

                    else if ((this.Category == "Assembly") && (other.Category == "Part"))
                        return -1;
                    else if ((this.Category == "Part") && (other.Category == "Assembly"))
                        return 1;

                    else
                        return 0;
                }
                else
                {
                    return this.IsStock.CompareTo(other.IsStock);
                }
            }

            catch { return 0; }
        }
    }

    
}
