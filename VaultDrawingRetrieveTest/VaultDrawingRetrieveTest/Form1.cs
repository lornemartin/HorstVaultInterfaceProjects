using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Inventor;
using Vault = Autodesk.DataManagement.Client.Framework.Vault;
using ACW = Autodesk.Connectivity.WebServices;
using VDF = Autodesk.DataManagement.Client.Framework;
using Autodesk.Connectivity.WebServices;

namespace VaultDrawingRetrieveTest
{
    public partial class Form1 : Form
    {
        private Vault.Currency.Connections.Connection m_conn { get; set; }
        private string VaultServer { get; set; }
        private string VaultUserName { get; set; }
        private string VaultPassword { get; set; }
        private string VaultName { get; set; }
        public Form1()
        {
            InitializeComponent();
            VaultServer = "hwvsvt01";
            VaultUserName = "lorne";
            VaultPassword = "lorne";
            VaultName = "Vault";

            LoginToVault();

        }


        private void button1_Click(object sender, EventArgs e)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            //string modelName = "BOQF10AL795Q80DEG-001.iam";
            //string modelName = "LA-LFU1.ipt";
            string modelName = "SB4000-014.ipt";
            string tempFolder = @"C:\temp\";

            Dictionary<long, string> idwDict = new Dictionary<long, string>();
            idwDict = GetIDWsAssociatedWithModelByVaultName(modelName);

            if (idwDict.Count >= 1)
            {
                string idwName = idwDict.First().Value;
                long idwID = idwDict.First().Key;
                int pageNumber = 0;
                int matchingPageStartNumber = 0;
                int matchingPageEndNumber = 0;

                searrchAndDownloadIDW(idwName, tempFolder);

                string idw = System.IO.Path.Combine(tempFolder, idwName);
                ApprenticeServerComponent oApprentice = new ApprenticeServerComponent();
                ApprenticeServerDrawingDocument drgDoc;
                oApprentice.Open(idw);
                drgDoc = (ApprenticeServerDrawingDocument)oApprentice.Document;


                VDF.Vault.Currency.Properties.PropertyDefinitionDictionary propDefs =
                                   new VDF.Vault.Currency.Properties.PropertyDefinitionDictionary();
                PropDef[] filePropDefs =
                                m_conn.WebServiceManager.PropertyService.GetPropertyDefinitionsByEntityClassId("FILE");
                PropDef vaultNamePropDef = filePropDefs.Single(n => n.SysName == "Name");

                Dictionary<long, bool> resultDict = new Dictionary<long, bool>();
                // for each sheet in the idw, check whether the current sheet has a drawing of the model in question
                foreach (Inventor.Sheet sh in drgDoc.Sheets)
                {
                    pageNumber += 1;
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
                                    //resultDict.Add(idwID, false);
                                }

                                else if (searchResults.Count() == 1)
                                {
                                    fIter = new VDF.Vault.Currency.Entities.FileIteration(m_conn, searchResults[0]);
                                    if (modelName == viewModelName)
                                    {
                                        //resultDict.Add(idwID, true);
                                        if (matchingPageStartNumber == 0)
                                            matchingPageStartNumber = pageNumber;
                                        else
                                            matchingPageEndNumber = pageNumber;
                                        
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                //resultDict.Add(idwID, false);
                                //return false;
                            }
                        }
                    }
                }

                if (matchingPageEndNumber == 0)
                    matchingPageEndNumber = matchingPageStartNumber;

                ApprenticeDrawingPrintManager pMgr = (ApprenticeDrawingPrintManager)drgDoc.PrintManager;
                pMgr.Printer = "Microsoft Print to PDF";
                pMgr.NumberOfCopies = 1;
                pMgr.SetSheetRange(matchingPageStartNumber,matchingPageEndNumber);
                pMgr.PrintRange = PrintRangeEnum.kPrintSheetRange;
                pMgr.Orientation = PrintOrientationEnum.kLandscapeOrientation;
                pMgr.PrintToFile(System.IO.Path.Combine(tempFolder, "output.pdf"));

                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;

                MessageBox.Show("Done in " + elapsedMs + " milliseconds.");
            }

        }

        bool LoginToVault()
        {
            try
            {
                // this login does not consume a license.
                Vault.Forms.Library.Initialize();

                Vault.Results.LogInResult results = Vault.Library.ConnectionManager.LogIn(VaultServer, VaultName, VaultUserName, VaultPassword, Vault.Currency.Connections.AuthenticationFlags.ReadOnly, null);

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
                        }
                    }
                }
            }

            // sort the idw files by date, this should get the last one checked in
            idwList = idwList.OrderBy(x => x.CkInDate.ToLongDateString()).ToList();
            foreach (ACW.File f in idwList)
            {
                VDF.Vault.Currency.Entities.FileIteration fileIter = new Autodesk.DataManagement.Client.Framework.Vault.Currency.Entities.FileIteration(m_conn, f);
                if (!sortedDict.ContainsKey(fileIter.EntityIterationId))
                {
                    sortedDict.Add(fileIter.EntityIterationId, fileIter.EntityName);
                }
            }

            return sortedDict;
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

        public bool searrchAndDownloadIDW(string fileName, string downloadFolder)
        {
            try
            {
                PropDef[] filePropDefs = m_conn.WebServiceManager.PropertyService.GetPropertyDefinitionsByEntityClassId("FILE");
                PropDef vaultNamePropDef = filePropDefs.Single(n => n.SysName == "Name");

                Autodesk.Connectivity.WebServices.SrchCond searchCondition = new SrchCond();
                searchCondition.PropDefId = vaultNamePropDef.Id;
                searchCondition.PropTyp = PropertySearchType.SingleProperty;
                searchCondition.SrchOper = 3;
                searchCondition.SrchTxt = fileName;


                string bookmark = "";
                SrchStatus st = new SrchStatus();

                Autodesk.Connectivity.WebServices.File[] files = m_conn.WebServiceManager.DocumentService.FindFilesBySearchConditions(new SrchCond[] { searchCondition }, null, null, true, true, ref bookmark, out st);
                if (files.Count() != 1)
                {
                    return false;
                }

                Vault.Settings.AcquireFilesSettings settings = new Vault.Settings.AcquireFilesSettings(m_conn);

                Vault.Currency.Entities.FileIteration file = new Vault.Currency.Entities.FileIteration(m_conn, files[0]);

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
    }
}
