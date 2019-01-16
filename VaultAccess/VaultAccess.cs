using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vault = Autodesk.DataManagement.Client.Framework.Vault;
using Autodesk.Connectivity.WebServices;
using System.Windows.Forms;
using ACW = Autodesk.Connectivity.WebServices;
using System.IO;
using VDF = Autodesk.DataManagement.Client.Framework;
using System.Threading.Tasks;
using System.Diagnostics;
using Autodesk.Connectivity.Explorer.Extensibility;
using System.Threading;

namespace VaultAccess
{
    public class VaultAccess
    {
        #region Member Variables
        private Vault.Currency.Connections.Connection m_conn { get; set; }
        private Vault.Currency.Entities.Folder m_lastAccessedFolder { get; set; }
        private List<string> m_downloadedFiles { get; set; }
        private string m_PDFPath { get; set; }
        private string m_pdfPrinterName { get; set; }
        private string m_psToPdfProgName { get; set; }
        private string m_ghostScriptWorkingFolder { get; set; }

        #endregion

        #region Constructors and Initialization Methods

        public VaultAccess()
        {
            m_conn = null;
            m_lastAccessedFolder = null;
            m_downloadedFiles = new List<string>();
            m_PDFPath = "";
        }
        public VaultAccess(string pdfPath, string pdfPrinterName, string psToPdfProgName, string ghostScriptWorkingFolder)
        {
            m_conn = null;
            m_lastAccessedFolder = null;
            m_downloadedFiles = new List<string>();
            m_PDFPath = pdfPath;
            m_pdfPrinterName = pdfPrinterName;
            m_psToPdfProgName = psToPdfProgName;
            m_ghostScriptWorkingFolder = ghostScriptWorkingFolder;
        }

        public VaultAccess(Vault.Currency.Connections.Connection conn, string PDFPath)
        {
            m_conn = conn;
            m_lastAccessedFolder = null;
            m_downloadedFiles = new List<string>();
            m_PDFPath = PDFPath;
        }

        ~VaultAccess()
        {
            // try and clean up any files which were downloaded
            if (m_downloadedFiles.Count > 0)
            {
                foreach (string file in m_downloadedFiles)
                {

                    try
                    {
                        //System.IO.File.SetAttributes(file, FileAttributes.Normal);  // not sure if this is proper, but can't access file otherwise to delete it...
                        //System.IO.File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        // if file can't be deleted, it will just stay in temp folder...
                    }

                }
            }
        }
        
        #endregion

        public bool IsConnectionActive()
        {
            if (m_conn != null)
                if (m_conn.IsConnected)
                    return true;
                else
                    return false;
            else
                return false;
        }

        public bool IsWebServiceManagerActive()
        {
            if (m_conn.WebServiceManager != null) return true;
            else return false;
        }

