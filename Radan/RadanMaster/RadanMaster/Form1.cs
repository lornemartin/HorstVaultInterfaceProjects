using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraBars.Ribbon;
using System.Data.Entity;
using RadanMaster.Models;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Columns;

using VaultItemProcessor;

using RadanInterface2;
using RadProject;
using System.IO;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;
using DevExpress.Utils;
using DevExpress.XtraSplashScreen;
using DevExpress.Utils.Menu;
using DevExpress.XtraGrid.Views.Layout.Modes;
using DevExpress.XtraEditors;
using System.Data.Entity.Infrastructure;

using log4net;
using log4net.Config;
using DevExpress.Data.Filtering;
using DevExpress.XtraBars.Native;
using VaultAccess;
using System.Reflection;
using DevExpress.Data.Mask;
using System.Data.Entity.Migrations;
using DevExpress.Utils.Extensions;

namespace RadanMaster
{
    public partial class Form1 : RibbonForm
    {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        RadanMaster.DAL.RadanMasterContext dbContext { get; set; }
        string radanProjectName { get; set; }
        RadanProject rPrj { get; set; }
        string symFolder { get; set; }
        RadanInterface radInterface { get; set; }
        BindingList<DisplayItem> DisplayItems { get; set; }
        GroupAndFilterSettings groupAndFilterSettings { get; set; }

        List<RadanID> radanIdList { get; set; }

        //private static readonly log4net.ILog logger =
        //log4net.LogManager.GetLogger(typeof(Program));

        RefreshHelper helper;

        public Form1()
        {
            try
            {
                SplashScreenManager.ShowForm(this, typeof(SplashScreen1), true, true, false);

                InitializeComponent();

                string version = AssemblyVersion;
                string productName = AssemblyProduct;

                this.Text = productName + " " + version;

                // load group and filter settings from config file
                groupAndFilterSettings = new GroupAndFilterSettings();
                groupAndFilterSettings.LoadSettingsFromFile();

                // set toggle switch state based on saved config file
                barToggleSwitchShowBatches.Checked = groupAndFilterSettings.ShowBatches;
                barToggleSwitchShowOrders.Checked = groupAndFilterSettings.ShowOrders;
                barToggleShowComplete.Checked = groupAndFilterSettings.ShowComplete;
                barToggleShowRadan.Checked = groupAndFilterSettings.ShowOnlyItemsInRadan;
                barToggleSwitchGroup1.Checked = groupAndFilterSettings.GroupByBatchAndThickness;
                barToggleSwitchGroup2.Checked = groupAndFilterSettings.GroupByScheduleAndThickness;
                barCheckItemShowAllCompletedOrders.Checked = groupAndFilterSettings.ShowAllCompletedOrders;
                barCheckItemShowCompletedOrdersFromLastDayOnly.Checked = groupAndFilterSettings.ShowCompletedOrdersFromLastNDays;
                barEditNumOfDays.EditValue = groupAndFilterSettings.NumberOfDays;

                log4net.Config.XmlConfigurator.Configure(); // configure logging
                logger.Info("Starting Program.");

                dbContext = new DAL.RadanMasterContext();

                logger.Info("dbContext initialized.");

                radanProjectName = (string)AppSettings.AppSettings.Get("RadanProjectPathAndFile");

                logger.Info("Radan Project Name = " + radanProjectName);

                barEditRadanProjectBrowse.EditValue = radanProjectName;
                symFolder = (string)AppSettings.AppSettings.Get("SymFilePath");
                logger.Info("SymFolder = " + symFolder);
                logger.Info("Test");

                radInterface = new RadanInterface();
                if (radInterface.Initialize())
                {
                    toolStripStatusLabel1.Text = "Connected to Radan";
                    logger.Info("Connected to Radan.");
                }
                else
                {
                    toolStripStatusLabel1.Text = "Could not connect to Radan";
                    logger.Info("Could not connect to Radan.");
                }

                dbContext.OrderItems.Load();
                dbContext.Parts.Load();
                dbContext.Orders.Load();
                dbContext.RadanIDs.Load();
                dbContext.Nests.Load();
                dbContext.NestedParts.Load();

                logger.Info("dbContext loaded.");
                logger.Info("Test2");

                orderItemsBindingSource.DataSource = dbContext.OrderItems.Local.ToBindingList();
                nestsBindingSource.DataSource = null;

                logger.Info("Datasources set.");

                rPrj.LoadData(radanProjectName);
                logger.Info("Radan project loaded.");

                progressPanel1.Hide();

                // load grid layout from file
                string SettingsFilePath = new FileInfo(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)) + @"\RadanMaster\ItemsGridLayout.xml";
                if (System.IO.File.Exists(SettingsFilePath))
                {
                    gridControlItems.ForceInitialize();
                    gridControlItems.MainView.RestoreLayoutFromXml(SettingsFilePath);
                }

                // load collapsed/expanded state of gridview
                helper = new RefreshHelper(gridViewItems, "ID");
                helper.LoadViewInfo();

                SplashScreenManager.HideImage();
            }
            catch (Exception ex)
            {
                progressPanel1.Hide();
                MessageBox.Show(ex.Message + ex.InnerException.Message);
                SplashScreenManager.HideImage();
            }

        }

        public int CalculateRadanID(OrderItem orderItem, List<RadanID> radanIdList)
        {
            // loop through RadanID referene table, till we find an id in the range of 0 to 500 that is available
            for (int i = 1; i <= 500; i++)
            {
                RadanID radanIDItem = (radanIdList.Where(r => r.RadanIDNumber == i).FirstOrDefault());
                if (radanIDItem == null)
                {
                    RadanID newRadanID = new RadanID();
                    newRadanID.OrderItem = orderItem;
                    newRadanID.OrderItemID = orderItem.ID;
                    newRadanID.RadanIDNumber = i;
                    radanIdList.Add(newRadanID);
                    orderItem.RadanID = newRadanID;
                    orderItem.RadanIDNumber = i;
                    return i;
                }
            }

            return -1;
            
        }

        private double NormalizeThickness(double thickness)
        {
            double normalizedThickness = 0;
            double tolerance = 0.007;

            if (thickness > 0.312 - tolerance && thickness < 0.312 + tolerance)
                normalizedThickness = 0.312;
            else if (thickness > 0.188 - tolerance && thickness < 0.188 + tolerance)
                normalizedThickness = 0.188;
            else if (thickness > 0.125 - 0.010 && thickness < 0.125 + 0.015)
                normalizedThickness = 0.125;
            else if (thickness > 0.075 - tolerance && thickness < 0.075 + tolerance)
                normalizedThickness = 0.075;
            else if (thickness > 0.062 - tolerance && thickness < 0.062 + tolerance)
                normalizedThickness = 0.062;
            else
                normalizedThickness = thickness;

            return normalizedThickness;
        }
        private void importXmlFile(string fileName)
        {
            string plantID = AppSettings.AppSettings.Get("PlantID").ToString();

            DailyScheduleAggregate dSchedule = new DailyScheduleAggregate(fileName);
            dSchedule.LoadFromFile();

            DirectoryInfo di = new DirectoryInfo(fileName);
            string batchName = di.Parent.Name;
            string schedName = di.Parent.Name;
            byte[] thumbnailByteArray = null;
            bool hasBends = false;

            RadanInterface radanInterface = new RadanInterface();

            foreach (AggregateLineItem lineItem in dSchedule.AggregateLineItemList)
            {
                if (lineItem.PlantID == plantID)
                {
                    thumbnailByteArray = null;
                    bool isBatch = batchName.ToUpper().Contains("BATCH");

                    if (lineItem.Operations == "Laser")
                    {
                        if ((isBatch && lineItem.IsStock == true) || (!isBatch && lineItem.IsStock == false))
                        {

                            string symName = symFolder + lineItem.Number + ".sym";
                            if (System.IO.File.Exists(symName))
                            {
                                char[] thumbnailCharArray = radanInterface.GetThumbnailDataFromSym(symName);
                                thumbnailByteArray = Convert.FromBase64CharArray(thumbnailCharArray, 0, thumbnailCharArray.Length);
                                //hasBends = radanInterface.HasBends(symName);
                                hasBends = false;
                            }

                            Part newPart = new Part();

                            newPart = dbContext.Parts.Where(p => p.FileName == lineItem.Number).FirstOrDefault();
                            if (newPart == null)    // create a new part if we don't have it in the list
                            {
                                newPart = new Part();
                                newPart.FileName = lineItem.Number;
                                newPart.Description = lineItem.ItemDescription;
                                string modifiedThickness = lineItem.MaterialThickness.Substring(0, lineItem.MaterialThickness.LastIndexOf(" "));
                                newPart.Thickness = NormalizeThickness(double.Parse(modifiedThickness));
                                // the following should work, but has not been tested yet.
                                if (lineItem.MaterialThickness.Contains("mm"))
                                  newPart.Thickness = newPart.Thickness / 25.4;
                                newPart.Material = lineItem.Material;
                                newPart.Thumbnail = thumbnailByteArray;
                                newPart.HasBends = hasBends;

                                dbContext.Parts.Add(newPart);
                                dbContext.SaveChanges();
                            }
                            else
                            {
                                // update properties if needed
                                newPart.Description = lineItem.ItemDescription;
                                string modifiedThickness = lineItem.MaterialThickness.Substring(0, lineItem.MaterialThickness.LastIndexOf(" "));
                                newPart.Thickness = NormalizeThickness(double.Parse(modifiedThickness));
                                // the following should work, but has not been tested yet
                                //if (lineItem.MaterialThickness.Contains("mm"))
                                //    newPart.Thickness = newPart.Thickness / 25.4;
                                newPart.Material = lineItem.Material;
                                newPart.Thumbnail = thumbnailByteArray;
                                newPart.HasBends = hasBends;
                                dbContext.SaveChanges();
                            }

                            foreach (OrderData oData in lineItem.AssociatedOrders)
                            {
                                Order searchOrder = new Order();
                                if (isBatch)
                                    searchOrder = dbContext.Orders.Where(o => o.BatchName == batchName).FirstOrDefault();
                                else
                                    searchOrder = dbContext.Orders.Where(o => o.OrderNumber == oData.OrderNumber).
                                                                   Where(o => o.ScheduleName == schedName).FirstOrDefault();
                                if (searchOrder == null)    // create new order if it doesn't already exist
                                {
                                    searchOrder = new Order();
                                    if (isBatch)
                                    {
                                        searchOrder.BatchName = batchName.TrimEnd(' ');
                                        searchOrder.OrderNumber = "";
                                    }
                                    else
                                    {
                                        searchOrder.OrderNumber = oData.OrderNumber.TrimEnd(' ');
                                        searchOrder.ScheduleName = schedName.TrimEnd(' ');
                                        searchOrder.BatchName = "";
                                    }
                                    searchOrder.IsComplete = false;
                                    searchOrder.DueDate = DateTime.Now;
                                    searchOrder.EntryDate = DateTime.Now;
                                    searchOrder.IsBatch = isBatch;
                                    dbContext.Orders.Add(searchOrder);
                                    dbContext.SaveChanges();
                                }

                                OrderItem searchOrderItem = new OrderItem();
                                if (isBatch)
                                    searchOrderItem = dbContext.OrderItems.Where(o => o.Order.BatchName == batchName).Where(o => o.Part.FileName == lineItem.Number).FirstOrDefault();
                                else
                                    searchOrderItem = dbContext.OrderItems.Where(o => o.Order.OrderNumber == oData.OrderNumber).
                                                                           Where(o => o.Part.FileName == lineItem.Number).
                                                                           Where(o => o.Order.ScheduleName == schedName).
                                                                           FirstOrDefault();
                                if (searchOrderItem == null)   // create a new order item if no match is found with this part number and order number and schedule number
                                {
                                    searchOrderItem = new OrderItem();
                                    searchOrderItem.Order = searchOrder;
                                    searchOrderItem.Part = newPart;
                                    searchOrderItem.QtyRequired = oData.OrderQty * oData.UnitQty;
                                    searchOrderItem.QtyNested = 0;
                                    searchOrderItem.IsComplete = false;

                                    dbContext.OrderItems.Add(searchOrderItem);
                                    dbContext.SaveChanges();

                                }
                                else       // adjust existing order item with new quantities if it already exists
                                {
                                    searchOrderItem.QtyRequired += oData.OrderQty * oData.UnitQty;
                                    dbContext.SaveChanges();
                                }
                            }
                        }
                    }
                }
            }
            dbContext.SaveChanges();
        }

