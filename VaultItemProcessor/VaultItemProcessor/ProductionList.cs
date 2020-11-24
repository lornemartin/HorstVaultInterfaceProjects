using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;
using PdfSharp;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using PdfSharp.Pdf.IO;
using System.Drawing;
using PdfSharp.Drawing.Layout;
using DevExpress.XtraEditors.Controls;
using DevExpress.DataAccess;
using DevExpress.DataAccess.ObjectBinding;

namespace VaultItemProcessor
{
    [System.ComponentModel.DisplayName("ProductionList")]
    [HighlightedClass]
    public class ProductionListDataSource
    {
        public string XmlFileName { get; set; }
        public string PdfInputPath { get; set; }
        public List<ProductionListProduct> productList { get; set; }
        public bool Finalized { get; set; }
        public int currentIndex { get; set; }

        // The HighlightedMember attribute highlights a constructor.
        [HighlightedMember]
        
        public ProductionListDataSource()
        {
            //XmlFileName = xmlFilePath + "ProductionList.xml";
            XmlFileName = (AppSettings.Get("ExportFilePath").ToString()) + @"ProductionList.xml";
            PdfInputPath = (AppSettings.Get("PdfPath").ToString());
            productList = new List<ProductionListProduct>();
            Finalized = false;
            currentIndex = 0;
        }




        public void AddProduct(ProductionListProduct prod)
        {
            if (productList.Count > 0)
            {
                int maxId = productList.Max(p => p.ID);
                prod.ID = maxId + 1;
                currentIndex = prod.ID;
            }
            else
                prod.ID = 0;

            productList.Add(prod);

            SaveToFile();
        }

        public ProductionListProduct GetPrev()
        {
            if (currentIndex >= 1)
            {
                currentIndex--;
                return productList.ElementAt(currentIndex);
            }
            else
            {
                return null;
            }
        }

        public ProductionListProduct GetNext()
        {
            if (currentIndex < productList.Count())
            {
                currentIndex++;
                return productList.ElementAt(currentIndex);
            }
            else
            {
                return null;
            }
        }

        // The HighlightedMember attribute highlights a data member.
        [HighlightedMember]
        public IEnumerable<ProductionListProduct> GetProductionList()
        {
            Load();
            return this.productList;
        }

        [HighlightedMember]
        public IEnumerable<ScheduleReportLineItem> GetSchedulelineItems()
        {
            Load();
            List<ScheduleReportLineItem> lineItemList = new List<ScheduleReportLineItem>();

            foreach(ProductionListProduct prod in productList)
            {
                ScheduleReportLineItem parent = new ScheduleReportLineItem(prod);
                lineItemList.Add(parent);

                foreach(ProductionListLineItem lineItem in prod.SubItems)
                {
                    ScheduleReportLineItem subItem = new ScheduleReportLineItem(lineItem,prod);
                    lineItemList.Add(subItem);
                }
            }

            return lineItemList;
        }

        [HighlightedMember]
        public IEnumerable<Reports1.BandsawReportProduct> GetBandsawReport()
        {
            Load();

            Reports1.BandsawReport report = new Reports1.BandsawReport();
            report.ReportProducts = new List<Reports1.BandsawReportProduct>();

            List<ScheduleReportLineItem> lineItemList = new List<ScheduleReportLineItem>();

            foreach (ProductionListProduct prod in productList)
            {
                Reports1.BandsawReportProduct reportProd = new Reports1.BandsawReportProduct(prod);

                foreach (ProductionListLineItem lineItem in prod.SubItems)
                {
                    //ScheduleReportLineItem subItem = new ScheduleReportLineItem(lineItem, prod);
                    //lineItemList.Add(subItem);
                    if(lineItem.Category == "Assembly")
                    {
                        Reports1.BandsawReportAssembly assy = new Reports1.BandsawReportAssembly();
                        assy.AssemblyName = lineItem.Number;
                        assy.AssemblyDesc = lineItem.ItemDescription;
                        reportProd.ReportAssemblies.Add(assy);
                    }
                    if(lineItem.Category == "Part")
                    {
                        if(lineItem.HasPdf)
                        {
                            Reports1.BandsawReportPart prt = new Reports1.BandsawReportPart();
                            prt.PartName = lineItem.Number;
                            prt.PartDesc = lineItem.ItemDescription;
                            prt.Material = lineItem.Material;
                            prt.Thickness = lineItem.MaterialThickness;
                            reportProd.ReportParts.Add(prt);
                        }
                        else
                        {
                            Reports1.BandsawReportCutListItem cutItem = new Reports1.BandsawReportCutListItem();
                            cutItem.ItemNumber = lineItem.Number;
                            cutItem.ItemDescription = lineItem.ItemDescription;
                            cutItem.Material = lineItem.Material;
                            reportProd.ReportCutListItems.Add(cutItem);
                        }
                    }
                }

                report.ReportProducts.Add(reportProd);        // add the product once all the subitems are added to it.
            }

            return report.ReportProducts.ToList();
        }

        [HighlightedMember]
        public IEnumerable<ScheduleReportLineItem> GetLaserReport()
        {
            Load();

            List<ScheduleReportLineItem> lineItemList = new List<ScheduleReportLineItem>();

            foreach (ProductionListProduct prod in productList)
            {
                // not displaying the products or assemblies in this report.

                foreach (ProductionListLineItem lineItem in prod.SubItems)
                {
                    ScheduleReportLineItem subItem = new ScheduleReportLineItem(lineItem, prod);
                    if(subItem.Operations == "Laser")
                        lineItemList.Add(subItem);
                }
            }

            return lineItemList;
        }
        public bool SaveToFile()
        {
            try
            {
                XmlSerializer xs = new XmlSerializer(typeof(ProductionListDataSource));
                TextWriter tw = new StreamWriter(XmlFileName, false);
                xs.Serialize(tw, this);
                tw.Close();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public ProductionListDataSource Load()
        {
            try
            {
                XmlSerializer xs = new XmlSerializer(typeof(ProductionListDataSource));
                TextReader tr = new StreamReader(XmlFileName, false);

                ProductionListDataSource pList = (ProductionListDataSource) xs.Deserialize(tr);
                //productList = (ProductionListProduct) xs.Deserialize(tr);
                tr.Close();
                productList = pList.productList;
                return pList;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        //public void FinalizeData()
        //{
        //    Finalized = true;
        //    SaveToFile();

        //}

        //public bool IsFinalized()
        //{
        //    return Finalized;
        //}
    }



    

}
