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

                    plProd.SubItems.Sort();

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

            productionList = new ProductionListDataSource();
            LoadData();
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

        private void btnLaserSched_Click(object sender, EventArgs e)
        {
            List<ProductionListLineItem> laserList = new List<ProductionListLineItem>();
            laserList = (List<ProductionListLineItem>) productionList.GetLaserScheduleReport();

            XtraReport14 report = new XtraReport14();

            ReportPrintTool printTool = new ReportPrintTool(new XtraReport14());

            printTool.ShowRibbonPreview();
        }
    }

}

    
