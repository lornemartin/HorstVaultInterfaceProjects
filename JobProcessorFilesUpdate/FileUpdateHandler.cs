/*=====================================================================
  
  This file is part of the Autodesk Vault API Code Samples.

  Copyright (C) Autodesk Inc.  All rights reserved.

THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
PARTICULAR PURPOSE.
=====================================================================*/

using System;
using System.Linq;
using System.IO;
using System.Data;
using System.Collections.Generic;
using PrintPDF;
using Inventor;

using ACJE = Autodesk.Connectivity.JobProcessor.Extensibility;
using ACW = Autodesk.Connectivity.WebServices;
using ACWT = Autodesk.Connectivity.WebServicesTools;
using VDF = Autodesk.DataManagement.Client.Framework;

using Autodesk.Connectivity.Extensibility.Framework;
using Autodesk.Connectivity.Explorer.Extensibility;
using Autodesk.Connectivity.WebServices;
using Autodesk.Connectivity.WebServicesTools;

using System.Data.Sql;
using System.Data.SqlClient;
using JobProcessorPrintPDF;

namespace JobProcessorFileUpdate
{
    public class FileUpdateHandler : ACJE.IJobHandler
    {
        private string m_TargetFolder { get; set; }
        private string m_PDFPath { get; set; }
        private string m_SqlConnectionString { get; set; }
        private string m_SymFolderName { get; set; }

        // this is what determines the pdf file naming convention, either by name, or by ERP Number
        //private const string VaultSearchEntity = "33da3ae9-2966-47a1-a049-7e57ace691a3";     // Vault ERPNumber
        private const string VaultSearchEntity = "Name";            // Vault Name

        public FileUpdateHandler()
        {
            m_TargetFolder = System.IO.Path.GetTempPath();
            m_PDFPath = JobProcessorPrintPDF.AppSettings.Get("PDFPath").ToString();
            //m_SqlConnectionString = "Data Source = HLAMFG; Initial Catalog = Horst Manufacturing DB; Integrated Security = True";
            m_SymFolderName = JobProcessorPrintPDF.AppSettings.Get("SymFileFolder").ToString();
        }

        #region IJobHandler Members

        public bool CanProcess(string jobType)
        {
            if (jobType.ToLower().Equals("Horst.File.FileUpdate".ToLower()))
            {
                return true;
            }

            return false;
        }

