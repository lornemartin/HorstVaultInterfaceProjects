using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inventor;
using System.IO;
using System.Threading;
using System.Security.AccessControl;
using System.Data;
using System.Diagnostics;
using System.Collections;
using System.Xml.Linq;

using PdfSharp;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Drawing;

using VDF = Autodesk.DataManagement.Client.Framework;
using Autodesk.Connectivity.Extensibility.Framework;
using Autodesk.Connectivity.Explorer.Extensibility;
using Autodesk.Connectivity.WebServices;
using Autodesk.Connectivity.WebServicesTools;
using log4net.Config;
using log4net;
using log4net.Appender;
using System.Reflection;
using Environment = System.Environment;

namespace PrintPDF
{

    public class PrintObject
    {
        public PrintObject()
        {
            if (!log4net.LogManager.GetRepository().Configured)
            {
                
                // my DLL is referenced by web service applications to log SOAP requests before
                // execution is passed to the web method itself, so I load the log4net.config
                // file that resides in the web application root folder
                //var configFileDirectory = (new DirectoryInfo(TraceExtension.AssemblyDirectory)).Parent; // not the bin folder but up one level
                var configFileDirectory = new DirectoryInfo(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                var configFile = new FileInfo(configFileDirectory.FullName + "\\log4net.config");

                if (!configFile.Exists)
                {
                    throw new FileLoadException(String.Format("The configuration file {0} does not exist", configFile));
                }

                log4net.Config.XmlConfigurator.Configure(configFile);
            }
        }

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Boolean printToPDF(string idw, string outputFolder, string pdfPrinterName, ref string errMessage, ref string logMessage)
        {
            {
                try
                {
                    // set log file location
                    XmlConfigurator.Configure();
                    log4net.Repository.Hierarchy.Hierarchy h =
                    (log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository();
                    foreach (IAppender a in h.Root.Appenders)
                    {
                        if (a is FileAppender)
                        {
                            FileAppender fa = (FileAppender)a;
                            // Programmatically set this to the desired location here
                            string logFileLocation = outputFolder + "PDFPrint.log";

                            // Uncomment the lines below if you want to retain the base file name
                            // and change the folder name...
                            //FileInfo fileInfo = new FileInfo(fa.File);
                            //logFileLocation = string.Format(@"C:\MySpecialFolder\{0}", fileInfo.Name);

                            fa.File = logFileLocation;
                            fa.ActivateOptions();
                            break;
                        }
                    }

                    ApprenticeServerComponent oApprentice = new ApprenticeServerComponent();
                    ApprenticeServerDrawingDocument drgDoc;
                    oApprentice.Open(idw);
                    drgDoc = (ApprenticeServerDrawingDocument)oApprentice.Document;
                    int pageCount = 1;
                    List<string> assemblyFileNameList = new List<string>();

                    idwFile idwFileToPrint = new idwFile();
                    idwFileToPrint.sheetNames = new List<string>();
                    idwFileToPrint.idwName = idw;
                    idwFileToPrint.pageCount = drgDoc.Sheets.Count;



                    // delete previous pdfs so we don't double up assembly drawings.
                    foreach (Sheet sh in drgDoc.Sheets)
                    {
                        if(!sh.ExcludeFromPrinting)
                        {
                            if (sh.DrawingViews.Count > 0)
                            {
                                string modelName = sh.DrawingViews[1].ReferencedDocumentDescriptor.DisplayName;
                                string pdfName = outputFolder + System.IO.Path.GetFileNameWithoutExtension(modelName) + ".pdf";

                                try
                                {
                                    if (System.IO.File.Exists(pdfName))
                                    {
                                        if (CheckIfFileIsBeingUsed(pdfName))
                                        {
                                            // if file is in use, can't delete it.
                                            return false;
                                        }
                                        System.IO.File.Delete(pdfName);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    logMessage += ex.Message;
                                    return false;
                                }
                            }
                        }
                    }








                    
                    foreach (Sheet sh in drgDoc.Sheets)
                    {
                        if (!sh.ExcludeFromPrinting)
                        {
                            if (sh.DrawingViews.Count > 0)
                            {
                                string modelName;
                                modelName = sh.DrawingViews[1].ReferencedDocumentDescriptor.DisplayName;
                                // this doesn't work right on files with special characters.
                                //modelName = Path.GetFileNameWithoutExtension(modelName);

                                if (modelName.EndsWith(".ipt") || modelName.EndsWith(".iam"))
                                {
                                    int index = modelName.LastIndexOf('.');
                                    modelName = index == -1 ? modelName : modelName.Substring(0, index);
                                }

                                idwFileToPrint.sheetNames.Add(modelName);
                                pageCount++;

                            }
                        }
                    }

                    log.Info("Sheet Names All Read When Printing " + idwFileToPrint.idwName);

                    string printer = pdfPrinterName;
                    string pdfFileName = "";

                    try
                    {
                        ApprenticeDrawingPrintManager pMgr;
                        drgDoc = (ApprenticeServerDrawingDocument)oApprentice.Document;
                        pMgr = (ApprenticeDrawingPrintManager)drgDoc.PrintManager;
                        pMgr.Printer = printer;
                        int actualSheetIndex = 1;
                        int modifiedSheetIndex = 1;
                        int missingSheetsCount = 0;

                        foreach (Sheet sh in drgDoc.Sheets)
                        {
                            if (!sh.ExcludeFromPrinting)
                            {
                                string modelName;
                                //string modelExtension;
                                if (sh.DrawingViews.Count > 0)  // added to make sure sheet has at least one drawing view
                                {
                                    modelName = sh.DrawingViews[1].ReferencedDocumentDescriptor.DisplayName;

                                    // this doesn't work right on files with special characters.
                                    //modelName = Path.GetFileNameWithoutExtension(modelName);

                                    if (modelName.EndsWith(".ipt") || modelName.EndsWith(".iam"))
                                    {
                                        int index = modelName.LastIndexOf('.');
                                        modelName = index == -1 ? modelName : modelName.Substring(0, index);

                                    }

                                    string newName = "";

                                    switch (sh.Orientation)
                                    {
                                        case PageOrientationTypeEnum.kLandscapePageOrientation:
                                            pMgr.Orientation = PrintOrientationEnum.kLandscapeOrientation;
                                            break;
                                        case PageOrientationTypeEnum.kDefaultPageOrientation:
                                            pMgr.Orientation = PrintOrientationEnum.kDefaultOrientation;
                                            break;
                                        case PageOrientationTypeEnum.kPortraitPageOrientation:
                                            pMgr.Orientation = PrintOrientationEnum.kPortraitOrientation;
                                            break;
                                    }

                                    pMgr.SetSheetRange(actualSheetIndex - missingSheetsCount, actualSheetIndex - missingSheetsCount);
                                    pMgr.PrintRange = PrintRangeEnum.kPrintSheetRange;
                                    pMgr.ScaleMode = PrintScaleModeEnum.kPrintBestFitScale;


                                    //if (more than one matching pdf name)
                                    if (idwFileToPrint.sheetNames.Where(x => x.Equals(idwFileToPrint.sheetNames[modifiedSheetIndex - 1])).Count() > 1)
                                    {
                                        newName = outputFolder + idwFileToPrint.sheetNames[modifiedSheetIndex - 1] + ".pdf";

                                        if (System.IO.File.Exists(outputFolder + idwFileToPrint.sheetNames[modifiedSheetIndex - 1] + ".pdf"))
                                        {
                                            assemblyFileNameList.Add(newName);
                                            newName = outputFolder + idwFileToPrint.sheetNames[modifiedSheetIndex - 1] + "~" + 1 + ".pdf";
                                            if (System.IO.File.Exists(newName)) System.IO.File.Delete(newName);
                                            System.IO.File.Move(outputFolder + idwFileToPrint.sheetNames[modifiedSheetIndex - 1] + ".pdf", newName);
                                            assemblyFileNameList.Add(newName);
                                        }
                                    }

                                    pdfFileName = outputFolder + idwFileToPrint.sheetNames[modifiedSheetIndex - 1] + ".pdf";

                                    // for some reason if a ps filename contains a comma it doesn't want to print.
                                    // we'll replace it with a tilde.
                                    //if (psFileName.Contains(","))
                                    //{
                                    //    psFileName = psFileName.Replace(',', '~');
                                    //    log.Warn("One or more characters replaced with '~' in " + pdfFileName);
                                    //    //logMessage += "One or more characters replaced with '~' in " + pdfFileName + "\r\n";
                                    //}

                                    //if (psFileName.Contains("°"))
                                    //{
                                    //    psFileName = psFileName.Replace('°', '~');
                                    //    log.Warn("One or more characters replaced with '°' in " + pdfFileName);
                                    //    //logMessage += "One or more characters replaced with '°' in " + pdfFileName + "\r\n";
                                    //}

                                    pMgr.PrintToFile(pdfFileName);

                                    if (System.IO.File.Exists(pdfFileName))
                                    {
                                        log.Info("PDF file generated for " + pdfFileName);
                                    }
                                    else
                                    {
                                        log.Warn("PDF file for " + pdfFileName + "could not be generated.");
                                        continue;   // skip trying to create a pdf if we couldn't generate a ps
                                    }

                                    if (assemblyFileNameList != null)
                                    {
                                        if (assemblyFileNameList.Count > 1)   // combine multiple assembly drawings into one pdf file
                                        {
                                            // Open the input files
                                            PdfDocument inputDocument1 = new PdfDocument();
                                            PdfDocument inputDocument2 = new PdfDocument();

                                            if (System.IO.File.Exists(assemblyFileNameList[0]))
                                            {
                                                inputDocument1 = PdfReader.Open(assemblyFileNameList[0], PdfDocumentOpenMode.Import);
                                            }

                                            if (System.IO.File.Exists(assemblyFileNameList[1]))
                                            {
                                                inputDocument2 = PdfReader.Open(assemblyFileNameList[1], PdfDocumentOpenMode.Import);
                                            }

                                            // Create the output document
                                            PdfDocument outputDocument = new PdfDocument();

                                            // Show consecutive pages facing. Requires Acrobat 5 or higher.
                                            outputDocument.PageLayout = inputDocument1.PageLayout;

                                            int count = Math.Max(inputDocument1.PageCount, inputDocument2.PageCount);
                                            for (int idx = 0; idx < count; idx++)
                                            {
                                                PdfPage page1 = new PdfPage();
                                                PdfPage page2 = new PdfPage();

                                                if (inputDocument1.PageCount > idx)
                                                {
                                                    page1 = inputDocument1.Pages[idx];
                                                    page1 = outputDocument.AddPage(page1);
                                                }

                                                if (inputDocument2.PageCount > idx)
                                                {
                                                    page2 = inputDocument2.Pages[idx];
                                                    page2 = outputDocument.AddPage(page2);
                                                }
                                            }

                                            if (System.IO.File.Exists(assemblyFileNameList[0]))
                                            {
                                                System.IO.File.Delete(assemblyFileNameList[0]);
                                            }

                                            // Save the document...
                                            while (!(System.IO.File.Exists(assemblyFileNameList[0])))
                                            {
                                                string filename = assemblyFileNameList[0];
                                                outputDocument.Save(filename);
                                            }

                                            // delete the temp file and clear the list
                                            if (System.IO.File.Exists(assemblyFileNameList[1]))
                                                System.IO.File.Delete(assemblyFileNameList[1]);

                                            assemblyFileNameList.Clear();
                                        }
                                    }

                                    actualSheetIndex++;
                                    modifiedSheetIndex++;
                                }
                                else
                                {
                                    actualSheetIndex++;   // still need to increment sheet index, even if no drawing view was found
                                                          // on current sheet...
                                                 
                                }

                                // double check to make sure file got generated and saved properly.
                                if (!System.IO.File.Exists(pdfFileName))
                                {
                                    log.Warn("No PDF Generated for " + pdfFileName);
                                    //logMessage += "No PDF Generated for " + pdfFileName + "\r\n";
                                }
                                else
                                {
                                    log.Info("PDF Generated for " + pdfFileName);
                                }
                            }
                            else
                            {
                                actualSheetIndex++;   // still need to increment sheet index, if sheet is set not to print
                                missingSheetsCount++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //errMessage += "PDF Generation Error in printToPDF\r\n";
                        //errMessage += ex.Message + "\r\n";
                        log.Error("PDF Generation Error in printToPDF");
                        log.Error(ex.Message);
                        return false;
                    }
                }

                catch (Exception ex)
                {
                    //errMessage += "IDW File Read Error in printToPDF\r\n";
                    //errMessage += ex.Message + "\r\n";
                    log.Error("IDW File Read Error in printToPDF");
                    log.Error(ex.Message);
                    return false;
                }
                return true;
            }
        }

        // deletePDFs based on ERP name
        public Boolean deletePDF(string fileName, string folder, ref string logMessage, ref string errMessage)
        {
            try
            {
                // set log file location
                XmlConfigurator.Configure();
                log4net.Repository.Hierarchy.Hierarchy h =
                (log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository();
                foreach (IAppender a in h.Root.Appenders)
                {
                    if (a is FileAppender)
                    {
                        FileAppender fa = (FileAppender)a;
                        // Programmatically set this to the desired location here
                        string logFileLocation = folder + "PDFPrint.log";

                        // Uncomment the lines below if you want to retain the base file name
                        // and change the folder name...
                        //FileInfo fileInfo = new FileInfo(fa.File);
                        //logFileLocation = string.Format(@"C:\MySpecialFolder\{0}", fileInfo.Name);

                        fa.File = logFileLocation;
                        fa.ActivateOptions();
                        break;
                    }
                }

                List<string> filesToDelete = new List<string>();
                string baseFileName = System.IO.Path.GetFileNameWithoutExtension(fileName);

                // get all the files relating to this ipt or iam
                filesToDelete = Directory.GetFiles(folder, baseFileName + "~*.pdf", SearchOption.AllDirectories).ToList();

                if (System.IO.File.Exists(folder + baseFileName + ".pdf"))
                {
                    filesToDelete.Add(Directory.GetFiles(folder, baseFileName + ".pdf", SearchOption.AllDirectories)[0]); // should only have one exact match
                }

                if (filesToDelete.Count > 0)
                {
                    //logMessage += @" " + "\r\n" + @" " + "Count of files: " + filesToDelete.Count() + @" " + "\r\n" + @" ";
                    log.Info(@" " + "\r\n" + @" " + "Count of files: " + filesToDelete.Count() + @" " + "\r\n" + @" ");


                    foreach (string f in filesToDelete)
                    {
                        //logMessage += "File to delete: " + f + @" " + "\r\n" + @" ";
                        log.Info("File to delete: " + f + @" " + "\r\n" + @" ");
                        if (System.IO.File.Exists(f))
                        {
                            System.IO.File.Delete(f);
                            //logMessage += "Deleted File " + f + @" " + "\r\n" + @" ";
                            log.Info("Deleted File " + f + @" " + "\r\n" + @" ");
                        }
                    }
                }
                else
                {
                    //logMessage += @" " + "\r\n" + @" " + "No File Found to Delete for " + fileName + @" " + "\r\n" + @" ";
                    log.Info(@" " + "\r\n" + @" " + "No File Found to Delete for " + fileName + @" " + "\r\n" + @" ");
                }

                return true;
            }
            catch (Exception)
            {
                errMessage += "Cannot Delete File " + fileName + @" " + "\r\n" + @" ";
                return false;
            }
        }


        bool CheckIfFileIsBeingUsed(string fileName)
        {
            try
            {
                using (System.IO.File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.None)) { };
            }

            catch (Exception)
            {
                return true;
            }

            return false;
        }


        struct idwFile
        {
            public string idwName;
            public int pageCount;
            public List<string> sheetNames;

        };
    }

    
    
        


}

    