        private bool masterItemToRadanPart(OrderItem oItem)
        {
            try
            {
                //rPrj = new RadanProject();
                //rPrj = rPrj.LoadData(radanProjectName);
                string symName = oItem.Part.FileName;

                // this is only needed if description was not correctly read from sym when part was entered into DB.  It should
                // eventually not be needed anymore, but will be handy for the short term to populate description field in radan project.
                if (radInterface != null)
                {
                    if (oItem.Part.Description == "")
                    {
                        oItem.Part.Description = radInterface.GetDescriptionFromSym(symFolder + symName + ".sym");
                    }
                }

                if (rPrj.Parts != null)
                {
                    RadanPart rPart = new RadanPart();
                    bool matchFound = false;
                    for (int i = 0; i < rPrj.Parts.Count(); i++)
                    {
                        rPart = rPrj.Parts.Part[i];
                        string radanName = Path.GetFileNameWithoutExtension(rPart.Symbol);

                        if (oItem.RadanIDNumber >= 0)
                        {
                            if (radanName == symName && rPart.Bin == oItem.RadanIDNumber.ToString())
                            {
                                matchFound = true;
                                rPart.Number = oItem.QtyRequired - oItem.QtyNested;  // item still exists in project, just need to update 

                                MessageBox.Show(Path.GetFileName(symName) + " already exists in this radan projecs, only qty required will be updated.");
                                oItem.IsInProject = true;
                                break;
                            }
                        }
                    }
                    if (!matchFound)
                    {
                        //create new part in project

                        string calculatedSymPath = "";
                        bool multiple = false;

                        if (File.Exists(symFolder + "\\" + symName + ".sym"))
                        {
                            calculatedSymPath = CalculateFolderPath(oItem);      // copy sym file from main sym folder to current project so that we can set custom attributes
                            if (!Directory.Exists(Path.GetDirectoryName(calculatedSymPath)))
                            {
                                System.IO.Directory.CreateDirectory(calculatedSymPath);     // create new folder if necessary.
                            }
                            else
                            {
                                if(File.Exists(calculatedSymPath + "\\" + symName + ".sym"))
                                    multiple = true;
                            }

                            File.Copy(symFolder + symName + ".sym", calculatedSymPath + symName + ".sym", true);

                            // save the custom attributes to the newly copied sym file
                            string orderNumber = "";
                            if (!multiple)
                                orderNumber = oItem.Order.OrderNumber == null ? "" : oItem.Order.OrderNumber;
                            else if(oItem.Order.OrderNumber != "")
                                orderNumber = "mulitiple";

                            string scheduleName = "";
                            if (!multiple)
                            {
                                scheduleName = oItem.Order.ScheduleName == null ? "" : oItem.Order.ScheduleName;
                            }
                            else if (oItem.Order.ScheduleName != "")
                                scheduleName = "multiple";

                            string batchName = "";
                            if (!multiple)
                                batchName = oItem.Order.BatchName == null ? "" : oItem.Order.BatchName;
                            else if (oItem.Order.BatchName != "")
                                batchName = "multiple";

                            string description = oItem.Part.Description == null ? "" : oItem.Part.Description;

                            //bool hasBends = radInterface.GetBendDataFromSym(symFolder + "\\" + symName + ".sym");

                            string errMessage = "";
                            radInterface.InsertAdditionalAttributes(calculatedSymPath + symName + ".sym", oItem.Part.Material, oItem.Part.Thickness.ToString(), "in", description, orderNumber, scheduleName, batchName, oItem.Part.HasBends, ref errMessage);
                        }

                        rPart = new RadanPart();
                        rPart.Symbol = calculatedSymPath + symName + ".sym";
                        rPart.Number = oItem.QtyRequired - oItem.QtyNested;
                        rPart.Made = 0;
                        rPart.ThickUnits = "in";
                        rPart.Thickness = oItem.Part.Thickness;
                        rPart.Material = oItem.Part.Material;
                         
                        int radanID = CalculateRadanID(oItem,radanIdList);
                        if (radanID != -1) // check to make sure we dont' have more than the max 500 parts in the radan project
                            rPart.Bin = radanID.ToString();
                        else
                            return false;   // 
                        rPrj.AddPart(rPart);

                        oItem.IsInProject = true;
                        //dbContext.SaveChanges();
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        private string CalculateFolderPath(OrderItem orderItem)
        {
            string path = "";

            string symFileName = orderItem.Part.FileName + ".sym";

            List<string> matchingFiles = new List<string>();

            if(Directory.Exists(Path.GetDirectoryName(radanProjectName) + "\\Symbols\\"))
                matchingFiles = Directory.GetFiles((Path.GetDirectoryName(radanProjectName) + "\\Symbols\\"), symFileName, SearchOption.AllDirectories).ToList();

            if (matchingFiles.Count() > 0)
            {
                path = Path.GetDirectoryName(matchingFiles[0]) + "\\";
            }
            else
            {
                if (orderItem.Order.IsBatch == false)
                {
                    if (orderItem.Order.OrderNumber == null) orderItem.Order.OrderNumber = "";
                    path = Path.GetDirectoryName(radanProjectName) + "\\Symbols" + "\\" + orderItem.Order.OrderNumber + "\\" + orderItem.Order.ScheduleName + "\\" + orderItem.Part.FileName + "\\";
                }
                else
                {
                    path = Path.GetDirectoryName(radanProjectName) + "\\Symbols" + "\\" + orderItem.Order.BatchName + "\\" + orderItem.Part.FileName + "\\";
                }
            }

            return path;
        }

        private bool SyncRadanToMaster()
        {
            try
            {
                if (saveRadan())
                {
                    rPrj = rPrj.LoadData(radanProjectName);
                    // check for parts in radan project that cannot be synced and warn user accordingly
                    foreach (RadanPart radanPart in rPrj.Parts.Part)
                    {
                        OrderItem masterItem = dbContext.OrderItems.Where(o => o.RadanIDNumber.ToString() == radanPart.Bin).FirstOrDefault();
                        if (masterItem == null || radanPart.Bin == "0")
                        {
                            MessageBox.Show(Path.GetFileName(radanPart.Symbol) + " cannot be synched back to RadanMaster, no matching record found.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        else
                        {
                            if (radanPart.Made >= radanPart.Number)
                                masterItem.IsComplete = true;
                        }
                    }

                    // check for completed orders....right now this only work one way, it won't uncheck completed orders that are no longer complete
                    bool orderComplete = true;
                    //List<OrderItem> orderItemsToSync = dbContext.OrderItems.ToList();               // this may cause performance problems eventually
                    List<OrderItem> orderItemsToSync = dbContext.OrderItems.Where(oi => oi.Order.IsComplete == false).Where(oi => oi.IsInProject).ToList();
                    foreach (OrderItem syncItem in orderItemsToSync)
                    {
                        if (syncItem.Order.IsComplete != true)      // no need to check if order is already complete
                        {
                            // check completion status of all associated order items
                            List<OrderItem> associatedOrderItems = dbContext.OrderItems.Where(o => o.Order.ID == syncItem.Order.ID).ToList();
                            orderComplete = true;
                            foreach (OrderItem item in associatedOrderItems)
                            {
                                if (item.IsComplete == false)
                                {
                                    orderComplete = false;
                                    break;
                                }
                            }

                            // if all associated order items are complete, consider whole order as complete
                            if (orderComplete == true)
                            {
                                syncItem.Order.IsComplete = true;
                                syncItem.Order.DateCompleted = DateTime.Now;

                            }
                        }
                    }

                    // check to make sure all order items are still in Radan and notify user if not
                    List<OrderItem> orderItemsInRadan = dbContext.OrderItems.Where(o => o.IsInProject).ToList();

                    foreach (OrderItem item in orderItemsInRadan)
                    {
                        RadanPart radanPart = rPrj.Parts.Part.Where(p => p.Bin == item.RadanIDNumber.ToString()).FirstOrDefault();
                        if (radanPart == null)
                        {
                            MessageBox.Show(item.Part.FileName + " no longer exists in Radan project.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            item.RadanID = null;
                            item.RadanIDNumber = 0;
                            item.IsInProject = false;
                        }
                    }
                    gridViewItems.RefreshData();
                }
                dbContext.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Sync with Radan Project was not successful" + "\n\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private bool RetrieveSelectedRadanPartToMasterList()
        {
            try
            {
                SyncRadanToMaster();

                rPrj = new RadanProject();
                rPrj = rPrj.LoadData(radanProjectName);

                int[] rows = gridViewItems.GetSelectedRows();

                foreach (int i in rows)
                {
                    OrderItem item = (OrderItem)gridViewItems.GetRow(i);

                    RadanPart rPart = GetPartFromRadanProject(rPrj, item.Part.FileName, item.RadanIDNumber);
                    if (rPart != null)
                    {
                        if (rPart.Made == 0)
                        {
                            item.IsInProject = false;
                            RadanID radanIdToRemove = dbContext.RadanIDs.Where(r => r.RadanIDNumber == item.RadanIDNumber).FirstOrDefault();
                            dbContext.RadanIDs.Remove(radanIdToRemove);
                            rPrj.Parts.Part.Remove(rPart);
                            item.RadanIDNumber = 0;
                           
                            RemoveSymFileFromProject(rPart.Symbol);
                        }

                        else
                        {
                            rPart.Number = rPart.Made;
                        }
                    }
                }

                rPrj.SaveData(radanProjectName);

                // need to open current project here...
                string errMessage = "";
                string prjName = radInterface.getOpenProjectName(ref errMessage);
                radInterface.LoadProject(prjName);

                dbContext.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private bool RetrieveAllRadanPartToMasterList()
        {
            try
            {
                SyncRadanToMaster();

                rPrj = new RadanProject();
                rPrj = rPrj.LoadData(radanProjectName);

                int[] rows = gridViewItems.GetSelectedRows();

                foreach (RadanPart rPart in rPrj.Parts.Part.ToList())
                {
                    OrderItem orderItem = dbContext.OrderItems.Where(o => o.RadanIDNumber.ToString() == rPart.Bin).FirstOrDefault();
                    if (orderItem != null)
                    {
                        if (rPart.Made == 0)
                        {
                            orderItem.IsInProject = false;

                            RadanID radanIdToRemove = dbContext.RadanIDs.Where(r => r.RadanIDNumber == orderItem.RadanIDNumber).FirstOrDefault();
                            if (radanIdToRemove != null)
                            {
                                dbContext.RadanIDs.Remove(radanIdToRemove);
                                rPrj.Parts.Part.Remove(rPart);
                                orderItem.RadanIDNumber = 0;
                               
                                RemoveSymFileFromProject(rPart.Symbol);
                            }

                        }

                        else
                        {
                            rPart.Number = rPart.Made;
                        }
                    }
                }

                rPrj.SaveData(radanProjectName);

                // need to open current project here...
                string errMessage = "";
                string prjName = radInterface.getOpenProjectName(ref errMessage);
                radInterface.LoadProject(prjName);

                dbContext.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private bool RemoveSymFileFromProject(string symFileName)
        {
            try
            {
                if (rPrj == null)
                {
                    rPrj = new RadanProject();
                    rPrj = rPrj.LoadData(radanProjectName);
                }

                string symFileNameWithoutPath = Path.GetFileName(symFileName);
                string symFileNameWithoutPathWithoutExtension = Path.GetFileNameWithoutExtension(symFileName);

                List<OrderItem> matchingItemList = dbContext.OrderItems.Where(o => o.Part.FileName == symFileNameWithoutPathWithoutExtension).Where(o => o.IsInProject).ToList();

                if(matchingItemList.Count == 1)
                {
                    List<string> matchingSymFiles = Directory.GetFiles((Path.GetDirectoryName(radanProjectName) + "\\Symbols\\"), symFileNameWithoutPath, SearchOption.AllDirectories).ToList();
                    File.Delete(matchingSymFiles[0]);   // there should only be one file to delete.
                }
                return true;
            }
            catch(Exception ex)
            {
                return false;
            }
        }

        private RadanPart GetPartFromRadanProject(RadanProject prj, string partName, int ID)
        {
            RadanPart rPart = new RadanPart();

            for (int i = 0; i < prj.Parts.Count(); i++)
            {
                rPart = prj.Parts.Part[i];
                string radanName = Path.GetFileNameWithoutExtension(rPart.Symbol);
                if ((radanName == (partName)) && (rPart.Bin == ID.ToString()))
                    return rPart;
            }

            return null;
        }

        private bool RefreshItemsGridFromActiveProject()
        {
            try
            {
                rPrj = rPrj.LoadData(radanProjectName);

                foreach (OrderItem item in dbContext.OrderItems)
                {
                    RadanPart rPart = GetPartFromRadanProject(rPrj, item.Part.FileName, item.RadanID.RadanIDNumber);
                    if (rPart != null)
                        item.IsInProject = true;
                    else
                        item.IsInProject = false;
                }

                dbContext.SaveChanges();
                gridViewItems.RefreshData();


                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private bool CreateNewRadanProject()
        {
            SplashScreenManager.ShowForm(this, typeof(WaitForm1), true, true, false);

            string oldProjectName = "";
            try
            {
                int numOfNestsSaved = 0;
                int numOfItemsUpdated = 0;
                rPrj.SaveData(radanProjectName);
                oldProjectName = radanProjectName;

                SyncRadanToMaster();    // make sure everything is synched before we begin here....

                // first copy all radan nests into DB.
                OrderItem orderItem = new OrderItem();
                if (rPrj.Nests.Count() > 0)
                {
                    foreach (RadanNest radanNest in rPrj.Nests)
                    {
                        Nest newMasterNest = new Nest();
                        newMasterNest.nestName = radanNest.FileName;
                        newMasterNest.nestPath = Path.GetDirectoryName(radanProjectName);
                        char[] thumbnailCharArray = radInterface.GetThumbnailDataFromSym(Path.GetDirectoryName(radanProjectName) + "\\nests\\" + radanNest.FileName);
                        byte[] thumbnailByteArray = null;
                        if (thumbnailCharArray != null)
                            thumbnailByteArray = Convert.FromBase64CharArray(thumbnailCharArray, 0, thumbnailCharArray.Length);
                        newMasterNest.Thumbnail = thumbnailByteArray;
                        newMasterNest.NestedParts = new List<NestedParts>();

                        foreach (NestsPartsMade nestedRadanPart in radanNest.PartsMade)
                        {
                            foreach(NestPartsMadeListItem partMade in nestedRadanPart.PartsListItems)
                            {
                                NestedParts masterNestedPart = new NestedParts();

                                //int radanPartID = (int)nestedRadanPart.PartsListItems[0].ID;
                                int radanPartID = (int) partMade.ID;

                                int radanID = int.Parse(rPrj.Parts.Part.Where(p => p.ID == radanPartID).FirstOrDefault().Bin);
                                orderItem = dbContext.OrderItems.Where(o => o.RadanIDNumber == radanID).FirstOrDefault();

                                masterNestedPart.OrderItem = orderItem;
                                masterNestedPart.Qty = partMade.Made;

                                dbContext.NestedParts.Add(masterNestedPart);

                                newMasterNest.NestedParts.Add(masterNestedPart);

                                if (orderItem.AssociatedNests == null)
                                    orderItem.AssociatedNests = new List<Nest>();

                                if (!orderItem.AssociatedNests.Contains(newMasterNest))
                                {
                                    orderItem.AssociatedNests.Add(newMasterNest);
                                    orderItem.QtyNested += partMade.Made;
                                }

                            }
                            

                        }
                        dbContext.Nests.Add(newMasterNest);
                        numOfNestsSaved++;
                    }
                }

                FileInfo oldProjFile = new FileInfo(radanProjectName);
                DirectoryInfo prjRootDir = oldProjFile.Directory.Parent;

                string newFolderName = string.Format("{0:yyy/MM/dd}", DateTime.Now);

                string newPrjDirName = prjRootDir.FullName + "\\" + newFolderName;

                DirectoryInfo uniqueNewPrjFolder = MakeUnique(newPrjDirName);

                uniqueNewPrjFolder.Create();

                // copy the old folder structure to a new folder
                CopyFolder(oldProjFile.DirectoryName, uniqueNewPrjFolder.FullName);

                // rename the project file
                string oldPrjFileName = uniqueNewPrjFolder.FullName + "\\" + oldProjFile.Name;
                string newPrjFileName = uniqueNewPrjFolder.FullName + "\\" + uniqueNewPrjFolder.Name + ".rpd";
                System.IO.File.Move(oldPrjFileName, newPrjFileName);

                radanProjectName = newPrjFileName;
                barEditRadanProjectBrowse.EditValue = newPrjFileName;

                rPrj = new RadanProject();
                rPrj = rPrj.LoadData(newPrjFileName);

                rPrj.Nests = new List<RadanNest>();
                rPrj.Parts.Part = new List<RadanPart>();
                rPrj.RadanSchedule[0].JobDetails[0].NestFolder = uniqueNewPrjFolder.FullName + "\\" + "nests";
                rPrj.RadanSchedule[0].JobDetails[0].RemnantSaveFolder = uniqueNewPrjFolder.FullName + "\\" + "remnants";
                rPrj.FirstNestNumber = rPrj.RadanSchedule[0].JobDetails[0].NextNestNum;
                rPrj.RadanSchedule[0].JobDetails[0].NextNestNum = rPrj.FirstNestNumber;

                // calculate the new number of sheets still available
                foreach (RadanSheet sheet in rPrj.Sheets.Sheet)
                {
                    sheet.NumAvailable -= sheet.Used;
                    if (sheet.NumAvailable < 0) sheet.NumAvailable = 0;
                }

                // remove all nests from newly created project
                DirectoryInfo newNestFolder = new DirectoryInfo(uniqueNewPrjFolder + "\\" + "nests");
                foreach (FileInfo file in newNestFolder.GetFiles())
                {
                    file.Delete();
                }

                // remove all symbol files from newly created project
                DirectoryInfo newSymbolFolder = new DirectoryInfo(uniqueNewPrjFolder + "\\" + "Symbols");
                newSymbolFolder.Delete(true);

                // we remove all parts from the radan project, so we clear all the associations from the RadanID table
                dbContext.RadanIDs.RemoveRange(dbContext.RadanIDs);

                //update the nested quantites from the old radan project.  Up till now they have always been calculated rather than stored in DB.
                foreach (OrderItem item in dbContext.OrderItems.ToList())
                {
                    int totalNested = 0;

                    if (item.AssociatedNests != null)
                    {
                        foreach (Nest associatedNest in item.AssociatedNests)
                        {
                            if (associatedNest.NestedParts != null)
                            {
                                foreach (NestedParts p in associatedNest.NestedParts)
                                {
                                    if (p.OrderItem == item && Path.GetDirectoryName(associatedNest.nestPath) == Path.GetDirectoryName(oldProjectName))
                                        totalNested += p.Qty;
                                }
                            }
                        }
                    }

                    item.QtyNested += totalNested;
                    item.IsInProject = false;
                    item.RadanID = null;
                    item.RadanIDNumber = 0;
                    numOfItemsUpdated++;
                }

                rPrj.SaveData(newPrjFileName);

                radInterface.LoadProject(newPrjFileName);

                // wait until the very end to save the DB, that way if something goes wrong, DB will not be changed.
                dbContext.SaveChanges();

                SplashScreenManager.HideImage();

                MessageBox.Show("Number of items updated: " + numOfItemsUpdated + "\n" +
                                "Number of nests saved: " + numOfNestsSaved);

                
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("New Radan Project has not been properly initialized.  Database has not been modified." + "\n" +
                                "New Radan project folder should be manually deleted." + "\n" +
                                "Please shut down and restart RadanMaster." + "\n\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                // reset dbContext to original state
                foreach (DbEntityEntry entry in dbContext.ChangeTracker.Entries())
                {
                    switch (entry.State)
                    {
                        case EntityState.Modified:
                            entry.State = EntityState.Unchanged;
                            break;
                        case EntityState.Added:
                            entry.State = EntityState.Detached;
                            break;
                        case EntityState.Deleted:
                            entry.Reload();
                            break;
                        default: break;
                    }
                }

                // load old radan project again.
                radanProjectName = oldProjectName;
                barEditRadanProjectBrowse.EditValue = oldProjectName;

                rPrj = new RadanProject();
                rPrj = rPrj.LoadData(oldProjectName);

                SplashScreenManager.HideImage();
                return false;
            }
        }

        static public void CopyFolder(string sourceFolder, string destFolder)
        {
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);
            string[] files = Directory.GetFiles(sourceFolder);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                string dest = Path.Combine(destFolder, name);
                File.Copy(file, dest);
            }
            string[] folders = Directory.GetDirectories(sourceFolder);
            foreach (string folder in folders)
            {
                string name = Path.GetFileName(folder);
                string dest = Path.Combine(destFolder, name);
                CopyFolder(folder, dest);
            }
        }

        public DirectoryInfo MakeUnique(string path)
        {
            string pathWithoutIncrement = path.Split('(')[0];
            for (int i = 1; ; i++)
            {
                if (!Directory.Exists(path))
                    return new DirectoryInfo(path);

                path = pathWithoutIncrement + "(" + i + ")";
            }
        }

        private bool saveRadan()
        {
            try
            {
                bool proceed = false;
                if (!radInterface.IsActive())
                {
                    DialogResult dialogResult = MessageBox.Show("Radan is not active, do you want to continue?", "Radan", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {
                        proceed = true;
                    }
                    else if (dialogResult == DialogResult.No)
                    {
                        proceed = false;
                    }
                }
                else
                    proceed = true;

                if (proceed)
                {
                    string errMessage = "";
                    string openNestName = radInterface.getOpenNestName(ref errMessage);
                    if (openNestName != "")
                        radInterface.SaveNest(ref errMessage);
                    if (radInterface.isProjectOpen(ref errMessage))
                        radInterface.SaveProject();

                    return true;
                }
                else
                    return false;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        #region gridView1 Event Handlers
        private void gridView1_RowUpdated(object sender, RowObjectEventArgs e)
        {
            dbContext.SaveChanges();
        }

        private void gridView1_RowDeleted(object sender, DevExpress.Data.RowDeletedEventArgs e)
        {
            dbContext.SaveChanges();
        }

        private void gridView1_KeyDown(object sender, KeyEventArgs e)
        {
            bool canDelete = true;
            if (e.KeyCode == Keys.Delete && e.Modifiers == Keys.Control)
            {
                GridView view = sender as GridView;
                int numRows = view.SelectedRowsCount;
                if (MessageBox.Show("Delete " + numRows + " row(s)?", "Confirmation", MessageBoxButtons.YesNo) !=
                  DialogResult.Yes)
                    return;

                List<int> rowHandleList = view.GetSelectedRows().ToList();
                foreach(int rowHandle in rowHandleList)
                {
                    object o = view.GetRow(rowHandle);
                    OrderItem itemToDelete = (OrderItem)o;
                    if(itemToDelete.IsInProject == true)
                    {
                        MessageBox.Show("Cannot delete items that are currently in a Radan project.  Please change your selection and try again.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        canDelete = false;
                        break;
                    }
                }

                if(canDelete)
                    view.DeleteSelectedRows();
            }
        }

        private void barButtonSendSelectionToRadan_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (radInterface.IsProjectReady())
            {
                radanIdList = dbContext.RadanIDs.ToList();

                string errMsg = "";
                if (radInterface.getOpenProjectName(ref errMsg) == radanProjectName)
                {
                    SplashScreenManager.ShowForm(this, typeof(WaitForm1), true, true, false);

                    if (saveRadan())
                    {
                        rPrj = new RadanProject();
                        rPrj = rPrj.LoadData(radanProjectName);
                        List<OrderItem> orderItemList = dbContext.OrderItems.ToList();

                        for (int i = 0; i < gridViewItems.DataRowCount; i++)
                        {
                            if (gridViewItems.IsRowSelected(i))
                            {

                                string partName = gridViewItems.GetRowCellValue(i, "Part.FileName").ToString();
                                string orderNumber = "";
                                if (gridViewItems.GetRowCellValue(i, "Order.OrderNumber") != null)
                                    orderNumber = gridViewItems.GetRowCellValue(i, "Order.OrderNumber").ToString();
                                string batchName = "";
                                if (gridViewItems.GetRowCellValue(i, "Order.BatchName") != null)
                                    batchName = gridViewItems.GetRowCellValue(i, "Order.BatchName").ToString();
                                string schedName = null;
                                if (gridViewItems.GetRowCellValue(i, "Order.ScheduleName") != null)
                                    schedName = gridViewItems.GetRowCellValue(i, "Order.ScheduleName").ToString();

                                OrderItem orderItem = orderItemList.Where(oi => oi.Part.FileName == partName)
                                                                          .Where(oi => oi.Order.OrderNumber == orderNumber)
                                                                          .Where(oi => oi.Order.BatchName == batchName)
                                                                          .Where(oi => oi.Order.ScheduleName == schedName).FirstOrDefault();
                                if (orderItem != null)
                                    if (!masterItemToRadanPart(orderItem))
                                    {
                                        MessageBox.Show("There are more than 500 items in the radan project, not all items have been entered into the project");
                                        break;
                                    }
                            }
                        }

                        foreach (RadanID rID in radanIdList)
                        {
                            dbContext.RadanIDs.AddOrUpdate(rID);
                        }

                        dbContext.SaveChanges();
                        rPrj.SaveData(radanProjectName);
                    }

                    // need to open current project here...
                    string errMessage = "";
                    string prjName = radInterface.getOpenProjectName(ref errMessage);
                    radInterface.LoadProject(prjName);

                    string path = barEditRadanProject.EditValue.ToString();
                    rPrj.SaveData(path);

                    radInterface.SaveNest(ref errMessage);  // save open nest again so quantities start updating right away.
                    
                    gridViewItems.RefreshData();

                    SplashScreenManager.HideImage();
                }
                else
                {
                    MessageBox.Show("The Radan project that is open does not match the project in RadanMaster.");
                }
            }
            else
            {
                MessageBox.Show("Radan Is Not Ready");
            }
        }

        private void barButtonRetrieveSelectionFromRadan_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (radInterface.IsProjectReady())
            {
                string errMsg = "";
                if (radInterface.getOpenProjectName(ref errMsg) == radanProjectName)
                {
                    SplashScreenManager.ShowForm(this, typeof(WaitForm1), true, true, false);
                    if (saveRadan())
                    {

                        RetrieveSelectedRadanPartToMasterList();
                        gridViewItems.RefreshData();

                    }
                    SplashScreenManager.HideImage();
                }

                else
                {
                    MessageBox.Show("The Radan project that is open does not match the project in RadanMaster.");
                }
            }
            else
            {
                MessageBox.Show("Radan Is Not Ready");
            }

        }

        private void barButtonRetrieveAll_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (radInterface.IsProjectReady())
            {
                string errMsg = "";
                if (radInterface.getOpenProjectName(ref errMsg) == radanProjectName)
                {
                    SplashScreenManager.ShowForm(this, typeof(WaitForm1), true, true, false);
                    if (saveRadan())
                    {
                        RetrieveAllRadanPartToMasterList();

                        SplashScreenManager.HideImage();
                    }
                    else
                    {
                        MessageBox.Show("The Radan project that is open does not match the project in RadanMaster.");
                    }
                }
                else
                {
                    MessageBox.Show("Radan Is Not Ready");
                }
            }
        }

        private void barButtonItemAdd_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (!radInterface.IsActive())
            {
                MessageBox.Show("Connect to Radan before importing in order to calculate whether the part requires bending or not.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
            string addItemFileName = "";
            Order newOrder = new Order();
            // Show the dialog and get result.
            openFileDialogAddItem.InitialDirectory = (string)AppSettings.AppSettings.Get("SymFilePath");
            openFileDialogAddItem.Filter = "sym files (*.sym) | *.sym";
            DialogResult result = openFileDialogAddItem.ShowDialog();
            if (result == DialogResult.OK) // Test result.
            {
                addItemFileName = (openFileDialogAddItem.FileName);
                RadanInterface radanInterface = new RadanInterface();
                AddItemDialog.AddItem addItemDialog = new AddItemDialog.AddItem(dbContext,addItemFileName);
                DialogResult addItemResult = addItemDialog.ShowDialog();

                if (addItemResult == DialogResult.OK)
                {
                    
                    //radanInterface.Initialize();

                    string name = System.IO.Path.GetFileNameWithoutExtension(addItemFileName);
                    string description = radanInterface.GetDescriptionFromSym(addItemFileName);
                    string thicknessStr = radanInterface.GetThicknessFromSym(addItemFileName);
                    string thicknessUnit = radanInterface.GetThicknessUnitsFromSym(addItemFileName);
                    double thickness = double.Parse(thicknessStr);
                    if (thicknessUnit == "mm") thickness = thickness / 25.4;
                    thickness = NormalizeThickness(thickness);
                    string material = radanInterface.GetMaterialTypeFromSym(addItemFileName);
                    char[] thumbnailCharArray = radanInterface.GetThumbnailDataFromSym(addItemFileName);
                    byte[] thumbnailByteArray = Convert.FromBase64CharArray(thumbnailCharArray, 0, thumbnailCharArray.Length);
                    bool hasBends = false;
                    if (AddItemDialog.AddItem.checkForBends)
                        hasBends = radanInterface.HasBends(addItemFileName);


                    Part newPart = dbContext.Parts.Where(p => p.FileName == name).FirstOrDefault();
                    if (newPart == null)
                    {
                        newPart = new Part();
                        newPart.FileName = name;
                        newPart.Description = description;
                        newPart.Thickness = thickness;
                        newPart.Material = material;
                        newPart.Thumbnail = thumbnailByteArray;
                        newPart.HasBends = hasBends;

                        dbContext.Parts.Add(newPart);
                        dbContext.SaveChanges();
                    }
                    else
                    {
                        // update properties if needed
                        newPart.Description = description;
                        newPart.Thickness = thickness;
                        newPart.Material = material;
                        newPart.Thumbnail = thumbnailByteArray;
                        newPart.HasBends = hasBends;
                        dbContext.SaveChanges();
                    }

                    if (AddItemDialog.AddItem.lastOrderNumber != "")
                    {
                        newOrder = dbContext.Orders.Where(o => o.OrderNumber == AddItemDialog.AddItem.lastOrderNumber).FirstOrDefault();
                    }
                    if (AddItemDialog.AddItem.lastBatchName != "")
                    {
                        newOrder = dbContext.Orders.Where(o => o.BatchName == AddItemDialog.AddItem.lastBatchName).FirstOrDefault();
                    }

                    if (newOrder == null || newOrder.BatchName == null)
                    {
                        newOrder = new Order();
                        newOrder.IsComplete = false;
                        newOrder.DueDate = DateTime.Now;
                        newOrder.EntryDate = DateTime.Now;
                        newOrder.OrderItems = new List<OrderItem>();
                        newOrder.OrderNumber = AddItemDialog.AddItem.lastOrderNumber.TrimEnd();
                        newOrder.ScheduleName = AddItemDialog.AddItem.lastSchedName.TrimEnd();
                        newOrder.BatchName = AddItemDialog.AddItem.lastBatchName.TrimEnd();
                        if (newOrder.BatchName == null) newOrder.BatchName = "";
                        newOrder.IsBatch = AddItemDialog.AddItem.isBatch;

                        dbContext.Orders.Add(newOrder);
                        dbContext.SaveChanges();

                    }

                    OrderItem newItem = dbContext.OrderItems.Where(oitem => oitem.Order.OrderNumber == AddItemDialog.AddItem.lastOrderNumber).Where(oitem => oitem.Part.FileName == name)
                                                            .Where(oitem => oitem.Order.BatchName == AddItemDialog.AddItem.lastBatchName).FirstOrDefault();

                    //OrderItem newItem = dbContext.OrderItems.Where(oitem => oitem.Order == newOrder).Where(oitem => oitem.Part.FileName == name).FirstOrDefault();
                    if (newItem == null)
                    {
                        newItem = new OrderItem();
                        newItem.IsComplete = false;
                        newItem.Order = newOrder;
                        newItem.Part = newPart;
                        newItem.QtyRequired = int.Parse(AddItemDialog.AddItem.qty);
                        newItem.QtyNested = 0;
                        newItem.Notes = AddItemDialog.AddItem.notes;

                        dbContext.OrderItems.Add(newItem);
                        dbContext.SaveChanges();
                    }
                    else
                    {
                        MessageBox.Show("This Order Item has already been entered. It will not be entered again");
                    }
                }
            }
        }

        private void barButtonItemImport_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            string importFileName = "";
            // Show the dialog and get result.
            openFileDialogImport.Filter = "xml files (*.xml) | *.xml";
            DialogResult result = openFileDialogImport.ShowDialog();
            if (result == DialogResult.OK) // Test result.
            {
                importFileName = (openFileDialogImport.FileName);

                SplashScreenManager.ShowForm(this, typeof(WaitForm1), true, true, false);
                importXmlFile(importFileName);
                SplashScreenManager.HideImage();
            }
        }

        private void barButtonBrowseRadanProject_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            openFileDialogProject.Filter = "rpd files (*.rpd) | *.rpd";
            DialogResult result = openFileDialogProject.ShowDialog();
            if (result == DialogResult.OK) // Test result.
            {
                string path = openFileDialogProject.FileName;

                radanProjectName = openFileDialogProject.FileName;
                barEditRadanProjectBrowse.EditValue = radanProjectName;

                rPrj = rPrj.LoadData(path);
            }
        }

        private void barButtonUpdateFromRadan_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (radInterface.IsProjectReady())
            {
                string errMsg = "";
                if (radInterface.getOpenProjectName(ref errMsg) == radanProjectName)
                {
                    SplashScreenManager.ShowForm(this, typeof(WaitForm1), true, true, false);
                    SyncRadanToMaster();
                    SplashScreenManager.HideImage();
                }
                else
                {
                    MessageBox.Show("The Radan project that is open does not match the project in RadanMaster.");
                }
            }
            else
            {
                MessageBox.Show("Radan Is Not Ready");
            }
        }

        private void barButtonItem1_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (radInterface.IsProjectReady())
            {
                string errMsg = "";
                if (radInterface.getOpenProjectName(ref errMsg) == radanProjectName)
                {
                    if (XtraMessageBox.Show("Are you sure you want to clear this Radan Project and initialize a new empty one?", "Confirmation", MessageBoxButtons.YesNo) != DialogResult.No)
                        CreateNewRadanProject();
                }
                else
                {
                    MessageBox.Show("The Radan project that is open does not match the project in RadanMaster.");
                }
            }
            else
            {
                MessageBox.Show("Radan Is Not Ready");
            }
        }

        private void barEditRadanProjectBrowse_EditValueChanged(object sender, EventArgs e)
        {
            AppSettings.AppSettings.Set("RadanProjectPathAndFile", barEditRadanProjectBrowse.EditValue);
            RefreshItemsGridFromActiveProject();
        }

        private void gridViewItems_RowClick(object sender, RowClickEventArgs e)
        {
            List<Nest> filteredNests = new List<Nest>();
            List<Nest> filteredNestsSubList = new List<Nest>();

            int[] rows = gridViewItems.GetSelectedRows();

            OrderItem selectedItem = (OrderItem)gridViewItems.GetRow(rows[0]);

            if (selectedItem != null && rows.Count() == 1 && rows[0] >= 0)
            {
                List<DisplayNest> displayNests = new List<DisplayNest>();


                // first find all the associated nests saved in DB that contain this part
                if (selectedItem.AssociatedNests != null)
                {
                    foreach (Nest nest in selectedItem.AssociatedNests)
                    {
                        DisplayNest displayNest = new DisplayNest();
                        displayNest.NestName = nest.nestName;
                        displayNest.NestPath = nest.nestPath;
                        NestedParts nestedPart = nest.NestedParts.Where(np => np.OrderItem == selectedItem).FirstOrDefault();
                        displayNest.QtyOnNest = nestedPart.Qty;
                        displayNest.Thumbnail = nest.Thumbnail;
                        displayNests.Add(displayNest);
                    }

                }

                // now find all the nests in the radan project that this part is on
                if (selectedItem.RadanIDNumber != 0)
                {
                    int radanID = selectedItem.RadanIDNumber;

                    foreach (RadanPart radanPart in rPrj.Parts.Part)
                    {
                        if (radanPart.Bin == radanID.ToString())
                        {
                            foreach (SheetUsedInNest nst in radanPart.UsedInNests)
                            {
                                int nestID = (int)nst.ID;
                                RadanNest radanNest = rPrj.Nests.Where(n => n.ID == nestID.ToString()).FirstOrDefault();
                                DisplayNest displayNest = new DisplayNest();
                                displayNest.NestName = radanNest.FileName;
                                displayNest.NestPath = Path.GetDirectoryName(radanProjectName);
                                displayNest.QtyOnNest = nst.Made;

                                char[] thumbnailCharArray = radInterface.GetThumbnailDataFromSym(displayNest.NestPath + "\\Nests\\" + displayNest.NestName);
                                byte[] thumbnailByteArray = null;
                                if (thumbnailCharArray != null)
                                    thumbnailByteArray = Convert.FromBase64CharArray(thumbnailCharArray, 0, thumbnailCharArray.Length);

                                displayNest.Thumbnail = thumbnailByteArray;

                                displayNests.Add(displayNest);
                            }
                        }
                    }
                }

                gridControlNests.DataSource = new System.ComponentModel.BindingList<RadanMaster.Models.DisplayNest>();
                gridControlNests.DataSource = displayNests;
                gridViewNests.Columns["NestPath"].SortOrder = DevExpress.Data.ColumnSortOrder.Descending;
                //gridViewNests.Columns["NestName"].SortOrder = DevExpress.Data.ColumnSortOrder.Ascending;


            }




            gridViewNests.RefreshData();
        }

        private void gridControlItems_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            GridHitInfo hinfo = gridViewItems.CalcHitInfo(e.Location);
            if (hinfo.Column == gridViewItems.Columns[1])
            {
                gridViewItems.Columns[1].OptionsColumn.AllowEdit = true;
                gridViewItems.ShowEditor();
            }
            ((DXMouseEventArgs)e).Handled = true;
        }

        private void gridViewItems_HiddenEditor(object sender, EventArgs e)
        {
            gridViewItems.Columns[1].OptionsColumn.AllowEdit = false;
        }

        private void gridViewItems_CustomUnboundColumnData(object sender, CustomColumnDataEventArgs e)
        {
            if (e.Column.FieldName == "calcQtyNested")
            {
                int origQtyNested = 0;

                if (rPrj == null)
                    e.Value = "??";
                else
                {
                    int totalNested = 0;
                    OrderItem calcItem = (OrderItem)e.Row;
                    origQtyNested = calcItem.QtyNested;

                    if (calcItem.RadanIDNumber != 0)
                    {
                        int radanID = calcItem.RadanIDNumber;

                        foreach (RadanPart radanPart in rPrj.Parts.Part)
                        {
                            if (radanPart.Bin == radanID.ToString())
                            {
                                totalNested += radanPart.Made;
                            }
                        }
                    }

                    e.Value = calcItem.QtyNested += totalNested;

                    // for some reason e.Value gets written to calcItem.QtyNested.
                    //    because this event fires multiple times, the quantity nested gets incremented multiple times.
                    //    This is not what we want so the following line is a work around.
                    calcItem.QtyNested = origQtyNested;

                    if ((int)e.Value >= calcItem.QtyRequired)
                        calcItem.IsComplete = true;
                    else
                        calcItem.IsComplete = false;
                }
            }

            if (e.Column.FieldName == "calcRemaining")
            {
                int origQtyNested = 0;

                if (rPrj == null)
                    e.Value = "??";
                else
                {
                    int totalNested = 0;
                    OrderItem calcItem = (OrderItem)e.Row;
                    origQtyNested = calcItem.QtyNested;

                    if (calcItem.RadanIDNumber != 0)
                    {
                        int radanID = calcItem.RadanIDNumber;

                        foreach (RadanPart radanPart in rPrj.Parts.Part)
                        {
                            if (radanPart.Bin == radanID.ToString())
                            {
                                totalNested += radanPart.Made;
                            }
                        }
                    }

                    e.Value = calcItem.QtyRequired - (calcItem.QtyNested += totalNested);

                    // for some reason e.Value gets written to calcItem.QtyNested.
                    //    because this event fires multiple times, the quantity nested gets incremented multiple times.
                    //    This is not what we want so the following line is a work around.
                    calcItem.QtyNested = origQtyNested;

                    //if ((int)e.Value >= calcItem.QtyRequired)
                    //    calcItem.IsComplete = true;
                    //else
                    //    calcItem.IsComplete = false;
                }
            }
        }

        private void barButtonItemConnectToRadan_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            radInterface = new RadanInterface();
            if (radInterface.Initialize())
            {
                toolStripStatusLabel1.Text = "Connected to Radan";
                logger.Info("Connected to Radan.");
            }
            else
            {
                toolStripStatusLabel1.Text = "Could not connect to Radan";
                logger.Info("Could not connect to Radan.");
            }
        }

        private void gridViewItems_RowCellStyle(object sender, RowCellStyleEventArgs e)
        {
            GridView view = sender as GridView;
            if (e.Column.FieldName == "calcRemaining")
            {

                int qtyRequired = (int)view.GetRowCellValue(e.RowHandle, "QtyRequired");

                int qtyNested = 0;
                if (e.CellValue != null && e.CellValue.ToString() != "??")
                    qtyNested = (int)view.GetRowCellValue(e.RowHandle, "calcQtyNested");

                if (qtyNested == qtyRequired)
                    e.Appearance.BackColor = Color.Green;
                else if (qtyNested > 0 && qtyNested < qtyRequired)
                    e.Appearance.BackColor = Color.Yellow;
                else if (qtyNested > qtyRequired)
                    e.Appearance.BackColor = Color.Red;
            }
        }

        private void gridViewItems_PopupMenuShowing(object sender, PopupMenuShowingEventArgs e)
        {
            GridView view = sender as GridView;
            if (e.MenuType == DevExpress.XtraGrid.Views.Grid.GridMenuType.Row)
            {
                List<int> selectedItemHandles = new List<int>();
                selectedItemHandles = gridViewItems.GetSelectedRows().ToList();

                List<OrderItem> selectedItems = new List<OrderItem>();

                foreach (int handle in selectedItemHandles)
                {
                    selectedItems.Add((OrderItem)gridViewItems.GetRow(handle));
                }

                e.Menu.Items.Clear();
                DXMenuItem updateItem = new DXMenuItem("Update Thumbnail", OnUpdateThumbnailClick);
                updateItem.Tag = selectedItems;
                e.Menu.Items.Add(updateItem);

                DXMenuItem convertItem = new DXMenuItem("Retrieve From Vault", OnRetrieveFromVaultClick);
                convertItem.Tag = selectedItems;
                e.Menu.Items.Add(convertItem);
            }
        }

        void OnUpdateThumbnailClick(object sender, EventArgs e)
        {
            {
                DXMenuItem menuItem = sender as DXMenuItem;

                List<OrderItem> selectedItems = (List<OrderItem>)menuItem.Tag;

                foreach (OrderItem item in selectedItems)
                {
                    updateThumbnail(item);
                }
            }
        }

        void updateThumbnail(OrderItem item)
        {
            string fileName = symFolder + "\\" + item.Part.FileName + ".sym";

            RadanInterface radanInterface = new RadanInterface();
            char[] thumbnailCharArray = radanInterface.GetThumbnailDataFromSym(fileName);
            if (thumbnailCharArray != null)
            {
                byte[] thumbnailByteArray = Convert.FromBase64CharArray(thumbnailCharArray, 0, thumbnailCharArray.Length);
                item.Part.Thumbnail = thumbnailByteArray;
            }
            else
            {
                item.Part.Thumbnail = null;
            }

            gridViewItems.RefreshData();

            dbContext.SaveChanges();
        }

        void OnRetrieveFromVaultClick(object sender, EventArgs e)
        {
            try
            {
                if (saveRadan())
                {
                    SplashScreenManager.ShowForm(this, typeof(WaitForm1), true, true, false);

                    VaultAccess.VaultAccess va = new VaultAccess.VaultAccess();

                    DXMenuItem menuItem = sender as DXMenuItem;
                    List<OrderItem> selectedItems = (List<OrderItem>) menuItem.Tag;

                    foreach (OrderItem item in selectedItems)
                    {
                        string fileToSearch = item.Part.FileName;
                        string tempFolder = Path.Combine(Path.GetTempPath(), "RadanMaster");
                        new FileInfo(tempFolder).Directory.Create();        // create folder if it doesn't exist

                        string vaultUserName = (string)AppSettings.AppSettings.Get("VaultUserName");
                        string vaultPassword = (string)AppSettings.AppSettings.Get("VaultPassword");
                        string vaultServer = (string)AppSettings.AppSettings.Get("VaultServer");
                        string vaultName = (string)AppSettings.AppSettings.Get("VaultName");

                        string loginResult = va.LoginWithResults(vaultUserName, vaultPassword, vaultServer, vaultName);
                        if (loginResult!="")
                        {
                            SplashScreenManager.HideImage();
                            MessageBox.Show(loginResult);
                            return;
                        }

                        if (!va.searrchAndDownloadFile(fileToSearch, tempFolder))
                        {
                            SplashScreenManager.HideImage();
                            MessageBox.Show("Error in retrieving file from Vault");
                            return;
                        }

                        string partName = "";
                        string partThickness = "";
                        string topPattern = "";
                        string materialName = "";
                        string errorMsg = "";
                        string symFileToSave = Path.Combine(symFolder, item.Part.FileName + ".sym");

                        radInterface.Open3DFileInRadan(Path.Combine(tempFolder, item.Part.FileName + ".ipt"), materialName, ref errorMsg);
                        if (errorMsg != "")
                        {
                            SplashScreenManager.HideImage();
                            MessageBox.Show(errorMsg);
                            return;
                        }
                        radInterface.UnfoldActive3DFile(ref partName, ref materialName, ref partThickness, ref topPattern, ref errorMsg);
                        if (errorMsg != "")
                        {
                            SplashScreenManager.HideImage();
                            MessageBox.Show(errorMsg);
                            return;
                        }
                        radInterface.SavePart(topPattern, symFileToSave, ref errorMsg);
                        if (errorMsg != "")
                        {
                            SplashScreenManager.HideImage();
                            MessageBox.Show(errorMsg);
                            return;
                        }
                        radInterface.InsertAttributes(symFileToSave, materialName, partThickness, "in", "", ref errorMsg);
                        if (errorMsg != "")
                        {
                            SplashScreenManager.HideImage();
                            MessageBox.Show(errorMsg);
                            return;
                        }

                        updateThumbnail(item);
                    }

                    SplashScreenManager.HideImage();
                   

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in retrieving file from Vault.");
                SplashScreenManager.HideImage();
            }

        }

        private void gridControlItems_DragDrop(object sender, DragEventArgs e)
        {

            string addItemFileName = "";
            RadanInterface radanInterface = new RadanInterface();
            Order newOrder = new Order();

            string[] FileDrop = (e.Data.GetData(DataFormats.FileDrop) as string[]);

            foreach (string fileDrop in FileDrop)
            {
                //Each "fileDrop" is a path to a file. Add it to your GridControl's data source  
                //as required  
                string s = fileDrop;

                addItemFileName = fileDrop;

                AddItemDialog.AddItem addItemDialog = new AddItemDialog.AddItem(dbContext, addItemFileName);
                DialogResult addItemResult = addItemDialog.ShowDialog();
                if (addItemResult == DialogResult.OK)
                {

                    string name = System.IO.Path.GetFileNameWithoutExtension(addItemFileName);
                    string description = radanInterface.GetDescriptionFromSym(addItemFileName);
                    string thicknessStr = radanInterface.GetThicknessFromSym(addItemFileName);
                    string thicknessUnit = radanInterface.GetThicknessUnitsFromSym(addItemFileName);
                    double thickness = double.Parse(thicknessStr);
                    if (thicknessUnit == "mm") thickness = thickness / 25.4;
                    thickness = NormalizeThickness(thickness);
                    string material = radanInterface.GetMaterialTypeFromSym(addItemFileName);
                    char[] thumbnailCharArray = radanInterface.GetThumbnailDataFromSym(addItemFileName);
                    byte[] thumbnailByteArray = Convert.FromBase64CharArray(thumbnailCharArray, 0, thumbnailCharArray.Length);
                    bool hasBends = false;
                    if (AddItemDialog.AddItem.checkForBends)
                        hasBends = radanInterface.HasBends(addItemFileName);


                    Part newPart = dbContext.Parts.Where(p => p.FileName == name).FirstOrDefault();
                    if (newPart == null)
                    {
                        newPart = new Part();
                        newPart.FileName = name;
                        newPart.Description = description;
                        newPart.Thickness = thickness;
                        newPart.Material = material;
                        newPart.Thumbnail = thumbnailByteArray;
                        newPart.HasBends = hasBends;

                        dbContext.Parts.Add(newPart);
                        dbContext.SaveChanges();
                    }
                    else
                    {
                        // update properties if needed
                        newPart.Description = description;
                        newPart.Thickness = thickness;
                        newPart.Material = material;
                        newPart.Thumbnail = thumbnailByteArray;
                        newPart.HasBends = hasBends;
                        dbContext.SaveChanges();
                    }

                    if (AddItemDialog.AddItem.lastOrderNumber != "")
                    {
                        newOrder = dbContext.Orders.Where(o => o.OrderNumber == AddItemDialog.AddItem.lastOrderNumber).FirstOrDefault();
                    }
                    if (AddItemDialog.AddItem.lastBatchName != "")
                    {
                        newOrder = dbContext.Orders.Where(o => o.BatchName == AddItemDialog.AddItem.lastBatchName).FirstOrDefault();
                    }

                    if (newOrder == null || newOrder.BatchName == null)
                    {
                        newOrder = new Order();
                        newOrder.IsComplete = false;
                        newOrder.DueDate = DateTime.Now;
                        newOrder.EntryDate = DateTime.Now;
                        newOrder.OrderItems = new List<OrderItem>();
                        newOrder.OrderNumber = AddItemDialog.AddItem.lastOrderNumber.TrimEnd();
                        newOrder.ScheduleName = AddItemDialog.AddItem.lastSchedName.TrimEnd();
                        newOrder.BatchName = AddItemDialog.AddItem.lastBatchName.TrimEnd();
                        if (newOrder.BatchName == null) newOrder.BatchName = "";
                        newOrder.IsBatch = AddItemDialog.AddItem.isBatch;

                        dbContext.Orders.Add(newOrder);
                        dbContext.SaveChanges();

                    }

                    OrderItem newItem = dbContext.OrderItems.Where(oitem => oitem.Order.OrderNumber == AddItemDialog.AddItem.lastOrderNumber).Where(oitem => oitem.Part.FileName == name)
                                                            .Where(oitem => oitem.Order.BatchName == AddItemDialog.AddItem.lastBatchName).FirstOrDefault();

                    //OrderItem newItem = dbContext.OrderItems.Where(oitem => oitem.Order == newOrder).Where(oitem => oitem.Part.FileName == name).FirstOrDefault();
                    if (newItem == null)
                    {
                        newItem = new OrderItem();
                        newItem.IsComplete = false;
                        newItem.Order = newOrder;
                        newItem.Part = newPart;
                        newItem.QtyRequired = int.Parse(AddItemDialog.AddItem.qty);
                        newItem.QtyNested = 0;
                        newItem.Notes = AddItemDialog.AddItem.notes;

                        dbContext.OrderItems.Add(newItem);
                        dbContext.SaveChanges();
                    }
                    else
                    {
                        MessageBox.Show("This Order Item has already been entered. It will not be entered again");
                    }
                }
            }
        }

        private void gridControlItems_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) == false)
            {
                e.Effect = DragDropEffects.None;
            }
            else
            {
                //Make sure the user is only dropping sym files 
                string[] fileDrop = (e.Data.GetData(DataFormats.FileDrop) as string[]);

                foreach (string file in fileDrop)
                {
                    if (file.EndsWith(".sym"))
                        e.Effect = DragDropEffects.Copy;
                    else
                    {
                        e.Effect = DragDropEffects.None;
                        return;
                    }
                }

                return;

            }
        }
        #endregion

        #region sorting and filtering

        private void barToggleSwitchShowBatches_CheckedChanged(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (barToggleSwitchShowBatches.Checked && barToggleSwitchShowOrders.Checked)
            {
                gridViewItems.Columns["Order.IsBatch"].ClearFilter();
                groupAndFilterSettings.ShowBatches = true;
                groupAndFilterSettings.ShowOrders = true;
            }
            else if (barToggleSwitchShowBatches.Checked && !barToggleSwitchShowOrders.Checked)
            {
                gridViewItems.Columns["Order.IsBatch"].FilterInfo =
                    new ColumnFilterInfo("[Order.IsBatch] == true");
                groupAndFilterSettings.ShowBatches = true;
                groupAndFilterSettings.ShowOrders = false;
            }
            else if (!barToggleSwitchShowBatches.Checked && barToggleSwitchShowOrders.Checked)
            {
                gridViewItems.Columns["Order.IsBatch"].FilterInfo =
                    new ColumnFilterInfo("[Order.IsBatch] == false");
                groupAndFilterSettings.ShowBatches = false;
                groupAndFilterSettings.ShowOrders = true;
            }

        }

        private void barToggleShowRadan_CheckedChanged(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (barToggleShowRadan.Checked)
            {
                gridViewItems.Columns["IsInProject"].FilterInfo =
                    new ColumnFilterInfo("[IsInProject] == true");
                groupAndFilterSettings.ShowOnlyItemsInRadan = true;
            }
            else
            {
                gridViewItems.Columns["IsInProject"].ClearFilter();
                groupAndFilterSettings.ShowOnlyItemsInRadan = false;

            }
        }

        private void barToggleSwitchShowOrders_CheckedChanged(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (barToggleSwitchShowBatches.Checked && barToggleSwitchShowOrders.Checked)
            {
                gridViewItems.Columns["Order.IsBatch"].ClearFilter();
                groupAndFilterSettings.ShowBatches = true;
                groupAndFilterSettings.ShowOrders = true;
            }
            else if (barToggleSwitchShowBatches.Checked && !barToggleSwitchShowOrders.Checked)
            {
                gridViewItems.Columns["Order.IsBatch"].FilterInfo =
                    new ColumnFilterInfo("[Order.IsBatch] == true");
                groupAndFilterSettings.ShowBatches = true;
                groupAndFilterSettings.ShowOrders = false;
            }
            else if (!barToggleSwitchShowBatches.Checked && barToggleSwitchShowOrders.Checked)
            {
                gridViewItems.Columns["Order.IsBatch"].FilterInfo =
                    new ColumnFilterInfo("[Order.IsBatch] == false");
                groupAndFilterSettings.ShowBatches = false;
                groupAndFilterSettings.ShowOrders = true;
            }
           
        }

        private void barToggleShowComplete_CheckedChanged(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (!barToggleShowComplete.Checked)
            {
                gridViewItems.Columns["IsComplete"].FilterInfo =
                    new ColumnFilterInfo("[IsComplete] == false");
                groupAndFilterSettings.ShowComplete = false;
            }
            else
            {
                gridViewItems.Columns["IsComplete"].ClearFilter();
                groupAndFilterSettings.ShowComplete = true;
            }
        }

        private void barToggleSwitchGroup1_CheckedChanged(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // group by batch and thickness
            if (barToggleSwitchGroup1.Checked)
            {
                gridViewItems.Columns["Part.Thickness"].GroupIndex = 1;
                gridViewItems.Columns["Order.BatchName"].GroupIndex = 2;
                
                gridViewItems.Columns["Order.BatchName"].SortOrder = DevExpress.Data.ColumnSortOrder.Descending;

                gridViewItems.Columns["Order.IsBatch"].FilterInfo =
                   new ColumnFilterInfo("[Order.IsBatch] == true");
                groupAndFilterSettings.ShowBatches = true;
                groupAndFilterSettings.ShowOrders = false;

                groupAndFilterSettings.GroupByBatchAndThickness = true;
                groupAndFilterSettings.GroupByScheduleAndThickness = false;
            }
            else
            {
                gridViewItems.Columns["Order.BatchName"].GroupIndex = -1;
                gridViewItems.Columns["Part.Thickness"].GroupIndex = -1;

                gridViewItems.Columns["Order.IsBatch"].FilterInfo =
                   new ColumnFilterInfo("[Order.IsBatch] == 'false'");
                groupAndFilterSettings.ShowBatches = false;
                groupAndFilterSettings.ShowOrders = true;

                gridViewItems.Columns["Part.Thickness"].GroupIndex = 1;
                gridViewItems.Columns["Order.ScheduleName"].GroupIndex = 2;
                
                gridViewItems.Columns["Order.ScheduleName"].SortOrder = DevExpress.Data.ColumnSortOrder.Descending;

                gridViewItems.Columns["Order.IsBatch"].FilterInfo =
                   new ColumnFilterInfo("[Order.IsBatch] == false");
                groupAndFilterSettings.ShowBatches = false;
                groupAndFilterSettings.ShowOrders = true;

                groupAndFilterSettings.GroupByBatchAndThickness = false;
                groupAndFilterSettings.GroupByScheduleAndThickness = true;
            }

            SetGroupAndFilterSwitches();

        }

        private void barToggleSwitchGroup2_CheckedChanged(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // group by schedule and thickness
            if (barToggleSwitchGroup2.Checked)
            {
                gridViewItems.Columns["Part.Thickness"].GroupIndex = 1;
                gridViewItems.Columns["Order.ScheduleName"].GroupIndex = 2;
                
                gridViewItems.Columns["Order.ScheduleName"].SortOrder = DevExpress.Data.ColumnSortOrder.Descending;

                gridViewItems.Columns["Order.IsBatch"].FilterInfo =
                   new ColumnFilterInfo("[Order.IsBatch] == false");
                groupAndFilterSettings.ShowBatches = false;
                groupAndFilterSettings.ShowOrders = true;

                groupAndFilterSettings.GroupByBatchAndThickness = false;
                groupAndFilterSettings.GroupByScheduleAndThickness = true;
            }
            else
            {
                gridViewItems.Columns["Order.ScheduleName"].GroupIndex = -1;
                gridViewItems.Columns["Part.Thickness"].GroupIndex = -1;

                gridViewItems.Columns["Order.IsBatch"].FilterInfo =
                   new ColumnFilterInfo("[Order.IsBatch] == true");
                groupAndFilterSettings.ShowBatches = true;
                groupAndFilterSettings.ShowOrders = false;

                gridViewItems.Columns["Part.Thickness"].GroupIndex = 1;
                gridViewItems.Columns["Order.BatchName"].GroupIndex = 2;
                
                gridViewItems.Columns["Order.BatchName"].SortOrder = DevExpress.Data.ColumnSortOrder.Descending;

                gridViewItems.Columns["Order.IsBatch"].FilterInfo =
                   new ColumnFilterInfo("[Order.IsBatch] == true");
                groupAndFilterSettings.ShowBatches = true;
                groupAndFilterSettings.ShowOrders = false;

                groupAndFilterSettings.GroupByBatchAndThickness = true;
                groupAndFilterSettings.GroupByScheduleAndThickness = false;
            }

            SetGroupAndFilterSwitches();
        }

        private void barToggleSwitchShowCompletedOrders_CheckedChanged(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (!barToggleSwitchShowCompletedOrders.Checked)
            {
                gridViewItems.Columns["Order.IsComplete"].FilterInfo =
                    new ColumnFilterInfo("[Order.IsComplete] == false");
                groupAndFilterSettings.ShowAllCompletedOrders = false;
            }
            else
            {
                gridViewItems.Columns["Order.IsComplete"].ClearFilter();
                groupAndFilterSettings.ShowAllCompletedOrders = true;
            }

        }

        private void barCheckItemShowCompletedOrdersFromLastDayOnly_CheckedChanged(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            barCheckItemShowAllCompletedOrders.Checked = !barCheckItemShowCompletedOrdersFromLastDayOnly.Checked;
            groupAndFilterSettings.ShowAllCompletedOrders = barCheckItemShowAllCompletedOrders.Checked;

            if (barCheckItemShowAllCompletedOrders.Checked)
            {
                gridViewItems.Columns["Order.IsComplete"].ClearFilter();

                gridViewItems.Columns["Order.DateCompleted"].ClearFilter();

                gridViewItems.ActiveFilter.Clear();

                barEditNumOfDays.Enabled = false;
            }
            else
            {
                filterOrdersByDate();
            }
        }

        private void barCheckItemShowAllCompletedOrders_CheckedChanged(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            barCheckItemShowCompletedOrdersFromLastDayOnly.Checked = !barCheckItemShowAllCompletedOrders.Checked;
            groupAndFilterSettings.ShowCompletedOrdersFromLastNDays = barCheckItemShowCompletedOrdersFromLastDayOnly.Checked;
        }

        private void filterOrdersByDate()
        {
            barEditNumOfDays.Enabled = true;

            DateTime FromDate;

            int numOfDays = int.Parse(barEditNumOfDays.EditValue.ToString());
            FromDate = DateTime.Now.AddDays(0 - (numOfDays));

            CriteriaOperator expr1 = new BinaryOperator("Order.IsComplete", false, BinaryOperatorType.Equal);
            CriteriaOperator expr2 = new BinaryOperator("Order.DateCompleted", FromDate, BinaryOperatorType.GreaterOrEqual);

            //gridViewItems.ActiveFilterCriteria = GroupOperator.And(new CriteriaOperator[] { GroupOperator.Or(new CriteriaOperator[] { expr1, expr2 }), gridViewItems.ActiveFilterCriteria });

            gridViewItems.ActiveFilterCriteria = GroupOperator.Or(new CriteriaOperator[] { expr1, expr2 });
        }

        private void barEditNumOfDays_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            filterOrdersByDate();
        }

        private void repositoryItemSpinEdit4_EditValueChanged(object sender, EventArgs e)
        {
            ribbonControl1.Manager.ActiveEditItemLink.PostEditor();
            filterOrdersByDate();

            groupAndFilterSettings.NumberOfDays = int.Parse(barEditNumOfDays.EditValue.ToString());
        }

        private void gridViewItems_CustomDrawGroupRow(object sender, RowObjectCustomDrawEventArgs e)
        {
            GridGroupRowInfo row = e.Info as GridGroupRowInfo;

            if (row.GroupText.Contains("Batch Name"))
            {
                int numComplete = 0;
                int numTotal = 0;
                int childCount = gridViewItems.GetChildRowCount(row.RowHandle);

                for (int i = 0; i < childCount; i++)
                {
                    int childRowHandle = gridViewItems.GetChildRowHandle(row.RowHandle, i);
                    object childRow = gridViewItems.GetRow(childRowHandle);

                    OrderItem item = (OrderItem)childRow;
                    numTotal++;
                    // the line below doesn't work when parent item is collapsed
                    //if (item.IsComplete) numComplete++;

                    // alternate code to calculate out parts nested in current project to see whether item is complete or not.
                    //  had trouble that all items showed complete if parent item was collapsed.

                    int origQtyNested = 0;
                    int qtyNested = 0;

                    if (rPrj == null)
                        qtyNested = -1;
                    else
                    {
                        int totalNested = 0;
                        OrderItem calcItem = item;
                        origQtyNested = calcItem.QtyNested;

                        if (calcItem.RadanIDNumber != 0)
                        {
                            int radanID = calcItem.RadanIDNumber;

                            foreach (RadanPart radanPart in rPrj.Parts.Part)
                            {
                                if (radanPart.Bin == radanID.ToString())
                                {
                                    totalNested += radanPart.Made;
                                }
                            }
                        }

                        qtyNested = calcItem.QtyNested += totalNested;

                        // for some reason e.Value gets written to calcItem.QtyNested.
                        //    because this event fires multiple times, the quantity nested gets incremented multiple times.
                        //    This is not what we want so the following line is a work around.
                        calcItem.QtyNested = origQtyNested;

                        if (qtyNested >= calcItem.QtyRequired)
                            calcItem.IsComplete = true;
                        else
                            calcItem.IsComplete = false;
                    }
                    if ((qtyNested >= item.QtyRequired)  || (item.QtyNested >= item.QtyRequired)) numComplete++;
                }

                double percentageDone = 0.0;
                if (numComplete > 0)
                {
                    double ratio = (double)numComplete / numTotal;
                    percentageDone = ratio * 100;
                    
                }
                else
                {
                    percentageDone = 0;
                }

                if (percentageDone > 0 && percentageDone <= 99)
                    e.Appearance.ForeColor = Color.Blue;
                else if (percentageDone == 100)
                    e.Appearance.ForeColor = Color.Green;

                if (!row.GroupText.Contains("%"))
                {
                    row.GroupText += "        " + String.Format("{0:0}", percentageDone) + "%";
                }

            }
        }

        public void SetGroupAndFilterSwitches()
        {
            barToggleSwitchShowBatches.Checked = groupAndFilterSettings.ShowBatches;
            barToggleSwitchShowOrders.Checked = groupAndFilterSettings.ShowOrders;
            barToggleShowComplete.Checked = groupAndFilterSettings.ShowComplete;
            barToggleShowRadan.Checked = groupAndFilterSettings.ShowOnlyItemsInRadan;
            barToggleSwitchGroup1.Checked = groupAndFilterSettings.GroupByBatchAndThickness;
            barToggleSwitchGroup2.Checked = groupAndFilterSettings.GroupByScheduleAndThickness;
            barToggleSwitchShowCompletedOrders.Checked = groupAndFilterSettings.ShowAllCompletedOrders;
            barCheckItemShowCompletedOrdersFromLastDayOnly.Checked = groupAndFilterSettings.ShowCompletedOrdersFromLastNDays;
            barEditNumOfDays.EditValue = groupAndFilterSettings.NumberOfDays;
        }

        #endregion

        #region Nests
        private void gridViewNests_PopupMenuShowing(object sender, PopupMenuShowingEventArgs e)
        {
            GridView view = sender as GridView;
            if (e.MenuType == DevExpress.XtraGrid.Views.Grid.GridMenuType.Row)
            {
                int rowHandle = e.HitInfo.RowHandle;

                if (rowHandle >= 0)
                {
                    DisplayNest currentNest = (DisplayNest)gridViewNests.GetRow(rowHandle);
                    e.Menu.Items.Clear();
                    DXMenuItem nestToDelete = new DXMenuItem("Delete Nest", OnDeleteNestClick);
                    nestToDelete.Tag = currentNest;
                    e.Menu.Items.Add(nestToDelete);
                }

            }
        }

        void OnDeleteNestClick(object sender, EventArgs e)
        {
            try
            {
                if (XtraMessageBox.Show("Are you sure you want to delete this nest?", "Confirmation", MessageBoxButtons.YesNo) != DialogResult.No)
                {
                    MessageBox.Show("Please note that the sheet quantities in the Radan project will have to be updated manually.\nAlso note that the actual nest will not be deleted in Radan, it will only be removed from the database\n");

                    DXMenuItem menuItem = sender as DXMenuItem;

                    DisplayNest displayNest = (DisplayNest)menuItem.Tag;

                    Nest nestToDelete = dbContext.Nests.Where(n => n.nestName == displayNest.NestName).Where(n => n.nestPath == displayNest.NestPath).FirstOrDefault();

                    if (nestToDelete == null)
                    {
                        MessageBox.Show("This nest has not been saved to the database yet.  Please remove it in the Radan project.");
                    }
                    else
                    {
                        // find all the nested parts associated with this nest
                        List<NestedParts> nestedPartsToRemove = nestToDelete.NestedParts.ToList();

                        // remove all the nested parts from the nest and from the db context
                        foreach (NestedParts nestedPart in nestedPartsToRemove)
                        {
                            // find the order item associated to this nested part
                            OrderItem item = nestedPart.OrderItem;
                            // adjust nested qty
                            item.QtyNested -= nestedPart.Qty;
                            // remove the association
                            item.AssociatedNests.Remove(nestToDelete);
                            // if item was complete before, it no longer is...
                            item.IsComplete = false;
                            item.Order.IsComplete = false;
                            // remove the nested part from the nest
                            nestToDelete.NestedParts.Remove(nestedPart);
                            // and from the db context
                            dbContext.NestedParts.Remove(nestedPart);
                        }

                        // then remove the nest
                        dbContext.Nests.Remove(nestToDelete);

                        dbContext.SaveChanges();

                        gridViewItems.RefreshData();
                        gridViewNests.RefreshData();
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in deleting nest.  Database has not been modified." + "\n" +
                                ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                // reset dbContext to original state if anything goes wrong.
                foreach (DbEntityEntry entry in dbContext.ChangeTracker.Entries())
                {
                    switch (entry.State)
                    {
                        case EntityState.Modified:
                            entry.State = EntityState.Unchanged;
                            break;
                        case EntityState.Added:
                            entry.State = EntityState.Detached;
                            break;
                        case EntityState.Deleted:
                            entry.Reload();
                            break;
                        default: break;
                    }
                }
            }

        }




        #endregion

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // save the grid and filter settings on the toolbar
            groupAndFilterSettings.SaveSettingsToFile();

            // save the layout 
            string SettingsFilePath = new FileInfo(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)) + @"\RadanMaster\ItemsGridLayout.xml";
            gridControlItems.MainView.SaveLayoutToXml(SettingsFilePath, OptionsLayoutBase.FullLayout);

            //save the expanded/contracted state of grouped rows
            helper.SaveViewInfo();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // first delete all the temp files from the previous run
            string tempDir = System.IO.Path.GetTempPath() + @"\RadanMaster\";
            if (Directory.Exists(tempDir))
            {
                DirectoryInfo dir = new DirectoryInfo(tempDir);
                foreach (FileInfo f in dir.GetFiles())
                {
                    System.IO.File.SetAttributes(f.FullName, FileAttributes.Normal);
                    f.Delete();
                }
            }
        }

        #region Assembly Attribute Accessors

        public string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            }
        }

        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public string AssemblyDescription
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        public string AssemblyProduct
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        public string AssemblyCopyright
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        public string AssemblyCompany
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }
        #endregion

        
    }
}

