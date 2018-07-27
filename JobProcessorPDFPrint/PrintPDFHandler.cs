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
using PrintPDF;
using System.Collections.Generic;

using ACJE = Autodesk.Connectivity.JobProcessor.Extensibility;
using ACW = Autodesk.Connectivity.WebServices;
using ACWT = Autodesk.Connectivity.WebServicesTools;
using VDF = Autodesk.DataManagement.Client.Framework;
using Inventor;

using Autodesk.Connectivity.Extensibility.Framework;
using Autodesk.Connectivity.Explorer.Extensibility;
using Autodesk.Connectivity.WebServices;
using Autodesk.Connectivity.WebServicesTools;

namespace JobProcessorPrintPDF
{
    public class PrintPDFHandler : ACJE.IJobHandler
    {
        private string TargetFolder { get; set; }
        private string PDFPath { get; set; }
        private string pdfPrinterName { get; set; }
        private string psToPdfProgName { get; set; }
        private string ghostScriptWorkingFolder { get; set; }

        // this is what determines the pdf file naming convention, either by name, or by ERP Number
        //private const string VaultSearchEntity = "33da3ae9-2966-47a1-a049-7e57ace691a3";     // Vault ERPNumber
        private const string VaultSearchEntity = "Name";            // Vault Name

        public PrintPDFHandler()
        {
            TargetFolder = System.IO.Path.GetTempPath();
            PDFPath = AppSettings.Get("PDFPath").ToString();
            pdfPrinterName = AppSettings.Get("PdfPrinterName").ToString();
            psToPdfProgName = AppSettings.Get("psToPdfProgName").ToString();
            ghostScriptWorkingFolder = AppSettings.Get("ghostScriptWorkingFolder").ToString();
        }

        #region IJobHandler Members

        public bool CanProcess(string jobType)
        {
            if (jobType.ToLower().Equals("Horst.File.PrintPDF".ToLower()))
            {
                return true;
            }

            return false;
        }

        public ACJE.JobOutcome Execute(ACJE.IJobProcessorServices context, ACJE.IJob job)
        {
            string errorMessage = "";
            string logMessage = "";
            try
            {
                long EntityId = Convert.ToInt64(job.Params["EntityId"]);
                string fileString = "";


                ACW.File file = null;
                VDF.Vault.Currency.Entities.FileIteration fileIter = null;

                if (job.Description.Contains("USER-REQUESTED"))  // process was started manually by user
                {
                    file = context.Connection.WebServiceManager.DocumentService.GetLatestFileByMasterId(EntityId);
                    fileIter = context.Connection.FileManager.GetFilesByIterationIds(new long[] { file.Id }).First().Value;
                    fileString = file.Name;
                }
                else                                 // process was triggered by an event change
                {
                    ACW.File[] fileArray = new ACW.File[10];    // I hope there's never more than 11 files returned :(
                    long[] EntityIDArray = new long[1];
                    EntityIDArray[0] = EntityId;
                    try
                    {
                        fileArray = context.Connection.WebServiceManager.DocumentService.GetLatestFilesByIds(EntityIDArray);
                        fileIter = context.Connection.FileManager.GetFilesByIterationIds(new long[] { fileArray[0].Id }).First().Value;
                        fileString = fileIter.ToString();
                    }
                    catch (Exception)
                    {
                        context.Log("No Vault File Found ", ACJE.MessageType.eInformation);
                        context.Log(logMessage, ACJE.MessageType.eInformation);

                        return ACJE.JobOutcome.Success;
                    }
                }

                // Download and print the file
                //
                if (PrintPDF(fileIter, context.Connection, ref errorMessage, ref logMessage))
                {
                    context.Log("Successfully printed " + fileString + " to PDF\n\r", ACJE.MessageType.eInformation);
                    context.Log(logMessage,ACJE.MessageType.eInformation);
                    
                    return ACJE.JobOutcome.Success;
                }
                else
                {
                    context.Log("Error printing " + fileString + " to PDF. \n\r" + errorMessage, ACJE.MessageType.eError);
                    context.Log(logMessage, ACJE.MessageType.eInformation);
                    return ACJE.JobOutcome.Failure;
                }
            }
            catch (Exception)
            {
                context.Log("Unknown Error in PDF Handler\n\r", ACJE.MessageType.eError);
                return ACJE.JobOutcome.Failure;
            }
        }

