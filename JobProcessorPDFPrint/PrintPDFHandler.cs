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

[assembly: ApiVersion("16.0")]
[assembly: ExtensionId("312306fb-a57f-410e-959d-79d9708f9fb7")]

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
            try
            {
                TargetFolder = System.IO.Path.GetTempPath();
                PDFPath = AppSettings.Get("PdfPath").ToString();
                pdfPrinterName = AppSettings.Get("PrintPDFPrinter").ToString();
                psToPdfProgName = AppSettings.Get("PrintPDFExecutable").ToString();
                ghostScriptWorkingFolder = AppSettings.Get("GhostScriptWorkingFolder").ToString();
            }
            catch(Exception)
            {
                
            }
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
                VDF.Vault.Settings.AcquireFilesSettings downloadSettings = new VDF.Vault.Settings.AcquireFilesSettings(context.Connection)
                {
                    LocalPath = new VDF.Currency.FolderPathAbsolute(targetDir.FullName),
                };
                downloadSettings.OptionsResolution.OverwriteOption = VDF.Vault.Settings.AcquireFilesSettings.AcquireFileResolutionOptions.OverwriteOptions.ForceOverwriteAll;
                downloadSettings.AddFileToAcquire(fileIter, VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Download);
                context.Connection.FileManager.AcquireFiles(downloadSettings);
                string fileName = downloadSettings.LocalPath.ToString() + @"\" + fileIter.ToString();
                PrintObject printOb = new PrintObject();
                string errMsg = "";
                string logMsg = "";
                if (printOb.printToPDF(fileName, PDFPath, pdfPrinterName, ref errMsg, ref logMsg))
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
            catch (Exception ex)
            {
                context.Log("Error in PDF Handler " + ex.Message + "\n\r", ACJE.MessageType.eError);
                return ACJE.JobOutcome.Failure;
            }
        }

        public void OnJobProcessorStartup(ACJE.IJobProcessorServices context) { }

        public void OnJobProcessorShutdown(ACJE.IJobProcessorServices context) { }

        public void OnJobProcessorWake(ACJE.IJobProcessorServices context) { }

        public void OnJobProcessorSleep(ACJE.IJobProcessorServices context) { }

        #endregion
        
    }

    
} 
        
