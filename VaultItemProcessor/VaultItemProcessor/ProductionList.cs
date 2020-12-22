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
using DevExpress.XtraPdfViewer;

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
        public IEnumerable<Reports2.BandsawReportProduct2> GetBatchReport()
        {
            Load();

            Reports2.BandsawReport2 report = new Reports2.BandsawReport2();
            report.ReportProducts = new List<Reports2.BandsawReportProduct2>();

            foreach (ProductionListProduct prod in productList)
            {
                Reports2.BandsawReportProduct2 reportProd = new Reports2.BandsawReportProduct2(prod);
                Reports2.BandsawReportProduct2 searchProd = new Reports2.BandsawReportProduct2(prod);

                searchProd = report.ReportProducts.Where(r => r.ProductName == prod.Number).FirstOrDefault();

                if (searchProd == null)
                {
                    Reports2.BandsawReportOrder2 order = new Reports2.BandsawReportOrder2();
                    order.Qty = prod.Qty;
                    order.OrderNumber = prod.OrderNumber;
                    reportProd.ReportOrders.Add(order);

                    foreach (ProductionListLineItem lineItem in prod.SubItems)
                    {
                        if (lineItem.Category == "Assembly")
                        {
                            Reports2.BandsawReportAssembly2 assy = new Reports2.BandsawReportAssembly2();
                            assy.AssemblyName = lineItem.Number;
                            assy.AssemblyDesc = lineItem.ItemDescription;
                            assy.Qty = lineItem.Qty;

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
        public IEnumerable<ProductionListLineItem> GetLaserScheduleReport()
        {
            Load();

            List<ProductionListLineItem> subList = new List<ProductionListLineItem>();
            List<ProductionListLineItem> completeList = new List<ProductionListLineItem>();

            foreach (ProductionListProduct prod in productList)
            {
                // this was used to retrieve only laser parts, now we want to retrieve all parts.
                //subList = prod.SubItems.Where(i => i.Operations == "Laser").ToList();

                subList = prod.SubItems;
                foreach (ProductionListLineItem lItem in subList)
                {
                    completeList.Add(lItem);
                }
            }

            
            completeList = completeList.OrderBy(i => i.MaterialThickness).ToList();

            return completeList;
        }

        [HighlightedMember]
        public IEnumerable<Reports2.BandsawReportProduct2> GetBandsawReport2()
        {
            Load();

            Reports2.BandsawReport2 report = new Reports2.BandsawReport2();
            report.ReportProducts = new List<Reports2.BandsawReportProduct2>();

            foreach (ProductionListProduct prod in productList)
            {
                Reports2.BandsawReportProduct2 reportProd = new Reports2.BandsawReportProduct2(prod);
                Reports2.BandsawReportProduct2 searchProd = new Reports2.BandsawReportProduct2(prod);

                searchProd = report.ReportProducts.Where(r => r.ProductName == prod.Number).FirstOrDefault();

                if (searchProd == null)
                {
                    Reports2.BandsawReportOrder2 order = new Reports2.BandsawReportOrder2();
                    order.Qty = prod.Qty;
                    order.OrderNumber = prod.OrderNumber;
                    reportProd.ReportOrders.Add(order);

                    foreach (ProductionListLineItem lineItem in prod.SubItems)
                    {
                        if (lineItem.Category == "Assembly")
                        {
                            Reports2.BandsawReportAssembly2 assy = new Reports2.BandsawReportAssembly2();
                            assy.AssemblyName = lineItem.Number;
                            assy.AssemblyDesc = lineItem.ItemDescription;
                            assy.Qty = lineItem.Qty;

                            order.Qty = prod.Qty;
                            order.OrderNumber = prod.OrderNumber;
                            assy.ReportOrders = new List<Reports2.BandsawReportOrder2>();
                            assy.ReportOrders.Add(order);
                            assy.Pages = new List<Bitmap>();

                            string filename = (AppSettings.Get("ExportFilePath").ToString() + "Pdfs\\") + lineItem.Number + ".pdf";
                            if (File.Exists(filename))
                            {
                                PdfViewer pdfViewer = new PdfViewer();
                                byte[] bytes = System.IO.File.ReadAllBytes(filename);

                                Stream stream = new MemoryStream(bytes);

                                pdfViewer.LoadDocument(stream);

                                int numPages = pdfViewer.PageCount;

                                PdfDocument doc = new PdfDocument();
                                doc = PdfReader.Open(filename, PdfDocumentOpenMode.InformationOnly);

                                for (int p = 1; p <= numPages; p++)
                                {
                                    PdfPage page = doc.Pages[p-1];
                                    Bitmap bitmap = pdfViewer.CreateBitmap(p, 950);

                                    float h = (float)page.Height;
                                    float w = (float)page.Width;
                                    var angle = page.Rotate;
                                    var orient = page.Orientation;
                                    if ((page.Orientation == PdfSharp.PageOrientation.Portrait && angle == 90) || (page.Orientation == PdfSharp.PageOrientation.Portrait && h < w))
                                        bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);
                                    
                                    assy.Pages.Add(bitmap);
                                }


                                pdfViewer.CloseDocument();
                                pdfViewer.Dispose();
                            }

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
                                prt.Pages = new List<Bitmap>();

                                order.Qty = prod.Qty;
                                order.OrderNumber = prod.OrderNumber;
                                prt.ReportOrders = new List<Reports2.BandsawReportOrder2>();
                                prt.ReportOrders.Add(order);

                                string filename = (AppSettings.Get("ExportFilePath").ToString() + "Pdfs\\") + lineItem.Number + ".pdf";
                                if (File.Exists(filename))
                                {
                                    PdfViewer pdfViewer = new PdfViewer();
                                    byte[] bytes = System.IO.File.ReadAllBytes(filename);

                                    Stream stream = new MemoryStream(bytes);

                                    pdfViewer.LoadDocument(stream);

                                    int numPages = pdfViewer.PageCount;

                                    PdfDocument doc = new PdfDocument();
                                    doc = PdfReader.Open(filename, PdfDocumentOpenMode.InformationOnly);

                                    for (int p = 1; p <= numPages; p++)
                                    {
                                        PdfPage page = doc.Pages[p - 1];
                                        Bitmap bitmap = pdfViewer.CreateBitmap(p, 950);

                                        float h = (float)page.Height;
                                        float w = (float)page.Width;
                                        var angle = page.Rotate;
                                        var orient = page.Orientation;
                                        if ((page.Orientation == PdfSharp.PageOrientation.Portrait && angle == 90) || (page.Orientation == PdfSharp.PageOrientation.Portrait && h < w))
                                            bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);

                                        prt.Pages.Add(bitmap);
                                    }

                                    pdfViewer.CloseDocument();
                                    pdfViewer.Dispose();
                                }


                                reportProd.ReportParts.Add(prt);
                            }
                            else
                            {
                                Reports2.BandsawReportCutListItem2 cutItem = new Reports2.BandsawReportCutListItem2();
                                cutItem.ItemNumber = lineItem.Number;
                                cutItem.ItemDescription = lineItem.ItemDescription;
                                cutItem.Material = lineItem.Material;
                                cutItem.Qty = lineItem.Qty;

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