        public void OnJobProcessorStartup(ACJE.IJobProcessorServices context) { }

        public void OnJobProcessorShutdown(ACJE.IJobProcessorServices context) { }

        public void OnJobProcessorWake(ACJE.IJobProcessorServices context) { }

        public void OnJobProcessorSleep(ACJE.IJobProcessorServices context) { }

        #endregion

        // prints each sheet of the idw(fileIter) to it's own pdf file.
        // if there's more than one sheet referencing the same assembly, they will be combined in one multi-page pdf

        private Boolean PrintPDF(VDF.Vault.Currency.Entities.FileIteration fileIter, VDF.Vault.Currency.Connections.Connection connection, ref string errMessage, ref string logMessage)
        {
            try
            {
                if (fileIter.EntityName.EndsWith(".idw")) // only print idws
                {
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
                    VDF.Vault.Settings.AcquireFilesSettings downloadSettings = new VDF.Vault.Settings.AcquireFilesSettings(connection)
                    {
                        LocalPath = new VDF.Currency.FolderPathAbsolute(targetDir.FullName),
                    };
                    downloadSettings.AddFileToAcquire(fileIter, VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Download);
                    connection.FileManager.AcquireFiles(downloadSettings);

                    string fileName = downloadSettings.LocalPath.ToString() + @"\" + fileIter.ToString();
                    string modelName = "";

                    logMessage += "OK" + "\r\n";

                    // set up lists for storing the actual model names the sheets are referencing
                    logMessage += "Setting up data structures to hold file names...";

                    // two failures here Oct. 26th...

                    List<string> modelNames = new List<string>();
                    List<VDF.Vault.Currency.Entities.FileIteration> fIterations = new List<VDF.Vault.Currency.Entities.FileIteration>();

                    VDF.Vault.Currency.Properties.PropertyDefinitionDictionary propDefs =
                                   new VDF.Vault.Currency.Properties.PropertyDefinitionDictionary();
                    Inventor.ApprenticeServerComponent oApprentice = new ApprenticeServerComponent();
                    Inventor.ApprenticeServerDrawingDocument drgDoc;
                    drgDoc = (Inventor.ApprenticeServerDrawingDocument)oApprentice.Document;
                    oApprentice.Open(fileName);
                    drgDoc = (Inventor.ApprenticeServerDrawingDocument)oApprentice.Document;
                    PropDef[] filePropDefs =
                                    connection.WebServiceManager.PropertyService.GetPropertyDefinitionsByEntityClassId("FILE");
                    PropDef vaultNamePropDef = filePropDefs.Single(n => n.SysName == "Name");

                    logMessage += "OK" + "\r\n";
                    logMessage += "Number of Sheets in drgDoc" + drgDoc.Sheets.Count + "\r\n";

                    // for each sheet in the idw, search the vault for the sheet's corresponding ipt or iam 
                    foreach (Sheet sh in drgDoc.Sheets)
                    {
                        logMessage += "Sheet Name: " + sh.Name + "\r\n";

                        // attempt to fix bug where multiple instances of assembly drawing are printed
                        // the problem shows up when there is an existing pdf of an assembly and we request to print it again.  The newly
                        // printed pdfs will be added onto the existing file, rather than the exsiting file being overwritten like it should be.
                        //if (sh.DrawingViews.Count > 0)
                        //{
                        //    modelName = sh.DrawingViews[1].ReferencedDocumentDescriptor.DisplayName;
                        //    string pdfName = PDFPath + System.IO.Path.GetFileNameWithoutExtension(modelName) + ".pdf";

                        //    if (System.IO.File.Exists(pdfName))
                        //    {
                        //        // make sure file is accessible....
                        //        FileInfo fileInfo = new FileInfo(pdfName);
                        //        fileInfo.IsReadOnly = false;
                        //        System.IO.File.Delete(pdfName);
                        //    }
                        //}

                        // ...2 failures here Oct. 23rd...
                        // ...1 failure here Oct 28th....

                        if (sh.DrawingViews.Count > 0)
                        {
                            modelName = sh.DrawingViews[1].ReferencedDocumentDescriptor.DisplayName;

                            // testing printing different levels of detail, didn't figure it out....
                            //if (sh.DrawingViews[1].ReferencedDocumentDescriptor.ReferencedLevelOfDetail == LevelOfDetailEnum.kMasterLevelOfDetail)
                            //    modelName = sh.DrawingViews[1].ReferencedDocumentDescriptor.DisplayName;
                            //else
                            //{
                            //    // remove the ' (*********************)' from the file name that represents the level of detail
                            //    // this wouldn't work because not all files with different levels of detail will have two sets of parenthises.
                            //}

                            VDF.Vault.Currency.Entities.FileIteration fIter;
                            try
                            {
                                logMessage += "Setting up to search condition...";

                                SrchCond vaultName = new SrchCond()
                                {
                                    PropDefId = vaultNamePropDef.Id,
                                    PropTyp = PropertySearchType.SingleProperty,
                                    SrchOper = 3, // equals
                                    SrchRule = SearchRuleType.Must,
                                    SrchTxt = modelName
                                };


                                string bookmark = string.Empty;
                                SrchStatus status = null;
                                Autodesk.Connectivity.WebServices.File[] searchResults =
                                    connection.WebServiceManager.DocumentService.FindFilesBySearchConditions(
                                    new SrchCond[] { vaultName },
                                    null, null, false, true, ref bookmark, out status);



                                propDefs = new VDF.Vault.Currency.Properties.PropertyDefinitionDictionary();
                                propDefs =
                                  connection.PropertyManager.GetPropertyDefinitions(
                                    VDF.Vault.Currency.Entities.EntityClassIds.Files,
                                    null,
                                    VDF.Vault.Currency.Properties.PropertyDefinitionFilter.IncludeAll
                                  );

                                logMessage += "OK, search successful" + "\r\n";

                                if (searchResults == null)
                                {
                                    logMessage += "No corresponding model file found for " + modelName + "\r\n";
                                    ACW.File emptyFile = new ACW.File();
                                    fIter = new VDF.Vault.Currency.Entities.FileIteration(connection, emptyFile);
                                    fIterations.Add(fIter);
                                }
                                else if (searchResults.Count() > 1)
                                {
                                    logMessage += "Multiple corresponding models found for " + modelName + "\r\n";
                                    ACW.File emptyFile = new ACW.File();
                                    fIter = new VDF.Vault.Currency.Entities.FileIteration(connection, emptyFile);
                                    fIterations.Add(fIter);
                                }
                                else
                                {
                                    fIter = new VDF.Vault.Currency.Entities.FileIteration(connection, searchResults[0]);
                                    fIterations.Add(fIter);
                                    logMessage += "Match found\r\n";
                                }
                            }
                            catch (Exception)
                            {
                                errMessage += "Unknown Error in function PrintPDF\r\n";
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
                    logMessage += "propList count: " + propList.Entities.Count() + "\r\n";

                    propDict = propList.GetValues(propDef);
                    logMessage += "propDict count: " + propDict.Values.Count() + "\r\n";

                    foreach (KeyValuePair<VDF.Vault.Currency.Entities.IEntity,
                                         VDF.Vault.Currency.Properties.PropertyValue> pair in propDict)
                    {
                        if (pair.Key.EntityMasterId != 0)
                        {
                            logMessage += "key: " + pair.Key + " value: " + pair.Value.Value + "\r\n";
                            modelNames.Add(pair.Value.Value.ToString());
                        }
                        else
                        {
                            logMessage += "Blank entry\r\n";
                            modelNames.Add("");
                        }
                    }

                    logMessage += "Defining Print Object...";

                    //...failed here one time Oct 28th...
                    PrintObject printOb = new PrintObject();
                    string errMsg = "";
                    string logMsg = "";
                    //if (printOb.printToPDFNew(fileName, propDict, PDFPath, ref errMsg, ref logMsg))
                    if (printOb.printToPDF(fileName,PDFPath,pdfPrinterName, psToPdfProgName,ghostScriptWorkingFolder, ref errMsg, ref logMsg))
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
                else
                {
                    logMessage += "File is not an idw, nothing to print.";
                    return true;
                }
            }
            catch (Exception ex)
            {
                errMessage += "Unknown Error in function PrintPDF\r\n";
                return false;
            }
        

        }
        
    }

    
} 
        