        public bool Login(string vaultUserName, string vaultPassword, string vaultServer, string vault)
        {
            try
            {
                // this login does not consume a license.
                Vault.Forms.Library.Initialize();

                Vault.Results.LogInResult results = Vault.Library.ConnectionManager.LogIn(vaultServer, vault, vaultUserName, vaultPassword, Vault.Currency.Connections.AuthenticationFlags.ReadOnly, null);

                if (results.Success)
                {
                    m_conn = results.Connection;
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

        public bool LoginForItems(string vaultUserName, string vaultPassword, string vaultServer, string vault)
        {
            try
            {
                // this login DOES consume a license.
                Vault.Forms.Library.Initialize();

                Vault.Results.LogInResult results = Vault.Library.ConnectionManager.LogIn(vaultServer, vault, vaultUserName, vaultPassword, Vault.Currency.Connections.AuthenticationFlags.Standard, null);

                if (results.Success)
                {
                    m_conn = results.Connection;
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

        public bool CloseVaultConnection()
        {
            try
            {
                if (m_conn != null)
                {
                    Vault.Library.ConnectionManager.CloseAllConnections();
                    return true;
                }
                else
                {
                    //nothing to close, return true as well
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool downloadFileAndAssociationsOrig(Vault.Currency.Entities.FileIteration file, string folderPath)
        {
            try
            {
                // set up a list to store the names of all files in the download folder before our download.
                List<string> fileNamesBeforeDownload = Directory.GetFiles(folderPath).ToList();

                // initialize the settings
                Vault.Settings.AcquireFilesSettings settings = new Vault.Settings.AcquireFilesSettings(m_conn);
                settings.AddEntityToAcquire(file);
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeChildren = true;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeLibraryContents = true;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeAttachments = false;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeHiddenEntities = true;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeParents = false;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.RecurseChildren = true;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.VersionGatheringOption =
                    Vault.Currency.VersionGatheringOption.Latest;

                // this forces all the files to go into one folder, it doesn't replicate the vault's folder structure..
                // this is needed so that Inventor view knows where to find the file
                settings.OrganizeFilesRelativeToCommonVaultRoot = false;

                // this does the actual work.
                settings.LocalPath = new Autodesk.DataManagement.Client.Framework.Currency.FolderPathAbsolute(folderPath);
                m_conn.FileManager.AcquireFiles(settings);

                // set up a list to store the names of all files in the download folder after the download.
                List<string> fileNamesAfterDownload = Directory.GetFiles(folderPath).ToList();

                // compare the two lists and flag the new files so we can delete them later.
                foreach (string f in fileNamesAfterDownload)
                {
                    if (!fileNamesBeforeDownload.Contains(f) && (f.EndsWith(".iam") || f.EndsWith(".ipt") || f.EndsWith(".idw") || f.EndsWith(".ipn")))
                    {
                        m_downloadedFiles.Add(f);
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public bool downloadFileAndAssociations(Vault.Currency.Entities.FileIteration file, string folderPath)
        {
            try
            {
                try
                {
                    if (Directory.Exists(folderPath))
                    {
                        DirectoryInfo dir = new DirectoryInfo(folderPath);
                        foreach (FileInfo f in dir.GetFiles())
                        {
                            System.IO.File.SetAttributes(f.FullName, FileAttributes.Normal);  // not sure if this is proper, but can't access file otherwise to delete it...
                            f.Delete();
                        }
                    }
                }
                catch(Exception)
                {
                    // cannot delete files, there is likely another Inventor View window open, but we won't worry about it, they'll get deleted next time
                }


                // initialize the settings
                Vault.Settings.AcquireFilesSettings settings = new Vault.Settings.AcquireFilesSettings(m_conn);
                settings.AddEntityToAcquire(file);
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeChildren = true;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeLibraryContents = true;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeAttachments = false;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeHiddenEntities = true;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeParents = false;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.RecurseChildren = true;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.VersionGatheringOption =
                    Vault.Currency.VersionGatheringOption.Latest;

                // this forces all the files to go into one folder, it doesn't replicate the vault's folder structure..
                // this is needed so that Inventor view knows where to find the file
                settings.OrganizeFilesRelativeToCommonVaultRoot = false;

                // this does the actual work.
                settings.LocalPath = new Autodesk.DataManagement.Client.Framework.Currency.FolderPathAbsolute(folderPath);
                m_conn.FileManager.AcquireFiles(settings);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public bool downloadFile(Vault.Currency.Entities.FileIteration file, string folderPath)
        {
            //*************************************************************************************
            // make sure this function gets updated in the ItemExport Project if changes are made.
            //*************************************************************************************
            try
            {
                try
                {
                    if (Directory.Exists(folderPath))
                    {
                        DirectoryInfo dir = new DirectoryInfo(folderPath);
                        foreach (FileInfo f in dir.GetFiles())
                        {
                            System.IO.File.SetAttributes(f.FullName, FileAttributes.Normal);  // not sure if this is proper, but can't access file otherwise to delete it...
                            f.Delete();
                        }
                    }
                }
                catch(Exception)
                {
                    // cannot delete files, there is likely another Inventor View window open, but we won't worry about it, they'll get deleted next time
                }

                // initialize the settings
                Vault.Settings.AcquireFilesSettings settings = new Vault.Settings.AcquireFilesSettings(m_conn);
                settings.AddEntityToAcquire(file);
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeChildren = false;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeLibraryContents = true;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeAttachments = false;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeHiddenEntities = true;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeParents = false;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.RecurseChildren = false;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.VersionGatheringOption =
                    Vault.Currency.VersionGatheringOption.Latest;

                // this forces all the files to go into one folder, it doesn't replicate the vault's folder structure..
                // this is needed so that Inventor view knows where to find the file
                settings.OrganizeFilesRelativeToCommonVaultRoot = false;

                // this does the actual work.
                settings.LocalPath = new Autodesk.DataManagement.Client.Framework.Currency.FolderPathAbsolute(folderPath);
                m_conn.FileManager.AcquireFiles(settings);

            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        public bool downloadFile(string fileName, string downloadFolder)
        {
            try
            {
                //try
                //{
                //    if (Directory.Exists(folderPath))
                //    {
                //        DirectoryInfo dir = new DirectoryInfo(folderPath);
                //        foreach (FileInfo f in dir.GetFiles())
                //        {
                //            System.IO.File.SetAttributes(f.FullName, FileAttributes.Normal);  // not sure if this is proper, but can't access file otherwise to delete it...
                //            f.Delete();
                //        }
                //    }
                //}
                //catch (Exception)
                //{
                //    // cannot delete files, there is likely another Inventor View window open, but we won't worry about it, they'll get deleted next time
                //}

                // initialize the settings

                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                Autodesk.Connectivity.WebServices.SrchCond searchCondition = new SrchCond();

                searchCondition.PropTyp = PropertySearchType.SingleProperty;
                searchCondition.SrchOper = 3;
                searchCondition.SrchTxt = fileNameWithoutExtension;


                string bookmark = "";
                SrchStatus st = new SrchStatus();

                Autodesk.Connectivity.WebServices.File[] files = m_conn.WebServiceManager.DocumentService.FindFilesBySearchConditions(new SrchCond[] { searchCondition }, null, null,true,true, ref bookmark, out st);
                Vault.Settings.AcquireFilesSettings settings = new Vault.Settings.AcquireFilesSettings(m_conn);

                Vault.Currency.Entities.IEntity file = (Vault.Currency.Entities.IEntity) files[0];

                settings.AddEntityToAcquire(file);
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeChildren = false;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeLibraryContents = true;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeAttachments = false;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeHiddenEntities = true;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeParents = false;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.RecurseChildren = false;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.VersionGatheringOption =
                    Vault.Currency.VersionGatheringOption.Latest;

                // this forces all the files to go into one folder, it doesn't replicate the vault's folder structure..
                // this is needed so that Inventor view knows where to find the file
                settings.OrganizeFilesRelativeToCommonVaultRoot = false;

                // this does the actual work.
                settings.LocalPath = new Autodesk.DataManagement.Client.Framework.Currency.FolderPathAbsolute(downloadFolder);
                m_conn.FileManager.AcquireFiles(settings);

            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        public bool downloadFileOrig(Vault.Currency.Entities.FileIteration file, string folderPath)
        {
            try
            {
                // set up a list to store the names of all files in the download folder before our download.
                List<string> fileNamesBeforeDownload = Directory.GetFiles(folderPath).ToList();

                // initialize the settings
                Vault.Settings.AcquireFilesSettings settings = new Vault.Settings.AcquireFilesSettings(m_conn);
                settings.AddEntityToAcquire(file);
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeChildren = false;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeLibraryContents = true;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeAttachments = false;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeHiddenEntities = true;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeParents = false;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.RecurseChildren = false;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.VersionGatheringOption =
                    Vault.Currency.VersionGatheringOption.Latest;

                // this forces all the files to go into one folder, it doesn't replicate the vault's folder structure..
                // this is needed so that Inventor view knows where to find the file
                settings.OrganizeFilesRelativeToCommonVaultRoot = false;

                // this does the actual work.
                settings.LocalPath = new Autodesk.DataManagement.Client.Framework.Currency.FolderPathAbsolute(folderPath);
                m_conn.FileManager.AcquireFiles(settings);

                // set up a list to store the names of all files in the download folder after the download.
                List<string> fileNamesAfterDownload = Directory.GetFiles(folderPath).ToList();

                // compare the two lists and flag the new files so we can delete them later.
                foreach (string f in fileNamesAfterDownload)
                {
                    if (!fileNamesBeforeDownload.Contains(f) && (f.EndsWith(".iam") || f.EndsWith(".ipt") || f.EndsWith(".idw") || f.EndsWith(".ipn")))
                    {
                        m_downloadedFiles.Add(f);
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        //public List<Dictionary<string, string>> BrowseVaultAndGetAssemblyProperties(ToolStripStatusLabel statLabel, bool matchPartNumber = true)
        //{
        //    List<string> propNames = new List<string>();
        //    propNames.Add("PartNumber");
        //    propNames.Add("Title");
        //    propNames.Add("Description");

        //    Dictionary<string, string> propDict = new Dictionary<string, string>();

        //    // a list of dictionaries of the downloaded inventor file properties
        //    List<Dictionary<string, string>> propDictList = new List<Dictionary<string, string>>();

        //    Vault.Currency.Entities.FileIteration selectedFile = null;


        //    if (m_conn != null)
        //    {
        //        VaultBrowser vbForm = new VaultBrowser(m_conn, m_lastAccessedFolder, "Assembly Files (*.iam)", ".+iam");
        //        DialogResult dResult = vbForm.ShowDialog();
        //        if (dResult == DialogResult.OK)
        //        {
        //            Application.UseWaitCursor = true;
        //            Application.DoEvents();

        //            selectedFile = vbForm.m_selectedItem;
        //            m_lastAccessedFolder = vbForm.m_currentFolder;

        //            propDictList = GetAssemblyProps(statLabel, selectedFile, matchPartNumber);
        //        }
        //        else
        //        {
        //            // not logged in, so return an empty list
        //        }
        //    }



        //    Application.UseWaitCursor = false;
        //    return propDictList;
        //}

        //public List<Dictionary<string, string>> BrowseVaultAndGetAssemblyProperties(bool matchPartNumber = true)
        //{
        //    List<string> propNames = new List<string>();
        //    propNames.Add("PartNumber");
        //    propNames.Add("Title");
        //    propNames.Add("Description");

        //    Dictionary<string, string> propDict = new Dictionary<string, string>();

        //    // a list of dictionaries of the downloaded inventor file properties
        //    List<Dictionary<string, string>> propDictList = new List<Dictionary<string, string>>();

        //    Vault.Currency.Entities.FileIteration selectedFile = null;


        //    if (m_conn != null)
        //    {
        //        VaultBrowser vbForm = new VaultBrowser(m_conn, m_lastAccessedFolder, "Assembly Files (*.iam)", ".+iam");
        //        DialogResult dResult = vbForm.ShowDialog();
        //        if (dResult == DialogResult.OK)
        //        {
        //            //Application.UseWaitCursor = true;
        //            //Application.DoEvents();

        //            selectedFile = vbForm.m_selectedItem;
        //            m_lastAccessedFolder = vbForm.m_currentFolder;

        //            propDictList = GetAssemblyProps(selectedFile, matchPartNumber);
        //        }
        //        else
        //        {
        //            // not logged in, so return an empty list
        //        }
        //    }



        //    //Application.UseWaitCursor = false;
        //    return propDictList;
        //}

        public Vault.Currency.Entities.FileIteration BrowseVaultAndReturnFile()
        {
            Vault.Currency.Entities.FileIteration selectedFile = null;

            if (m_conn != null)
            {
                VaultBrowser vbForm = new VaultBrowser(m_conn, m_lastAccessedFolder, "Assembly Files (*.iam)", ".+iam");
                DialogResult dResult = vbForm.ShowDialog();
                if (dResult == DialogResult.OK)
                {
                    selectedFile = vbForm.m_selectedItem;
                    m_lastAccessedFolder = vbForm.m_currentFolder;
                    return selectedFile;
                }
                else
                {
                    return null;
                }
            }
            else
                return null;
        }

        public List<Dictionary<string, string>> BrowseVaultAndGetPartProperties(bool matchPartNumber = true)
        {
            List<string> propNames = new List<string>();
            propNames.Add("PartNumber");
            propNames.Add("Title");
            propNames.Add("Description");

            Dictionary<string, string> propDict = new Dictionary<string, string>();

            // a list of dictionaries of the downloaded inventor file properties
            List<Dictionary<string, string>> propDictList = new List<Dictionary<string, string>>();

            Vault.Currency.Entities.FileIteration selectedFile = null;


            if (m_conn != null)
            {
                VaultBrowser vbForm = new VaultBrowser(m_conn, m_lastAccessedFolder, "Part Files (*.ipt)", ".+ipt");
                DialogResult dResult = vbForm.ShowDialog();
                if (dResult == DialogResult.OK)
                {
                    Application.UseWaitCursor = true;
                    Application.DoEvents();

                    selectedFile = vbForm.m_selectedItem;
                    m_lastAccessedFolder = vbForm.m_currentFolder;

                    propDictList = GetPartProperties(selectedFile, matchPartNumber);
                }
                else
                {
                    // not logged in, so return an empty list
                }
            }

            Application.UseWaitCursor = false;
            return propDictList;
        }

        //public List<Dictionary<string, string>> RefreshAssemblyPropertiesFromVault(long vaultID, bool matchPartNumber = true)
        //{
        //    Dictionary<string, string> propDict = new Dictionary<string, string>();

        //    // a list of dictionaries of the downloaded inventor file properties
        //    List<Dictionary<string, string>> propDictList = new List<Dictionary<string, string>>();

        //    Autodesk.Connectivity.WebServices.File vaultFile = m_conn.WebServiceManager.DocumentService.GetFileById(vaultID);
        //    Vault.Currency.Entities.FileIteration selectedFile = new Vault.Currency.Entities.FileIteration(m_conn, vaultFile);

        //    if (m_conn != null)
        //    {
        //        //selectedFile = vbForm.m_selectedItem;

        //        propDictList = GetAssemblyProps(selectedFile, matchPartNumber);
        //    }
        //    else
        //    {
        //        // not logged in, so return an empty list
        //    }

        //return propDictList;
        //}

        public Task<List<Dictionary<string, string>>> RefreshAssemblyPropertiesFromVault(IProgress<string> prog, long vaultID)
        {
            return Task.Run(async () =>
                {
                    Dictionary<string, string> propDict = new Dictionary<string, string>();
                    // a list of dictionaries of the downloaded inventor file properties
                    List<Dictionary<string, string>> propDictList = new List<Dictionary<string, string>>();

                    Autodesk.Connectivity.WebServices.File vaultFile = m_conn.WebServiceManager.DocumentService.GetFileById(vaultID);
                    Vault.Currency.Entities.FileIteration selectedFile = new Vault.Currency.Entities.FileIteration(m_conn, vaultFile);

                    if (m_conn != null)
                    {
                        prog.Report("Starting to read vault properties");
                        //propDictList = GetAssemblyProps(selectedFile);
                        propDictList = await GetAssemblyProps(prog, selectedFile, true);
                        prog.Report("Done reading vault properties");
                    }
                    return propDictList;
                });

        }

        //public List<Dictionary<string, string>> GetAssemblyProps(System.Windows.Forms.ToolStripStatusLabel statLabel, Vault.Currency.Entities.FileIteration assemblyFile, bool matchPartNumbers = true)
        //{
        //    List<string> propNames = new List<string>();
        //    propNames.Add("PartNumber");
        //    propNames.Add("Title");
        //    propNames.Add("Description");

        //    Dictionary<string, string> propDict = new Dictionary<string, string>();

        //    // a list of dictionaries of the vault properties of the associated files
        //    List<Dictionary<string, string>> vaultPropDictList = new List<Dictionary<string, string>>();

        //    // a list of dictionaries of the downloaded inventor file properties
        //    List<Dictionary<string, string>> inventorPropDictList = new List<Dictionary<string, string>>();

        //    Vault.Currency.Entities.FileIteration selectedFile = assemblyFile;

        //    if (m_conn != null)
        //    {

        //        Application.UseWaitCursor = true;
        //        Application.DoEvents();


        //        // get the vault properties
        //        statLabel.Text = "Reading Vault Properties...";
        //        vaultPropDictList = GetVaultAssemblyProperties(selectedFile, propNames);
        //        statLabel.Text = "Done.";

        //        // get the inventor properties
        //        statLabel.Text = "Reading Inventor Properties...";
        //        inventorPropDictList = GetInventorAssemblyProperties(selectedFile);
        //        statLabel.Text = "Done.";

        //        // initially the part quantites are set per root assembly
        //        //   this needs to be changed to per parent.
        //        statLabel.Text = "Setting Quantities...";
        //        CorrectQuantities(inventorPropDictList);
        //        statLabel.Text = "Done.";



        //        // now merge the two dictionaries
        //        // iterate through the inventorPropDictList, and query the vaultPropDictList for matching part numbers
        //        // if a match is found, the vaultPropDictList properties are copied to the matched inventorPropDictList dictionary
        //        // if no match is found, the vaultPropDictList specific properties are not copied over.
        //        statLabel.Text = "Merging Dictionaries...";
        //        foreach (Dictionary<string, string> inventorPropDictItem in inventorPropDictList)
        //        {
        //            string searchPartNum = null;
        //            string matchPartNum = null;
        //            string entityIterationId = null;
        //            string entityName = null;
        //            string title = null;

        //            inventorPropDictItem.TryGetValue("PartNumber", out searchPartNum);

        //            foreach (Dictionary<string, string> vaultPropDict in vaultPropDictList)
        //            {
        //                vaultPropDict.TryGetValue("EntityName", out matchPartNum);
        //                matchPartNum = Path.GetFileNameWithoutExtension(matchPartNum);
        //                // if there is discrepancy between the vault part number and the inventor part number
        //                //   then we have the option of including those or not
        //                if (searchPartNum == matchPartNum || matchPartNumbers == false)
        //                {
        //                    vaultPropDict.TryGetValue("EntityIterationId", out entityIterationId);
        //                    vaultPropDict.TryGetValue("EntityName", out entityName);
        //                    vaultPropDict.TryGetValue("Title", out title);
        //                    try
        //                    {
        //                        if(!inventorPropDictItem.ContainsKey("EntityIterationId"))
        //                            inventorPropDictItem.Add("EntityIterationId", entityIterationId);
        //                        if(!inventorPropDictItem.ContainsKey("EntityName"))
        //                            inventorPropDictItem.Add("EntityName", entityName);
        //                        if(!inventorPropDictItem.ContainsKey("Title"))
        //                            inventorPropDictItem.Add("Title", title);
        //                    }
        //                    catch (Exception)
        //                    {
        //                        break;
        //                    }
        //                }
        //            }
        //        }
        //        statLabel.Text = "Done.";


        //    }
        //    Application.UseWaitCursor = false;
        //    return inventorPropDictList;

        //}

        //public List<Dictionary<string, string>> GetAssemblyProps(Vault.Currency.Entities.FileIteration assemblyFile, bool matchPartNumbers = true)
        //{
        //    List<string> propNames = new List<string>();
        //    propNames.Add("PartNumber");
        //    propNames.Add("Title");
        //    propNames.Add("Description");

        //    Dictionary<string, string> propDict = new Dictionary<string, string>();

        //    // a list of dictionaries of the vault properties of the associated files
        //    List<Dictionary<string, string>> vaultPropDictList = new List<Dictionary<string, string>>();

        //    // a list of dictionaries of the downloaded inventor file properties
        //    List<Dictionary<string, string>> inventorPropDictList = new List<Dictionary<string, string>>();

        //    Vault.Currency.Entities.FileIteration selectedFile = assemblyFile;

        //    if (m_conn != null)
        //    {

        //        Application.UseWaitCursor = true;
        //        Application.DoEvents();


        //        // get the vault properties
        //        //statLabel.Text = "Reading Vault Properties...";
        //        vaultPropDictList = GetVaultAssemblyProperties(selectedFile, propNames);
        //        //statLabel.Text = "Done.";

        //        // get the inventor properties
        //        //statLabel.Text = "Reading Inventor Properties...";
        //        inventorPropDictList = GetInventorAssemblyProperties(selectedFile);
        //        //statLabel.Text = "Done.";

        //        // initially the part quantites are set per root assembly
        //        //   this needs to be changed to per parent.
        //        //statLabel.Text = "Setting Quantities...";
        //        CorrectQuantities(inventorPropDictList);
        //        //statLabel.Text = "Done.";



        //        // now merge the two dictionaries
        //        // iterate through the inventorPropDictList, and query the vaultPropDictList for matching part numbers
        //        // if a match is found, the vaultPropDictList properties are copied to the matched inventorPropDictList dictionary
        //        // if no match is found, the vaultPropDictList specific properties are not copied over.
        //        //statLabel.Text = "Merging Dictionaries...";
        //        foreach (Dictionary<string, string> inventorPropDictItem in inventorPropDictList)
        //        {
        //            string searchPartNum = null;
        //            string matchPartNum = null;
        //            string entityIterationId = null;
        //            string entityName = null;
        //            string title = null;

        //            inventorPropDictItem.TryGetValue("PartNumber", out searchPartNum);

        //            foreach (Dictionary<string, string> vaultPropDict in vaultPropDictList)
        //            {
        //                vaultPropDict.TryGetValue("EntityName", out matchPartNum);
        //                matchPartNum = Path.GetFileNameWithoutExtension(matchPartNum);
        //                // if there is discrepancy between the vault part number and the inventor part number
        //                //   then we have the option of including those or not
        //                if (searchPartNum == matchPartNum || matchPartNumbers == false)
        //                {
        //                    vaultPropDict.TryGetValue("EntityIterationId", out entityIterationId);
        //                    vaultPropDict.TryGetValue("EntityName", out entityName);
        //                    vaultPropDict.TryGetValue("Title", out title);
        //                    try
        //                    {
        //                        if (!inventorPropDictItem.ContainsKey("EntityIterationId"))
        //                            inventorPropDictItem.Add("EntityIterationId", entityIterationId);
        //                        if (!inventorPropDictItem.ContainsKey("EntityName"))
        //                            inventorPropDictItem.Add("EntityName", entityName);
        //                        if (!inventorPropDictItem.ContainsKey("Title"))
        //                            inventorPropDictItem.Add("Title", title);
        //                    }
        //                    catch (Exception)
        //                    {
        //                        break;
        //                    }
        //                }
        //            }
        //        }
        //        //statLabel.Text = "Done.";


        //    }
        //    Application.UseWaitCursor = false;
        //    return inventorPropDictList;

        //}

        public Task<List<Dictionary<string, string>>> GetAssemblyProps(IProgress<string> prog, Vault.Currency.Entities.FileIteration assemblyFile, bool matchPartNumbers = true)
        {
            return Task.Run(async () =>
            {
                List<string> propNames = new List<string>();
                propNames.Add("PartNumber");
                propNames.Add("Title");
                propNames.Add("Description");

                Dictionary<string, string> propDict = new Dictionary<string, string>();

                // a list of dictionaries of the vault properties of the associated files
                List<Dictionary<string, string>> vaultPropDictList = new List<Dictionary<string, string>>();

                // a list of dictionaries of the downloaded inventor file properties
                List<Dictionary<string, string>> inventorPropDictList = new List<Dictionary<string, string>>();

                Vault.Currency.Entities.FileIteration selectedFile = assemblyFile;

                if (m_conn != null)
                {

                    Application.UseWaitCursor = true;
                    Application.DoEvents();


                    // get the vault properties
                    //statLabel.Text = "Reading Vault Properties...";
                    prog.Report("Reading vault properties...");
                    vaultPropDictList = await GetVaultAssemblyProperties(prog, selectedFile, propNames);
                    prog.Report("Done...");
                    //statLabel.Text = "Done.";

                    // get the inventor properties
                    //statLabel.Text = "Reading Inventor Properties...";
                    inventorPropDictList = await GetInventorAssemblyProperties(prog, selectedFile);
                    //statLabel.Text = "Done.";

                    // initially the part quantites are set per root assembly
                    //   this needs to be changed to per parent.
                    //statLabel.Text = "Setting Quantities...";
                    CorrectQuantities(inventorPropDictList);
                    //statLabel.Text = "Done.";



                    // now merge the two dictionaries
                    // iterate through the inventorPropDictList, and query the vaultPropDictList for matching part numbers
                    // if a match is found, the vaultPropDictList properties are copied to the matched inventorPropDictList dictionary
                    // if no match is found, the vaultPropDictList specific properties are not copied over.
                    //statLabel.Text = "Merging Dictionaries...";
                    foreach (Dictionary<string, string> inventorPropDictItem in inventorPropDictList)
                    {
                        string searchPartNum = null;
                        string matchPartNum = null;
                        string entityIterationId = null;
                        string entityName = null;
                        string title = null;

                        inventorPropDictItem.TryGetValue("PartNumber", out searchPartNum);

                        foreach (Dictionary<string, string> vaultPropDict in vaultPropDictList)
                        {
                            vaultPropDict.TryGetValue("EntityName", out matchPartNum);
                            matchPartNum = Path.GetFileNameWithoutExtension(matchPartNum);
                            // if there is discrepancy between the vault part number and the inventor part number
                            //   then we have the option of including those or not
                            if (searchPartNum == matchPartNum || matchPartNumbers == false)
                            {
                                vaultPropDict.TryGetValue("EntityIterationId", out entityIterationId);
                                vaultPropDict.TryGetValue("EntityName", out entityName);
                                vaultPropDict.TryGetValue("Title", out title);
                                try
                                {
                                    if (!inventorPropDictItem.ContainsKey("EntityIterationId"))
                                        inventorPropDictItem.Add("EntityIterationId", entityIterationId);
                                    if (!inventorPropDictItem.ContainsKey("EntityName"))
                                        inventorPropDictItem.Add("EntityName", entityName);
                                    if (!inventorPropDictItem.ContainsKey("Title"))
                                        inventorPropDictItem.Add("Title", title);
                                }
                                catch (Exception)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    //statLabel.Text = "Done.";


                }
                Application.UseWaitCursor = false;
                return inventorPropDictList;
            });
        }

        public List<Dictionary<string, string>> GetPartProperties(Vault.Currency.Entities.FileIteration partFile, bool matchPartNumbers = true)
        {
            List<string> propNames = new List<string>();
            propNames.Add("PartNumber");
            propNames.Add("Title");
            propNames.Add("Description");

            Dictionary<string, string> propDict = new Dictionary<string, string>();

            // a list of dictionaries of the vault properties of the associated files
            List<Dictionary<string, string>> vaultPropDictList = new List<Dictionary<string, string>>();

            // a list of dictionaries of the downloaded inventor file properties
            List<Dictionary<string, string>> inventorPropDictList = new List<Dictionary<string, string>>();

            Vault.Currency.Entities.FileIteration selectedFile = partFile;


            if (m_conn != null)
            {

                Application.UseWaitCursor = true;
                Application.DoEvents();

                // get the vault properties
                vaultPropDictList = GetVaultPartProperties(selectedFile, propNames);

                // get the inventor properties
                inventorPropDictList = GetInventorPartProperties(selectedFile);

                // now merge the two dictionaries
                // iterate through the inventorPropDictList, and query the vaultPropDictList for matching part numbers
                // if a match is found, the vaultPropDictList properties are copied to the matched inventorPropDictList dictionary
                // if no match is found, the vaultPropDictList specific properties are not copied over.
                foreach (Dictionary<string, string> inventorPropDictItem in inventorPropDictList)
                {
                    string searchPartNum = null;
                    string matchPartNum = null;
                    string entityIterationId = null;
                    string entityName = null;
                    string title = null;

                    inventorPropDictItem.TryGetValue("PartNumber", out searchPartNum);

                    foreach (Dictionary<string, string> vaultPropDict in vaultPropDictList)
                    {
                        vaultPropDict.TryGetValue("PartNumber", out matchPartNum);
                        // if there is discrepancy between the vault part number and the inventor part number
                        //   then we have the option of including those or not
                        if (searchPartNum == matchPartNum || matchPartNumbers == false)
                        {
                            vaultPropDict.TryGetValue("EntityIterationId", out entityIterationId);
                            vaultPropDict.TryGetValue("EntityName", out entityName);
                            vaultPropDict.TryGetValue("Title", out title);
                            try
                            {
                                inventorPropDictItem.Add("EntityIterationId", entityIterationId);
                                inventorPropDictItem.Add("EntityName", entityName);
                                inventorPropDictItem.Add("Title", title);
                            }
                            catch (Exception)
                            {
                                break;
                            }
                        }
                    }
                }
            }


            Application.UseWaitCursor = false;
            return inventorPropDictList;
        }

        //private List<Dictionary<string,string>> GetVaultAssemblyProperties(Vault.Currency.Entities.FileIteration selectedFile, List<string> propNames)
        //{
        //    // a list of dictionaries of the vault properties of the associated files
        //    List<Dictionary<string, string>> vaultPropDictList = new List<Dictionary<string, string>>();

        //    // first get all parent and child information for the selectedFile
        //    // and store in a dictionary of FileIterations indexed by their Vault IDs

        //    ACW.FileAssocArray[] associationArrays = m_conn.WebServiceManager.DocumentService.GetFileAssociationsByIds(
        //        new long[] { selectedFile.EntityIterationId },
        //        ACW.FileAssociationTypeEnum.None, false,        // parent associations
        //        ACW.FileAssociationTypeEnum.Dependency, true,   // child associations
        //        false, true);

        //    if (associationArrays != null && associationArrays.Length > 0 &&
        //        associationArrays[0].FileAssocs != null && associationArrays[0].FileAssocs.Length > 0)
        //    {
        //        ACW.FileAssoc[] associations = associationArrays[0].FileAssocs;

        //        // organize the return values by the parent file
        //        Dictionary<long, List<Vault.Currency.Entities.FileIteration>> associationsByFile = new Dictionary<long, List<Vault.Currency.Entities.FileIteration>>();
        //        foreach (ACW.FileAssoc association in associations)
        //        {
        //            ACW.File parent = association.ParFile;
        //            if (associationsByFile.ContainsKey(parent.Id))
        //            {
        //                // parent is already in the hashtable, add an new child entry
        //                List<Vault.Currency.Entities.FileIteration> list = associationsByFile[parent.Id];
        //                list.Add(new Vault.Currency.Entities.FileIteration(m_conn, association.CldFile));
        //            }
        //            else
        //            {
        //                // add the parent to the hashtable.
        //                List<Vault.Currency.Entities.FileIteration> list = new List<Vault.Currency.Entities.FileIteration>();
        //                list.Add(new Vault.Currency.Entities.FileIteration(m_conn, parent));
        //                list.Add(new Vault.Currency.Entities.FileIteration(m_conn, association.CldFile));
        //                associationsByFile.Add(parent.Id, list);
        //            }
        //        }

        //        // now convert this Dictionary of fileIterations into a List of Dictionaries containing all accessible properties.

        //        // set up a temporary list to hold all associated files.
        //        List<Vault.Currency.Entities.FileIteration> tempList = new List<Vault.Currency.Entities.FileIteration>();


        //        for (int index = 0; index < associationsByFile.Count; index++)
        //        {
        //            var item = associationsByFile.ElementAt(index);
        //            var itemKey = item.Key;
        //            var itemValue = item.Value;
        //            foreach (var subItem in itemValue)
        //            {
        //                tempList.Add((Vault.Currency.Entities.FileIteration)subItem);
        //            }
        //        }

        //        // define a dictionary to hold all the properties pertaining to a vault entity
        //        Vault.Currency.Properties.PropertyDefinitionDictionary allPropDefDict = new Vault.Currency.Properties.PropertyDefinitionDictionary();
        //        allPropDefDict = m_conn.PropertyManager.GetPropertyDefinitions(Vault.Currency.Entities.EntityClassIds.Files, null, Vault.Currency.Properties.PropertyDefinitionFilter.IncludeAll);


        //        // define a list of only the properties that we are concerned about.
        //        List<Vault.Currency.Properties.PropertyDefinition> filteredPropDefList =
        //                new List<Vault.Currency.Properties.PropertyDefinition>();

        //        // copy only definitions in propNames list from allPropDefDict to filteredPropDefList
        //        foreach (string s in propNames)
        //        {
        //            Vault.Currency.Properties.PropertyDefinition propDefs =
        //                new Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties.PropertyDefinition();
        //            propDefs = allPropDefDict[s];

        //            filteredPropDefList.Add(propDefs);
        //        }

        //        // the following line should return a list of properties
        //        Vault.Currency.Properties.PropertyValues propValues = new Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties.PropertyValues();
        //        propValues = m_conn.PropertyManager.GetPropertyValues(tempList, filteredPropDefList, null);

        //        // convert the propValues into a list of dictionaries

        //        foreach (Vault.Currency.Entities.FileIteration f in tempList)
        //        {
        //            Dictionary<Vault.Currency.Properties.PropertyDefinition, Vault.Currency.Properties.PropertyValue> d = propValues.GetValues(f);
        //            Dictionary<string, string> dictToAdd = new Dictionary<string, string>();
        //            dictToAdd.Add("EntityIterationId", f.EntityIterationId.ToString());
        //            dictToAdd.Add("EntityName", f.EntityName);
        //            foreach (string s in propNames)
        //            {
        //                Vault.Currency.Properties.PropertyDefinition def = new Vault.Currency.Properties.PropertyDefinition(s);
        //                Vault.Currency.Properties.PropertyValue val;
        //                d.TryGetValue(def, out val);

        //                string key = null;
        //                if (def != null)
        //                {
        //                    key = (def.SystemName == null) ? null : def.SystemName;

        //                    string value = null;
        //                    if (val != null)
        //                    {
        //                        value = (val.Value == null) ? "" : val.Value.ToString();
        //                    }
        //                    else
        //                        value = "";
        //                    if (key != null)
        //                        dictToAdd.Add(key, value);
        //                }
        //            }
        //            vaultPropDictList.Add(dictToAdd);
        //        }
        //    }
        //    return vaultPropDictList;
        //}

        private Task<List<Dictionary<string, string>>> GetVaultAssemblyProperties(IProgress<string> prog, Vault.Currency.Entities.FileIteration selectedFile, List<string> propNames)
        {
            return Task.Run(() =>
            {
                // a list of dictionaries of the vault properties of the associated files
                List<Dictionary<string, string>> vaultPropDictList = new List<Dictionary<string, string>>();

                // first get all parent and child information for the selectedFile
                // and store in a dictionary of FileIterations indexed by their Vault IDs

                prog.Report("Reading file associations...");
                ACW.FileAssocArray[] associationArrays = m_conn.WebServiceManager.DocumentService.GetFileAssociationsByIds(
                    new long[] { selectedFile.EntityIterationId },
                    ACW.FileAssociationTypeEnum.None, false,        // parent associations
                    ACW.FileAssociationTypeEnum.Dependency, true,   // child associations
                    false, true);

                prog.Report("Sorting file associations...");
                if (associationArrays != null && associationArrays.Length > 0 &&
                    associationArrays[0].FileAssocs != null && associationArrays[0].FileAssocs.Length > 0)
                {
                    ACW.FileAssoc[] associations = associationArrays[0].FileAssocs;

                    // organize the return values by the parent file
                    Dictionary<long, List<Vault.Currency.Entities.FileIteration>> associationsByFile = new Dictionary<long, List<Vault.Currency.Entities.FileIteration>>();
                    foreach (ACW.FileAssoc association in associations)
                    {
                        ACW.File parent = association.ParFile;
                        if (associationsByFile.ContainsKey(parent.Id))
                        {
                            // parent is already in the hashtable, add an new child entry
                            List<Vault.Currency.Entities.FileIteration> list = associationsByFile[parent.Id];
                            list.Add(new Vault.Currency.Entities.FileIteration(m_conn, association.CldFile));
                        }
                        else
                        {
                            // add the parent to the hashtable.
                            List<Vault.Currency.Entities.FileIteration> list = new List<Vault.Currency.Entities.FileIteration>();
                            list.Add(new Vault.Currency.Entities.FileIteration(m_conn, parent));
                            list.Add(new Vault.Currency.Entities.FileIteration(m_conn, association.CldFile));
                            associationsByFile.Add(parent.Id, list);
                        }
                    }

                    prog.Report("Converting file associations to dictionaries of properties...");
                    // now convert this Dictionary of fileIterations into a List of Dictionaries containing all accessible properties.

                    // set up a temporary list to hold all associated files.
                    List<Vault.Currency.Entities.FileIteration> tempList = new List<Vault.Currency.Entities.FileIteration>();


                    for (int index = 0; index < associationsByFile.Count; index++)
                    {
                        var item = associationsByFile.ElementAt(index);
                        var itemKey = item.Key;
                        var itemValue = item.Value;
                        foreach (var subItem in itemValue)
                        {
                            tempList.Add((Vault.Currency.Entities.FileIteration)subItem);
                        }
                    }

                    // define a dictionary to hold all the properties pertaining to a vault entity
                    Vault.Currency.Properties.PropertyDefinitionDictionary allPropDefDict = new Vault.Currency.Properties.PropertyDefinitionDictionary();
                    allPropDefDict = m_conn.PropertyManager.GetPropertyDefinitions(Vault.Currency.Entities.EntityClassIds.Files, null, Vault.Currency.Properties.PropertyDefinitionFilter.IncludeAll);


                    // define a list of only the properties that we are concerned about.
                    List<Vault.Currency.Properties.PropertyDefinition> filteredPropDefList =
                            new List<Vault.Currency.Properties.PropertyDefinition>();

                    // copy only definitions in propNames list from allPropDefDict to filteredPropDefList
                    foreach (string s in propNames)
                    {
                        Vault.Currency.Properties.PropertyDefinition propDefs =
                            new Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties.PropertyDefinition();
                        propDefs = allPropDefDict[s];

                        filteredPropDefList.Add(propDefs);
                    }

                    // the following line should return a list of properties
                    prog.Report("Reading properties...");
                    Vault.Currency.Properties.PropertyValues propValues = new Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties.PropertyValues();
                    propValues = m_conn.PropertyManager.GetPropertyValues(tempList, filteredPropDefList, null);

                    // convert the propValues into a list of dictionaries

                    prog.Report("Converting properties...");
                    foreach (Vault.Currency.Entities.FileIteration f in tempList)
                    {
                        Dictionary<Vault.Currency.Properties.PropertyDefinition, Vault.Currency.Properties.PropertyValue> d = propValues.GetValues(f);
                        Dictionary<string, string> dictToAdd = new Dictionary<string, string>();
                        dictToAdd.Add("EntityIterationId", f.EntityIterationId.ToString());
                        dictToAdd.Add("EntityName", f.EntityName);
                        foreach (string s in propNames)
                        {
                            Vault.Currency.Properties.PropertyDefinition def = new Vault.Currency.Properties.PropertyDefinition(s);
                            Vault.Currency.Properties.PropertyValue val;
                            d.TryGetValue(def, out val);

                            string key = null;
                            if (def != null)
                            {
                                key = (def.SystemName == null) ? null : def.SystemName;

                                string value = null;
                                if (val != null)
                                {
                                    value = (val.Value == null) ? "" : val.Value.ToString();
                                }
                                else
                                    value = "";
                                if (key != null)
                                    dictToAdd.Add(key, value);
                            }
                        }
                        vaultPropDictList.Add(dictToAdd);
                    }
                }
                return vaultPropDictList;
            });
        }

        private List<Dictionary<string, string>> GetVaultPartProperties(Vault.Currency.Entities.FileIteration selectedFile, List<string> propNames)
        {
            // a list of dictionaries of the vault properties of the associated files
            List<Dictionary<string, string>> vaultPropDictList = new List<Dictionary<string, string>>();

            // function that gets properties requires a list so we'll define a list and add the selected file to it...
            List<Vault.Currency.Entities.FileIteration> fileList = new List<Vault.Currency.Entities.FileIteration>();
            fileList.Add(selectedFile);

            // define a dictionary to hold all the properties pertaining to a vault entity
            Vault.Currency.Properties.PropertyDefinitionDictionary allPropDefDict = new Vault.Currency.Properties.PropertyDefinitionDictionary();
            allPropDefDict = m_conn.PropertyManager.GetPropertyDefinitions(Vault.Currency.Entities.EntityClassIds.Files, null, Vault.Currency.Properties.PropertyDefinitionFilter.IncludeAll);

            // define a list of only the properties that we are concerned about.
            List<Vault.Currency.Properties.PropertyDefinition> filteredPropDefList =
                    new List<Vault.Currency.Properties.PropertyDefinition>();

            // copy only definitions in propNames list from allPropDefDict to filteredPropDefList
            foreach (string s in propNames)
            {
                Vault.Currency.Properties.PropertyDefinition propDefs =
                    new Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties.PropertyDefinition();
                propDefs = allPropDefDict[s];

                filteredPropDefList.Add(propDefs);
            }

            // the following line should return a list of properties
            Vault.Currency.Properties.PropertyValues propValues = new Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties.PropertyValues();
            propValues = m_conn.PropertyManager.GetPropertyValues(fileList, filteredPropDefList, null);

            // convert the propValues into a list of dictionaries

            foreach (Vault.Currency.Entities.FileIteration f in fileList)
            {
                Dictionary<Vault.Currency.Properties.PropertyDefinition, Vault.Currency.Properties.PropertyValue> d = propValues.GetValues(f);
                Dictionary<string, string> dictToAdd = new Dictionary<string, string>();
                dictToAdd.Add("EntityIterationId", f.EntityIterationId.ToString());
                dictToAdd.Add("EntityName", f.EntityName);
                foreach (string s in propNames)
                {
                    Vault.Currency.Properties.PropertyDefinition def = new Vault.Currency.Properties.PropertyDefinition(s);
                    Vault.Currency.Properties.PropertyValue val;
                    d.TryGetValue(def, out val);

                    string key = null;
                    if (def != null)
                    {
                        key = (def.SystemName == null) ? null : def.SystemName;

                        string value = null;
                        if (val != null)
                        {
                            value = (val.Value == null) ? "" : val.Value.ToString();
                        }
                        else
                            value = "";
                        if (key != null)
                            dictToAdd.Add(key, value);
                    }
                }
                vaultPropDictList.Add(dictToAdd);
            }
            return vaultPropDictList;
        }

        //private List<Dictionary<string,string>> GetInventorAssemblyProperties(Vault.Currency.Entities.FileIteration selectedFile)
        //{
        //    // a list of dictionaries of the downloaded inventor file properties
        //    List<Dictionary<string, string>> inventorPropDictList = new List<Dictionary<string, string>>();

        //    // first download all the associated files
        //    string filePath = Path.Combine(System.IO.Path.GetTempPath(), selectedFile.EntityName);

        //    if (System.IO.File.Exists(filePath))
        //        System.IO.File.SetAttributes(filePath, FileAttributes.Normal);

        //    //determine if the file already exists
        //    if (System.IO.File.Exists(filePath))
        //    {
        //        //we'll try to delete the file so we can get the latest copy
        //        try
        //        {
        //            System.IO.File.Delete(filePath);

        //            //remove the file from the collection of downloaded files that need to be removed when the application exits
        //            if (m_downloadedFiles.Contains(filePath))
        //                m_downloadedFiles.Remove(filePath);
        //        }
        //        catch (System.IO.IOException)
        //        {
        //            throw new Exception("The file you are attempting to open already exists and can not be overwritten. This file may currently be open, try closing any application you are using to view this file and try opening the file again.");
        //        }
        //    }
        //    if (downloadFileAndAssociations(selectedFile, Path.GetDirectoryName(filePath)))
        //    {
        //        string downloadedFileName = Path.GetDirectoryName(filePath) + "\\" + selectedFile.EntityName;
        //        m_downloadedFiles.Add(filePath);

        //        Inventor.ApprenticeServerComponent apprentice = new Inventor.ApprenticeServerComponent();
        //        Inventor.ApprenticeServerDocument apprenticeDoc = apprentice.Open(downloadedFileName);
        //        Inventor.ComponentOccurrences oComponentOccurences = apprenticeDoc.ComponentDefinition.Occurrences;


        //        // get properties of root component
        //        Inventor.DocumentTypeEnum documentType = Inventor.DocumentTypeEnum.kUnknownDocumentObject;
        //        string thickness = "";
        //        string material = "";
        //        string desc = "";
        //        Dictionary<string, string> inventorPropDict = new Dictionary<string, string>();
        //        apprenticeDoc = apprentice.Open(downloadedFileName);

        //        string fileName = "";
        //        try
        //        {
        //           fileName = Path.GetFileNameWithoutExtension(apprenticeDoc.FullDocumentName);
        //        }
        //        catch(Exception)
        //        {
        //            fileName = "undefined";
        //        }

        //        try
        //        {
        //            Inventor.PropertySet userDefinedPropertiesSet;
        //            userDefinedPropertiesSet = apprenticeDoc.PropertySets["User Defined Properties"];
        //            Inventor.Property thickProp = userDefinedPropertiesSet["Thickness"];
        //            thickness = thickProp.Value;
        //        }
        //        catch (Exception)
        //        {
        //            thickness = "undefined";
        //        }

        //        try
        //        {
        //            Inventor.PropertySet designTrackingProperties = apprenticeDoc.PropertySets["Design Tracking Properties"];
        //            Inventor.Property materialProp = designTrackingProperties["Material"];
        //            material = materialProp.Value;
        //        }
        //        catch (Exception)
        //        {
        //            material = "undefined";
        //        }

        //        try
        //        {
        //            Inventor.PropertySet designTrackingProperties = apprenticeDoc.PropertySets["Design Tracking Properties"];
        //            Inventor.Property materialProp = designTrackingProperties["Description"];
        //            desc = materialProp.Value;
        //        }
        //        catch (Exception)
        //        {
        //            desc = "undefined";
        //        }

        //        inventorPropDict.Add("ParentPartNumber", "none");


        //        documentType = Inventor.DocumentTypeEnum.kAssemblyDocumentObject;

        //        inventorPropDict.Add("PartNumber", fileName);
        //        inventorPropDict.Add("Material Thickness", thickness);
        //        inventorPropDict.Add("Material Type", material);
        //        inventorPropDict.Add("Description", desc);
        //        inventorPropDict.Add("Quantity", "1");
        //        inventorPropDict.Add("DocumentType", documentType.ToString());

        //        // add the properties of the top level component to the list.
        //        inventorPropDictList.Add(inventorPropDict);

        //        // get the rest of the sub components
        //        GetComponents(fileName, oComponentOccurences, ref inventorPropDictList);
        //        // these are all stored in a list of dictionaries.  Each dictionary holds all the attributes of each part (inventorPropDictList)
        //    }
        //    return inventorPropDictList;
        //}

        private Task<List<Dictionary<string, string>>> GetInventorAssemblyProperties(IProgress<string> prog, Vault.Currency.Entities.FileIteration selectedFile)
        {
            return Task.Run(async () =>
            {
                prog.Report("Downloading Inventor files...");
                // a list of dictionaries of the downloaded inventor file properties
                List<Dictionary<string, string>> inventorPropDictList = new List<Dictionary<string, string>>();

                // first download all the associated files
                string filePath = Path.Combine(System.IO.Path.GetTempPath(), selectedFile.EntityName);

                if (System.IO.File.Exists(filePath))
                    System.IO.File.SetAttributes(filePath, FileAttributes.Normal);

                //determine if the file already exists
                if (System.IO.File.Exists(filePath))
                {
                    //we'll try to delete the file so we can get the latest copy
                    try
                    {
                        System.IO.File.Delete(filePath);

                        //remove the file from the collection of downloaded files that need to be removed when the application exits
                        if (m_downloadedFiles.Contains(filePath))
                            m_downloadedFiles.Remove(filePath);
                    }
                    catch (System.IO.IOException)
                    {
                        throw new Exception("The file you are attempting to open already exists and can not be overwritten. This file may currently be open, try closing any application you are using to view this file and try opening the file again.");
                    }
                }
                if (downloadFileAndAssociations(selectedFile, Path.GetDirectoryName(filePath)))
                {
                    prog.Report("Reading Inventor Properties of root assembly...");
                    string downloadedFileName = Path.GetDirectoryName(filePath) + "\\" + selectedFile.EntityName;
                    m_downloadedFiles.Add(filePath);

                    Inventor.ApprenticeServerComponent apprentice = new Inventor.ApprenticeServerComponent();
                    Inventor.ApprenticeServerDocument apprenticeDoc = apprentice.Open(downloadedFileName);
                    Inventor.ComponentOccurrences oComponentOccurences = apprenticeDoc.ComponentDefinition.Occurrences;


                    // get properties of root component
                    Inventor.DocumentTypeEnum documentType = Inventor.DocumentTypeEnum.kUnknownDocumentObject;
                    string thickness = "";
                    string material = "";
                    string desc = "";
                    Dictionary<string, string> inventorPropDict = new Dictionary<string, string>();
                    apprenticeDoc = apprentice.Open(downloadedFileName);

                    string fileName = "";
                    try
                    {
                        fileName = Path.GetFileNameWithoutExtension(apprenticeDoc.FullDocumentName);
                    }
                    catch (Exception)
                    {
                        fileName = "undefined";
                    }

                    try
                    {
                        Inventor.PropertySet userDefinedPropertiesSet;
                        userDefinedPropertiesSet = apprenticeDoc.PropertySets["User Defined Properties"];
                        Inventor.Property thickProp = userDefinedPropertiesSet["Thickness"];
                        thickness = thickProp.Value;
                    }
                    catch (Exception)
                    {
                        thickness = "undefined";
                    }

                    try
                    {
                        Inventor.PropertySet designTrackingProperties = apprenticeDoc.PropertySets["Design Tracking Properties"];
                        Inventor.Property materialProp = designTrackingProperties["Material"];
                        material = materialProp.Value;
                    }
                    catch (Exception)
                    {
                        material = "undefined";
                    }

                    try
                    {
                        Inventor.PropertySet designTrackingProperties = apprenticeDoc.PropertySets["Design Tracking Properties"];
                        Inventor.Property materialProp = designTrackingProperties["Description"];
                        desc = materialProp.Value;
                    }
                    catch (Exception)
                    {
                        desc = "undefined";
                    }

                    inventorPropDict.Add("ParentPartNumber", "none");


                    documentType = Inventor.DocumentTypeEnum.kAssemblyDocumentObject;

                    inventorPropDict.Add("PartNumber", fileName);
                    inventorPropDict.Add("Material Thickness", thickness);
                    inventorPropDict.Add("Material Type", material);
                    inventorPropDict.Add("Description", desc);
                    inventorPropDict.Add("Quantity", "1");
                    inventorPropDict.Add("DocumentType", documentType.ToString());

                    // add the properties of the top level component to the list.
                    inventorPropDictList.Add(inventorPropDict);

                    // get the rest of the sub components
                    prog.Report("Reading Inventor properties of sub components...");
                    await GetComponents(prog, fileName, oComponentOccurences, ref inventorPropDictList);
                    // these are all stored in a list of dictionaries.  Each dictionary holds all the attributes of each part (inventorPropDictList)
                }
                return inventorPropDictList;
            });
        }

        private List<Dictionary<string, string>> GetInventorPartProperties(Vault.Currency.Entities.FileIteration selectedFile)
        {
            // a list of dictionaries of the downloaded inventor file properties
            List<Dictionary<string, string>> inventorPropDictList = new List<Dictionary<string, string>>();

            // first download all the associated files
            string filePath = Path.Combine(System.IO.Path.GetTempPath(), selectedFile.EntityName);

            if (System.IO.File.Exists(filePath))
                System.IO.File.SetAttributes(filePath, FileAttributes.Normal);

            //determine if the file already exists
            if (System.IO.File.Exists(filePath))
            {
                //we'll try to delete the file so we can get the latest copy
                try
                {
                    System.IO.File.Delete(filePath);

                    //remove the file from the collection of downloaded files that need to be removed when the application exits
                    if (m_downloadedFiles.Contains(filePath))
                        m_downloadedFiles.Remove(filePath);
                }
                catch (System.IO.IOException)
                {
                    throw new Exception("The file you are attempting to open already exists and can not be overwritten. This file may currently be open, try closing any application you are using to view this file and try opening the file again.");
                }
            }

            if (downloadFile(selectedFile, Path.GetDirectoryName(filePath)))
            {
                string downloadedFileName = Path.GetDirectoryName(filePath) + "\\" + selectedFile.EntityName;
                m_downloadedFiles.Add(filePath);

                Inventor.ApprenticeServerComponent apprentice = new Inventor.ApprenticeServerComponent();
                Inventor.ApprenticeServerDocument apprenticeDoc = apprentice.Open(downloadedFileName);
                Inventor.ComponentOccurrences oComponentOccurences = apprenticeDoc.ComponentDefinition.Occurrences;


                // get properties of root component
                Inventor.DocumentTypeEnum documentType = Inventor.DocumentTypeEnum.kUnknownDocumentObject;
                string thickness = "";
                string material = "";
                string desc = "";
                Dictionary<string, string> inventorPropDict = new Dictionary<string, string>();
                apprenticeDoc = apprentice.Open(downloadedFileName);

                string fileName = "";
                try
                {
                    fileName = Path.GetFileNameWithoutExtension(apprenticeDoc.FullDocumentName);
                }
                catch (Exception)
                {
                    fileName = "undefined";
                }

                try
                {
                    Inventor.PropertySet userDefinedPropertiesSet;
                    userDefinedPropertiesSet = apprenticeDoc.PropertySets["User Defined Properties"];
                    Inventor.Property thickProp = userDefinedPropertiesSet["Thickness"];
                    thickness = thickProp.Value;
                }
                catch (Exception)
                {
                    thickness = "undefined";
                }

                try
                {
                    Inventor.PropertySet designTrackingProperties = apprenticeDoc.PropertySets["Design Tracking Properties"];
                    Inventor.Property materialProp = designTrackingProperties["Material"];
                    material = materialProp.Value;
                }
                catch (Exception)
                {
                    material = "undefined";
                }

                try
                {
                    Inventor.PropertySet designTrackingProperties = apprenticeDoc.PropertySets["Design Tracking Properties"];
                    Inventor.Property materialProp = designTrackingProperties["Description"];
                    desc = materialProp.Value;
                }
                catch (Exception)
                {
                    desc = "undefined";
                }

                inventorPropDict.Add("ParentPartNumber", "none");


                documentType = Inventor.DocumentTypeEnum.kPartDocumentObject;

                inventorPropDict.Add("PartNumber", fileName);
                inventorPropDict.Add("Material Thickness", thickness);
                inventorPropDict.Add("Material Type", material);
                inventorPropDict.Add("Description", desc);
                inventorPropDict.Add("Quantity", "1");
                inventorPropDict.Add("DocumentType", documentType.ToString());

                // add the properties of the part to the list.
                inventorPropDictList.Add(inventorPropDict);
            }
            return inventorPropDictList;
        }

        //private void GetComponents(string parentPartNum, Inventor.ComponentOccurrences inCollection, ref List<Dictionary<string, string>> dictList)
        //{
        //    // this is a recursive function that gets the properties of all the components and sub components in 'inCollection'

        //    List<Dictionary<string, string>> inventorPropDictList = dictList;

        //    Inventor.ApprenticeServerComponent apprentice = new Inventor.ApprenticeServerComponent();

        //    var assemblyQuery =
        //        from Inventor.ComponentOccurrence assembly in inCollection
        //        where assembly.DefinitionDocumentType == Inventor.DocumentTypeEnum.kAssemblyDocumentObject ||
        //              assembly.DefinitionDocumentType == Inventor.DocumentTypeEnum.kPartDocumentObject
        //        orderby assembly.DefinitionDocumentType descending
        //        select assembly;

        //    //iterate through the components in the current collection
        //    IEnumerator objoccsEnumerator = assemblyQuery.GetEnumerator();

        //    Inventor.ComponentOccurrence objcompOccurrence;

        //    while (objoccsEnumerator.MoveNext() == true)
        //    {
        //        Inventor.DocumentTypeEnum documentType = new Inventor.DocumentTypeEnum();
        //        string thickness = "";
        //        string material = "";
        //        string desc = "";
        //        string displayName = "";
        //        string fileName = "";
        //        string parentFileName = "";

        //        Dictionary<string, string> inventorPropDict = new Dictionary<string, string>();
        //        Inventor.ApprenticeServerDocument apprenticeDoc;
        //        try
        //        {
        //            objcompOccurrence = (Inventor.ComponentOccurrence)objoccsEnumerator.Current;
        //            try
        //            {
        //                Inventor.ComponentDefinition cDef = objcompOccurrence.Definition;
        //                apprenticeDoc = objcompOccurrence.Definition.Document;
        //                apprentice.Open(apprenticeDoc.FullFileName);

        //                try
        //                {

        //                    fileName = Path.GetFileNameWithoutExtension(apprenticeDoc.FullDocumentName);
        //                }
        //                catch(Exception)
        //                {
        //                    fileName = "undefined";
        //                }

        //                try
        //                {
        //                    Inventor.PropertySet userDefinedPropertiesSet;
        //                    userDefinedPropertiesSet = apprenticeDoc.PropertySets["User Defined Properties"];
        //                    Inventor.Property thickProp = userDefinedPropertiesSet["Thickness"];
        //                    thickness = thickProp.Value;
        //                }
        //                catch (Exception)
        //                {
        //                    thickness = "undefined";
        //                }

        //                try
        //                {
        //                    Inventor.PropertySet designTrackingProperties = apprenticeDoc.PropertySets["Design Tracking Properties"];
        //                    Inventor.Property materialProp = designTrackingProperties["Material"];
        //                    material = materialProp.Value;
        //                }
        //                catch (Exception)
        //                {
        //                    material = "undefined";
        //                }

        //                try
        //                {
        //                    Inventor.PropertySet designTrackingProperties = apprenticeDoc.PropertySets["Design Tracking Properties"];
        //                    Inventor.Property materialProp = designTrackingProperties["Description"];
        //                    desc = materialProp.Value;
        //                }
        //                catch (Exception)
        //                {
        //                    desc = "undefined";
        //                }

        //                if (objcompOccurrence.Parent != null)
        //                {
        //                    objcompOccurrence = (Inventor.ComponentOccurrence)objoccsEnumerator.Current;
        //                    Inventor.ApprenticeServerDocument apprenticeParentDoc = objcompOccurrence.Parent.Document;
        //                    apprentice.Open(apprenticeParentDoc.FullFileName);

        //                    parentFileName = Path.GetFileNameWithoutExtension(apprenticeParentDoc.FullDocumentName);

        //                    inventorPropDict.Add("ParentPartNumber", parentFileName);

        //                    if (parentFileName == fileName)    // seems there are some components referencing themselves as parents.  We don't want them...
        //                    {
        //                        continue;
        //                    }
        //                }
        //                else
        //                {
        //                    inventorPropDict.Add("ParentPartNumber", "");
        //                }

        //                documentType = objcompOccurrence.DefinitionDocumentType;

        //                inventorPropDict.Add("PartNumber", fileName);
        //                inventorPropDict.Add("Material Thickness", thickness);
        //                inventorPropDict.Add("Material Type", material);
        //                inventorPropDict.Add("Description", desc);
        //                inventorPropDict.Add("Quantity", "1");
        //                inventorPropDict.Add("DocumentType", documentType.ToString());

        //                string checkPartNum = null;
        //                string checkExistingParentPartNum = null;
        //                string checkQty = null;
        //                bool matchfound = false;
        //                Dictionary<string, string> dictToRemove = new Dictionary<string, string>();

        //                foreach (Dictionary<string, string> t in inventorPropDictList)
        //                {

        //                    if (t.TryGetValue("PartNumber", out checkPartNum))  // do we already have this part in the list?
        //                    {
        //                        if (checkPartNum == fileName)
        //                        {

        //                            t.TryGetValue("ParentPartNumber", out checkExistingParentPartNum);  // get its parent part number
        //                            if (checkExistingParentPartNum == parentFileName)        // if parentPartNumber of check item matches parentPartNumber of current item, we only need to increase quantity
        //                            {
        //                                t.TryGetValue("Quantity", out checkQty);     // and its current quantity
        //                                dictToRemove = t;
        //                                matchfound = true;  // we found a match
        //                                break;
        //                            }
        //                        }
        //                    }
        //                }

        //                if (matchfound)
        //                {
        //                    if (parentFileName == checkExistingParentPartNum)
        //                    {
        //                        int newQty = int.Parse(checkQty);
        //                        newQty++;
        //                        inventorPropDict["Quantity"] = newQty.ToString();
        //                        inventorPropDictList.Remove(dictToRemove);
        //                    }
        //                }


        //                inventorPropDictList.Add(inventorPropDict);

        //                Inventor.ComponentOccurrences oSubComponentOccurences = apprenticeDoc.ComponentDefinition.Occurrences;
        //                GetComponents(fileName, oSubComponentOccurences, ref inventorPropDictList);
        //            }
        //            catch (Exception ex)
        //            {
        //                // create undefined component
        //                inventorPropDict.Clear();
        //                inventorPropDict.Add("EntityName", null);
        //                string partNum = Path.GetFileNameWithoutExtension(objcompOccurrence.ReferencedDocumentDescriptor.FullDocumentName);
        //                partNum = partNum + " - (Inaccessible Component)";
        //                inventorPropDict.Add("PartNumber", partNum);
        //                inventorPropDict.Add("Material Thickness", "undefined");
        //                inventorPropDict.Add("Material Type", "undefined");
        //                inventorPropDict.Add("Quantity", "1");
        //                inventorPropDict.Add("DocumentType", objcompOccurrence.ReferencedDocumentDescriptor.ReferencedDocumentType.ToString());
        //                inventorPropDict.Add("ParentPartNumber", parentPartNum);
        //                inventorPropDict.Add("Description", "Inaccessible Component");
        //                inventorPropDict.Add("Title",null);
        //                inventorPropDictList.Add(inventorPropDict);

        //                continue;
        //            }
        //        }

        //        catch (Exception ex)
        //        {
        //            // create undefined component
        //            // not fully tested yet....
        //            inventorPropDict.Clear();
        //            inventorPropDict.Add("EntityName", "Inaccessible Component");
        //            inventorPropDict.Add("PartNumber", displayName);
        //            inventorPropDict.Add("Material Thickness", "undefined");
        //            inventorPropDict.Add("Material Type", "undefined");
        //            inventorPropDict.Add("Quantity", "1");
        //            inventorPropDict.Add("DocumentType", documentType.ToString());
        //            inventorPropDict.Add("ParentPartNumber", parentPartNum);
        //            inventorPropDictList.Add(inventorPropDict);
        //            continue;
        //        }

        //    }

        //}

        private Task GetComponents(IProgress<string> prog, string parentPartNum, Inventor.ComponentOccurrences inCollection, ref List<Dictionary<string, string>> dictList)
        {
            List<Dictionary<string, string>> inventorPropDictList = dictList;

            return Task.Run(async () =>
            {
                // this is a recursive function that gets the properties of all the components and sub components in 'inCollection'

                Inventor.ApprenticeServerComponent apprentice = new Inventor.ApprenticeServerComponent();

                var assemblyQuery =
                    from Inventor.ComponentOccurrence assembly in inCollection
                    where assembly.DefinitionDocumentType == Inventor.DocumentTypeEnum.kAssemblyDocumentObject ||
                          assembly.DefinitionDocumentType == Inventor.DocumentTypeEnum.kPartDocumentObject
                    orderby assembly.DefinitionDocumentType descending
                    select assembly;

                //iterate through the components in the current collection
                IEnumerator objoccsEnumerator = assemblyQuery.GetEnumerator();

                Inventor.ComponentOccurrence objcompOccurrence;

                while (objoccsEnumerator.MoveNext() == true)
                {
                    Inventor.DocumentTypeEnum documentType = new Inventor.DocumentTypeEnum();
                    string thickness = "";
                    string material = "";
                    string desc = "";
                    string displayName = "";
                    string fileName = "";
                    string parentFileName = "";

                    Dictionary<string, string> inventorPropDict = new Dictionary<string, string>();
                    Inventor.ApprenticeServerDocument apprenticeDoc;
                    try
                    {
                        objcompOccurrence = (Inventor.ComponentOccurrence)objoccsEnumerator.Current;
                        try
                        {
                            // the following line throws an exception sometimes when getting components more than one level deep.
                            //  I have no idea why....except that this is a recursive function, so it must have something to do with that.

                            if (objcompOccurrence.ReferencedDocumentDescriptor.ReferencedDocument == null)
                            {
                                // this block of code was added because I kept getting the occasional objcompOccurrence that had a null referenced document..
                                // but as soon as I put in this if block, it never seemed to happen anymore!  So I have never observed this code
                                // being hit yet....I have no idea if it would work or not....

                                // ok i have seen it get hit now...but only when the component in question is not released.
                                // the following code I had figured would search for an alternative part with the same file name that was in the temp
                                // folder already.  I see now that it would need to be revised to include the path and the file extension.  But in this case,
                                // if the part is not released, we really don't want to find an alternate file, so i'll just leave it for now....
                                string fName = Path.GetFileNameWithoutExtension(objcompOccurrence.ReferencedDocumentDescriptor.FullDocumentName);
                                Inventor.ApprenticeServerDocument appDoc = apprentice.Open(fName);
                                objcompOccurrence = appDoc.ComponentDefinition.Document;
                            }

                            Debug.WriteLine("Before cDef");
                            Inventor.ComponentDefinition cDef = objcompOccurrence.Definition;
                            Debug.WriteLine("Before appenticeDoc");
                            apprenticeDoc = objcompOccurrence.Definition.Document;
                            Debug.WriteLine("Before apprentice");
                            apprentice.Open(apprenticeDoc.FullFileName);



                            try
                            {

                                fileName = Path.GetFileNameWithoutExtension(apprenticeDoc.FullDocumentName);
                                prog.Report("Reading Inventor properties for " + fileName + "...");
                            }
                            catch (Exception)
                            {
                                fileName = "undefined";
                            }

                            try
                            {
                                Inventor.PropertySet userDefinedPropertiesSet;
                                userDefinedPropertiesSet = apprenticeDoc.PropertySets["User Defined Properties"];
                                Inventor.Property thickProp = userDefinedPropertiesSet["Thickness"];
                                thickness = thickProp.Value;
                            }
                            catch (Exception)
                            {
                                thickness = "undefined";
                            }

                            try
                            {
                                Inventor.PropertySet designTrackingProperties = apprenticeDoc.PropertySets["Design Tracking Properties"];
                                Inventor.Property materialProp = designTrackingProperties["Material"];
                                material = materialProp.Value;
                            }
                            catch (Exception)
                            {
                                material = "undefined";
                            }

                            try
                            {
                                Inventor.PropertySet designTrackingProperties = apprenticeDoc.PropertySets["Design Tracking Properties"];
                                Inventor.Property materialProp = designTrackingProperties["Description"];
                                desc = materialProp.Value;
                            }
                            catch (Exception)
                            {
                                desc = "undefined";
                            }

                            if (objcompOccurrence.Parent != null)
                            {
                                Debug.WriteLine("Before cDef2");
                                objcompOccurrence = (Inventor.ComponentOccurrence)objoccsEnumerator.Current;
                                Debug.WriteLine("Before appenticeDoc2");
                                Inventor.ApprenticeServerDocument apprenticeParentDoc = objcompOccurrence.Parent.Document;
                                Debug.WriteLine("Before apprentice2");
                                apprentice.Open(apprenticeParentDoc.FullFileName);

                                parentFileName = Path.GetFileNameWithoutExtension(apprenticeParentDoc.FullDocumentName);

                                inventorPropDict.Add("ParentPartNumber", parentFileName);

                                if (parentFileName == fileName)    // seems there are some components referencing themselves as parents.  We don't want them...
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                inventorPropDict.Add("ParentPartNumber", "");
                            }

                            documentType = objcompOccurrence.DefinitionDocumentType;

                            inventorPropDict.Add("PartNumber", fileName);

                            inventorPropDict.Add("Material Thickness", thickness);
                            inventorPropDict.Add("Material Type", material);
                            inventorPropDict.Add("Description", desc);
                            inventorPropDict.Add("Quantity", "1");
                            inventorPropDict.Add("DocumentType", documentType.ToString());

                            string checkPartNum = null;
                            string checkExistingParentPartNum = null;
                            string checkQty = null;
                            bool matchfound = false;
                            Dictionary<string, string> dictToRemove = new Dictionary<string, string>();

                            foreach (Dictionary<string, string> t in inventorPropDictList)
                            {

                                if (t.TryGetValue("PartNumber", out checkPartNum))  // do we already have this part in the list?
                                {
                                    prog.Report("Reading Inventor properties for " + checkPartNum + "...");
                                    if (checkPartNum == fileName)
                                    {

                                        t.TryGetValue("ParentPartNumber", out checkExistingParentPartNum);  // get its parent part number
                                        if (checkExistingParentPartNum == parentFileName)        // if parentPartNumber of check item matches parentPartNumber of current item, we only need to increase quantity
                                        {
                                            t.TryGetValue("Quantity", out checkQty);     // and its current quantity
                                            dictToRemove = t;
                                            matchfound = true;  // we found a match
                                            break;
                                        }
                                    }
                                }
                            }

                            if (matchfound)
                            {
                                if (parentFileName == checkExistingParentPartNum)
                                {
                                    int newQty = int.Parse(checkQty);
                                    newQty++;
                                    inventorPropDict["Quantity"] = newQty.ToString();
                                    inventorPropDictList.Remove(dictToRemove);
                                }
                            }


                            inventorPropDictList.Add(inventorPropDict);

                            Inventor.ComponentOccurrences oSubComponentOccurences = apprenticeDoc.ComponentDefinition.Occurrences;
                            //apprentice.Close();
                            apprenticeDoc.Close();

                            await GetComponents(prog, fileName, oSubComponentOccurences, ref inventorPropDictList);
                        }
                        catch (Exception ex)
                        {
                            // create undefined component
                            inventorPropDict.Clear();
                            inventorPropDict.Add("EntityName", null);
                            string partNum = Path.GetFileNameWithoutExtension(objcompOccurrence.ReferencedDocumentDescriptor.FullDocumentName);
                            partNum = partNum + " - (Inaccessible Component)";
                            inventorPropDict.Add("PartNumber", partNum);
                            inventorPropDict.Add("Material Thickness", "undefined");
                            inventorPropDict.Add("Material Type", "undefined");
                            inventorPropDict.Add("Quantity", "1");
                            inventorPropDict.Add("DocumentType", objcompOccurrence.ReferencedDocumentDescriptor.ReferencedDocumentType.ToString());
                            inventorPropDict.Add("ParentPartNumber", parentPartNum);
                            inventorPropDict.Add("Description", "Inaccessible Component");
                            inventorPropDict.Add("Title", null);
                            inventorPropDictList.Add(inventorPropDict);

                            continue;
                        }
                    }

                    catch (Exception ex)
                    {
                        // create undefined component
                        // not fully tested yet....
                        inventorPropDict.Clear();
                        inventorPropDict.Add("EntityName", "Inaccessible Component");
                        inventorPropDict.Add("PartNumber", displayName);
                        inventorPropDict.Add("Material Thickness", "undefined");
                        inventorPropDict.Add("Material Type", "undefined");
                        inventorPropDict.Add("Quantity", "1");
                        inventorPropDict.Add("DocumentType", documentType.ToString());
                        inventorPropDict.Add("ParentPartNumber", parentPartNum);
                        inventorPropDictList.Add(inventorPropDict);
                        continue;
                    }
                }
            });
        }

        private bool GetQtyOfAllParents(string currentPartNum, List<Dictionary<string, string>> inventorPropDictList, ref int qty)
        {
            // return the quantity of the item defined by currentPartNum in 'inventorPropDictList'
            string checkPartNum = "";
            string parentPartNum = "";
            try
            {
                foreach (Dictionary<string, string> d in inventorPropDictList)
                {
                    if (d.TryGetValue("PartNumber", out checkPartNum))
                    {
                        if (checkPartNum == currentPartNum)
                        {
                            // get the parent part number of the current part.
                            d.TryGetValue("ParentPartNumber", out parentPartNum);

                            if (parentPartNum != "none")
                            {
                                // now search through the dictionary again to find the parent component
                                foreach (Dictionary<string, string> d2 in inventorPropDictList)
                                {
                                    if (d2.TryGetValue("PartNumber", out checkPartNum))
                                    {
                                        if (checkPartNum == parentPartNum)
                                        {
                                            string checkQty = "";
                                            d2.TryGetValue("Quantity", out checkQty);
                                            qty = qty * int.Parse(checkQty);
                                            GetQtyOfAllParents(checkPartNum, inventorPropDictList, ref qty);
                                        }

                                    }
                                }
                            }
                            else
                            {
                                if (qty == 0) qty = 1;
                                return true;
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception)
            {
                qty = 1;
                return false;
            }
        }

        private void CorrectQuantities(List<Dictionary<string, string>> inventorPropDictList)
        {
            // this routine corrects the quantities from a per root assembly count to per parent assembly count.

            int qtyOfAllParents = 1, oldQty = 0, newQty = 0;
            string curPartNumber = "", oldQtyString = "";

            foreach (Dictionary<string, string> d in inventorPropDictList)
            {
                d.TryGetValue("PartNumber", out curPartNumber);
                d.TryGetValue("Quantity", out oldQtyString);
                oldQty = int.Parse(oldQtyString);



                GetQtyOfAllParents(curPartNumber, inventorPropDictList, ref qtyOfAllParents);
                newQty = oldQty / qtyOfAllParents;

                d["Quantity"] = newQty.ToString();
                qtyOfAllParents = 1;
            }
        }

        public Boolean PrintAssociatedIDWToPDF(string vaultID)
        {
            if (vaultID == "")
            {
                MessageBox.Show("No Vault File associated with this Component");
                return false;
            }
            else
            {
                // get model name from vaultID
                using (Autodesk.Connectivity.WebServicesTools.WebServiceManager serviceManager = new Autodesk.Connectivity.WebServicesTools.WebServiceManager(m_conn.WebServiceManager.WebServiceCredentials))
                {
                    ACW.File[] fArray;
                    fArray = serviceManager.DocumentService.FindFilesByIds(new long[] { long.Parse(vaultID) });
                    VDF.Vault.Currency.Entities.FileIteration fIter = new Autodesk.DataManagement.Client.Framework.Vault.Currency.Entities.FileIteration(m_conn, fArray[0]);

                    string logMessage = "";
                    string errMessage = "";
                    if (!PrintIDWToPDF(fIter, ref errMessage, ref logMessage))
                        return false;

                    //string debugFileName = AppSettings.Get("VaultExportFilePath") + "debug.txt";
                    //System.IO.File.WriteAllText(debugFileName, logMessage);

                }
            }

            return true;
        }

        public Dictionary<long, string> GetIDWsAssociatedWithModelByVaultID(long vaultID)
        {
            List<ACW.File> idwList = new List<ACW.File>();
            List<long> sortedIDList = new List<long>();
            Dictionary<long, string> idwDict = new Dictionary<long, string>();
            Dictionary<long, string> sortedDict = new Dictionary<long, string>();

            using (Autodesk.Connectivity.WebServicesTools.WebServiceManager serviceManager = new Autodesk.Connectivity.WebServicesTools.WebServiceManager(m_conn.WebServiceManager.WebServiceCredentials))
            {
                // get all parent and child information for the file
                ACW.FileAssocLite[] associationArray = m_conn.WebServiceManager.DocumentService.GetFileAssociationLitesByIds(
                    new long[] { vaultID },
                    ACW.FileAssocAlg.LatestTip,
                    ACW.FileAssociationTypeEnum.Attachment, true,        // parent associations
                    ACW.FileAssociationTypeEnum.None, true,   // child associations
                    true, true, false);

                if (associationArray != null && associationArray.Length > 0)
                {
                    List<long> IDList = new List<long>();
                    foreach (ACW.FileAssocLite fileAssoc in associationArray)
                    {
                        IDList.Add(fileAssoc.ParFileId);
                    }
                    ACW.File[] allAssociationsArray = serviceManager.DocumentService.FindFilesByIds(IDList.ToArray());

                    foreach (ACW.File f in allAssociationsArray)
                    {
                        VDF.Vault.Currency.Entities.FileIteration fileIter = new Autodesk.DataManagement.Client.Framework.Vault.Currency.Entities.FileIteration(m_conn, f);
                        if (fileIter.EntityName.EndsWith(".idw"))
                        {
                            idwList.Add(fileIter);
                            if (!idwDict.ContainsKey(fileIter.EntityIterationId))
                            {
                                idwDict.Add(fileIter.EntityIterationId, fileIter.EntityName);
                            }
                        }
                    }
                }
            }

            // sort the idw files by date
            //idwList.OrderByDescending(x => x.CkInDate).ToList();
            idwDict.OrderByDescending(x => x.Value).ToList();

            // return a list of vault IDs sorted by date from newest to oldest.
            //foreach(ACW.File f in idwList)
            //{
            //    sortedIDList.Add(f.Id);
            //}


            return idwDict;
        }

        public Dictionary<long, string> GetIDWsAssociatedWithModelByVaultName(string fileName)
        {
            try
            {
                PropDef[] filePropDefs =
                                    m_conn.WebServiceManager.PropertyService.GetPropertyDefinitionsByEntityClassId("FILE");
                PropDef vaultNamePropDef = filePropDefs.Single(n => n.SysName == "Name");

                SrchCond vaultName = new SrchCond()
                {
                    PropDefId = vaultNamePropDef.Id,
                    PropTyp = PropertySearchType.SingleProperty,
                    SrchOper = 3, // equals
                    SrchRule = SearchRuleType.Must,
                    SrchTxt = fileName
                };


                string bookmark = string.Empty;
                SrchStatus status = null;
                Autodesk.Connectivity.WebServices.File[] searchResults =
                    m_conn.WebServiceManager.DocumentService.FindFilesBySearchConditions(
                    new SrchCond[] { vaultName },
                    null, null, false, true, ref bookmark, out status);


                // this needs to be completed yet...
                Dictionary<long, string> idwFileList = new Dictionary<long, string>();
                if (searchResults != null)
                {
                    if (searchResults.Count() == 1)
                    {
                        VDF.Vault.Currency.Entities.FileIteration f = new Autodesk.DataManagement.Client.Framework.Vault.Currency.Entities.FileIteration(m_conn, searchResults[0]);

                        idwFileList = GetIDWsAssociatedWithModelByVaultID(f.EntityIterationId);

                        return idwFileList;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                return null;
            }

        }

        public bool CheckIDWsForDrawingOfModel(long modelID, List<long> vaultIDsOfIDWsList, out Dictionary<long, bool> resultDict)
        {
            string TargetFolder = System.IO.Path.GetTempPath();
            string modelName = "";
            ACW.File[] idwFileArray;
            resultDict = new Dictionary<long, bool>();

            vaultIDsOfIDWsList = vaultIDsOfIDWsList.Distinct().ToList();

            // get model name from vaultID
            using (Autodesk.Connectivity.WebServicesTools.WebServiceManager serviceManager = new Autodesk.Connectivity.WebServicesTools.WebServiceManager(m_conn.WebServiceManager.WebServiceCredentials))
            {
                idwFileArray = serviceManager.DocumentService.FindFilesByIds(vaultIDsOfIDWsList.ToArray());
            }

            // make sure folder exists for downloading idw into.
            System.IO.DirectoryInfo targetDir = new System.IO.DirectoryInfo(TargetFolder);
            if (!targetDir.Exists)
            {
                targetDir.Create();
            }

            // download the idws from the vault
            VDF.Vault.Settings.AcquireFilesSettings downloadSettings = new VDF.Vault.Settings.AcquireFilesSettings(m_conn)
            {
                LocalPath = new VDF.Currency.FolderPathAbsolute(targetDir.FullName),
            };

            // download all the files
            foreach (ACW.File idwFile in idwFileArray)
            {
                VDF.Vault.Currency.Entities.FileIteration f = new Autodesk.DataManagement.Client.Framework.Vault.Currency.Entities.FileIteration(m_conn, idwFile);
                downloadSettings.AddFileToAcquire(f, VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Download);
            }
            m_conn.FileManager.AcquireFiles(downloadSettings);

            // iterate through all the downloaded idws and check each sheet to whether
            // it has a drawing of the model
            foreach (ACW.File idwFile in idwFileArray)
            {
                VDF.Vault.Currency.Entities.FileIteration f = new Autodesk.DataManagement.Client.Framework.Vault.Currency.Entities.FileIteration(m_conn, idwFile);

                string fileName = downloadSettings.LocalPath.ToString() + @"\" + f.ToString();

                VDF.Vault.Currency.Properties.PropertyDefinitionDictionary propDefs =
                                   new VDF.Vault.Currency.Properties.PropertyDefinitionDictionary();
                Inventor.ApprenticeServerComponent oApprentice = new Inventor.ApprenticeServerComponent();
                Inventor.ApprenticeServerDrawingDocument drgDoc;
                drgDoc = (Inventor.ApprenticeServerDrawingDocument)oApprentice.Document;
                oApprentice.Open(fileName);
                drgDoc = (Inventor.ApprenticeServerDrawingDocument)oApprentice.Document;
                PropDef[] filePropDefs =
                                m_conn.WebServiceManager.PropertyService.GetPropertyDefinitionsByEntityClassId("FILE");
                PropDef vaultNamePropDef = filePropDefs.Single(n => n.SysName == "Name");

                // for each sheet in the idw, check whether the current sheet has a drawing of the model in question
                foreach (Inventor.Sheet sh in drgDoc.Sheets)
                {
                    if (sh.DrawingViews.Count > 0)
                    {
                        if (sh != null)
                        {
                            try
                            {
                                string viewModelName = sh.DrawingViews[1].ReferencedDocumentDescriptor.DisplayName;
                                VDF.Vault.Currency.Entities.FileIteration fIter;

                                SrchCond vaultName = new SrchCond()
                                {
                                    PropDefId = vaultNamePropDef.Id,
                                    PropTyp = PropertySearchType.SingleProperty,
                                    SrchOper = 3, // equals
                                    SrchRule = SearchRuleType.Must,
                                    SrchTxt = viewModelName
                                };


                                string bookmark = string.Empty;
                                SrchStatus status = null;
                                Autodesk.Connectivity.WebServices.File[] searchResults =
                                    m_conn.WebServiceManager.DocumentService.FindFilesBySearchConditions(
                                    new SrchCond[] { vaultName },
                                    null, null, false, true, ref bookmark, out status);



                                propDefs = new VDF.Vault.Currency.Properties.PropertyDefinitionDictionary();
                                propDefs =
                                  m_conn.PropertyManager.GetPropertyDefinitions(
                                    VDF.Vault.Currency.Entities.EntityClassIds.Files,
                                    null,
                                    VDF.Vault.Currency.Properties.PropertyDefinitionFilter.IncludeAll
                                  );


                                if (searchResults == null)
                                {
                                    resultDict.Add(idwFile.Id, false);
                                }

                                else if (searchResults.Count() == 1)
                                {
                                    fIter = new VDF.Vault.Currency.Entities.FileIteration(m_conn, searchResults[0]);
                                    if (modelName == viewModelName)
                                    {
                                        resultDict.Add(idwFile.Id, true);
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                resultDict.Add(idwFile.Id, false);
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        private Boolean PrintIDWToPDF(VDF.Vault.Currency.Entities.FileIteration fileIter, ref string errMessage, ref string logMessage)
        {
            // prints each sheet of the idw(fileIter) to it's own pdf file.
            // if there's more than one sheet referencing the same assembly, they will be combined in one multi-page pdf

            try
            {
                //string TargetFolder = System.IO.Path.GetTempPath();
                //string VaultSearchEntity = "Name";

                //// make sure folder exists for downloading idw into.
                //logMessage += "Checking Target Directory...";
                //System.IO.DirectoryInfo targetDir = new System.IO.DirectoryInfo(TargetFolder);
                //if (!targetDir.Exists)
                //{
                //    targetDir.Create();
                //}
                //logMessage += "OK" + "\r\n";

                //// download the idw from the vault
                //logMessage += "Downloading idw from the vault...";
                //VDF.Vault.Settings.AcquireFilesSettings downloadSettings = new VDF.Vault.Settings.AcquireFilesSettings(m_conn)
                //{
                //    LocalPath = new VDF.Currency.FolderPathAbsolute(targetDir.FullName),
                //};
                //downloadSettings.AddFileToAcquire(fileIter, VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Download);
                //m_conn.FileManager.AcquireFiles(downloadSettings);

                //string fileName = downloadSettings.LocalPath.ToString() + @"\" + fileIter.ToString();
                //string modelName = "";

                //logMessage += "OK" + "\r\n";

                //// set up lists for storing the actual model names the sheets are referencing
                //logMessage += "Setting up data structures to hold file names...";

                //// two failures here Oct. 26th...

                //List<string> modelNames = new List<string>();
                //List<VDF.Vault.Currency.Entities.FileIteration> fIterations = new List<VDF.Vault.Currency.Entities.FileIteration>();

                //VDF.Vault.Currency.Properties.PropertyDefinitionDictionary propDefs =
                //               new VDF.Vault.Currency.Properties.PropertyDefinitionDictionary();
                //Inventor.ApprenticeServerComponent oApprentice = new Inventor.ApprenticeServerComponent();
                //Inventor.ApprenticeServerDrawingDocument drgDoc;
                //drgDoc = (Inventor.ApprenticeServerDrawingDocument)oApprentice.Document;
                //oApprentice.Open(fileName);
                //drgDoc = (Inventor.ApprenticeServerDrawingDocument)oApprentice.Document;
                //PropDef[] filePropDefs =
                //                m_conn.WebServiceManager.PropertyService.GetPropertyDefinitionsByEntityClassId("FILE");
                //PropDef vaultNamePropDef = filePropDefs.Single(n => n.SysName == "Name");

                //logMessage += "OK" + "\r\n";
                //logMessage += "Number of Sheets in drgDoc" + drgDoc.Sheets.Count + "\r\n";

                //// for each sheet in the idw, search the vault for the sheet's corresponding ipt or iam 
                //foreach (Inventor.Sheet sh in drgDoc.Sheets)
                //{
                //    logMessage += "Sheet Name: " + sh.Name + "\r\n";

                //    // ...2 failures here Oct. 23rd...
                //    // ...1 failure here Oct 28th....

                //    if (sh.DrawingViews.Count > 0)
                //    {
                //        modelName = sh.DrawingViews[1].ReferencedDocumentDescriptor.DisplayName;
                //        VDF.Vault.Currency.Entities.FileIteration fIter;
                //        try
                //        {
                //            logMessage += "Setting up to search condition...";

                //            SrchCond vaultName = new SrchCond()
                //            {
                //                PropDefId = vaultNamePropDef.Id,
                //                PropTyp = PropertySearchType.SingleProperty,
                //                SrchOper = 3, // equals
                //                SrchRule = SearchRuleType.Must,
                //                SrchTxt = modelName
                //            };


                //            string bookmark = string.Empty;
                //            SrchStatus status = null;
                //            Autodesk.Connectivity.WebServices.File[] searchResults =
                //                m_conn.WebServiceManager.DocumentService.FindFilesBySearchConditions(
                //                new SrchCond[] { vaultName },
                //                null, null, false, true, ref bookmark, out status);



                //            propDefs = new VDF.Vault.Currency.Properties.PropertyDefinitionDictionary();
                //            propDefs =
                //              m_conn.PropertyManager.GetPropertyDefinitions(
                //                VDF.Vault.Currency.Entities.EntityClassIds.Files,
                //                null,
                //                VDF.Vault.Currency.Properties.PropertyDefinitionFilter.IncludeAll
                //              );

                //            logMessage += "OK, search successful" + "\r\n";

                //            if (searchResults == null)
                //            {
                //                logMessage += "No corresponding model file found for " + modelName + "\r\n";
                //                ACW.File emptyFile = new ACW.File();
                //                fIter = new VDF.Vault.Currency.Entities.FileIteration(m_conn, emptyFile);
                //                fIterations.Add(fIter);
                //            }
                //            else if (searchResults.Count() > 1)
                //            {
                //                logMessage += "Multiple corresponding models found for " + modelName + "\r\n";
                //                ACW.File emptyFile = new ACW.File();
                //                fIter = new VDF.Vault.Currency.Entities.FileIteration(m_conn, emptyFile);
                //                fIterations.Add(fIter);
                //            }
                //            else
                //            {
                //                fIter = new VDF.Vault.Currency.Entities.FileIteration(m_conn, searchResults[0]);
                //                fIterations.Add(fIter);
                //                logMessage += "Match found\r\n";
                //            }
                //        }
                //        catch (Exception)
                //        {
                //            errMessage += "Unknown Error in function PrintPDF\r\n";
                //            return false;
                //        }
                //    }
                //}

                //// now we have a list of model file names stored in 'fIterations', one for every sheet in the idw.
                //// If we couldn't match the sheet up with a model, the list will have a blank entry.
                //// next we have to match each name up with whatever vault field we are using to name the pdf files...
                //// e.g. if we're using names, this is totally unnecessary, but if we want to use ERP Numbers, we have to 
                //// search them out.
                //// we'll then end up with a dictionary matching names up with the chosen vault field.

                //Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties.PropertyValues propList =
                //    new VDF.Vault.Currency.Properties.PropertyValues();

                //System.Collections.Generic.Dictionary<VDF.Vault.Currency.Entities.IEntity,
                //                                      Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties.PropertyValue>
                //                                      propDict = new Dictionary<VDF.Vault.Currency.Entities.IEntity,
                //                                                          VDF.Vault.Currency.Properties.PropertyValue>();

                //Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties.PropertyDefinition propDef =
                //    new VDF.Vault.Currency.Properties.PropertyDefinition(propDefs[VaultSearchEntity]);

                //propList = m_conn.PropertyManager.GetPropertyValues(
                //            fIterations, new Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties.PropertyDefinition[] { propDef }, null);
                //logMessage += "propList count: " + propList.Entities.Count() + "\r\n";

                //propDict = propList.GetValues(propDef);
                //logMessage += "propDict count: " + propDict.Values.Count() + "\r\n";

                //foreach (KeyValuePair<VDF.Vault.Currency.Entities.IEntity,
                //                     VDF.Vault.Currency.Properties.PropertyValue> pair in propDict)
                //{
                //    if (pair.Key.EntityMasterId != 0)
                //    {
                //        logMessage += "key: " + pair.Key + " value: " + pair.Value.Value + "\r\n";
                //        modelNames.Add(pair.Value.Value.ToString());
                //    }
                //    else
                //    {
                //        logMessage += "Blank entry\r\n";
                //        modelNames.Add("");
                //    }
                //}

                //logMessage += "Defining Print Object...";

                //...failed here one time Oct 28th...
                PrintPDF.PrintObject printOb = new PrintPDF.PrintObject();
                string errMsg = "";
                string logMsg = "";


                string TargetFolder = System.IO.Path.GetTempPath();
                string VaultSearchEntity = "Name";

                // make sure folder exists for downloading idw into.
                logMessage += "Checking Target Directory...";
                System.IO.DirectoryInfo targetDir = new System.IO.DirectoryInfo(TargetFolder);
                if (!targetDir.Exists)
                {
                    targetDir.Create();
                }
                logMessage += "OK" + "\r\n";

                // download the idw from the vault
                logMessage += "Downloading idw from the vault...";
                VDF.Vault.Settings.AcquireFilesSettings downloadSettings = new VDF.Vault.Settings.AcquireFilesSettings(m_conn)
                {
                    LocalPath = new VDF.Currency.FolderPathAbsolute(targetDir.FullName),
                };
                downloadSettings.AddFileToAcquire(fileIter, VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Download);
                m_conn.FileManager.AcquireFiles(downloadSettings);

                string fileName = downloadSettings.LocalPath.ToString() + @"\" + fileIter.ToString();

                //if (printOb.printToPDFNew(fileName, propDict, m_PDFPath, ref errMsg, ref logMsg))
                if (printOb.printToPDF(fileName, m_PDFPath, m_pdfPrinterName, m_psToPdfProgName, m_ghostScriptWorkingFolder, ref errMsg, ref logMsg))
                {
                    logMessage += logMsg;
                    return true;
                }
                else
                {
                    logMessage += logMsg;
                    errMessage += errMsg;
                    return false;
                }

            }
            catch (Exception ex)
            {
                errMessage += "Unknown Error in function PrintPDF\r\n";
                return false;
            }
        }

        public Boolean CheckForOutOfDateSheets(string idwFilePath)
        {
            // I never did get this to work....
            try
            {
                Inventor.ApprenticeServerComponent oApprentice = new Inventor.ApprenticeServerComponent();
                Inventor.ApprenticeServerDrawingDocument drgDoc;
                drgDoc = (Inventor.ApprenticeServerDrawingDocument)oApprentice.Document;
                oApprentice.Open(idwFilePath);
                drgDoc = (Inventor.ApprenticeServerDrawingDocument)oApprentice.Document;

                foreach (Inventor.Sheet sh in drgDoc.Sheets)
                {
                    if (sh.DrawingViews.Count > 0)
                    {
                        foreach (Inventor.DrawingView dv in sh.DrawingViews)
                        {
                            if (dv.ReferencedFile.DisplayName != sh._DisplayName)
                            {
                                int i = 0;//return true;       // found out of date idw...
                            }
                        }
                    }


                }

                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }

        public string GetVaultCheckOutComment(Vault.Currency.Entities.FileIteration selectedFile)
        {
            // a list of dictionaries of the vault properties of the associated files
            List<Dictionary<string, string>> vaultPropDictList = new List<Dictionary<string, string>>();

            // function that gets properties requires a list so we'll define a list and add the selected file to it...
            List<Vault.Currency.Entities.FileIteration> fileList = new List<Vault.Currency.Entities.FileIteration>();
            fileList.Add(selectedFile);

            // define a dictionary to hold all the properties pertaining to a vault entity
            Vault.Currency.Properties.PropertyDefinitionDictionary allPropDefDict = new Vault.Currency.Properties.PropertyDefinitionDictionary();
            allPropDefDict = m_conn.PropertyManager.GetPropertyDefinitions(Vault.Currency.Entities.EntityClassIds.Files, null, Vault.Currency.Properties.PropertyDefinitionFilter.IncludeAll);

            // define a list of only the properties that we are concerned about.
            List<Vault.Currency.Properties.PropertyDefinition> filteredPropDefList =
                    new List<Vault.Currency.Properties.PropertyDefinition>();

            List<string> propNames = new List<string> { "Comment" };

            // copy only definitions in propNames list from allPropDefDict to filteredPropDefList
            foreach (string s in propNames)
            {
                Vault.Currency.Properties.PropertyDefinition propDefs =
                    new Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties.PropertyDefinition();
                propDefs = allPropDefDict[s];

                filteredPropDefList.Add(propDefs);
            }

            // the following line should return a list of properties
            Vault.Currency.Properties.PropertyValues propValues = new Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties.PropertyValues();
            propValues = m_conn.PropertyManager.GetPropertyValues(fileList, filteredPropDefList, null);
            


            Vault.Currency.Properties.PropertyDefinition def = new Vault.Currency.Properties.PropertyDefinition(propNames[0]);
            Vault.Currency.Properties.PropertyValue val;

            Dictionary<Vault.Currency.Properties.PropertyDefinition, Vault.Currency.Properties.PropertyValue> d = propValues.GetValues(selectedFile);
            d.TryGetValue(def, out val);

            string key = null;
            if (def != null)
            {
                key = (def.SystemName == null) ? null : def.SystemName;

                string value = null;
                if (val != null)
                {
                    value = (val.Value == null) ? "" : val.Value.ToString();
                    return value;
                }
                else
                    value = "";
            }

            return null;

        }

        public Task<string> ExportVaultItemsByBatch(IProgress<string> prog, string batchName, ref List<string[]> returnList)
        {
            //*************************************************************************************
            // make sure this function gets updated in the ItemExport Project if changes are made.
            //*************************************************************************************
            List<string[]> productList = returnList;
            string logMessage = "";

            return Task.Run(() =>
            {
                try
                {
                    List<string[]> exportFileList = new List<string[]>();

                    int i = 0;
                    using (var fs = System.IO.File.OpenRead(batchName))
                    using (var reader = new StreamReader(fs))
                    {
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();
                            string[] values;
                            values = new string[4];
                            values = line.Split(',');
                            int qty = int.Parse(values[0]);
                            string itemName = values[1];
                            string batchTitle = values[3];

                            ACW.Item searchItem = null;

                            try
                            {
                                searchItem = m_conn.WebServiceManager.ItemService.GetLatestItemByItemNumber(itemName);
                            }
                            catch (Exception)
                            {
                                logMessage = "Error in retrieving item \"" + itemName + "\" from the Vault.\nBatch processing will be aborted.";
                                return logMessage;
                            }

                            prog.Report("Retrieving " + itemName + " From the Vault.");

                            // update the item before we export it
                            //if (!UpdateItem(searchItem, m_conn))
                            //{
                            //    logMessage = "Error updating " + searchItem;
                            //    return logMessage;
                            //}

                            string filename = (string)PrintPDF.AppSettings.Get("VaultExportFilePath") + "VaultExportData" + i + ".txt";

                            PackageService packageSvc = m_conn.WebServiceManager.PackageService;

                            // export to CSV file
                            PkgItemsAndBOM pkgBom = packageSvc.GetLatestPackageDataByItemIds(new long[] { searchItem.Id }, BOMTyp.Latest);

                            // Create a mapping between Item properties and columns in the CSV file
                            MapPair parentPair = new MapPair();
                            parentPair.ToName = "Parent";
                            parentPair.FromName = "BOMStructure-41FF056B-8EEF-47E2-8F9E-490BC0C52C71";

                            MapPair numberPair = new MapPair();
                            numberPair.ToName = "Number";
                            numberPair.FromName = "Number";

                            MapPair titlePair = new MapPair();
                            titlePair.ToName = "Title (Item,CO)";
                            titlePair.FromName = "Title(Item,CO)";

                            MapPair descriptionPair = new MapPair();
                            descriptionPair.ToName = "Item Description";
                            descriptionPair.FromName = "Description(Item,CO)";

                            MapPair categoryNamePair = new MapPair();
                            categoryNamePair.ToName = "CategoryName";
                            categoryNamePair.FromName = "CategoryName";

                            MapPair thicknessPair = new MapPair();
                            thicknessPair.ToName = "Thickness";
                            thicknessPair.FromName = "7c5169ad-9081-4aa7-b1a3-4670edae0b8c";

                            MapPair materialPair = new MapPair();
                            materialPair.ToName = "Material";
                            materialPair.FromName = "Material";

                            MapPair operationsPair = new MapPair();
                            operationsPair.ToName = "Operations";
                            operationsPair.FromName = "794d5b7d-49b5-49ba-938a-7e341a7ff8e4";

                            MapPair quantityPair = new MapPair();
                            quantityPair.ToName = "Quantity";
                            quantityPair.FromName = "Quantity-41FF056B-8EEF-47E2-8F9E-490BC0C52C71";

                            MapPair structCodePair = new MapPair();
                            structCodePair.ToName = "Structural Code";
                            structCodePair.FromName = "e3811c7a-a3ee-4f67-b34e-cbc892640616";

                            MapPair plantIDPair = new MapPair();
                            plantIDPair.ToName = "Plant ID";
                            plantIDPair.FromName = "eff195ae-da71-4929-b3df-2d6fd1e25f53";

                            MapPair isStockPair = new MapPair();
                            isStockPair.ToName = "Is Stock";
                            isStockPair.FromName = "f78c17cd-86d1-4728-96b5-8001fb58b67f";

                            MapPair requiresPDFPair = new MapPair();
                            requiresPDFPair.ToName = "Requires PDF";
                            requiresPDFPair.FromName = "6df4ae8b-fbd9-4e62-b801-a46097d4f9c5";

                            MapPair commentPair = new MapPair();
                            commentPair.ToName = "Comment";
                            commentPair.FromName = "Comment";

                            MapPair modDatePair = new MapPair();
                            modDatePair.ToName = "Date Modified";
                            modDatePair.FromName = "ModDate";

                            MapPair statePair = new MapPair();
                            statePair.ToName = "State";
                            statePair.FromName = "State";

                            MapPair stockNamePair = new MapPair();
                            stockNamePair.ToName = "Stock Name";
                            stockNamePair.FromName = "a42ca550-c503-4835-99dd-8c4d4ff6dbaf";

                            MapPair keywordsPair = new MapPair();
                            keywordsPair.ToName = "Keywords";
                            keywordsPair.FromName = "Keywords";

                            MapPair notesPair = new MapPair();
                            notesPair.ToName = "Notes";
                            notesPair.FromName = "0d012a5c-cc28-443c-b44e-735372eee117";

                            MapPair revisionNumberPair = new MapPair();
                            revisionNumberPair.ToName = "Revision";
                            revisionNumberPair.FromName = "Revision";

                            FileNameAndURL fileNameAndUrl = packageSvc.ExportToPackage(pkgBom, FileFormat.TDL_LEVEL,
                                    new MapPair[] { parentPair, numberPair, titlePair, descriptionPair,categoryNamePair, thicknessPair,
                                    materialPair,operationsPair,quantityPair,structCodePair,plantIDPair,isStockPair,requiresPDFPair,
                                    commentPair,modDatePair,statePair,stockNamePair,keywordsPair,notesPair,revisionNumberPair});

                            long currentByte = 0;
                            long partSize = m_conn.PartSizeInBytes;
                            using (FileStream fsOut = new FileStream(filename, FileMode.Create))
                            {
                                while (currentByte < fileNameAndUrl.FileSize)
                                {
                                    long lastByte = currentByte + partSize < fileNameAndUrl.FileSize ? currentByte + partSize : fileNameAndUrl.FileSize;
                                    byte[] contents = packageSvc.DownloadPackagePart(fileNameAndUrl.Name, currentByte, lastByte);
                                    fsOut.Write(contents, 0, (int)(lastByte - currentByte));
                                    currentByte += partSize;
                                }
                            }

                            // create a list to hold all the IDs of the BOM items
                            List<long> idList = new List<long>();

                            // create a dictionary to match up the exported ids with the exported item numbers
                            Dictionary<string, long> bomDict = new Dictionary<string, long>();

                            // loop through all the bomItems and extract the IDs along with the item numbers
                            foreach (var v in pkgBom.PkgItemArray)
                            {
                                idList.Add(v.ID);
                                bomDict.Add(v.ItemNum, v.ID);
                            }

                            // now create a list to store all the BOM items in
                            List<BOMComp> bomList = new List<BOMComp>();

                            // we now need to replace the item number with the primary file name field.  I can't figure out how to map it above, so we have to open the text file,
                            // search through it line by line, and query the vault for the primary file name link of of each item number.  We then save the text file again with the
                            // primary file name now in place of the item number....

                            List<string> lineList = new List<string>(); // create a list of lines to save the file text in.

                            using (StreamReader reader2 = System.IO.File.OpenText(filename))
                            {
                                string line2;
                                line2 = reader2.ReadLine();       // first line is header, just save it the the list, don't process it
                                lineList.Add(line2);

                                int lineNum = 1;
                                while ((line2 = reader2.ReadLine()) != null)
                                {
                                    line2 = line2.Replace("\"", "");

                                    string[] items = line2.Split('\t');
                                    string origItemNumber = items[1];

                                    // download all the bom items into a list in one API call
                                    bomList = m_conn.WebServiceManager.ItemService.GetPrimaryComponentsByItemIds(idList.ToArray()).ToList();

                                    // create another dictionary to match up the IDs with the bomComps
                                    Dictionary<long, BOMComp> bomDict2 = new Dictionary<long, BOMComp>();

                                    // populate the second dictionary
                                    int index2 = 0;
                                    foreach (long l in idList)
                                    {
                                        bomDict2.Add(l, bomList[index2]);
                                        index2++;
                                    }

                                    long searchID = 0;
                                    bomDict.TryGetValue(origItemNumber, out searchID);

                                    BOMComp searchbomComp = new BOMComp();
                                    bomDict2.TryGetValue(searchID, out searchbomComp);

                                    long primaryLinkID = searchbomComp.XRefId;  // get the id of the primary linked file
                                    if (primaryLinkID != -1)    // if we have a primary file name, use it
                                    {
                                        // we could likely speed this up some more if we grouped this all into one call rather than one by one, but code would get messier yet...
                                        Autodesk.Connectivity.WebServices.File primaryLinkFile = m_conn.WebServiceManager.DocumentService.GetFileById(primaryLinkID);
                                        string primaryLinkName = primaryLinkFile.Name;

                                        items[1] = primaryLinkName;

                                        string newItemString = "";

                                        foreach (string s in items)
                                        {
                                            newItemString += s + "\t";
                                        }
                                        lineList.Add(newItemString);
                                    }
                                    else        // otherwise just use the item number, for example top level items will fall into this category
                                    {
                                        lineList.Add(line2);
                                    }

                                    lineNum++;
                                }
                            }

                            using (StreamWriter writer = new StreamWriter(filename, false))
                            {
                                foreach (string s in lineList)
                                {
                                    writer.WriteLine(s);
                                }
                            }

                            string[] returnVals;
                            returnVals = new string[5];

                            int index = 0;
                            foreach (string s in values)
                            {
                                returnVals[index] = values[index];
                                index++;
                            }
                            returnVals[4] = filename;
                            productList.Add(returnVals);
                            i++;
                        }
                    }
                    productList = exportFileList;

                }
                catch (Exception ex)
                {
                    logMessage = "Unknown Error in Importing Batch.  \nPossible Formatting Error in Batch File.";
                    return logMessage;
                }
                return logMessage;
            });

        }

        public bool UpdateItem(Autodesk.Connectivity.WebServices.Item item, VDF.Vault.Currency.Connections.Connection connection)
        {
            //*************************************************************************************
            // make sure this function gets updated in the ItemExport Project if changes are made.
            //*************************************************************************************

            if (item == null)
            {
                return false;
            }

            long[] itemRevisionIds = new long[1];
            itemRevisionIds[0] = item.RevId;

            Item[] itemsToCommit = new Item[0];
            long[] itemsToCommit_Ids = new long[0];
            try
            {
                ItemService itemSvc =
                   connection.WebServiceManager.ItemService;

                // doesn't seem to be necessary to put items into edit state to update them
                //itemSvc.EditItems(itemRevisionIds);     

                itemSvc.UpdatePromoteComponents(itemRevisionIds, ItemAssignAll.Default, false);

                DateTime now = DateTime.Now;

                GetPromoteOrderResults compO = itemSvc.GetPromoteComponentOrder(out now);
                //long[] compO = itemSvc.GetPromoteComponentOrder(out now);

                ArrayList compOrder = new ArrayList(compO.PrimaryArray);
                ArrayList subSet = new ArrayList();
                int setSize = 100;
                int setNum = 0;

                //get full sets
                while (setNum < compOrder.Count / setSize)
                {
                    subSet = compOrder.GetRange
                                  (setNum * setSize, setSize);
                    itemSvc.PromoteComponents
                  (now, (long[])subSet.ToArray(typeof(long)));
                    setNum++;
                }

                //get remaining set
                if (compOrder.Count % setSize > 0)
                {
                    subSet = compOrder.GetRange
                (setNum * setSize, compOrder.Count % setSize);
                    itemSvc.PromoteComponents
                  (now, (long[])subSet.ToArray(typeof(long)));
                }

                ItemsAndFiles result = itemSvc.
                             GetPromoteComponentsResults(now);
                Item[] items = null;
                items = result.ItemRevArray;
                int[] statusArray = result.StatusArray;
                // loop through the Items in the ItemRevArray
                for (int i = 0; i < items.Length; i++)
                {
                    // see if the item in the ItemRevArray
                    //has been updated (not equal to 1)
                    if (statusArray[i] != 1)
                    {
                        //change the size of the array
                        Array.Resize(ref itemsToCommit,
                                    itemsToCommit.Length + 1);
                        //add the updated item to the array
                        //that will be committed
                        itemsToCommit[itemsToCommit.Length - 1]
                                                      = items[i];
                    }


                }
                //commit the updated items             
                itemSvc.UpdateAndCommitItems(itemsToCommit);
                // Testing catch - this could cause error
                // as items contains Items that may
                // not need to be updated
                // itemSvc.UpdateAndCommitItems(items);
            }
            catch
            {
                // get the items that need to be undone
                for (int i = 0; i < itemsToCommit.Length; i++)
                {
                    // change the size of the array
                    // of Ids (long)
                    Array.Resize(ref itemsToCommit_Ids,
                               itemsToCommit_Ids.Length + 1);
                    //Add the id to the array that will be
                    // used in UndoEditItems()     
                    itemsToCommit_Ids
                        [itemsToCommit_Ids.Length - 1]
                                       = itemsToCommit[i].Id;
                }
                connection.WebServiceManager.ItemService.
                            UndoEditItems(itemsToCommit_Ids);

            }

            return true;
        }

    }

}




