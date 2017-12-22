using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VaultItemProcessor
{
    public class ExportLineItem : IComparable<ExportLineItem>
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
        public int Qty { get; set; }
        public bool RequiresPdf { get; set; }
        public bool HasPdf { get; set; }
        public bool IsStock { get; set; }
        public string PlantID { get; set; }
        public string Comment { get; set; }
        public DateTime DateModified { get; set; }
        public string LifeCycleState { get; set; }
        public string Keywords { get; set; }
        public string Notes { get; set; }

        public IList<ExportLineItem> subItems { get; set; }

        public ExportLineItem()
        {
            Parent = "";
            Number = "";
            Title = "";
            ItemDescription = "";
            Category = "";
            MaterialThickness = "";
            Operations = "";
            StructCode = "";
            Material = "";
            Qty = 0;
            RequiresPdf = true;
            HasPdf = false;
            IsStock = false;
            PlantID = "";
            Comment = "";
            DateModified = new DateTime();
            LifeCycleState = "";
            Keywords = "";
            Notes = "";
            subItems = new List<ExportLineItem>();
        }

        public ExportLineItem(string parent, string number, string title, string itemDesc, string category, string thickness, string material, string structCode, string ops, DateTime dt, int qty = 1, string plantID = "", bool isStock = false, bool hasPdf = false, bool requiresPdf = false, string comment = "", string lifeCycleState = "", string keywords = "", string notes = "")
        {
            Parent = parent;
            Number = number;
            Title = title;
            ItemDescription = itemDesc;
            Category = category;
            MaterialThickness = thickness;
            Operations = ops;
            Material = material;
            StructCode = structCode;
            Qty = qty;
            PlantID = plantID;
            IsStock = isStock;
            HasPdf = hasPdf;
            Comment = comment;
            DateModified = dt;
            LifeCycleState = lifeCycleState;
            Keywords = keywords;
            Notes = notes;
            RequiresPdf = requiresPdf;
            if (Qty == 0) Qty = 1;
            subItems = new List<ExportLineItem>();
        }

        public int CompareTo(ExportLineItem other)
        {
            // this sorts the underlying data that the treelist is displaying.
            // it controls the sort order of the pdfs in the root level pdf folder.
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
                                    if(this.Category=="Assembly")
                                    {
                                        if (this.Parent == other.Parent)
                                        {
                                            return this.Number.CompareTo(other.Number);
                                        }
                                        else
                                        {
                                            return this.Parent.CompareTo(other.Parent);
                                        }
                                    }
                                    return 0;
                                }
                                else
                                {
                                    return this.StructCode.CompareTo(other.StructCode);
                                }
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

                    else if (this.Category == "Product")
                        return -1;
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

        public static Comparison<ExportLineItem> CompareToBatchItems = delegate (ExportLineItem first, ExportLineItem second)
        {
            // this sorts the underlying data that the treelist is displaying.
            // it controls the sort order of the pdfs in the root level pdf folder.
            try
            {
                if (first.Category == second.Category)
                {
                    if (first.IsStock == second.IsStock)
                    {

                        if (first.Operations == second.Operations)
                        {
                            if (first.MaterialThickness == second.MaterialThickness)
                            {
                                if (first.StructCode == second.StructCode)
                                {
                                    return 0;
                                }
                                else
                                {
                                    return first.StructCode.CompareTo(second.StructCode);
                                }
                            }
                            else
                            {
                                return first.MaterialThickness.CompareTo(second.MaterialThickness);
                            }
                        }
                        else
                        {
                            return first.Operations.CompareTo(second.Operations);
                        }
                    }
                    else
                    {
                        return first.IsStock.CompareTo(second.IsStock);
                    }
                }


                else if (first.Category == "Product")
                    return -1;
                else if ((first.Category == "Assembly") && (second.Category == "Part"))
                    return -1;
                else if ((first.Category == "Part") && (second.Category == "Assembly"))
                    return 1;
                else
                    return 0;
            }

            catch { return 0; }
        };
    }
}