        public ACJE.JobOutcome Execute(ACJE.IJobProcessorServices context, ACJE.IJob job)
        {
            long EntityId = Convert.ToInt64(job.Params["EntityId"]);
            string logText = "";
            string errText = "";
            ACJE.JobOutcome jobOutComeStatus = ACJE.JobOutcome.Success;
            try
            {
                // Retrieve the file object from the server
                //
                ACW.File[] fileArray = new ACW.File[10];    // I hope there's never more than 11 files returned :(
                long[] EntityIDArray = new long[1];
                string logString = "", errString = "";

                EntityIDArray[0] = EntityId;
                try
                {
                    fileArray = context.Connection.WebServiceManager.DocumentService.GetLatestFilesByIds(EntityIDArray);
                }
                catch (Exception)
                {
                    // if the above call fails, we know the vault file that we want to process doesn't exist anymore
                    // so we don't worry about it and call it a success!
                    context.Log("No vault file found", ACJE.MessageType.eInformation);
                    return ACJE.JobOutcome.Success;
                }
                context.Log("number of items in array: " + fileArray.Length + "\n\r", ACJE.MessageType.eInformation);


                VDF.Vault.Currency.Entities.FileIteration fileIter =
                       context.Connection.FileManager.GetFilesByIterationIds(new long[] { fileArray[0].Id }).First().Value;

                if (GetVaultCheckOutComment(fileIter, context.Connection) != "IM")
                {

                    // check for PDF files
                    if (PDFfileUpdate(fileIter, context.Connection, ref logString, ref errString))
                    {
                        logText += "Processing PDF File for " + fileIter.ToString() + "...\n";
                        logText += logString + "\n";  // information returned from FileUpdate
                        context.Log(logText, ACJE.MessageType.eInformation);
                        jobOutComeStatus = ACJE.JobOutcome.Success;
                    }
                    else
                    {
                        errText = "Error in processing PDF File for " + fileIter.ToString();
                        errText += errString + "\n";  // information returned from FileUpdate
                        context.Log(errText, ACJE.MessageType.eError);
                        jobOutComeStatus = ACJE.JobOutcome.Failure;
                    }

                    // check for DB records
                    //int dbUpdateVal = DBUpdate(fileIter);
                    //if (dbUpdateVal == 1)
                    //{
                    //    logText += "Changed DB VaultSyncStatus for" + fileIter.ToString() + "...\n";
                    //    context.Log(logText, ACJE.MessageType.eInformation);
                    //    if (jobOutComeStatus != ACJE.JobOutcome.Failure) jobOutComeStatus = ACJE.JobOutcome.Success;
                    //}
                    //else if (dbUpdateVal == 0)
                    //{
                    //    logText = "No DB match found for " + fileIter.ToString() + "...\n";
                    //    context.Log(logText, ACJE.MessageType.eInformation);
                    //    if (jobOutComeStatus != ACJE.JobOutcome.Failure) jobOutComeStatus = ACJE.JobOutcome.Success;
                    //}
                    //else if (dbUpdateVal == -1)
                    //{
                    //    errText = "Error in processing Database for " + fileIter.ToString();
                    //    context.Log(errText, ACJE.MessageType.eError);
                    //    jobOutComeStatus = ACJE.JobOutcome.Failure;
                    //}

                    // check for sym files
                    int symUpdateVal = processRadanFile(fileIter, false);
                    if (symUpdateVal == 1)
                    {
                        logText += "Moved Radan File for " + fileIter.ToString() + "...\n";
                        context.Log(logText, ACJE.MessageType.eInformation);
                        if (jobOutComeStatus != ACJE.JobOutcome.Failure) jobOutComeStatus = ACJE.JobOutcome.Success;
                    }
                    else if (symUpdateVal == 0)
                    {
                        logText = "No Radan File moved for " + fileIter.ToString() + "...\n";
                        context.Log(logText, ACJE.MessageType.eInformation);
                        if (jobOutComeStatus != ACJE.JobOutcome.Failure) jobOutComeStatus = ACJE.JobOutcome.Success;
                    }
                    else if (symUpdateVal == -1)
                    {
                        errText = "Error in processing Radan File for " + fileIter.ToString();
                        context.Log(errText, ACJE.MessageType.eError);
                        jobOutComeStatus = ACJE.JobOutcome.Failure;
                    }

                    return jobOutComeStatus;
                }
                else
                {
                    logText = "Skipping over " + fileIter.ToString() + "because only item master properties were changed";
                    context.Log(logText, ACJE.MessageType.eInformation);
                    return ACJE.JobOutcome.Success;
                }
            }
            catch
            {
                context.Log(logText, ACJE.MessageType.eError);
                return ACJE.JobOutcome.Failure;
            }
        }

        public void OnJobProcessorStartup(ACJE.IJobProcessorServices context) { }

        public void OnJobProcessorShutdown(ACJE.IJobProcessorServices context) { }

        public void OnJobProcessorWake(ACJE.IJobProcessorServices context) { }

        public void OnJobProcessorSleep(ACJE.IJobProcessorServices context) { }

        #endregion
    
