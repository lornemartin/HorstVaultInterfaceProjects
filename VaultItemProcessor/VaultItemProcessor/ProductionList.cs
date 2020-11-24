﻿using System;
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
                    if(lineItem.Category == "Assembly")
                    {
                        Reports1.BandsawReportAssembly assy = new Reports1.BandsawReportAssembly();
                        assy.AssemblyName = lineItem.Number;
                        assy.AssemblyDesc = lineItem.ItemDescription;
                        assy.Qty = lineItem.Qty;
                        
                        

                        reportProd.ReportAssemblies.Add(assy);
                    }
                    if(lineItem.Category == "Part" && lineItem.IsStock == false && lineItem.Operations == "Bandsaw")
                    {
                        if(lineItem.HasPdf)
                        {
                            Reports1.BandsawReportPart prt = new Reports1.BandsawReportPart();
                            prt.PartName = lineItem.Number;
                            prt.PartDesc = lineItem.ItemDescription;
                            prt.Material = lineItem.Material;
                            prt.Thickness = lineItem.MaterialThickness;
                            prt.Qty = lineItem.Qty;
                           
                            reportProd.ReportParts.Add(prt);
                        }
                        else
                        {
                            Reports1.BandsawReportCutListItem cutItem = new Reports1.BandsawReportCutListItem();
                            cutItem.ItemNumber = lineItem.Number;
                            cutItem.ItemDescription = lineItem.ItemDescription;
                            cutItem.Material = lineItem.Material;
                            cutItem.Qty = lineItem.Qty;

                            
                            reportProd.ReportCutListItems.Add(cutItem);
                        }
                    }
                }

                report.ReportProducts.Add(reportProd);        // add the product once all the subitems are added to it.
            }

            return report.ReportProducts.ToList();
        }

        [HighlightedMember]
        public IEnumerable<Reports2.BandsawReportProduct2> GetBandsawReport2()
        {
            Load();

            Reports2.BandsawReport2 report = new Reports2.BandsawReport2();
            report.ReportProducts = new List<Reports2.BandsawReportProduct2>();

            List<ScheduleReportLineItem> lineItemList = new List<ScheduleReportLineItem>();

            foreach (ProductionListProduct prod in productList)
            {
                Reports2.BandsawReportProduct2 reportProd = new Reports2.BandsawReportProduct2(prod);
                Reports2.BandsawReportProduct2 searchProd = new Reports2.BandsawReportProduct2(prod);

                searchProd = report.ReportProducts.Where(r => r.ProductName == prod.Number).FirstOrDefault();

                if (searchProd == null)
                {
                    foreach (ProductionListLineItem lineItem in prod.SubItems)
                    {
                        if (lineItem.Category == "Assembly")
                        {
                            Reports2.BandsawReportAssembly2 assy = new Reports2.BandsawReportAssembly2();
                            assy.AssemblyName = lineItem.Number;
                            assy.AssemblyDesc = lineItem.ItemDescription;
                            assy.Qty = lineItem.Qty;

                            Reports2.BandsawReportOrder2 order = new Reports2.BandsawReportOrder2();
                            order.Qty = prod.Qty;
                            order.OrderNumber = prod.OrderNumber;
                            assy.ReportOrders = new List<Reports2.BandsawReportOrder2>();
                            assy.ReportOrders.Add(order);

                            reportProd.ReportAssemblies.Add(assy);
                        }
                        if (lineItem.Category == "Part" && lineItem.IsStock == false && lineItem.Operations == "Bandsaw")
                        {
                            if (lineItem.HasPdf)
                            {
                                Reports2.BandsawReportPart2 prt = new Reports2.BandsawReportPart2();
                                prt.PartName = lineItem.Number;
                                prt.PartDesc = lineItem.ItemDescription;
                                prt.Material = lineItem.Material;
                                prt.Thickness = lineItem.MaterialThickness;
                                prt.Qty = lineItem.Qty;

                                Reports2.BandsawReportOrder2 order = new Reports2.BandsawReportOrder2();
                                order.Qty = prod.Qty;
                                order.OrderNumber = prod.OrderNumber;
                                prt.ReportOrders = new List<Reports2.BandsawReportOrder2>();
                                prt.ReportOrders.Add(order);
                                reportProd.ReportParts.Add(prt);
                            }
                            else
                            {
                                Reports2.BandsawReportCutListItem2 cutItem = new Reports2.BandsawReportCutListItem2();
                                cutItem.ItemNumber = lineItem.Number;
                                cutItem.ItemDescription = lineItem.ItemDescription;
                                cutItem.Material = lineItem.Material;
                                cutItem.Qty = lineItem.Qty;

                                Reports2.BandsawReportOrder2 order = new Reports2.BandsawReportOrder2();
                                order.Qty = prod.Qty;
                                order.OrderNumber = prod.OrderNumber;
                                cutItem.ReportOrders = new List<Reports2.BandsawReportOrder2>();
                                cutItem.ReportOrders.Add(order);

                                reportProd.ReportCutListItems.Add(cutItem);
                            }
                        }
                    }
                    report.ReportProducts.Add(reportProd);        // add the product once all the subitems are added to it.
                }
                else
                {
                    // only add the order number to the existing product, instead of adding a new record
                    Reports2.BandsawReportOrder2 newOrder = new Reports2.BandsawReportOrder2();
                    newOrder.Qty = prod.Qty;
                    newOrder.OrderNumber = prod.OrderNumber;

                    searchProd.AddReportOrder(newOrder);
                }
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
