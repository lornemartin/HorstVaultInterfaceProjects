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
using JobProcessorPrintPDF;
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
using PrintPDF;

namespace JobProcessorPrintPDF
{
    public class PrintPDFHandler : ACJE.IJobHandler
    {
        private string TargetFolder { get; set; }
        private string PDFPath { get; set; }
        private string PDFPrinterName { get; set; }
        private string PS2PDFProgrameName { get; set; }
        private string GSWorkingFolder { get; set; }

        // this is what determines the pdf file naming convention, either by name, or by ERP Number
        //private const string VaultSearchEntity = "33da3ae9-2966-47a1-a049-7e57ace691a3";     // Vault ERPNumber
        private const string VaultSearchEntity = "Name";            // Vault Name

        public PrintPDFHandler()
        {
            TargetFolder = System.IO.Path.GetTempPath();
            PDFPath = AppSettings.Get("PDFPath").ToString();
            PDFPrinterName = AppSettings.Get("PrintPDFPrinter").ToString();
            PS2PDFProgrameName = AppSettings.Get("PrintPDFPS2PDF").ToString();
            GSWorkingFolder = AppSettings.Get("GhostScriptWorkingFolder").ToString();
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
            catch (Exception ex)
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

        // this is not at all tested yet....
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

                    PrintObject printOb = new PrintObject();
                    string errMsg = "";
                    string logMsg = "";

                    if (printOb.printToPDF(fileName, PDFPath, PDFPrinterName, PS2PDFProgrameName, GSWorkingFolder, ref errMsg, ref logMsg))
                    {
                        return false;
                    }
                    else
                        return true; 
                }
                else
                {
                    logMessage += "File is not an idw, nothing to print.";
                    return true;
                }
            }
            catch (Exception)
            {
                errMessage += "Unknown Error in function PrintPDF\r\n";
                return false;
            }
        

        }
        
    }

    
} 
        