        //  use ERP names instead of file names.
        private Boolean PDFfileUpdate(VDF.Vault.Currency.Entities.FileIteration fileIter, VDF.Vault.Currency.Connections.Connection connection, ref string logMessage, ref string errMessage)
        {
            try
            {
                // download the file
                System.IO.DirectoryInfo targetDir = new System.IO.DirectoryInfo(m_TargetFolder);
                if (!targetDir.Exists)
                {
                    targetDir.Create();
                }

                VDF.Vault.Settings.AcquireFilesSettings downloadSettings = new VDF.Vault.Settings.AcquireFilesSettings(connection)
                {
                    LocalPath = new VDF.Currency.FolderPathAbsolute(targetDir.FullName),
                };
                downloadSettings.AddFileToAcquire(fileIter, VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Download);

                VDF.Vault.Results.AcquireFilesResults results =
                    connection.FileManager.AcquireFiles(downloadSettings);

                string fileName = downloadSettings.LocalPath.ToString() + @"\" + fileIter.ToString();

                // ipts and iams are easier to deal with than idws
                if (fileName.EndsWith(".ipt") || fileName.EndsWith(".iam"))
                {
                    List<VDF.Vault.Currency.Entities.FileIteration> fIterations = new List<VDF.Vault.Currency.Entities.FileIteration>();
                    VDF.Vault.Currency.Properties.PropertyDefinitionDictionary propDefs =
                                   new VDF.Vault.Currency.Properties.PropertyDefinitionDictionary();
                    propDefs =
                      connection.PropertyManager.GetPropertyDefinitions(
                        VDF.Vault.Currency.Entities.EntityClassIds.Files,
                        null,
                        VDF.Vault.Currency.Properties.PropertyDefinitionFilter.IncludeAll
                      );

                    fIterations.Add(fileIter);

                    Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties.PropertyValues propList =
                        new VDF.Vault.Currency.Properties.PropertyValues();

                    System.Collections.Generic.Dictionary<VDF.Vault.Currency.Entities.IEntity,
                                                          Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties.PropertyValue>
                                                          propDict = new Dictionary<VDF.Vault.Currency.Entities.IEntity,
                                                                              VDF.Vault.Currency.Properties.PropertyValue>();

                    Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties.PropertyDefinition propDef =
                        new VDF.Vault.Currency.Properties.PropertyDefinition(propDefs[VaultSearchEntity]);

                    propList = connection.PropertyManager.GetPropertyValues(
                                fIterations, new Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties.PropertyDefinition[] { propDef }, null);

                    propDict = propList.GetValues(propDef);

                    string entityName = "";

                    foreach (KeyValuePair<VDF.Vault.Currency.Entities.IEntity,
                                         VDF.Vault.Currency.Properties.PropertyValue> pair in propDict)
                    {
                        if (pair.Key.EntityMasterId != 0)
                        {
                            entityName = pair.Value.Value.ToString();
                        }
                        else
                        {
                            errMessage += "No match found for: " + fileName;
                            return false;
                        }
                    }

                    PrintObject printOb = new PrintObject();

                    if (printOb.deletePDF(entityName, m_PDFPath, ref logMessage, ref errMessage))
                        return true;
                    else
                        return false;
                }
                else if (fileName.EndsWith(".idw")) // for idws we have to loop through each drawing sheet
                {
                    try
                    {
                        string modelName = "";

                        // set up lists for storing the actual model names the sheets are referencing
                        List<string> modelNames = new List<string>();
                        List<VDF.Vault.Currency.Entities.FileIteration> fIterations = new List<VDF.Vault.Currency.Entities.FileIteration>();

                        VDF.Vault.Currency.Properties.PropertyDefinitionDictionary propDefs =
                                       new VDF.Vault.Currency.Properties.PropertyDefinitionDictionary();
                        Inventor.ApprenticeServerComponent oApprentice = new ApprenticeServerComponent();
                        Inventor.ApprenticeServerDrawingDocument drgDoc;
                        drgDoc = (Inventor.ApprenticeServerDrawingDocument)oApprentice.Document;
                        oApprentice.Open(fileName);
                        drgDoc = (Inventor.ApprenticeServerDrawingDocument)oApprentice.Document;
                        ACW.PropDef[] filePropDefs =
                                        connection.WebServiceManager.PropertyService.GetPropertyDefinitionsByEntityClassId("FILE");
                        ACW.PropDef vaultNamePropDef = filePropDefs.Single(n => n.SysName == "Name");

                        // for each sheet in the idw, search the vault for the sheet's corresponding ipt or iam 
                        foreach (Sheet sh in drgDoc.Sheets)
                        {
                            if (sh.DrawingViews.Count > 0)  // I added this line because one pdf with a BOM only sheet
                                                            // kept failing.  This line fixed the problemf or that file
                                                            // but it is definitely not well tested...
                            {
                                errMessage += " " + sh.DrawingViews[1].ReferencedDocumentDescriptor.DisplayName + "found";
                                if (sh.DrawingViews.Count > 0)
                                {
                                    modelName = sh.DrawingViews[1].ReferencedDocumentDescriptor.DisplayName;
                                    VDF.Vault.Currency.Entities.FileIteration fIter;
                                    try
                                    {
                                        ACW.SrchCond vaultName = new ACW.SrchCond()
                                        {
                                            PropDefId = vaultNamePropDef.Id,
                                            PropTyp = ACW.PropertySearchType.SingleProperty,
                                            SrchOper = 3, // equals
                                            SrchRule = ACW.SearchRuleType.Must,
                                            SrchTxt = modelName
                                        };

                                        string bookmark = string.Empty;
                                        ACW.SrchStatus status = null;
                                        Autodesk.Connectivity.WebServices.File[] searchResults =
                                            connection.WebServiceManager.DocumentService.FindFilesBySearchConditions(
                                            new ACW.SrchCond[] { vaultName },
                                            null, null, false, true, ref bookmark, out status);

                                        propDefs = new VDF.Vault.Currency.Properties.PropertyDefinitionDictionary();
                                        propDefs =
                                          connection.PropertyManager.GetPropertyDefinitions(
                                            VDF.Vault.Currency.Entities.EntityClassIds.Files,
                                            null,
                                            VDF.Vault.Currency.Properties.PropertyDefinitionFilter.IncludeAll
                                          );

                                        if (searchResults == null)
                                        {
                                            errMessage += "No corresponding model file found for " + modelName + "\r\n";
                                            ACW.File emptyFile = new ACW.File();
                                            fIter = new VDF.Vault.Currency.Entities.FileIteration(connection, emptyFile);
                                            fIterations.Add(fIter);
                                        }
                                        else if (searchResults.Count() > 1)
                                        {
                                            errMessage += "Multiple corresponding models found for " + modelName + "\r\n";
                                            ACW.File emptyFile = new ACW.File();
                                            fIter = new VDF.Vault.Currency.Entities.FileIteration(connection, emptyFile);
                                            fIterations.Add(fIter);
                                        }
                                        else
                                        {
                                            fIter = new VDF.Vault.Currency.Entities.FileIteration(connection, searchResults[0]);
                                            fIterations.Add(fIter);
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        logMessage += logMessage;
                                        errMessage += "Error Matching Model Name to ERP Name in function FileUpdate\r\n";
                                        return false;
                                    }
                                }
                            }

                            // now we have a list of model file names stored in 'fIterations', one for every sheet in the idw.
                            // If we couldn't match the sheet up with a model, the list will have a blank entry.
                            // next we have to match each name up with whatever vault field we are using to name the pdf files...
                            // e.g. if we're using names, this is totally unnecessary, but if we want to use ERP Numbers, we have to 
                            // search them out.
                            // we'll then end up with a dictionary matching names up with the chosen vault field.

                            Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties.PropertyValues propList =
                                new VDF.Vault.Currency.Properties.PropertyValues();

                            System.Collections.Generic.Dictionary<VDF.Vault.Currency.Entities.IEntity,
                                                                  Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties.PropertyValue>
                                                                  propDict = new Dictionary<VDF.Vault.Currency.Entities.IEntity,
                                                                                      VDF.Vault.Currency.Properties.PropertyValue>();

                            Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties.PropertyDefinition propDef =
                                new VDF.Vault.Currency.Properties.PropertyDefinition(propDefs[VaultSearchEntity]);

                            propList = connection.PropertyManager.GetPropertyValues(
                                        fIterations, new Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties.PropertyDefinition[] { propDef }, null);

                            propDict = propList.GetValues(propDef);

                            // now loop through the dictionary, processing each entry.
                            foreach (KeyValuePair<VDF.Vault.Currency.Entities.IEntity,
                                                 VDF.Vault.Currency.Properties.PropertyValue> pair in propDict)
                            {
                                if (pair.Key.EntityMasterId != 0)
                                {
                                    PrintObject printOb = new PrintObject();
                                    if (printOb.deletePDF(pair.Value.Value.ToString(), m_PDFPath, ref logMessage, ref errMessage))
                                    {
                                        //logMessage += "Deleted PDF: " + pair.Value.Value.ToString() + "\r\n";
                                        // we already logged a message in the deletePDF function
                                    }
                                    else
                                    {
                                        logMessage += logMessage;
                                        errMessage += "Can not delete PDF Error1 in function FileUpdate\r\n";
                                        return false;
                                    }
                                }
                                else
                                {
                                    logMessage += "No model to delete for " + pair.Value.Value + "\r\n";
                                    //errMessage += "Can not delete PDF Error2 in function FileUpdate\r\n";
                                    //return false;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logMessage += logMessage;
                        errMessage += "Can not delete PDF Error2 in function FileUpdate\r\n" + ex.Message + "\r\n";
                        return false;
                    }
                }
                return true;
            }

            catch (Exception)
            {
                logMessage += logMessage;
                errMessage += errMessage;
                return false;
            }
        }

        private int DBUpdate(VDF.Vault.Currency.Entities.FileIteration fileIter)
        {
            // this routine will return 1 if it successfully updates a flag,
            //                          0 if it finds no record, or
            //                          -1 if it encounters an error.
            try
            {
                string vaultName = fileIter.EntityName;
                int returnVal = 0;
                int statusFlag = 0;

                using (SqlConnection dbConnection = new SqlConnection(m_SqlConnectionString))
                {
                    dbConnection.Open();

                    // First search for assembly with selected name.
                    SqlCommand command = new SqlCommand("SELECT ID FROM Assembly WHERE VaultName = @0", dbConnection);
                    // Add the parameters.
                    command.Parameters.Add(new SqlParameter("0", vaultName));

                    long vaultID = 0;

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            vaultID = (long)reader[0];
                            // if we get a vault ID here we know we've found a matching assembly in the DB.
                        }
                    }

                    // only modify status flag if we did find an assembly with a matching vault name
                    if (vaultID != 0)
                    {
                        using (SqlCommand alterCommand = new SqlCommand("UPDATE Assembly SET SyncWithVault = @0 WHERE VaultName = @1", dbConnection))
                        {
                            {
                                SqlParameter param1 = new SqlParameter();
                                param1.DbType = DbType.String;
                                param1.Direction = ParameterDirection.Input;
                                param1.ParameterName = "0";
                                param1.Value = statusFlag;   // this is the value to set
                                alterCommand.Parameters.Add(param1);
                                alterCommand.Parameters.Add(new SqlParameter("1", vaultName));
                                returnVal = alterCommand.ExecuteNonQuery();

                            }
                        }
                    }

                    else
                    {
                        // no matching assembly found so we will search for a matching part.
                        command = new SqlCommand("SELECT ID FROM Part WHERE VaultName = @0", dbConnection);
                        // Add the parameters.
                        command.Parameters.Add(new SqlParameter("0", vaultName));

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                vaultID = (long)reader[0];
                                // if we get a vault ID here we know we've found a matching part in the DB.
                            }
                        }

                        // only modify status flag if we did find a part with a matching vault name
                        if (vaultID != 0)
                        {
                            using (SqlCommand alterCommand = new SqlCommand("UPDATE Part SET SyncWithVault = @0 WHERE VaultName = @1", dbConnection))
                            {
                                {
                                    SqlParameter param1 = new SqlParameter();
                                    param1.DbType = DbType.String;
                                    param1.Direction = ParameterDirection.Input;
                                    param1.ParameterName = "0";
                                    param1.Value = statusFlag;   // this is the value to set
                                    alterCommand.Parameters.Add(param1);
                                    alterCommand.Parameters.Add(new SqlParameter("1", vaultName));
                                    returnVal = alterCommand.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
                return returnVal;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private int processRadanFile(VDF.Vault.Currency.Entities.FileIteration fileIter, bool simulate)
        {
            string vaultName = fileIter.EntityName;
            string symFileName = "";

            if (System.IO.Path.GetExtension(m_SymFolderName + vaultName) == ".ipt")
            {
                try
                {
                    DirectoryInfo symFolder = new DirectoryInfo(m_SymFolderName);
                    DirectoryInfo symParentFolder = symFolder.Parent;
                    DirectoryInfo symFolderNonControlled = new DirectoryInfo(symParentFolder.FullName + "\\Vault Sym Files - non controlled");

                    symFileName = m_SymFolderName + System.IO.Path.GetFileNameWithoutExtension(vaultName) + ".sym";
                    System.IO.FileInfo symFile = new System.IO.FileInfo(symFileName);
                    if (!symFile.Exists)
                    {
                        // no sym file found
                        return 0;
                    }

                    if (!simulate)
                    {
                        if (!symFolderNonControlled.Exists)
                        {
                            symFolderNonControlled.Create();
                        }

                        string nonControlledFileName = (symFolderNonControlled.FullName + "\\" + symFile.Name);
                        FileInfo nonControlledFile = new FileInfo(nonControlledFileName);
                        if (nonControlledFile.Exists) nonControlledFile.Delete();
                        symFile.MoveTo(nonControlledFile.FullName);
                        nonControlledFile.LastWriteTime = DateTime.Now;

                        if (!nonControlledFile.Exists)
                        {
                            // error
                            return -1;
                        }

                        // sym file moved successfully
                        return 1;
                    }
                    else
                    {
                        // simulation mode only, no sym file moved
                        return 0;
                    }

                }
                catch (Exception)
                {
                    //error
                    return -1;
                }
            }
            else
            {
                // vault file is not an ipt, no need to search for sym file
                return 0;
            }
        }

        public string GetVaultCheckOutComment(VDF.Vault.Currency.Entities.FileIteration selectedFile, VDF.Vault.Currency.Connections.Connection connection)
        {
            // a list of dictionaries of the vault properties of the associated files
            List<Dictionary<string, string>> vaultPropDictList = new List<Dictionary<string, string>>();

            // function that gets properties requires a list so we'll define a list and add the selected file to it...
            List<VDF.Vault.Currency.Entities.FileIteration> fileList = new List<VDF.Vault.Currency.Entities.FileIteration>();
            fileList.Add(selectedFile);

            // define a dictionary to hold all the properties pertaining to a vault entity
            VDF.Vault.Currency.Properties.PropertyDefinitionDictionary allPropDefDict = new VDF.Vault.Currency.Properties.PropertyDefinitionDictionary();
            allPropDefDict = connection.PropertyManager.GetPropertyDefinitions(VDF.Vault.Currency.Entities.EntityClassIds.Files, null, VDF.Vault.Currency.Properties.PropertyDefinitionFilter.IncludeAll);

            // define a list of only the properties that we are concerned about.
            List<VDF.Vault.Currency.Properties.PropertyDefinition> filteredPropDefList =
                    new List<VDF.Vault.Currency.Properties.PropertyDefinition>();

            List<string> propNames = new List<string> { "Comment" };

            // copy only definitions in propNames list from allPropDefDict to filteredPropDefList
            foreach (string s in propNames)
            {
                VDF.Vault.Currency.Properties.PropertyDefinition propDefs =
                    new Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties.PropertyDefinition();
                propDefs = allPropDefDict[s];

                filteredPropDefList.Add(propDefs);
            }

            // the following line should return a list of properties
            VDF.Vault.Currency.Properties.PropertyValues propValues = new Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties.PropertyValues();
            propValues = connection.PropertyManager.GetPropertyValues(fileList, filteredPropDefList, null);

            VDF.Vault.Currency.Properties.PropertyDefinition def = new VDF.Vault.Currency.Properties.PropertyDefinition(propNames[0]);
            VDF.Vault.Currency.Properties.PropertyValue val;

            Dictionary<VDF.Vault.Currency.Properties.PropertyDefinition, VDF.Vault.Currency.Properties.PropertyValue> d = propValues.GetValues(selectedFile);
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
    }
}
        
