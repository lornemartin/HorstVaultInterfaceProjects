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

namespace VaultItemProcessor
{
    public class DailyScheduleAggregate
    {
        public string XmlFileName { get; set; }
        public string PdfInputPath { get; set; }
        public List<AggregateLineItem> AggregateLineItemList { get; set; }
        public bool Finalized { get; set; }

        internal DailyScheduleAggregate()
        {
            XmlFileName = "";
            PdfInputPath = "";
            AggregateLineItemList = new List<AggregateLineItem>();
            Finalized = false;
        }


        public DailyScheduleAggregate(string xmlFilePath, string pdfPath)
        {
            //XmlFileName = xmlFilePath;
            XmlFileName = (AppSettings.Get("ExportFilePath").ToString()) + (AppSettings.Get("DailyScheduleData").ToString());
            PdfInputPath = pdfPath;
            AggregateLineItemList = new List<AggregateLineItem>();
            Finalized = false;
        }
        public bool AddLineItem(ExportLineItem item, string orderNumber, int orderQty, string batchItemName, string currentProduct)
        {
            try
            {
                XmlFileName = (AppSettings.Get("ExportFilePath").ToString()) + (AppSettings.Get("DailyScheduleData").ToString());

                string batchName = orderNumber;
                // are we processing an order or a batch?
                bool isBatch = false;
                if (orderNumber.IndexOf("batch", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    isBatch = true;
                    orderNumber = batchItemName;
                }

                AggregateLineItem searchLineItem = AggregateLineItemList.Find(i => i.Number == item.Number);
                if (searchLineItem == null)
                {
                    // add a new line to the list
                    AggregateLineItem newAggregateLineItem = new AggregateLineItem(item, orderNumber, orderQty);
                    AggregateLineItemList.Add(newAggregateLineItem);
                    PrintLineItem(newAggregateLineItem, isBatch, batchItemName, currentProduct,batchName);
                }
                else
                {
                    // add a new order record to associated orders of the existing line
                    OrderData newOrder = new OrderData(orderNumber, item.Qty, orderQty);

                    //update existing info
                    searchLineItem.Category = item.Category;
                    searchLineItem.Number = item.Number;
                    searchLineItem.HasPdf = item.HasPdf;
                    searchLineItem.ItemDescription = item.ItemDescription;
                    searchLineItem.Material = item.Material;
                    searchLineItem.MaterialThickness = item.MaterialThickness;
                    searchLineItem.Parent = item.Parent;
                    searchLineItem.Title = item.Title;
                    searchLineItem.Operations = item.Operations;

                    // add new order information
                    searchLineItem.AssociatedOrders.Add(newOrder);
                    PrintLineItem(searchLineItem, isBatch, batchItemName, currentProduct);
                }

               
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool PrintLineItem(AggregateLineItem item, bool isBatch, string batchItemName, string currentProduct, string batchName="")
        {

            try
            {
                string outputPdfPath = ProcessPDF.CalculateSubFolder(PdfInputPath, Path.GetDirectoryName(XmlFileName), item, isBatch);

                string watermark = "";
                if (isBatch)
                    watermark = "Batch Name: " + batchName + "\n";

                if (item.Category == "Part")  // only process parts at this level
                {
                    string itemNumber = "";
                    if (item.Keywords != "")
                        itemNumber = item.Keywords;
                    else
                        itemNumber = item.Number;

                    watermark += "Item Number: " + itemNumber + "      Desc: " + item.ItemDescription + "\n";
                    if (item.Category == "Part")
                    {
                        watermark += "Material: " + item.StructCode + "\n";
                        watermark += "Operation: " + item.Operations + "\n";
                    }
                    int totalQty = 0;

                    if (item.Notes != "")
                    {
                        watermark += "Notes: " + item.Notes + "\n";
                    }

                    foreach (var order in item.AssociatedOrders)
                    {

                        int lineTotalQty = order.UnitQty * order.OrderQty;
                        if(!isBatch)
                            watermark += "Order: " + order.OrderNumber + "--- Order Qty: " + order.OrderQty + " x per unit Qty: " + order.UnitQty + "---Line Total Qty: " + lineTotalQty + "\n";
                        else
                            watermark += "Batch Line: " + order.OrderNumber + "----- Line Qty: " + order.OrderQty + " x per unit Qty: " + order.UnitQty + "---Line Total Qty: " + lineTotalQty + "\n";
                        totalQty += lineTotalQty;
                    }

                    watermark += "Total Quantity: " + totalQty + "\n";

                    string fileNameToCopy = item.Number + ".pdf";
                    string outputFolder = Path.GetDirectoryName(outputPdfPath) + "\\";

                    if (ProcessPDF.CopyPDF(PdfInputPath, new List<string> { fileNameToCopy }, new List<string> { watermark }, outputFolder))
                        return true;
                    else
                        return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in Processing PDFs.\n" + ex.Message);
                return false;
            }
        }

        public bool SaveToFile()
        {
            try
            {
                XmlSerializer xs = new XmlSerializer(typeof(DailyScheduleAggregate));
                TextWriter tw = new StreamWriter(XmlFileName,false);
                xs.Serialize(tw, this);
                tw.Close();
                return true;
            }
            catch(Exception ex)
            {
                return false;
            }
        }

        public void FinalizeData()
        {
            Finalized = true;
            SaveToFile();

        }

        public bool IsFinalized()
        {
            return Finalized;
        }
    }

    

}
