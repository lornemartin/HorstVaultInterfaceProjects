using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PdfSharp;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using PdfSharp.Pdf.IO;
using VaultAccess;
using DevExpress.XtraTreeList.Menu;
using DevExpress.XtraSplashScreen;
using System.Xml.Serialization;
using PdfSharp.Drawing.Layout;
using Npgsql;
using System.Security.AccessControl;
using System.Text.RegularExpressions;
using DevExpress.Utils.Design;
using DevExpress.XtraRichEdit.Model;
using DevExpress.XtraReports.UI;
using DevExpress.XtraPrinting.Preview;

namespace VaultItemProcessor
{
    public partial class Form1 : Form
    {
        public string vaultExportFile { get; set; }
        public string vaultExportFilePath { get; set; }
        public string exportFilePath { get; set; }
        public string vaultExportFileWithPath { get; set; }
        public string pdfPath { get; set; }
        public string jobName { get; set; }
        public List<string> pdfList { get; set; }
        public List<ExportLineItem> lineItemList { get; set; }
        public VaultAccess.VaultAccess hlaVault { get; set; }
        public string vaultUserName { get; set; }
        public string vaultPassword { get; set; }
        public string vaultServer { get; set; }
        public string vaultVault { get; set; }
        public DailyScheduleAggregate dailyScheduleData { get; set; }
        public string currentItem { get; set; }

        public ProductionListDataSource productionList { get; set; }

        ProductionListProduct plProd = new ProductionListProduct();

        public DateTime lastFileUpdateTime { get; set; }

        public Form1()
        {
            try
            {
                InitializeComponent();
                if (File.Exists(AppSettings.SettingsFilePath))
                {
                    vaultExportFile = AppSettings.Get("VaultExportFileName").ToString();
                    vaultExportFilePath = AppSettings.Get("VaultExportFilePath").ToString();
                    vaultExportFileWithPath = vaultExportFilePath + vaultExportFile;
                    exportFilePath = AppSettings.Get("ExportFilePath").ToString();
                    pdfPath = AppSettings.Get("PdfPath").ToString();        // need a double slash at the end, or else printPDF gets confused
                    jobName = AppSettings.Get("JobName").ToString();
                    lineItemList = new List<ExportLineItem>();
                    hlaVault = null;
                    vaultUserName = AppSettings.Get("VaultUserName").ToString();
                    vaultPassword = AppSettings.Get("VaultPassword").ToString();
                    vaultServer = AppSettings.Get("VaultServer").ToString();
                    vaultVault = AppSettings.Get("VaultVault").ToString();
                    dailyScheduleData = new DailyScheduleAggregate(exportFilePath + AppSettings.Get("DailyScheduleData").ToString(), pdfPath);
                    productionList = new ProductionListDataSource();
                    productionList.Load();
                    lastFileUpdateTime = new DateTime();
                }
                else
                {
                    MessageBox.Show("Cannot find app settings file in " + AppSettings.SettingsFilePath);
                    this.Close();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                // first delete all the temp files from the previous run
                string tempDir = System.IO.Path.GetTempPath() + @"\VaultItemProcessor\";
                if (Directory.Exists(tempDir))
                {
                    DirectoryInfo dir = new DirectoryInfo(tempDir);
                    foreach (FileInfo f in dir.GetFiles())
                    {
                        System.IO.File.SetAttributes(f.FullName, FileAttributes.Normal);  // not sure if this is proper, but can't access file otherwise to delete it...
                        f.Delete();
                    }
                }

                if (System.IO.File.Exists(exportFilePath + "AggregateData.xml"))
                {
                    XmlSerializer xs = new XmlSerializer(typeof(DailyScheduleAggregate));
                    using (var sr = new StreamReader(exportFilePath + "AggregateData.xml"))
                    {
                        dailyScheduleData = (DailyScheduleAggregate)xs.Deserialize(sr);
                    }

                    if (dailyScheduleData.IsFinalized()) btnConfirm.Enabled = false;
                }
                textBoxOutputFolder.Text = exportFilePath;

                string watchPath = AppSettings.Get("VaultExportFilePath").ToString();
                string watchFile = AppSettings.Get("VaultExportFileName").ToString();
                FileSystemWatcher watcher = new FileSystemWatcher(watchPath, watchFile);
                watcher.EnableRaisingEvents = true;
                watcher.Path = watchPath;
                //watcher.NotifyFilter = NotifyFilters.FileName;
                watcher.IncludeSubdirectories = false;
                watcher.Changed += new FileSystemEventHandler(watcher_Changed);
                watcher.Created += new FileSystemEventHandler(watcher_Created);

                btnConfirm.Enabled = false;
                btnPreviousRecord.Enabled = false;
                btnNextRecord.Enabled = false;

                //if (textBoxOutputFolder.Text.Contains("Batch") || (textBoxOutputFolder.Text.Contains("batch")))
                //{
                //    btnRemoveBatchItem.Enabled = true;
                //    btnRemoveOrder.Enabled = false;
                //}
                //else
                //{
                //    btnRemoveBatchItem.Enabled = false;
                //    btnRemoveOrder.Enabled = true;

                //}

                LoadData();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void watcher_Changed(object sender, System.IO.FileSystemEventArgs e)
        {
            FileInfo f = new FileInfo(vaultExportFileWithPath);
            while (IsFileLocked(f))
            {
                Thread.Sleep(TimeSpan.FromSeconds(0.5));
            }
            this.Invoke(new Action(
                delegate ()
                {
                    processVaultItemExport();
                    
                }));

            this.Invoke(new Action(
                delegate ()
                {
                    toolStripStatusLabel1.Text = "Export File Loaded";
                }));
        }

        private void watcher_Created(object sender, System.IO.FileSystemEventArgs e)
        {
            FileInfo f = new FileInfo(vaultExportFileWithPath);
            while (IsFileLocked(f))
            {

            }
            this.Invoke(new Action(
                delegate ()
                {
                    processVaultItemExport();
                }));

            this.Invoke(new Action(
                delegate ()
                {
                    toolStripStatusLabel1.Text = "Export File Loaded";
                }));
        }
        private void btnLoad_Click(object sender, EventArgs e)
        {

            LoadData();
        }

        private void LoadData()
        {
            productionList = productionList.Load();

            if (productionList != null)
            {
                productionList.currentIndex = 0;

                ProductionListProduct prod = productionList.productList.FirstOrDefault();
                txtboxCurrentRecordName.Text = prod.Number;
                txtBoxOrderHeader.Text = prod.OrderNumber;
                textBoxCurrentProductDesc.Text = prod.ItemDescription;
                txtBoxQtyHeader.Text = prod.Qty.ToString();

                BindingList<ProductionListLineItem> bindingLineItemList = new BindingList<ProductionListLineItem>(prod.SubItems);

                exportTreeList.DataSource = bindingLineItemList;
                exportTreeList.ClearNodes();

                exportTreeList.RefreshDataSource();
                exportTreeList.Refresh();
                exportTreeList.Cursor = Cursors.Default;

                if (productionList.productList.Count > 1)
                    btnNextRecord.Enabled = true;
                else
                    btnNextRecord.Enabled = false;
            }
            else
            {
                productionList = new ProductionListDataSource();

                txtboxCurrentRecordName.Clear();
                txtBoxOrderHeader.Clear();
                textBoxCurrentProductDesc.Clear();
                txtBoxQtyHeader.Clear();

                exportTreeList.ClearNodes();

                exportTreeList.Refresh();
                exportTreeList.Cursor = Cursors.Default;

                btnPreviousRecord.Enabled = false;
                btnNextRecord.Enabled = false;
                MessageBox.Show("Production List is Empty");
            }
        }

        private int getCalculatedQty(ExportLineItem item, List<ExportLineItem> itemList)
        {
            try
            {
                string parentString = "";
                int originalQty = item.Qty;
                int itemQty = item.Qty;
                int totalQty = item.Qty;
                ExportLineItem parentItem = new ExportLineItem();

                parentString = item.Parent;
                if (parentString == "<top>")
                    return totalQty;
                parentItem = itemList.Where(i => i.Number == parentString).FirstOrDefault();

                if (parentItem == null)
                {
                    parentItem = itemList.Where(i => i.Number == parentString).FirstOrDefault();    // first search for exact match
                    if (parentItem == null)     // if not exact match, use contains instead of equals, I forget why this is necessary but I don't think it'll hurt....
                        parentItem = itemList.Where(i => i.Number.Contains(parentString)).FirstOrDefault();

                    if (parentItem == null)        // if no parent found, try searching for item without .iam extension
                    {
                        parentString = Path.GetFileNameWithoutExtension(parentString);
                        parentItem = itemList.Where(i => i.Number == parentString).FirstOrDefault();    // first search for exact match
                        if (parentItem == null)             // if not exact match, use contains instead of equals, I forget why this is necessary but I don't think it'll hurt....
                            parentItem = itemList.Where(i => i.Number.Contains(parentString)).FirstOrDefault();
                    }

                }

                if (item != null)
                {
                    totalQty = totalQty * parentItem.Qty;
                }
                else
                    totalQty = item.Qty;

                return totalQty;
            }
            catch (Exception)
            {
                return 0;
            }

        }

        public List<string> processVaultItemExport(string fileName = "")
        {
            // export from vault triggers this 4 times, we only want one of them.
            //  waiting 10 seconds before products should be lots of time for things to settle down.
            if (DateTime.Now - lastFileUpdateTime > TimeSpan.FromSeconds(10))
            {

                //bool allItemsReleased = true;
                List<string> itemsWithoutDrawings = new List<string>();
                bool topItemReleased = true;
                DateTime earliestDateReleased = DateTime.Now;
                DateTime topLevelItemDateReleased = new DateTime();
                string processOnlyIfReleased = AppSettings.Get("ProcessOnlyIfReleased").ToString();
                string orderNumber = "";

                radioGroup1.SelectedIndex = -1;
                try
                {
                    lineItemList.Clear();
                    exportTreeList.ClearNodes();
                    spinEditOrderQty.Value = 0;
                    Dictionary<string, string> parentDict = new Dictionary<string, string>();

                    // set up a list that will contain all the duplicate items as well
                    List<ExportLineItem> allExportItemsList = new List<ExportLineItem>();



                    if (fileName == "") fileName = vaultExportFileWithPath;
                    using (StreamReader reader = File.OpenText(fileName))
                    {
                        string line;
                        line = reader.ReadLine();       // first line is header, we don't want it.
                        int lineNum = 1;

                        while ((line = reader.ReadLine()) != null)
                        {
                            string level = "";
                            string parentLevel = "";
                            string parent = "";

                            line = line.Replace("\"", "");

                            string[] items = line.Split('\t');

                            string number = items[1];
                            string title = items[2];
                            string itemDesc = items[3];
                            string category = items[4];
                            string thickness = items[5];
                            string material = items[6];
                            string ops = items[7];

                            string qtyString = items[8];
                            int qty = 0, originalQty = 0;
                            string structCode = items[9];
                            string plantID = items[10];
                            string isStockString = items[11];
                            bool isStock = (isStockString == "True" ? true : false);
                            string requiresPdfString = items[12];
                            bool requiresPdfFlag = (requiresPdfString == "False" ? false : true);
                            string comment = items[13];

                            if (lineNum == 1)
                            {
                                orderNumber = comment;    // top level item should have order number in comment field
                                comment = "";
                            }


                            string dateString = items[14];
                            DateTime dateTime = new DateTime();
                            if (dateString != "")
                                dateTime = Convert.ToDateTime(dateString);


                            string lifeCycleState = items[15];

                            //if (lifeCycleState != "Released")
                            //    allItemsReleased = false;

                            string stockNumber = items[16];
                            if (stockNumber != "")
                                number = stockNumber;

                            try
                            {
                                if (qtyString != "") qty = int.Parse(qtyString);
                                originalQty = qty;
                            }
                            catch (Exception)
                            {
                                qty = 0;
                            }

                            level = items[0];
                            parentLevel = "";
                            parent = "";

                            if (level == "1") parent = "<top>";   ///////this is where I left off it seems to work now....

                            if (level.Contains('.'))
                            {
                                parentLevel = level.Remove(level.LastIndexOf('.'));

                                if (!parentDict.ContainsKey(level))
                                {
                                    parentDict.Add(level, number);
                                }

                                if (parentDict.ContainsKey(parentLevel))
                                {
                                    parentDict.TryGetValue(parentLevel, out parent);
                                }
                            }
                            else
                            {
                                parentDict.Add(level, number);
                            }

                            string keywords = items[17];
                            string notes = items[18];

                            if (lineNum == 1)        // top level component
                            {
                                //jobName = items[1];
                                if (keywords != "")
                                    jobName = keywords;
                                else
                                    jobName = number;

                                if (lifeCycleState != "Released")
                                    topItemReleased = false;
                                else
                                    topItemReleased = true;

                                topLevelItemDateReleased = dateTime;
                                txtboxCurrentRecordName.Text = items[1];
                                textBoxCurrentProductDesc.Text = items[3];

                                btnConfirm.Enabled = true;
                            }

                            // some exported files seem to have extension, others don't.
                            if (number.EndsWith(".ipt") || number.EndsWith(".iam"))
                            {
                                number = Path.GetFileNameWithoutExtension(number);
                            }

                            bool pdfExists = false;
                            if (System.IO.File.Exists(pdfPath + number + ".pdf"))
                                pdfExists = true;

                            if (dateTime < earliestDateReleased)
                                earliestDateReleased = dateTime;

                            // set sheet metal properties
                            if (ops == "Laser")
                            {
                                if (thickness == "")
                                {
                                    if (structCode != "")
                                    {
                                        switch (structCode)
                                        {
                                            case "SH-062          16GA SHEET METAL":
                                                thickness = "0.062 in";
                                                break;
                                            case "SH-075          14GA SHEET METAL":
                                                thickness = "0.075 in";
                                                break;
                                            case "SH-125          1/8” SHEET METAL":
                                                thickness = "0.120 in";
                                                break;
                                            case "SH-188          3/16” SHEET METAL":
                                                thickness = "0.188 in";
                                                break;
                                            case "SH-250          ¼” SHEET METAL":
                                                thickness = "0.250 in";
                                                break;
                                            case "SH-312           5/16” SHEET METAL":
                                                thickness = "0.312 in";
                                                break;
                                            case "SH-375           3/8” SHEET METAL":
                                                thickness = "0.375 in";
                                                break;
                                            case "SH-500           ½” SHEET METAL":
                                                thickness = "0.500 in";
                                                break;
                                            case "SH-625           5/8” SHEET METAL":
                                                thickness = "0.625 in";
                                                break;
                                            case "SH-750          ¾” SHEET METAL":
                                                thickness = "0.750 in";
                                                break;
                                            case "SH-875            7/8” SHEET METAL":
                                                thickness = "0.875 in";
                                                break;
                                            case "SH-1000          1” SHEET METAL":
                                                thickness = "1.000 in";
                                                break;
                                            case "SH-1250           1 ¼” SHEET METAL":
                                                thickness = "1.250 in";
                                                break;
                                            case "SH-1500           1 ½” SHEET METAL":
                                                thickness = "1.500 in";
                                                break;
                                            default:
                                                thickness = "";
                                                break;
                                        }
                                    }
                                }
                            }

                            ExportLineItem newItem = new ExportLineItem(parent, number, title, itemDesc, category, thickness, material, structCode, ops, dateTime, qty, plantID, isStock, pdfExists, requiresPdfFlag, comment, lifeCycleState, keywords, notes);

                            newItem.Qty = getCalculatedQty(newItem, lineItemList);

                            // known limitation of this quantity calculating code.  There may not be more than one component with the same name on the same level
                            // for example, if there's multiple parts on the same level identical specs, and the name gets overridden with the 'stock name' field,
                            //  this can easily happen.  A possible workaround may be to do the calculating with the acutal part name, but then use the 'stock name' only 
                            //  for  inserting the item....


                            // create a list of items without drawings
                            if (pdfExists == false && requiresPdfFlag == true && category != "Purchased")
                            {
                                if (!itemsWithoutDrawings.Contains(number))
                                {
                                    itemsWithoutDrawings.Add(number);
                                }
                            }


                            if (newItem.Category != "Purchased")
                            {
                                // see if we already have a line item with this part number
                                // we found a matching part number, does it have the same parent?

                                // do we already have a matching item/parent combination in the list?
                                int index = allExportItemsList.FindIndex(i => i.Number == number && i.Parent == parent);

                                if (index < 0)  // we don't want to add it again if we already did
                                {
                                    int index2 = lineItemList.FindIndex(item => item.Number == number);
                                    if (index2 >= 0)    // item already exists, just increment qty
                                    {
                                        allExportItemsList.Add(newItem);
                                        lineItemList[index2].Qty += newItem.Qty;
                                        lineNum++;
                                    }
                                    else
                                    {
                                        allExportItemsList.Add(newItem);
                                        lineItemList.Add(newItem);
                                        lineNum++;
                                    }
                                }
                                else
                                {
                                    // we already added an item with this parent/child relationship.  If we add it again, we'll get too many
                                    //  because we already added it when there was only one of the parent in the list, so we have to do some math.
                                    allExportItemsList.Add(newItem);
                                    // how many instances of this parent/child relationship do we have?
                                    List<ExportLineItem> searchList = allExportItemsList.Where(item => item.Number == number && item.Parent == parent).ToList();
                                    int relationCount = searchList.Count;


                                    int index2 = lineItemList.FindIndex(item => item.Number == number);
                                    if (index2 >= 0)    // item already exists, just increment qty
                                    {
                                        lineItemList[index2].Qty += (newItem.Qty / relationCount);
                                        lineNum++;
                                    }

                                }
                            }
                        }

                        reader.Close();



                        // clear the pdf view window
                        pdfViewer1.CloseDocument();
                    }

                    plProd = new ProductionListProduct();
                    plProd.Qty = (int)(spinEditOrderQty.Value);
                    plProd.Number = lineItemList[0].Number;
                    plProd.ItemDescription = lineItemList[0].ItemDescription;
                    plProd.Category = lineItemList[0].Category;
                    plProd.PlantID = lineItemList[0].PlantID;
                    plProd.IsStock = lineItemList[0].IsStock;
                    plProd.Keywords = lineItemList[0].Keywords;
                    plProd.Notes = lineItemList[0].Notes;

                    int idx = 0;
                    foreach (ExportLineItem lItem in lineItemList)
                    {
                        if (idx != 0)     // don't add parent item to list, this is the product.
                        {
                            ProductionListLineItem lineItem = new ProductionListLineItem();
                            lineItem.Number = lItem.Number;
                            lineItem.Title = lItem.Title;
                            lineItem.ItemDescription = lItem.ItemDescription;
                            lineItem.Category = lItem.Category;
                            lineItem.Material = lItem.Material;
                            lineItem.MaterialThickness = lItem.MaterialThickness;
                            lineItem.StructCode = lItem.StructCode;
                            lineItem.Operations = lItem.Operations;
                            lineItem.HasPdf = lItem.HasPdf;
                            lineItem.PlantID = lItem.PlantID;
                            lineItem.IsStock = lItem.IsStock;
                            lineItem.Keywords = lItem.Keywords;
                            lineItem.Notes = lItem.Notes;
                            lineItem.Qty = lItem.Qty;


                            plProd.SubItems.Add(lineItem);
                        }
                        idx++;
                    }

                    //if (textBoxOutputFolder.Text.Contains("Batch") || textBoxOutputFolder.Text.Contains("batch"))
                    //    lineItemList.Sort(ExportLineItem.CompareToBatchItems);
                    //else
                    //    lineItemList.Sort();    // default sorting

                    //BindingList<ExportLineItem> bindingLineItemList = new BindingList<ExportLineItem>(lineItemList);

                    //exportTreeList.DataSource = bindingLineItemList;
                    ////exportTreeList.DataSource = lineItemList;

                    //exportTreeList.RefreshDataSource();
                    //exportTreeList.Refresh();
                    //exportTreeList.Cursor = Cursors.Default;

                    BindingList<ProductionListLineItem> bindingLineItemList = new BindingList<ProductionListLineItem>(plProd.SubItems);

                    exportTreeList.DataSource = bindingLineItemList;
                    exportTreeList.ClearNodes();

                    exportTreeList.RefreshDataSource();
                    exportTreeList.Refresh();
                    exportTreeList.Cursor = Cursors.Default;
                    
                    txtBoxQtyHeader.Clear();
                    txtboxCurrentRecordName.Clear();
                    textBoxCurrentProductDesc.Clear();
                    txtBoxOrderHeader.Clear();
                    btnPreviousRecord.Enabled = false;
                    btnNextRecord.Enabled = false;

                    if (processOnlyIfReleased == "true" || processOnlyIfReleased == "True")
                    {
                        TimeSpan tSpan = new TimeSpan(0, 10, 0);

                        // make sure all items are released before we process the order
                        // and make sure all items were released within the past day before we process the order

                        //if (DateTime.Now - earliestDateReleased < tSpan && allItemsReleased == true)
                        //{
                        //    btnProcess.Enabled = true;
                        //}


                        // make sure top level item is released before we process the order
                        // and make sure all items were released within the past day before we process the order
                        if (DateTime.Now - topLevelItemDateReleased < tSpan && topItemReleased == true)
                        {
                            btnConfirm.Enabled = true;
                        }
                        else
                        {
                            //if (topItemReleased == false)
                            //    MessageBox.Show("Top level item has not been released.  You will not be able to process this order.");
                            if (DateTime.Now - earliestDateReleased < tSpan)
                                MessageBox.Show("Item has not been updated in the last 10 minutes. You will not be able to process this order.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            btnConfirm.Enabled = false;
                        }
                    }
                    else
                        btnConfirm.Enabled = true;

                    if (orderNumber != "")
                        txtBoxOrderNumber.Text = orderNumber;
                    else
                        txtBoxOrderNumber.Text = "";

                    // check external file for final ok to process check
                    string processFileName = AppSettings.Get("VaultExportFilePath").ToString() + "Process.txt";
                    string line2;
                    using (StreamReader reader = new StreamReader(processFileName))
                    {
                        line2 = reader.ReadLine();
                    }
                    if (line2 == "false")
                        btnConfirm.Enabled = false;
                    lastFileUpdateTime = DateTime.Now;

                    return itemsWithoutDrawings;
                }
                catch (Exception ex)
                {

                    return new List<string>();
                }

            }
            else
            {
                return new List<string>();
            }
        }

        private void exportTreeList_CompareNodeValues(object sender, DevExpress.XtraTreeList.CompareNodeValuesEventArgs e)
        {
            // all sorting is done now by ExportLineItem.CompareTo
            // this sorts the underlying data source, so we don't need to try to sort the view.

            //this sorts the list in the treeList View
            //if(e.Column.Caption == "Stock")
            //{
            //    if ((e.NodeValue1.ToString() == "False") && (e.NodeValue2.ToString() == "True"))
            //        e.Result = -1;
            //    else if ((e.NodeValue1.ToString() == "True") && (e.NodeValue2.ToString() == "False"))
            //        e.Result = 1;
            //}

            //if (e.Column.Caption != "Category") return;
            //try
            //{
            //    if (e.NodeValue1 == e.NodeValue2)
            //        e.Result = 0;
            //    else if ((e.NodeValue1.ToString() == "Product") && (e.NodeValue2.ToString() == "Part"))
            //        e.Result = -1;
            //    else if ((e.NodeValue1.ToString() == "Product") && (e.NodeValue2.ToString() == "Assembly"))
            //        e.Result = -1;
            //    else if ((e.NodeValue1.ToString() == "Assembly") && (e.NodeValue2.ToString() == "Part"))
            //        e.Result = -1;
            //    else if ((e.NodeValue1.ToString() == "Assembly") && (e.NodeValue2.ToString() == "Product"))
            //        e.Result = 1;
            //    else if ((e.NodeValue1.ToString() == "Part") && (e.NodeValue2.ToString() == "Assembly"))
            //        e.Result = 1;
            //    else if ((e.NodeValue1.ToString() == "Part") && (e.NodeValue2.ToString() == "Product"))
            //        e.Result = 1;
            //}
            //catch { e.Result = 0; }
        }

        private bool processOrder()
        {
            try
            {
                if (!dailyScheduleData.IsFinalized())
                {
                    bool matchingOrderFound = false;

                    // are we processing an order or a batch?
                    bool isBatch = false;
                    string batchItemName = "";
                    if (txtBoxOrderNumber.Text.IndexOf("batch", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        isBatch = true;
                        if (lineItemList != null)
                            batchItemName = lineItemList[0].Number;
                    }

                   

                    foreach (AggregateLineItem item in dailyScheduleData.AggregateLineItemList)
                    {
                        foreach (var o in item.AssociatedOrders)
                        {
                            if (o.OrderNumber == txtBoxOrderNumber.Text) matchingOrderFound = true;
                        }
                    }

                    if (matchingOrderFound == false || isBatch == true) // batches can have matching order numbers
                    {

                        // search for parts that get manufactured at different location than their parent assembly.
                        if (isBatch == false)
                        {
                            int itemsFound = 0;
                            foreach (ExportLineItem item in lineItemList)
                            {

                                ExportLineItem parentItem = lineItemList.Find(x => x.Number == item.Parent);
                                if (parentItem!=null)
                                {
                                    if (item.PlantID != "" && parentItem.PlantID != "" && item.Category == "Part")
                                    {
                                        string localPlant = AppSettings.Get("LocalPlantName").ToString();
                                        string remotePlant = AppSettings.Get("RemotePlantName").ToString();

                                        if (parentItem.PlantID == localPlant && item.PlantID == remotePlant)
                                        {
                                            itemsFound++;
                                        }
                                    }
                                }
                            }
                            if (itemsFound > 0)
                            {
                                DialogResult continueResult = MessageBox.Show("Warning! Found " + itemsFound + " item(s) that gets manufactured at a different location than its parent.  Continue, yes or no?", "Continue?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                                if (continueResult == DialogResult.No)
                                {
                                    return false;
                                }
                            }
                            itemsFound = 0;
                        }



                        if (spinEditOrderQty.Value != 0)
                        {
                            if (txtBoxOrderNumber.Text != "" && txtBoxOrderNumber.Text != "Order Number")
                            {
                                toolStripStatusLabel1.Text = "Saving PDFs to output folder...";

                                int orderQty = (int)spinEditOrderQty.Value;

                                // check for missing material values on laser cut parts
                                bool missingValFound = false;
                                bool OkToContinue = true;
                                int missingOperationFound = 0;

                                if (!isBatch)
                                    lineItemList.Sort();
                                else
                                    lineItemList.Sort(ExportLineItem.CompareToBatchItems);

                                foreach (ExportLineItem item in lineItemList)
                                {
                                    if (item.Operations == "Laser" && (item.MaterialThickness == "" || item.Material == ""))
                                    {
                                        missingValFound = true;
                                    }

                                    if (item.Category == "Part" && item.Operations == "")
                                    {
                                        missingOperationFound++;
                                    }
                                }

                                if (missingValFound == true)
                                {
                                    DialogResult result = MessageBox.Show("There are one or more files that are specified as laser cut files, but have missing material specs.  Do you want to continue?", "Continue?", MessageBoxButtons.YesNo);
                                    if (result == DialogResult.Yes) OkToContinue = true;
                                    else OkToContinue = false;
                                }

                                if (missingOperationFound >= 1)
                                {
                                    DialogResult result = MessageBox.Show("There are " + missingOperationFound + " part file(s) that have no operation assigned. Do you want to continue?", "Continue?", MessageBoxButtons.YesNo);
                                    if (result == DialogResult.Yes) OkToContinue = true;
                                    else OkToContinue = false;
                                }

                                string currentProduct = "";

                                if (OkToContinue)
                                {


                                    SplashScreenManager.ShowForm(this, typeof(WaitForm1), true, true, false);

                                    if (!isBatch)
                                        lineItemList.Sort(); // first sort by category using custom sorting
                                    else
                                        lineItemList.Sort(ExportLineItem.CompareToBatchItems);

                                    // print each line item to it's corresponding folder.
                                    foreach (ExportLineItem item in lineItemList)
                                    {
                                        if ((item.Category == "Product" || item.Category == "Assembly") && item.Parent == "<top>")
                                            currentProduct = item.Number;

                                        dailyScheduleData.AddLineItem(item, txtBoxOrderNumber.Text, orderQty, batchItemName, currentProduct);
                                        dailyScheduleData.SaveToFile();
                                    }

                                    if (dailyScheduleData.HasNextItem(lineItemList[0].Number)) btnNextRecord.Enabled = true;
                                    else btnNextRecord.Enabled = false;

                                    if (dailyScheduleData.HasPreviousItem(lineItemList[0].Number)) btnPreviousRecord.Enabled = true;
                                    else btnPreviousRecord.Enabled = false;

                                    // also create pdf of all drawings associated with this order.
                                    //  we'll save it in the root folder.

                                    string outputPdfPath = "";
                                    outputPdfPath = exportFilePath + "\\" + txtBoxOrderNumber.Text + "-" + jobName + ".pdf";
                                    List<string> fileNamesToCopy = new List<string>();
                                    List<string> watermarksToCopy = new List<string>();

                                    currentProduct = "";



                                    foreach (ExportLineItem item in lineItemList)
                                    {
                                        if ((item.Category == "Product" || item.Category == "Assembly") && item.Parent == "<top>")
                                            currentProduct = item.Number;

                                        string watermark = "";
                                        if (isBatch)
                                            watermark = "Batch Name: " + txtBoxOrderNumber.Text + "\n";

                                        if (!isBatch)       // if we are processing a batch item, no need to sort out stock and make to order
                                        {
                                            if (item.IsStock == true)
                                                watermark += "\nStock Item\n";
                                            else
                                                watermark += "\nMake To Order Item\n";
                                        }

                                        string itemNumber = "";
                                        //if (item.Keywords != "")
                                        //    itemNumber = item.Keywords;
                                        //else
                                        itemNumber = item.Number;

                                        watermark += "Product Number: " + currentProduct + "\n" +
                                        watermark + "Item Number: " + itemNumber + "     Desc: " + item.ItemDescription + "\n";
                                        watermark += "Manufacture Location: " + item.PlantID + "\n";

                                        if (item.Category == "Part")
                                        {
                                            watermark += "Material: " + item.StructCode + "\n";
                                            watermark += "Operation: " + item.Operations + "\n";
                                        }

                                        if (item.Notes != "")
                                        {
                                            watermark += "Notes: " + item.Notes + "\n";
                                        }

                                        orderQty = (int)spinEditOrderQty.Value;
                                        int unitQty = item.Qty;
                                        int totalQty = orderQty * unitQty;
                                        watermark += "Quantity: " + totalQty + "\n";



                                        fileNamesToCopy.Add(item.Number + ".pdf");
                                        watermarksToCopy.Add(watermark);
                                    }
                                    string outputPath = Path.GetDirectoryName(outputPdfPath) + "\\";
                                    ProcessPDF.CopyPDF(pdfPath, fileNamesToCopy, watermarksToCopy, outputPath, txtBoxOrderNumber.Text, jobName);

                                    toolStripStatusLabel1.Text = "Saving PDFs to output folder...Done";
                                    SplashScreenManager.CloseForm(false);
                                    return true;
                                }
                                else
                                {
                                    MessageBox.Show("Please Enter a Valid Order Number");
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show("Please enter a quantity");
                        }
                    }
                    else
                    {
                        MessageBox.Show("This Order Number Already Exists. Please Enter a Different One.");
                        return false;
                    }
                }
                else
                {
                    MessageBox.Show("Cannot add new schedule data.\n Schedule has already been finalized.");
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in Processing PDF Files\n" + ex.Message);
                return false;
            }
        }

        private int updatePDFs(bool isBatch)
        {
            try
            {
                int drawingsModified = 0;
                //string currentProduct = "";

                foreach (AggregateLineItem item in dailyScheduleData.AggregateLineItemList)
                {
                    //if ((item.Category == "Product" || item.Category == "Assembly") && item.Parent == "<top>")
                    //    currentProduct = item.Number;

                    string watermark = "";
                    if (isBatch)
                        watermark = "Batch Name: " + txtBoxOrderNumber.Text + "\n";

                    if (!isBatch)       // if we are processing a batch item, no need to sort out stock and make to order
                    {
                        if (item.IsStock == true)
                            watermark += "\nStock Item\n";
                        else
                            watermark += "\nMake To Order Item\n";
                    }

                    if (item.AssociatedOrders.Count >= 1 && item.Category == "Part")
                    {
                        string outputPdfPath = ProcessPDF.CalculateSubFolder(pdfPath, exportFilePath, item, isBatch);

                        //string watermark = "Item Number: " + item.Number + "\n" +
                        //string watermark = "Product Number: " + currentProduct + "\n" +
                        watermark += "Item Number: " + item.Number + "      Desc: " + item.ItemDescription + "\n";
                        int totalQty = 0;

                        if (item.Category == "Part")
                        {
                            watermark += "Material: " + item.StructCode + "\n";
                            watermark += "Operation: " + item.Operations + "\n";
                        }

                        foreach (OrderData o in item.AssociatedOrders)
                        {
                           
                            int lineTotalQty = o.UnitQty * o.OrderQty;
                            watermark += "Order Number: " + o.OrderNumber + "----- Order Qty: " + o.OrderQty + " x per unit Qty: " + o.UnitQty + "---Line Total Qty: " + lineTotalQty + "\n";
                            totalQty += lineTotalQty;
                        }
                        watermark += "Total Quantity: " + totalQty + "\n";

                        if (item.Notes != "")
                        {
                            watermark += "Notes: " + item.Notes + "\n";
                        }

                        string outputPath = Path.GetDirectoryName(outputPdfPath) + "\\";
                        ProcessPDF.CopyPDF(pdfPath, new List<string> { item.Number + ".pdf" }, new List<string> { watermark }, outputPath);
                    }
                    drawingsModified++;
                }
                return drawingsModified;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private void btnConfirm_Click(object sender, EventArgs e)
        {
            plProd.OrderNumber = txtBoxOrderNumber.Text;
            txtBoxOrderHeader.Text = plProd.OrderNumber;

            plProd.Qty = (int) (spinEditOrderQty.Value);
            txtBoxQtyHeader.Text = plProd.Qty.ToString();

            txtboxCurrentRecordName.Text = plProd.Number;
            textBoxCurrentProductDesc.Text = plProd.ItemDescription;

            foreach (ProductionListLineItem lItem in plProd.SubItems)
            {
                if (lItem.HasPdf)
                {
                    try
                    {
                        string srcPdfName = Path.Combine(pdfPath, lItem.Number + ".pdf");
                        string destPdfName = Path.Combine(exportFilePath, "Pdfs", lItem.Number + ".pdf");

                        Directory.CreateDirectory(Path.Combine(exportFilePath, "Pdfs"));

                        if(!File.Exists(destPdfName))
                            System.IO.File.Copy(srcPdfName, destPdfName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error in saving PDF to local folder");
                    }
                }
            }

            productionList.AddProduct(plProd);
            btnConfirm.Enabled = false;
            btnNextRecord.Enabled = false;

            txtBoxOrderNumber.Text = "";
            spinEditOrderQty.Value = 0;

            if (productionList.productList.Count() > 1)
                btnPreviousRecord.Enabled = true;
            else
                btnPreviousRecord.Enabled = false;

            
        }

        private void btnPreviousRecord_Click(object sender, EventArgs e)
        {
            plProd = productionList.GetPrev();
            txtboxCurrentRecordName.Text = plProd.Number;
            textBoxCurrentProductDesc.Text = plProd.ItemDescription;
            BindingList<ProductionListLineItem> bindingLineItemList = new BindingList<ProductionListLineItem>(plProd.SubItems);

            if (plProd.ID < 1) btnPreviousRecord.Enabled = false;
            else btnPreviousRecord.Enabled = true;
            if (plProd.ID >= productionList.productList.Count()-1) btnNextRecord.Enabled = false;
            else btnNextRecord.Enabled = true;

            txtBoxOrderHeader.Text = plProd.OrderNumber;
            txtBoxQtyHeader.Text = plProd.Qty.ToString();

            exportTreeList.DataSource = bindingLineItemList;

            exportTreeList.RefreshDataSource();
            exportTreeList.Refresh();
            exportTreeList.Cursor = Cursors.Default;
        }

        private void btnNextRecord_Click(object sender, EventArgs e)
        {
            plProd = productionList.GetNext();
            txtboxCurrentRecordName.Text = plProd.Number;
            textBoxCurrentProductDesc.Text = plProd.ItemDescription;
            BindingList<ProductionListLineItem> bindingLineItemList = new BindingList<ProductionListLineItem>(plProd.SubItems);

            if (plProd.ID < 1) btnPreviousRecord.Enabled = false;
            else btnPreviousRecord.Enabled = true;
            if (plProd.ID >= productionList.productList.Count()-1) btnNextRecord.Enabled = false;
            else btnNextRecord.Enabled = true;

            txtBoxOrderHeader.Text = plProd.OrderNumber;
            txtBoxQtyHeader.Text = plProd.Qty.ToString();

            exportTreeList.DataSource = bindingLineItemList;

            exportTreeList.RefreshDataSource();
            exportTreeList.Refresh();
            exportTreeList.Cursor = Cursors.Default;
        }

        private void btnUpdateRecord_Click(object sender, EventArgs e)
        {
            productionList.productList[productionList.currentIndex].OrderNumber = txtBoxOrderHeader.Text;
            productionList.productList[productionList.currentIndex].Qty = int.Parse(txtBoxQtyHeader.Text);
            productionList.productList[productionList.currentIndex].ItemDescription = textBoxCurrentProductDesc.Text;
            productionList.productList[productionList.currentIndex].Number = txtboxCurrentRecordName.Text;

            productionList.SaveToFile();

        }

        private void btnRemoveRecord_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to remove the current record?","Confirm", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                if (productionList.productList.Count() > 1)
                {
                    int i = productionList.currentIndex;

                    productionList.productList.RemoveAt(i); // remove the current object

                    for (i = productionList.currentIndex; i < productionList.productList.Count; i++)
                    {
                        productionList.productList[i].ID--;

                    }

                    if (productionList.currentIndex > 0)    // removing any record except for the first one
                    {
                        plProd = productionList.GetPrev();

                        txtboxCurrentRecordName.Text = plProd.Number;
                        textBoxCurrentProductDesc.Text = plProd.ItemDescription;

                        if (plProd.ID < 1) btnPreviousRecord.Enabled = false;
                        else btnPreviousRecord.Enabled = true;
                        if (plProd.ID >= productionList.productList.Count() - 1) btnNextRecord.Enabled = false;
                        else btnNextRecord.Enabled = true;
                    }
                    else  // removing first record
                    {
                        plProd = productionList.productList[0];

                        txtboxCurrentRecordName.Text = plProd.Number;
                        textBoxCurrentProductDesc.Text = plProd.ItemDescription;

                        //if (plProd.ID < 1) btnPreviousRecord.Enabled = false;
                        //else btnPreviousRecord.Enabled = true;
                        btnPreviousRecord.Enabled = false;

                        if (productionList.productList.Count() > 1) btnNextRecord.Enabled = true;
                        else btnNextRecord.Enabled = false;
                    }

                    BindingList<ProductionListLineItem> bindingLineItemList = new BindingList<ProductionListLineItem>(plProd.SubItems);
                    txtBoxOrderHeader.Text = plProd.OrderNumber;
                    txtBoxQtyHeader.Text = plProd.Qty.ToString();

                    exportTreeList.DataSource = bindingLineItemList;

                    exportTreeList.RefreshDataSource();
                    exportTreeList.Refresh();
                    exportTreeList.Cursor = Cursors.Default;

                    productionList.SaveToFile();
                }
                else
                {
                    MessageBox.Show("Cannot Remove Last Item From Production List.");
                }
            }
        }
        private void btnProcess_Click(object sender, EventArgs e)
        {
            bool thicknessOk = true;
            bool materialOk = true;
            bool structOk = true;
            bool operationsOk = true;
            int numOfPartsToCheck = 0;
            DialogResult result = new DialogResult();

            //exportFilePath = folderBrowserDialogOutputFolderSelect.SelectedPath + "\\";
            //textBoxOutputFolder.Text = exportFilePath;

            exportFilePath = textBoxOutputFolder.Text;

            AppSettings.Set("ExportFilePath", exportFilePath);
            string scheduleFileName = exportFilePath + AppSettings.Get("DailyScheduleData").ToString();

            loadScheduleData(scheduleFileName);

            foreach (ExportLineItem lineItem in lineItemList)
            {
                if (lineItem.Operations == "Laser" && lineItem.MaterialThickness == "")
                {
                    thicknessOk = false;
                }
                if (lineItem.Operations == "Laser" && lineItem.Material == "")
                {
                    materialOk = false;
                }
                if ((lineItem.Operations == "Machine Shop" || lineItem.Operations == "Bandsaw" || lineItem.Operations == "Ironworker") && (lineItem.StructCode == ""))
                {
                    structOk = false;
                }
                if (lineItem.Operations == "")
                {
                    operationsOk = false;
                }
                //if (lineItem.Notes.ToUpper().Contains("CHECK"))
                if (lineItem.Notes != "")
                {
                    numOfPartsToCheck++;
                }
            }

            bool cancelled = false;

            if (thicknessOk == false && cancelled == false)
            {
                result = MessageBox.Show("One or more material thicknesses for laser cut items have not been defined. Did you want to continue?", "Missing Thickness", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    thicknessOk = true;
                }
                else
                {
                    cancelled = true;
                }
            }

            if (materialOk == false && cancelled == false)
            {
                result = MessageBox.Show("One or more material types for laser cut items have not been defined. Did you want to continue?", "Missing Material", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    materialOk = true;
                }
                else
                {
                    cancelled = true;
                }
            }

            if (structOk == false && cancelled == false)
            {
                result = MessageBox.Show("One or more structural material types have not been defined. Did you want to continue?", "Missing Structural Information", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    structOk = true;
                }
                else
                {
                    cancelled = true;
                }
            }

            if (operationsOk == false && cancelled == false)
            {
                result = MessageBox.Show("One or more operations have not been defined. Did you want to continue?", "Missing Operations", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    thicknessOk = true;
                }
                else
                {
                    cancelled = true;
                }
            }

            if(numOfPartsToCheck>0)
            {
                result = MessageBox.Show("There are " + numOfPartsToCheck + "item(s) that raise a flag.  Did you want to continue?", "Items To Check", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.No)
                {
                    cancelled = true;
                }
            }

            if (thicknessOk && materialOk && structOk && operationsOk && cancelled == false)
            {
                if (processOrder())
                {
                    string fileName = textBoxOutputFolder.Text + "batch.csv";
                    string line = spinEditOrderQty.Value.ToString() + "," + lineItemList[0].Number + ",," + txtBoxOrderNumber.Text;
                    using (StreamWriter writer = new StreamWriter(fileName, true))
                    {
                        writer.WriteLine(line);
                    }
                }
            }
        }

        List<string> getFilesRecursive(string rootFolderPath, string searchFileName, List<string> filesFound)
        {
            try
            {
                //List<string> files = new List<string>();
                foreach (string d in Directory.GetDirectories(rootFolderPath))
                {
                    foreach (string f in Directory.GetFiles(d,searchFileName))
                    {
                        filesFound.Add(f);
                    }
                    getFilesRecursive(d, searchFileName, filesFound);
                }
                return filesFound;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        private void btnRemoveOrder_Click(object sender, EventArgs e)
        {
            removeItem(false);
        }

        private void btnRemoveBatchItem_Click(object sender, EventArgs e)
        {
            removeItem(true);
        }

        private void removeItem(bool isBatch)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to remove all drawings associated with " + txtBoxOrderNumber.Text + "?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
            if (result == DialogResult.Yes)
            {
                SplashScreenManager.ShowForm(this, typeof(WaitForm1), true, true, false);

                try
                {
                    int drawingsDeleted = 0;
                    int drawingsModified = 0;
                    bool warningShown = false;

                    if (!dailyScheduleData.IsFinalized())
                    {
                        bool matchingOrderFound = false;

                        foreach (AggregateLineItem item in dailyScheduleData.AggregateLineItemList)
                        {
                            
                            foreach (var o in item.AssociatedOrders)
                            {
                                if (o.OrderNumber == txtBoxOrderNumber.Text)
                                {
                                    matchingOrderFound = true;
                                    string otherMatchingOrders = "";
                                    if (!warningShown) {
                                        if (!item.IsStock && (item.Operations == "Bandsaw" || item.Operations == "Iron Worker"))
                                        {
                                            foreach (var o2 in item.AssociatedOrders)
                                            {
                                                if (o2.OrderNumber != txtBoxOrderNumber.Text)
                                                {
                                                    otherMatchingOrders += o2.OrderNumber.ToString() + "\n";
                                                }
                                            }

                                            if (otherMatchingOrders != "")
                                            {
                                                MessageBox.Show("The following orders will also need to be removed because they contain parts linked to this order\n" + otherMatchingOrders);
                                                warningShown = true;
                                            }
                                        }
                                    }
                                }
                                
                            }
                        }

                        if (matchingOrderFound != false)
                        {
                            if (txtBoxOrderNumber.Text != "" && txtBoxOrderNumber.Text != "Order Number")
                            {
                                string rootFolderPath = textBoxOutputFolder.Text;
                                // first delete the order pdf in the root folder
                                try
                                {
                                    string[] files;
                                    if(!isBatch)
                                        files = System.IO.Directory.GetFiles(rootFolderPath, "*" + txtBoxOrderNumber.Text + "-*", System.IO.SearchOption.TopDirectoryOnly);
                                    else
                                        files = System.IO.Directory.GetFiles(rootFolderPath, "*" + txtBoxOrderNumber.Text + "*", System.IO.SearchOption.TopDirectoryOnly);

                                    if (files.Length == 1)
                                    {
                                        System.IO.File.Delete(files[0]);
                                        drawingsDeleted++;
                                    }
                                    else if (files.Length > 1)
                                    {
                                        DialogResult multipleCheckResult = MessageBox.Show("Warning: " + files.Length + " matches found. Ok to delete?", "Multiple matches found", MessageBoxButtons.YesNo);
                                        if (multipleCheckResult == DialogResult.Yes)
                                        {
                                            foreach (string f in files)
                                            {
                                                System.IO.File.Delete(f);
                                            }
                                        }
                                        else
                                            return;
                                    }
                                    else if (files.Length < 1)
                                    {
                                        MessageBox.Show("Cannot delete main order file, file not found,\nAborting Operation\n");
                                        SplashScreenManager.CloseForm();
                                        return;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show("Cannot delete main order file\n" + ex.Message);
                                    SplashScreenManager.CloseForm();
                                    return;
                                }

                                foreach (AggregateLineItem item in dailyScheduleData.AggregateLineItemList)
                                {
                                    List<OrderData> searchOrders = item.AssociatedOrders.Where(o => o.OrderNumber == txtBoxOrderNumber.Text).ToList();
                                    int orderCount = item.AssociatedOrders.Count();
                                    if (searchOrders.Count != 0)
                                    {
                                        foreach (OrderData searchOrder in searchOrders)
                                        {
                                            item.AssociatedOrders.Remove(searchOrder);
                                            //if (orderCount <= 1)
                                            //{
                                            // then search for part specific files
                                            string filesToDelete = item.Number + ".pdf";
                                            List<string> fileList = new List<string>();
                                            fileList = getFilesRecursive(rootFolderPath, filesToDelete, fileList);
                                            if (fileList.Count() > 0)
                                            {
                                                foreach (string file in fileList)
                                                {
                                                    bool successfulDelete = false;
                                                    do
                                                    {
                                                        try
                                                        {
                                                            // delete the file
                                                            System.IO.File.Delete(file);
                                                            successfulDelete = true;
                                                            drawingsDeleted++;
                                                        }
                                                        catch (Exception)
                                                        {
                                                            result = MessageBox.Show("Problem in deleting " + file + "\n" +
                                                                             "You may not have permission to delete the file or the file may be open in Windows Explorer.  Please close it and click Retry to try again.", "Continue?", MessageBoxButtons.RetryCancel, MessageBoxIcon.Question);
                                                            if (result == DialogResult.Cancel)
                                                                successfulDelete = true;
                                                        }
                                                    } while (!successfulDelete);
                                                }
                                            }
                                        }
                                    }
                                }

                                if (isBatch == false)
                                    // remove items with no associated orders
                                    foreach (AggregateLineItem item in dailyScheduleData.AggregateLineItemList.ToList())
                                    {
                                        if (item.AssociatedOrders.Count == 0)
                                        {
                                            dailyScheduleData.AggregateLineItemList.Remove(item);
                                        }
                                    }
                                else
                                {
                                    foreach (AggregateLineItem item in dailyScheduleData.AggregateLineItemList.ToList())
                                    {
                                        if (item.AssociatedOrders.Count == 0)
                                        {
                                            dailyScheduleData.AggregateLineItemList.Remove(item);
                                        }
                                    }
                                }

                                // refresh all the pdfs
                                dailyScheduleData.SaveToFile();
                                drawingsModified = updatePDFs(isBatch);

                                SplashScreenManager.CloseForm();
                                MessageBox.Show("Number of PDFs Removed: " + drawingsDeleted + "\nNumber of PDFs modified: " + drawingsModified);
                            }
                        }
                        else
                        {
                            MessageBox.Show("No matching order found for " + txtBoxOrderNumber.Text);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Cannot remove orders after project has been finalized.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error in Removing Order\n" + ex.Message);
                }

                SplashScreenManager.CloseForm(false);
            }
        }

        private void exportTreeList_PopupMenuShowing(object sender, DevExpress.XtraTreeList.PopupMenuShowingEventArgs e)
        {
            if (e.Menu is TreeListNodeMenu)
            {
                exportTreeList.FocusedNode = ((TreeListNodeMenu)e.Menu).Node;
                e.Menu.Items.Add(new DevExpress.Utils.Menu.DXMenuItem("Get Pdf", menuGetPdf_ItemClick));

                //var v = exportTreeList.GetDataRecordByNode(exportTreeList.FocusedNode);
                //exportTreeList.FocusedNode.Tag = (ExportLineItem)v;
            }
        }

        private void menuGetPdf_ItemClick(object sender, EventArgs e)
        {

            ExportLineItem selectedItem = (ExportLineItem) exportTreeList.GetDataRecordByNode(exportTreeList.FocusedNode);

            string fileName = selectedItem.Number;


            // attempt to fix bug where multiple instances of assembly drawing are printed
            // the problem shows up when there is an existing pdf of an assembly and we request to print it again.  The newly
            // printed pdfs will be added onto the existing file, rather than the exsiting file being overwritten like it should be.

            //string pdfName = pdfPath +  fileName+ ".pdf";
            //if (System.IO.File.Exists(pdfName))
            //{
            //    FileInfo file = new FileInfo(pdfName);
            //    if (IsFileLocked(file))
            //    {
            //        MessageBox.Show("Cannot print pdf if file is selected.");
            //        return;
            //    }
            //    else
            //    {
            //        System.IO.File.Delete(pdfName);
            //    }
            //}





            if (selectedItem.Category == "Assembly")
                fileName += ".iam";
            if(selectedItem.Category == "Product")
                fileName += ".iam";
            if (selectedItem.Category == "Part")
                fileName += ".ipt";

            if (hlaVault == null)
            {
                SplashScreenManager.ShowForm(this, typeof(WaitFormVaultLogin), true, true, false);
                toolStripStatusLabel1.Text = "Logging into Vault...";
                statusStrip1.Refresh();
                hlaVault = new VaultAccess.VaultAccess(pdfPath, AppSettings.Get("PrintPDFPrinterMicrosoft").ToString());
                hlaVault.Login(vaultUserName, vaultPassword, vaultServer, vaultVault);
                toolStripStatusLabel1.Text = "Logging into Vault...Done";
                SplashScreenManager.CloseForm(false);
            }

            if(hlaVault!=null && !hlaVault.IsConnectionActive())
            {
                SplashScreenManager.ShowForm(this, typeof(WaitFormVaultLogin), true, true, false);
                toolStripStatusLabel1.Text = "Logging into Vault...";
                statusStrip1.Refresh();
                hlaVault = new VaultAccess.VaultAccess(pdfPath, AppSettings.Get("PrintPDFPrinterMicrosoft").ToString());
                hlaVault.Login(vaultUserName, vaultPassword, vaultServer, vaultVault);
                toolStripStatusLabel1.Text = "Logging into Vault...Done";
                SplashScreenManager.CloseForm(false);
            }

            if (hlaVault.IsConnectionActive())
            {
                SplashScreenManager.ShowForm(this, typeof(WaitForm3), true, true, false);
                Dictionary<long, string> idwList = hlaVault.GetIDWsAssociatedWithModelByVaultName(fileName);

                SplashScreenManager.CloseForm(false);
                if (idwList != null)
                {
                    PrintIDW printDialog = new PrintIDW(idwList);

                    DialogResult result = printDialog.ShowDialog();

                    if (result == DialogResult.OK)
                    {
                        SplashScreenManager.ShowForm(this, typeof(WaitForm1), true, true, false);
                        long vaultIDToPrint = printDialog.SelectedID;
                        if(!hlaVault.PrintAssociatedIDWToPDF(vaultIDToPrint.ToString()))
                        {
                            MessageBox.Show("Problem in printing pdfs. File may be open in windows explorer.");
                        }
                        SplashScreenManager.CloseForm(false);
                    }
                }
                else
                {
                    MessageBox.Show("No Associated Idw File Found");
                }
            }
            else
            {
                MessageBox.Show("Vault Connection is not active.");
            }

           
        }

        protected virtual bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }

        private void exportTreeList_MouseDown(object sender, MouseEventArgs e)
        {
            string selectedItem = "";

            DevExpress.XtraTreeList.TreeListHitInfo hi = exportTreeList.CalcHitInfo(e.Location);
            if (hi.HitInfoType == DevExpress.XtraTreeList.HitInfoType.Cell)
                selectedItem = (hi.Node[Number].ToString());

            if (selectedItem.EndsWith(".ipt") || selectedItem.EndsWith(".iam"))
            {
                selectedItem = Path.GetFileNameWithoutExtension(selectedItem);
            }

            if (System.IO.File.Exists(pdfPath + selectedItem + ".pdf"))
            {
                pdfViewer1.LoadDocument(pdfPath + selectedItem + ".pdf");
            }
            else
                pdfViewer1.CloseDocument();
        }

        private void btnSelectOutputFolder_Click(object sender, EventArgs e)
        {
            // Show the dialog and get result.
            folderBrowserDialogOutputFolderSelect.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            folderBrowserDialogOutputFolderSelect.SelectedPath = exportFilePath;
            DialogResult result = folderBrowserDialogOutputFolderSelect.ShowDialog();
            if (result == DialogResult.OK) // Test result.
            {
                exportFilePath = folderBrowserDialogOutputFolderSelect.SelectedPath + "\\";
                textBoxOutputFolder.Text = exportFilePath;
                AppSettings.Set("ExportFilePath", exportFilePath);
            }
           
            string scheduleFileName = exportFilePath + AppSettings.Get("DailyScheduleData").ToString();

            loadScheduleData(scheduleFileName);

            productionList = new ProductionListDataSource();
            LoadData();
        }

        private void loadScheduleData(string scheduleFileName)
        {
            if (!File.Exists(scheduleFileName))
            {
                dailyScheduleData = new DailyScheduleAggregate(scheduleFileName, pdfPath);
            }
            else
            {
                XmlSerializer xs = new XmlSerializer(typeof(DailyScheduleAggregate));
                using (var sr = new StreamReader(exportFilePath + "AggregateData.xml"))
                {
                    dailyScheduleData = (DailyScheduleAggregate)xs.Deserialize(sr);
                }
            }

            toolStripStatusLabel1.Text = "Ouput Folder Selected.";

            if (dailyScheduleData.IsFinalized()) btnConfirm.Enabled = false;
            else btnConfirm.Enabled = true;

            //if (textBoxOutputFolder.Text.Contains("Batch") || (textBoxOutputFolder.Text.Contains("batch")))
            //{
            //    btnRemoveBatchItem.Enabled = true;
            //    btnRemoveOrder.Enabled = false;
            //}
            //else
            //{
            //    btnRemoveBatchItem.Enabled = false;
            //    btnRemoveOrder.Enabled = true;

            //}
        }

        private void btnFinalize_Click(object sender, EventArgs e)
        {
            System.IO.DirectoryInfo rootDir = new DirectoryInfo(textBoxOutputFolder.Text);

            if (dailyScheduleData.IsFinalized())
            {
                MessageBox.Show("Schedule is already finalized.");
            }
            else
            {
                DialogResult r = MessageBox.Show("This command will combine all drawings in each folder together into one combined drawing set." +
                                                 "  You will not be able to add any more orders to this project.  Are you sure you want to continue?  ", "Continue?", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if (r == DialogResult.Yes)
                {
                    SplashScreenManager.ShowForm(this, typeof(WaitForm2), true, true, false);
                    CombinePDFs(rootDir);
                    //GroupBandSawDrawings(rootDir);
                    dailyScheduleData.FinalizeData();
                    btnConfirm.Enabled = false;
                    SplashScreenManager.CloseForm(false);
                }
            }
        }

        void CombinePDFs(System.IO.DirectoryInfo root)
        {
            PdfDocument inputDocument = new PdfDocument();
            PdfDocument outputDocument = new PdfDocument();

            XUnit height = new XUnit();
            XUnit width = new XUnit();
            List<XUnit[]> xUnitArrayList = new List<XUnit[]>();

            System.IO.FileInfo[] files = null;
            System.IO.DirectoryInfo[] subDirs = null;

            // First, process all the files directly under this folder
            try
            {
                if (root.Name.Contains("Bandsaw Drawings"))
                {
                    // sort bandsaw drawings by file name, they all start with a number
                    files = root.GetFiles("*.pdf");
                    var orderedFiles = files.OrderBy(n => Regex.Replace(n.Name, @"\d+", f => f.Value.PadLeft(5, '0')));
                    files = orderedFiles.ToArray();

                }
                else
                {
                    // otherwise sort by creation time
                    files = root.GetFiles("*.pdf");
                    var orderedFiles = files.OrderBy(f => f.CreationTime);
                    files = orderedFiles.ToArray();
                }

            }
            // This is thrown if even one of the files requires permissions greater
            // than the application provides.
            catch (UnauthorizedAccessException e)
            {
                // This code just writes out the message and continues to recurse.
                // You may decide to do something different here. For example, you
                // can try to elevate your privileges and access the file again.
                MessageBox.Show(e.Message);
            }

            catch (System.IO.DirectoryNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }

            if (files != null && !textBoxOutputFolder.Text.Contains(root.Name))
            {
                //string fNames = "";
                foreach (System.IO.FileInfo fi in files)
                {
                    if (fi.Name != "_Combined Drawing Set.pdf")       // if we already ran finalizer before, we don't want to duplicate all the files...
                        inputDocument = PdfReader.Open(fi.FullName, PdfDocumentOpenMode.Import);

                    int count = inputDocument.PageCount;
                    for (int idx = 0; idx < count; idx++)
                    {
                        PdfPage page = inputDocument.Pages[idx];
                        // store page width and height in array list so we can reference again when we are producing output
                        height = page.Height;
                        width = page.Width;
                        XUnit[] pageDims = new XUnit[] { page.Height, page.Width };
                        xUnitArrayList.Add(pageDims);

                        outputDocument.AddPage(page);
                    }
                }

                string outputFile = root.FullName + @"\_Combined Drawing Set.pdf";
                outputDocument.Info.Title = "Combined PDF Created by Vault Item Processor";
                if (outputDocument.PageCount > 0)
                {
                    try
                    {
                        outputDocument.Save(outputFile);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }

            }

            // Now find all the subdirectories under this directory.
            subDirs = root.GetDirectories();

            foreach (System.IO.DirectoryInfo dirInfo in subDirs)
            {
                // Resursive call for each subdirectory.
                CombinePDFs(dirInfo);
            }

        }

        private bool GroupBandSawDrawings(System.IO.DirectoryInfo rootFolder)
        {
            try
            {
                SplashScreenManager.ShowForm(this, typeof(WaitForm1), true, true, false);
                {
                    bool isBatch;
                    if (textBoxOutputFolder.Text.Contains("Batch") || (textBoxOutputFolder.Text.Contains("batch")))
                        isBatch = true;
                    else
                        isBatch = false;

                    int index = 0;

                    string currentProduct = "";
                    foreach (AggregateLineItem item in dailyScheduleData.AggregateLineItemList)
                    {
                        if ((item.Category == "Product" || item.Category == "Assembly") && item.Parent == "<top>")
                            currentProduct = item.Number;
                        if (item.AssociatedOrders.Count >= 1 && item.IsStock == false && (item.Operations == "Bandsaw" || item.Operations == "Iron Worker" || item.Category == "Assembly" || item.Category == "Product"))
                        {
                            string inputPdfPath;
                            
                            inputPdfPath = ProcessPDF.CalculateSubFolder(pdfPath, exportFilePath, item, isBatch);

                            if (item.Category == "Assembly")
                            {
                                inputPdfPath = AppSettings.Get("PdfPath").ToString() + item.Number + ".pdf";
                            }

                            string plantString = item.PlantID;
                            if (item.PlantID == "") plantString = "Plant 1";

                            string outputPdfPath = rootFolder + "\\" + plantString + @"\Bandsaw Drawings\" + index + " - " + item.Number + ".pdf";

                            System.IO.Directory.CreateDirectory(rootFolder + "\\" + plantString + @"\Bandsaw Drawings\");


                            string itemText = item.Number;

                            string watermark = "Product Number: " + currentProduct + "\n\n\n" +
                                               "Item Number: " + itemText + "\n\n" + 
                                               "Material:" + item.StructCode;

                            //if (item.Category == "Part")
                            //{
                            //    watermark += "Material: " + item.StructCode + "\n" + "Operations: " + item.Operations + "\n";
                            //    ProcessPDF.AddWatermark(outputPdfPath, watermark);
                            //}


                            watermark += "Order Number: " + item.AssociatedOrders[0].OrderNumber + "\n" +
                                         "Quantity: " + item.AssociatedOrders[0].UnitQty + "\n\n";


                            if ((item.Category == "Assembly") || (item.Category == "Product"))
                            {
                                if (item.AssociatedOrders.Count > 1)
                                {
                                    int i = 0;
                                    int totalQty = item.AssociatedOrders[0].UnitQty;
                                    foreach (OrderData orderItem in item.AssociatedOrders)
                                    {
                                        if (i != 0) // skip the first associated order, we already have it
                                        {
                                            watermark += "Order Number: " + orderItem.OrderNumber + "\n" +
                                                          "Quantity: " + orderItem.UnitQty + "\n\n";
                                            totalQty += orderItem.UnitQty;
                                            
                                        }
                                        i++;
                                    }

                                    watermark += "Total Order Qty: " + totalQty + "\n";
                                }
                            }
                            


                            if (!File.Exists(inputPdfPath)&&item.HasPdf)
                            {
                                ProcessPDF.CreateEmptyPageWithWatermark(outputPdfPath, watermark);
                            }

                            if (File.Exists(inputPdfPath))
                            {
                                File.Copy(inputPdfPath, outputPdfPath);

                                // had some exceptions getting thrown here if the file is read-only.  Will set full permissions to the output file to avoid this.
                                FileInfo fileInfo = new FileInfo(outputPdfPath);
                                fileInfo.IsReadOnly = false;

                                System.IO.File.SetLastWriteTime(outputPdfPath, DateTime.Now);   // getting exceptions thrown on this line

                                if(item.Category == "Assembly")
                                {
                                    ProcessPDF.AddWatermark(outputPdfPath, watermark);
                                }
                                //if (item.Category == "Part")
                                //{
                                //    watermark += "Material: " + item.StructCode + "\n" + "Operations: " + item.Operations + "\n";
                                //    ProcessPDF.AddWatermark(outputPdfPath, watermark);
                                //}
                            }
                            else
                            {
                                if (item.Category == "Product")
                                {
                                    ProcessPDF.CreateEmptyPageWithWatermark(outputPdfPath, watermark);
                                    string path = Path.GetDirectoryName(outputPdfPath);

                                    // I've had some trouble with saw drawings not being ordered properly. I originally only had a 1 second delay in here, but that wasn't
                                    // enough when i tested on VM.  I then changed it to file check only, but that didn't seem to be enough either.  I then put in both file
                                    // check and 100 ms delay, and that seems to be working.

                                    // wait till file is done writing.
                                    //var file = new FileInfo(outputPdfPath);
                                    // While File is not accesable because of writing process
                                    //while (IsFileLocked(file)) { }

                                    // it seems the above wait still isn't quite enough so I put in a 1/2 second delay here.
                                    //System.Threading.Thread.Sleep(100);        // this is a terrible kludge, but it works to sort files properly


                                    //string emptyBackFileName = path + "\\" + Path.GetFileNameWithoutExtension(outputPdfPath) + "-Back.pdf";
                                    //ProcessPDF.CreateEmptyPageWithWatermark(emptyBackFileName, "");
                                    
                                }
                            }
                            index++;
                        }
                    }
                }
                SplashScreenManager.CloseForm(false);

                return true;
            }
            catch(Exception ex)
            {
                SplashScreenManager.CloseForm(false);
                MessageBox.Show(ex.Message);
                return false;
            }


        }

        private bool GroupBandSawDrawings2(System.IO.DirectoryInfo rootFolder)
        {
            try
            {
                SplashScreenManager.ShowForm(this, typeof(WaitForm1), true, true, false);
                {
                    bool isBatch;
                    if (textBoxOutputFolder.Text.Contains("Batch") || (textBoxOutputFolder.Text.Contains("batch")))
                        isBatch = true;
                    else
                        isBatch = false;

                    int index = 0;

                    string currentProduct = "";

                    foreach (AggregateLineItem item in dailyScheduleData.AggregateLineItemList)
                    {
                        if (item.Category == "Product" || item.Category == "Assembly")
                        {
                            if(!SawOrIronWorkerPartBeforeNextProduct(dailyScheduleData.AggregateLineItemList, item)) // if assembly has no saw or ironworker parts, no need to put these products in.
                                //if (!NextAssemblyisPart(dailyScheduleData.AggregateLineItemList, item))  // this should cull out doubled up products that have their children under an assembly further down the list.
                                    continue;
                        }
                        if ((item.Category == "Product" || item.Category == "Assembly") && item.Parent == "<top>")
                        {
                            currentProduct = item.Number;
                        }

                        if (item.AssociatedOrders.Count >= 1 && item.IsStock == false && (item.Operations == "Bandsaw" || item.Operations == "Iron Worker" || item.Category == "Assembly" || item.Category == "Product"))
                        {
                            string inputPdfPath;

                            inputPdfPath = ProcessPDF.CalculateSubFolder(pdfPath, exportFilePath, item, isBatch);

                            if (item.Category == "Assembly")
                            {
                                inputPdfPath = AppSettings.Get("PdfPath").ToString() + item.Number + ".pdf";
                            }

                            string plantString = item.PlantID;
                            if (item.PlantID == "") plantString = "Plant 1";

                            string outputPdfPath = rootFolder + "\\" + plantString + @"\Bandsaw Drawings\" + index + " - " + item.Number + ".pdf";

                            System.IO.Directory.CreateDirectory(rootFolder + "\\" + plantString + @"\Bandsaw Drawings\");


                            string itemText = item.Number;

                            string watermark = "Product Number: " + currentProduct + "\n" +
                                               "Description:" + item.ItemDescription + "\n\n";

                            //if (item.Category == "Part")
                            //{
                            //    watermark += "Material: " + item.StructCode + "\n" + "Operations: " + item.Operations + "\n";
                            //    ProcessPDF.AddWatermark(outputPdfPath, watermark);
                            //}


                            watermark += "Order Number: " + item.AssociatedOrders[0].OrderNumber + "\n" +
                                         "Quantity: " + item.AssociatedOrders[0].OrderQty*item.AssociatedOrders[0].UnitQty + "\n\n";


                            if ((item.Category == "Assembly") || (item.Category == "Product"))
                            {
                                if (item.AssociatedOrders.Count > 1)
                                {
                                    int i = 0;
                                    int totalQty = item.AssociatedOrders[0].OrderQty * item.AssociatedOrders[0].UnitQty;
                                    foreach (OrderData orderItem in item.AssociatedOrders)
                                    {
                                        if (i != 0) // skip the first associated order, we already have it
                                        {
                                            watermark += "Order Number: " + orderItem.OrderNumber + "\n" +
                                                          "Quantity: " + orderItem.OrderQty * orderItem.UnitQty + "\n\n";
                                            totalQty += orderItem.OrderQty * orderItem.UnitQty;

                                        }
                                        i++;
                                    }

                                    watermark += "Total Order Qty: " + totalQty + "\n";
                                }
                            }



                            if (!File.Exists(inputPdfPath) && item.HasPdf)
                            {
                                ProcessPDF.CreateEmptyPageWithWatermark(outputPdfPath, watermark);
                            }

                            if (File.Exists(inputPdfPath))
                            {
                                File.Copy(inputPdfPath, outputPdfPath, true);

                                // had some exceptions getting thrown here if the file is read-only.  Will set full permissions to the output file to avoid this.
                                FileInfo fileInfo = new FileInfo(outputPdfPath);
                                fileInfo.IsReadOnly = false;

                                System.IO.File.SetLastWriteTime(outputPdfPath, DateTime.Now);   // getting exceptions thrown on this line

                                if (item.Category == "Assembly")
                                {
                                    ProcessPDF.AddWatermark(outputPdfPath, watermark);
                                }
                                //if (item.Category == "Part")
                                //{
                                //    watermark += "Material: " + item.StructCode + "\n" + "Operations: " + item.Operations + "\n";
                                //    ProcessPDF.AddWatermark(outputPdfPath, watermark);
                                //}
                            }
                            else
                            {
                                if (item.Category == "Product")
                                {
                                    ProcessPDF.CreateEmptyPageWithWatermark(outputPdfPath, watermark);
                                    string path = Path.GetDirectoryName(outputPdfPath);
                                }
                            }
                            index++;
                        }
                    }
                }
                SplashScreenManager.CloseForm(false);

                return true;
            }
            catch (Exception ex)
            {
                SplashScreenManager.CloseForm(false);
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        private bool GroupBandSawDrawings3(System.IO.DirectoryInfo rootFolder)
        {
            try
            {
                SplashScreenManager.ShowForm(this, typeof(WaitForm1), true, true, false);
                {
                    List<AggregateLineItem> cutList = new List<AggregateLineItem>();
                    bool isBatch;
                    if (textBoxOutputFolder.Text.Contains("Batch") || (textBoxOutputFolder.Text.Contains("batch")))
                        isBatch = true;
                    else
                        isBatch = false;

                    int index = 0;

                    string currentProduct = "";
                    foreach (AggregateLineItem item in dailyScheduleData.AggregateLineItemList)
                    {
                        cutList.Clear();
                        if (item.Category == "Product" || item.Category == "Assembly")
                        {
                            if (SawOrIronWorkerPartBeforeNextProduct(dailyScheduleData.AggregateLineItemList, item))
                            {
                                cutList = GetSawOrIronWorkerPartsWithoutPDFsBeforeNextProduct(dailyScheduleData.AggregateLineItemList, item);
                            }

                            if (item.Category == "Product")
                            {
                                List<string> associatedOrders = new List<string>();
                                associatedOrders = GetAssociatedOrders(dailyScheduleData.AggregateLineItemList, item);
                                if (associatedOrders.Count > 0)
                                {
                                    item.Notes += "See the following orders for parts that get used on this order: \n";
                                    foreach (string order in associatedOrders)
                                    {
                                        string productNumber = GetProductNumberOfOrderNumber(order);
                                        item.Notes += "Order Number: " + order + "      Product: " + productNumber + "\n";
                                    }
                                }
                            }
                            //else
                            //{
                            //    //continue;

                            //    // if we continue here, we skip over assemblies that don't have saw cut parts
                            //    // we also skip over those which have parts that were printed with previous orders, which is likely not desireable.

                            //}
                        }
                        else
                        {

                            foreach (AggregateLineItem item2 in dailyScheduleData.AggregateLineItemList)
                            {
                                if (item2.Parent == item.Number && item2.IsStock == false && (item2.Operations == "Bandsaw" || item2.Operations == "Iron Worker"))
                                {
                                    cutList.Add(item2);
                                }
                            }
                        }
                        if ((item.Category == "Product" || item.Category == "Assembly") && item.Parent == "<top>")
                        {
                            currentProduct = item.Number;
                        }

                        if (item.AssociatedOrders.Count >= 1 && item.IsStock == false && (item.Operations == "Bandsaw" || item.Operations == "Iron Worker" || item.Category == "Assembly" || item.Category == "Product"))
                        {
                            string inputPdfPath;

                            inputPdfPath = ProcessPDF.CalculateSubFolder(pdfPath, exportFilePath, item, isBatch);

                            if (item.Category == "Assembly")
                            {
                                inputPdfPath = AppSettings.Get("PdfPath").ToString() + item.Number + ".pdf";
                            }

                            string plantString = item.PlantID;
                            if (item.PlantID == "") plantString = "Plant 1";

                            string outputPdfPath = rootFolder + "\\" + plantString + @"\Bandsaw Drawings\" + index + " - " + item.Number + ".pdf";

                            System.IO.Directory.CreateDirectory(rootFolder + "\\" + plantString + @"\Bandsaw Drawings\");


                            string itemText = item.Number;

                            string watermark = "Product Number: " + currentProduct + "\n" +
                                               "Description:" + item.ItemDescription + "\n\n";

                            //if (item.Category == "Part")
                            //{
                            //    watermark += "Material: " + item.StructCode + "\n" + "Operations: " + item.Operations + "\n";
                            //    ProcessPDF.AddWatermark(outputPdfPath, watermark);
                            //}

                            string productNumber = GetProductNumberOfOrderNumber(item.AssociatedOrders[0].OrderNumber);
                            watermark += "Order Number: " + item.AssociatedOrders[0].OrderNumber + "            Product: " + productNumber + "\n" +
                                        "Quantity: " + item.AssociatedOrders[0].UnitQty * item.AssociatedOrders[0].OrderQty + "\n\n";


                            if ((item.Category == "Assembly") || (item.Category == "Product"))
                            {
                                if (item.AssociatedOrders.Count > 1)
                                {
                                    int i = 0;
                                    int totalQty = item.AssociatedOrders[0].UnitQty * item.AssociatedOrders[0].OrderQty;
                                    foreach (OrderData orderItem in item.AssociatedOrders)
                                    {
                                        if (i != 0) // skip the first associated order, we already have it
                                        {
                                            productNumber = GetProductNumberOfOrderNumber(orderItem.OrderNumber);
                                            watermark += "Order Number: " + orderItem.OrderNumber + "            Product: " + productNumber + "\n" +
                                                          "Quantity: " + orderItem.UnitQty*orderItem.OrderQty  + "\n\n";
                                            totalQty += orderItem.UnitQty*orderItem.OrderQty;

                                        }
                                        i++;
                                    }

                                    watermark += "Total Order Qty: " + totalQty + "\n";
                                }
                            }



                            if (!File.Exists(inputPdfPath) && item.HasPdf)
                            {
                                // this shouldn't be necessary anymore because we are doing cutlists on the cover page now.
                                //ProcessPDF.CreateEmptyPageWithWatermark(outputPdfPath, watermark);
                            }

                            if (File.Exists(inputPdfPath) && item.HasPdf)
                            {
                                File.Copy(inputPdfPath, outputPdfPath,true);

                                // had some exceptions getting thrown here if the file is read-only.  Will set full permissions to the output file to avoid this.
                                FileInfo fileInfo = new FileInfo(outputPdfPath);
                                fileInfo.IsReadOnly = false;

                                System.IO.File.SetLastWriteTime(outputPdfPath, DateTime.Now);   // getting exceptions thrown on this line

                                if (item.Category == "Assembly")
                                {
                                    ProcessPDF.AddWatermark(outputPdfPath, watermark);
                                }
                                //if (item.Category == "Part")
                                //{
                                //    watermark += "Material: " + item.StructCode + "\n" + "Operations: " + item.Operations + "\n";
                                //    ProcessPDF.AddWatermark(outputPdfPath, watermark);
                                //}
                            }
                            else
                            {
                                if (item.Category == "Product")
                                {
                                    if (!ProcessPDF.CreateDoubleCoverPageWithCutList(outputPdfPath, item, cutList))
                                        MessageBox.Show("Error in generating cover page for " + item.Number);
                                    string path = Path.GetDirectoryName(outputPdfPath);
                                }
                            }
                            index++;
                        }
                    }
                }
                SplashScreenManager.CloseForm(false);

                return true;
            }
            catch (Exception ex)
            {
                SplashScreenManager.CloseForm(false);
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        private bool GroupBandSawDrawings4(System.IO.DirectoryInfo rootFolder)
        {
            try
            {
                SplashScreenManager.ShowForm(this, typeof(WaitForm1), true, true, false);
                {
                    List<AggregateLineItem> cutList = new List<AggregateLineItem>();
                    bool isBatch;
                    if (textBoxOutputFolder.Text.Contains("Batch") || (textBoxOutputFolder.Text.Contains("batch")))
                        isBatch = true;
                    else
                        isBatch = false;

                    int index = 0;

                    string currentProduct = "";
                    foreach (AggregateLineItem item in dailyScheduleData.AggregateLineItemList)
                    {
                        cutList.Clear();
                        if (item.Category == "Product" || item.Category == "Assembly")
                        {
                            if (SawOrIronWorkerPartBeforeNextProduct(dailyScheduleData.AggregateLineItemList, item))
                            {
                                cutList = GetSawOrIronWorkerPartsWithoutPDFsBeforeNextProduct(dailyScheduleData.AggregateLineItemList, item);
                            }

                            if (item.Category == "Product")
                            {
                                List<string> associatedOrders = new List<string>();
                                associatedOrders = GetAssociatedOrders(dailyScheduleData.AggregateLineItemList, item);
                                if (associatedOrders.Count > 0)
                                {
                                    item.Notes += "See the following orders for parts that get used on this order: \n";
                                    foreach (string order in associatedOrders)
                                    {
                                        string productNumber = GetProductNumberOfOrderNumber(order);
                                        item.Notes += "Order Number: " + order + "      Product: " + productNumber + "\n";
                                    }
                                }
                            }
                            //else
                            //{
                            //    //continue;

                            //    // if we continue here, we skip over assemblies that don't have saw cut parts
                            //    // we also skip over those which have parts that were printed with previous orders, which is likely not desireable.

                            //}
                        }
                        else
                        {

                            foreach (AggregateLineItem item2 in dailyScheduleData.AggregateLineItemList)
                            {
                                if (item2.Parent == item.Number && item2.IsStock == false && (item2.Operations == "Bandsaw" || item2.Operations == "Iron Worker"))
                                {
                                    cutList.Add(item2);
                                }
                            }
                        }
                        if ((item.Category == "Product" || item.Category == "Assembly") && item.Parent == "<top>")
                        {
                            currentProduct = item.Number;
                        }

                        if (item.AssociatedOrders.Count >= 1 && item.IsStock == false && (item.Operations == "Bandsaw" || item.Operations == "Iron Worker" || item.Category == "Assembly" || item.Category == "Product"))
                        {
                            string inputPdfPath;

                            inputPdfPath = ProcessPDF.CalculateSubFolder(pdfPath, exportFilePath, item, isBatch);

                            if (item.Category == "Assembly")
                            {
                                inputPdfPath = AppSettings.Get("PdfPath").ToString() + item.Number + ".pdf";
                            }

                            string plantString = item.PlantID;
                            if (item.PlantID == "") plantString = "Plant 1";

                            string outputPdfPath = rootFolder + "\\" + plantString + @"\Bandsaw Drawings\" + index + " - " + item.Number + ".pdf";

                            System.IO.Directory.CreateDirectory(rootFolder + "\\" + plantString + @"\Bandsaw Drawings\");


                            string itemText = item.Number;

                            string watermark = "Product Number: " + currentProduct + "\n" +
                                               "Description:" + item.ItemDescription + "\n\n";

                            //if (item.Category == "Part")
                            //{
                            //    watermark += "Material: " + item.StructCode + "\n" + "Operations: " + item.Operations + "\n";
                            //    ProcessPDF.AddWatermark(outputPdfPath, watermark);
                            //}

                            string productNumber = GetProductNumberOfOrderNumber(item.AssociatedOrders[0].OrderNumber);
                            watermark += "Order Number: " + item.AssociatedOrders[0].OrderNumber + "            Product: " + productNumber + "\n" +
                                        "Quantity: " + item.AssociatedOrders[0].UnitQty * item.AssociatedOrders[0].OrderQty + "\n\n";


                            if ((item.Category == "Assembly") || (item.Category == "Product"))
                            {
                                if (item.AssociatedOrders.Count > 1)
                                {
                                    int i = 0;
                                    int totalQty = item.AssociatedOrders[0].UnitQty * item.AssociatedOrders[0].OrderQty;
                                    foreach (OrderData orderItem in item.AssociatedOrders)
                                    {
                                        if (i != 0) // skip the first associated order, we already have it
                                        {
                                            productNumber = GetProductNumberOfOrderNumber(orderItem.OrderNumber);
                                            watermark += "Order Number: " + orderItem.OrderNumber + "            Product: " + productNumber + "\n" +
                                                          "Quantity: " + orderItem.UnitQty * orderItem.OrderQty + "\n\n";
                                            totalQty += orderItem.UnitQty * orderItem.OrderQty;

                                        }
                                        i++;
                                    }

                                    watermark += "Total Order Qty: " + totalQty + "\n";
                                }
                            }



                            if (!File.Exists(inputPdfPath) && item.HasPdf)
                            {
                                // this shouldn't be necessary anymore because we are doing cutlists on the cover page now.
                                //ProcessPDF.CreateEmptyPageWithWatermark(outputPdfPath, watermark);
                            }

                            if (File.Exists(inputPdfPath) && item.HasPdf)
                            {
                                File.Copy(inputPdfPath, outputPdfPath, true);

                                // had some exceptions getting thrown here if the file is read-only.  Will set full permissions to the output file to avoid this.
                                FileInfo fileInfo = new FileInfo(outputPdfPath);
                                fileInfo.IsReadOnly = false;

                                System.IO.File.SetLastWriteTime(outputPdfPath, DateTime.Now);   // getting exceptions thrown on this line

                                if(item.Category == "Part")
                                {
                                    ProcessPDF.AddWatermarkOnFrontAndDwgOnBackOfPartDwg(outputPdfPath);
                                }

                                if (item.Category == "Assembly")
                                {
                                    ProcessPDF.AddWatermarkOnFrontAndDwgOnBackOfAssyDwg(outputPdfPath, watermark);
                                }
                                //if (item.Category == "Part")
                                //{
                                //    watermark += "Material: " + item.StructCode + "\n" + "Operations: " + item.Operations + "\n";
                                //    ProcessPDF.AddWatermark(outputPdfPath, watermark);
                                //}
                            }
                            else
                            {
                                if (item.Category == "Product")
                                {
                                    if (!ProcessPDF.CreateCoverPageWithCutList(outputPdfPath, item, cutList))
                                        MessageBox.Show("Error in generating cover page for " + item.Number);
                                    string path = Path.GetDirectoryName(outputPdfPath);
                                }
                            }
                            index++;
                        }
                    }
                }
                SplashScreenManager.CloseForm(false);

                return true;
            }
            catch (Exception ex)
            {
                SplashScreenManager.CloseForm(false);
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        private string GetProductNumberOfOrderNumber(string orderNumber)
        {
            string productName = "";

            for (int i = 0; i < 10; i++)        // search up to 10 levels deep for the product number of this order
            {
                foreach (AggregateLineItem lItem in dailyScheduleData.AggregateLineItemList)
                {
                    if (i < lItem.AssociatedOrders.Count())
                    {
                        if (lItem.AssociatedOrders[i].OrderNumber == orderNumber) return lItem.Number;
                    }
                }
            }

            return productName;
        }

        //Boolean NextAssemblyisPart(List<AggregateLineItem> itemList, AggregateLineItem itemToCheck)
        //{
        //    try
        //    {
        //        int curIndex = itemList.IndexOf(itemToCheck);
        //        AggregateLineItem nextItem;

        //        if (curIndex < itemList.Count)
        //            nextItem = itemList[curIndex + 1];
        //        else
        //            return false;

        //        if (nextItem.Category == "Part" || nextItem.Category == "Assembly")
        //            return true;
        //        else
        //            return false;
        //    }
        //    catch (Exception)
        //    {
        //        return true;
        //    }
        //}

        Boolean SawOrIronWorkerPartBeforeNextProduct(List<AggregateLineItem> itemList, AggregateLineItem itemToCheck)
        {
            try
            {
                int curIndex = itemList.IndexOf(itemToCheck);
                AggregateLineItem nextItem;
                do
                {
                    if (curIndex < itemList.Count-1)
                    {
                        nextItem = itemList[curIndex + 1];
                        if (nextItem.Operations == "Bandsaw" || nextItem.Operations == "Iron Worker")
                            if(nextItem.IsStock == false)
                                return true;
                        if (nextItem.Category == "Product")
                            return false;
                        curIndex++;
                    }
                } while (curIndex < itemList.Count-1);

                return false;       // if no bandsaw or ironworker parts and no other products are found after the itemToCheck, we won't need to print it

                
            }
            catch (Exception)
            {
                return false;
            }
        }

        List<AggregateLineItem> GetSawOrIronWorkerPartsWithoutPDFsBeforeNextProduct(List<AggregateLineItem> itemList, AggregateLineItem itemToCheck)
        {
            List<AggregateLineItem> cutList = new List<AggregateLineItem>();
            try
            {
                int curIndex = itemList.IndexOf(itemToCheck);
                AggregateLineItem nextItem;
                do
                {
                    if (curIndex < itemList.Count - 1)
                    {
                        nextItem = itemList[curIndex + 1];
                        if (nextItem.Operations == "Bandsaw" || nextItem.Operations == "Iron Worker")
                            if (nextItem.IsStock == false && nextItem.HasPdf == false)
                                cutList.Add(nextItem);
                        if (nextItem.Category == "Product")
                            return cutList;
                        curIndex++;
                    }
                } while (curIndex < itemList.Count - 1);

                return cutList;       // if no bandsaw or ironworker parts and no other products are found after the itemToCheck, we won't need to print it


            }
            catch (Exception)
            {
                cutList = new List<AggregateLineItem>();
                return cutList;     // return empty cutList on error
            }
        }

        List<string> GetAssociatedOrders(List<AggregateLineItem> itemList, AggregateLineItem itemToCheck)
        {
            List<string> associatedOrders = new List<string>();
            try
            {
                foreach (AggregateLineItem item in itemList)
                {
                    if ((item.Operations == "Bandsaw" || item.Operations == "Iron Worker") && item.IsStock == false && item.AssociatedOrders.Count > 1)
                    {
                        foreach (OrderData o in item.AssociatedOrders)
                        {
                            if (o.OrderNumber == itemToCheck.AssociatedOrders[0].OrderNumber)
                            {
                                if(!associatedOrders.Contains(item.AssociatedOrders[0].OrderNumber))
                                    if(item.AssociatedOrders[0].OrderNumber!= itemToCheck.AssociatedOrders[0].OrderNumber) // added this line
                                        associatedOrders.Add(item.AssociatedOrders[0].OrderNumber);
                            }
                        }
                    }
                }
                return associatedOrders;
            }
            catch (Exception)
            {
                associatedOrders = new List<string>();
                return associatedOrders;     // return empty order list on error
            }
        }

        private void groupBoxOutput_Enter(object sender, EventArgs e)
        {

        }

        private async void btnProcessBatch_Click(object sender, EventArgs e)
        {
            {
                DialogResult confirmBatchResult = MessageBox.Show("Note that items do not get udpated when processed in a batch.\nPlease ensure items are up to date before running this command.", "Confirm", MessageBoxButtons.OKCancel);

                try
                {
                    //exportFilePath = folderBrowserDialogOutputFolderSelect.SelectedPath + "\\";
                    //textBoxOutputFolder.Text = exportFilePath;

                    exportFilePath = textBoxOutputFolder.Text;

                    AppSettings.Set("ExportFilePath", exportFilePath);
                    string scheduleFileName = exportFilePath + AppSettings.Get("DailyScheduleData").ToString();

                    loadScheduleData(scheduleFileName);

                    if (confirmBatchResult == DialogResult.OK)
                    {
                        dailyScheduleData = new DailyScheduleAggregate(textBoxOutputFolder.Text, pdfPath);
                        if (hlaVault==null || (hlaVault!=null&&!hlaVault.IsWebServiceManagerActive()))
                        {
                            //hlaVault.CloseVaultConnection();
                            toolStripStatusLabel1.Text = "Logging into Vault...";
                            statusStrip1.Refresh();
                            hlaVault = new VaultAccess.VaultAccess(pdfPath, AppSettings.Get("PrintPDFPrinterMicrosoft").ToString());
                            hlaVault.LoginForItems("lorne", "lorne", vaultServer, vaultVault);
                            toolStripStatusLabel1.Text = "Logging into Vault...Done";
                        }

                        if (hlaVault.IsConnectionActive())
                        {
                            string batchFileName = @"C:\Users\lorne\Desktop\Vault Export\batch.csv";
                            string exportFileName = AppSettings.Get("VaultExportFilePath").ToString() + AppSettings.Get("VaultExportFileName").ToString();
                            string logMessage = "";

                            DialogResult result = openFileDialog1.ShowDialog();
                            if (result == DialogResult.OK) // Test result.
                            {
                                batchFileName = openFileDialog1.FileName;
                            }

                            List<string[]> exportFileList = new List<string[]>();

                            var progressHandler = new Progress<string>(value =>
                            {
                                toolStripStatusLabel1.Text = value;
                            });
                            var progress = progressHandler as IProgress<string>;

                            logMessage = await hlaVault.ExportVaultItemsByBatch(progress, batchFileName, ref exportFileList);


                            if (logMessage != "")
                            {
                                MessageBox.Show(logMessage);
                                hlaVault.CloseVaultConnection();
                                return;
                            }

                            List<string> allItemsWithoutDrawings = new List<string>();

                            foreach (var v in exportFileList)
                            {
                                File.Delete(exportFileName);
                                File.Copy(v[4], exportFileName);

                                List<string> itemsWithoutDrawings = new List<string>();
                                itemsWithoutDrawings = processVaultItemExport();

                                foreach (string item in itemsWithoutDrawings)
                                {
                                    if (!allItemsWithoutDrawings.Contains(item))
                                    {
                                        allItemsWithoutDrawings.Add(item);
                                    }
                                }

                                txtBoxOrderNumber.Text = v[3];
                                spinEditOrderQty.Value = Decimal.Parse(v[0]);

                                processOrder();
                            }

                            if (allItemsWithoutDrawings.Count > 0)
                            {
                                string outFile = textBoxOutputFolder.Text + "Missing Drawings.txt";

                                try
                                {
                                    using (StreamWriter writetext = new StreamWriter(outFile))
                                    {
                                        foreach (string line in allItemsWithoutDrawings)
                                        {
                                            writetext.WriteLine(line);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show("Error in printing log file\n" + ex.Message);
                                }

                                MessageBox.Show("Found " + allItemsWithoutDrawings.Count + " Items Without Drawings");
                            }

                            hlaVault.CloseVaultConnection();        // close connection with elevated privileges
                        }
                        else
                        {
                            MessageBox.Show("Vault Connection is not active.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    hlaVault.CloseVaultConnection();    // make sure connection that consumes a license gets closed, even if we encounter an error.
                }
            }

        }

        private void btnGroupSawDrawings_Click(object sender, EventArgs e)
        {
            System.IO.DirectoryInfo rootDir = new DirectoryInfo(textBoxOutputFolder.Text);
            GroupBandSawDrawings(rootDir);
        }

        private void btnGroup2_Click(object sender, EventArgs e)
        {
            System.IO.DirectoryInfo rootDir = new DirectoryInfo(textBoxOutputFolder.Text);
            GroupBandSawDrawings2(rootDir);
        }

        private void btnGroupSawDrawings3_Click(object sender, EventArgs e)
        {
            System.IO.DirectoryInfo rootDir = new DirectoryInfo(textBoxOutputFolder.Text);
            GroupBandSawDrawings3(rootDir);
        }

        public bool processList(List<ExportLineItem> itemList)
        {
            try
            {
                int product_template_newRecord = 0;
                int product_product_newRecord = 0;
                int mrp_bom_parentRecord = 0;

                int childRelationNum = 1;

                string Host = "127.0.0.1";
                string User = "openpg";
                string DBname = "Horst Production";
                string Password = "openpgpwd";
                string Port = "5432";

                foreach (ExportLineItem item in itemList)
                {
                    // Build connection string using parameters from portal
                    //
                    string connString =
                        String.Format(
                            "Server={0}; User Id={1}; Database={2}; Port={3}; Password={4};",
                            Host,
                            User,
                            DBname,
                            Port,
                            Password);

                    var conn = new NpgsqlConnection(connString);

                    Console.Out.WriteLine("Opening connection");
                    conn.Open();

                    var command = conn.CreateCommand();

                    command.CommandText = "SELECT id FROM product_template WHERE name = '" + item.Number + "';";
                    var reader = command.ExecuteReader();

                    reader.Read();

                    if (reader.HasRows)
                    {
                        product_template_newRecord = int.Parse(reader[0].ToString());    // we found a record with this name already, don't create it again.
                        reader.Close();
                    }
                    else
                    {
                        conn.Close();
                        conn = new NpgsqlConnection(connString);
                        conn.Open();
                        command = new NpgsqlCommand();
                        command = conn.CreateCommand();
                        command.CommandText = @"insert into product_template (
                        warranty, list_price, weight, sequence, write_uid, 
                        uom_id, create_date, create_uid, 
                        sale_ok, purchase_ok, company_id, uom_po_id, 
                        volume, write_date, active, categ_id, 
                        name, rental, type, tracking, 
                        sale_delay, produce_delay, sale_line_warn,
                        track_service, sale_line_warn_msg,
                        invoice_policy, expense_policy)" +
                        @"values(@warranty, @list_price, @weight, @sequence, @write_uid, 
                        @uom_id, @create_date, @create_uid, 
                        @sale_ok, @purchase_ok, @company_id, @uom_po_id, 
                        @volume, @write_date, @active, @categ_id, 
                        @name, @rental, @type, @tracking, 
                        @sale_delay, @produce_delay, @sale_line_warn,
                        @track_service, @sale_line_warn_msg,
                        @invoice_policy, @expense_policy); ";

                        command.Parameters.AddWithValue("@warranty", 0);
                        command.Parameters.AddWithValue("@list_price", 1);
                        command.Parameters.AddWithValue("@weight", 0);
                        command.Parameters.AddWithValue("@sequence", 1);
                        command.Parameters.AddWithValue("@write_uid", 1);
                        command.Parameters.AddWithValue("@uom_id", 1);
                        command.Parameters.AddWithValue("@create_date", NpgsqlTypes.NpgsqlDateTime.Now);
                        command.Parameters.AddWithValue("@create_uid", 1);
                        command.Parameters.AddWithValue("@sale_ok", false);
                        command.Parameters.AddWithValue("@purchase_ok", true);
                        command.Parameters.AddWithValue("@company_id", 1);
                        command.Parameters.AddWithValue("@uom_po_id", 1);
                        command.Parameters.AddWithValue("@volume", 0);
                        command.Parameters.AddWithValue("@write_date", NpgsqlTypes.NpgsqlDateTime.Now);
                        command.Parameters.AddWithValue("@active", true);
                        command.Parameters.AddWithValue("@categ_id", 1);
                        command.Parameters.AddWithValue("@name", item.Number);                    // define name here
                        command.Parameters.AddWithValue("@rental", false);
                        command.Parameters.AddWithValue("@type", "product");
                        command.Parameters.AddWithValue("@tracking", "none");
                        command.Parameters.AddWithValue("@sale_delay", 0);
                        command.Parameters.AddWithValue("@produce_delay", 0);
                        command.Parameters.AddWithValue("@sale_line_warn", "no-message");
                        command.Parameters.AddWithValue("@track_service", "manual");
                        command.Parameters.AddWithValue("@sale_line_warn_msg", "");
                        command.Parameters.AddWithValue("@invoice_policy", "order");
                        command.Parameters.AddWithValue("@expense_policy", "no");

                        command.ExecuteNonQuery();  // this command should create the new record

                        command.CommandText = "SELECT id FROM product_template WHERE name = '" + item.Number + "';";
                        reader.Close();
                        reader = command.ExecuteReader();

                        reader.Read();

                        if (reader.HasRows)
                        {
                            product_template_newRecord = int.Parse(reader[0].ToString());    // this is now the id of the newly created record

                            //-----------------------create the record in the product_product table, we need a record number from this table in the last step
                            conn.Close();
                            conn = new NpgsqlConnection(connString);
                            conn.Open();
                            command = new NpgsqlCommand();
                            command = conn.CreateCommand();
                            command.CommandText = @"insert into product_product (
                                            create_date, product_tmpl_id, 
                                            create_uid, write_uid, write_date, active)" +
                                                    @"values(@create_date, @product_tmpl_id,
                                            @create_uid, @write_uid, @write_date, @active); ";

                            command.Parameters.AddWithValue("@create_date", NpgsqlTypes.NpgsqlDateTime.Now);
                            command.Parameters.AddWithValue("@product_tmpl_id", product_template_newRecord);   // this needs to be product_template record number created in the first step.
                            command.Parameters.AddWithValue("@create_uid", 1);
                            command.Parameters.AddWithValue("@write_uid", 1);
                            command.Parameters.AddWithValue("@write_date", NpgsqlTypes.NpgsqlDateTime.Now);
                            command.Parameters.AddWithValue("@active", true);

                            command.ExecuteNonQuery();

                            command.CommandText = "SELECT id FROM product_product WHERE product_tmpl_id = '" + product_template_newRecord + "';";
                            reader.Close();
                            reader = command.ExecuteReader();

                            reader.Read();

                            if (reader.HasRows)
                            {
                                product_product_newRecord = int.Parse(reader[0].ToString());    // this is now the id of the newly created product_product record

                            }

                            // if we already have a bom record for this item, we don't want another one
                            command.CommandText = "SELECT id FROM mrp_bom WHERE product_tmpl_id = '" + product_template_newRecord + "';";
                            reader.Close();
                            reader = command.ExecuteReader();

                            reader.Read();

                            if (!reader.HasRows)
                            {
                                if (item.Category == "Assembly" || item.Category == "Product")        // all assemblies need a bom record created
                                {
                                    // item is top level so we create a record in mrp_bom table
                                    // create a bom item
                                    conn.Close();
                                    conn = new NpgsqlConnection(connString);
                                    conn.Open();
                                    command = new NpgsqlCommand();
                                    command = conn.CreateCommand();
                                    command.CommandText = @"insert into mrp_bom (
                                            create_date, sequence, write_uid, product_qty, create_uid, 
                                            company_id, product_tmpl_id, 
                                            type, ready_to_produce, write_date, active, product_uom_id
                                            )" +
                                                @"values(@create_date, @sequence, @write_uid, @product_qty, @create_uid, 
                                            @company_id, @product_tmpl_id, 
                                            @type, @ready_to_produce, @write_date, @active, @product_uom_id 
                                            ); ";

                                    command.Parameters.AddWithValue("@create_date", NpgsqlTypes.NpgsqlDateTime.Now);
                                    command.Parameters.AddWithValue("@sequence", 1);
                                    command.Parameters.AddWithValue("@write_uid", 1);
                                    //command.Parameters.AddWithValue("@product_qty", item.Qty);
                                    command.Parameters.AddWithValue("@product_qty", 1);
                                    command.Parameters.AddWithValue("@create_uid", 1);
                                    command.Parameters.AddWithValue("@company_id", 1);
                                    command.Parameters.AddWithValue("@product_tmpl_id", product_template_newRecord);
                                    command.Parameters.AddWithValue("@type", "normal");
                                    command.Parameters.AddWithValue("@ready_to_produce", "asap");
                                    command.Parameters.AddWithValue("@write_date", NpgsqlTypes.NpgsqlDateTime.Now);
                                    command.Parameters.AddWithValue("@active", true);
                                    command.Parameters.AddWithValue("@product_uom_id", 1);

                                    command.ExecuteNonQuery();

                                    // we have to save the id of this parent record to insert into mrp_bom_line table in the last step
                                    command.CommandText = "SELECT id FROM mrp_bom WHERE product_tmpl_id = '" + product_template_newRecord + "';";
                                    reader.Close();
                                    reader = command.ExecuteReader();

                                    reader.Read();

                                    if (reader.HasRows)
                                    {
                                        mrp_bom_parentRecord = int.Parse(reader[0].ToString());    // this is now the id of the newly created bom record
                                    }
                                }
                            }






                            if (item.Parent != "<top>")     // unless this is the top level item, it will also be a child
                            {
                                command.CommandText = "SELECT id FROM product_template WHERE name = '" + item.Parent + "';";
                                reader.Close();
                                reader = command.ExecuteReader();

                                reader.Read();

                                int product_template_id = 0;
                                int mrp_bom_id = 0;

                                if (reader.HasRows)
                                {
                                    product_template_id = int.Parse(reader[0].ToString());    
                                }

                                command.CommandText = "SELECT id FROM mrp_bom WHERE product_tmpl_id = '" + product_template_id + "';";
                                reader.Close();
                                reader = command.ExecuteReader();

                                reader.Read();

                                if (reader.HasRows)
                                {
                                    mrp_bom_id = int.Parse(reader[0].ToString());    
                                }

                                // item is not top level, so we need to create a parent child relationship in mrp_bom_line table
                                conn.Close();
                                conn = new NpgsqlConnection(connString);
                                conn.Open();
                                command = new NpgsqlCommand();
                                command = conn.CreateCommand();
                                command.CommandText = @"insert into mrp_bom_line (
                                            create_uid, product_id, sequence, product_uom_id, 
                                            create_date, write_date, product_qty, bom_id, write_uid
                                            )" +
                                            @"values(@create_uid, @product_id, @sequence, @product_uom_id,
                                            @create_date, @write_date, @product_qty, @bom_id,@write_uid
                                            ); ";

                                command.Parameters.AddWithValue("@create_uid", 1);
                                command.Parameters.AddWithValue("@product_id", product_product_newRecord);
                                command.Parameters.AddWithValue("@sequence", 1);    // this should likely be incremented
                                command.Parameters.AddWithValue("@product_uom_id", 1);
                                command.Parameters.AddWithValue("@create_date", NpgsqlTypes.NpgsqlDateTime.Now);
                                command.Parameters.AddWithValue("write_date", NpgsqlTypes.NpgsqlDateTime.Now);
                                command.Parameters.AddWithValue("product_qty", item.Qty);
                                command.Parameters.AddWithValue("bom_id", mrp_bom_id);
                                command.Parameters.AddWithValue("write_uid", 1);

                                command.ExecuteNonQuery();
                                childRelationNum++;
                            }
                        }
                    }


                    Console.Out.WriteLine("Closing connection");
                    conn.Close();
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public List<ExportLineItem> sortList(List<ExportLineItem> itemList)
        {
            // sort list so that no items appear before their respective parents.
            try
            {
                List<ExportLineItem> sortedList = new List<ExportLineItem>();

                sortedList.Add(itemList.First());
                itemList.Remove(itemList.First());

                do
                {
                    foreach (ExportLineItem item in itemList.ToList())
                    {

                        string parentName = item.Parent;

                        var value = sortedList.Find(x => x.Number == parentName);
                        if (value != null)  // is item's parent already on sorted list?
                        {
                            sortedList.Add(item);
                            itemList.Remove(item);
                        }
                    }
                }
                while (itemList.Count > 0);
                
                return sortedList;
            }
            catch(Exception ex)
            {
                return null;
            }
        }

        private void btnOdoo_Click(object sender, EventArgs e)
        {
            lineItemList = sortList(lineItemList);  // sort list so that no items appear before their respective parents
            processList(lineItemList);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            System.IO.DirectoryInfo rootDir = new DirectoryInfo(textBoxOutputFolder.Text);
            GroupBandSawDrawings4(rootDir);
        }

        private void radioGroup1_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool allStock = false;
            foreach (ExportLineItem item in lineItemList)
            {
                if (radioGroup1.SelectedIndex == 0)
                    allStock = true;
                else
                    allStock = false;
                    
                item.IsStock = allStock;
            }

            exportTreeList.RefreshDataSource();
        }

        private void radioGroup2_SelectedIndexChanged(object sender, EventArgs e)
        {
            string mfgLocation = "Plant 1";
            foreach (ExportLineItem item in lineItemList)
            {
                if (radioGroup2.SelectedIndex == 0)
                    mfgLocation = "Plant 1";
                else //(radioGroup1.SelectedIndex == 1)
                    mfgLocation = "Plant 2";

                item.PlantID = mfgLocation;
            }

            exportTreeList.RefreshDataSource();
        }

        private void btnReport_Click(object sender, EventArgs e)
        {
            XtraReport11 report = new XtraReport11();

            ReportPrintTool printTool = new ReportPrintTool(new XtraReport11());

            printTool.ShowRibbonPreview();
        }

        private void btnReport2_Click(object sender, EventArgs e)
        {
            XtraReport12 report = new XtraReport12();

            ReportPrintTool printTool = new ReportPrintTool(new XtraReport12());

            printTool.ShowRibbonPreview();
        }

        private void btnBandsawReport2_Click(object sender, EventArgs e)
        {
            XtraReport13 report = new XtraReport13();

            ReportPrintTool printTool = new ReportPrintTool(new XtraReport13());

            printTool.ShowRibbonPreview();
        }
    }

}

    
