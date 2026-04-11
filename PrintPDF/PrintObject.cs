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
using System.Reflection;
using Environment = System.Environment;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Core;

namespace PrintPDF
{

    public class PrintObject
    {
        public PrintObject()
        {
        }
        public Boolean printToPDF(string idw, string outputFolder, string pdfPrinterName, ref string errMessage, ref string logMessage)
        {
            {
                try
                {
                    LoggingLevelSwitch levelSwitch = new LoggingLevelSwitch();
                    levelSwitch.MinimumLevel = LogEventLevel.Verbose;
                    string logFileLocation = outputFolder + "PDFPrint2.log";
                    Log.Logger = new LoggerConfiguration()
                                    // add a rolling file for all logs
                                    .WriteTo.File(logFileLocation,
                                         shared: true, fileSizeLimitBytes: 5000000)
                                    .WriteTo.Console()
                                    // set default minimum level
                                    .MinimumLevel.ControlledBy(levelSwitch)
                                   .CreateLogger();

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

                    List<DrawingSheet> drawingSheets = new List<DrawingSheet>();

                    // delete previous pdfs so we don't double up assembly drawings.
                    foreach (Sheet sh in drgDoc.Sheets)
                    {
                        if(!sh.ExcludeFromPrinting)
                        {
                            if (sh.DrawingViews.Count > 0)
                            {
                                DrawingSheet drawingSheet = new DrawingSheet();
                                //string modelName = sh.DrawingViews[1].ReferencedDocumentDescriptor.DisplayName;
                                // we were using the DisplayName property here until the 2023 update, when accessing it would thrown an exception quite often
                                drawingSheet.modelName = System.IO.Path.GetFileName(sh.DrawingViews[1].ReferencedDocumentDescriptor.ReferencedFileDescriptor.FullFileName);
                                drawingSheet.pdfName = outputFolder + System.IO.Path.GetFileNameWithoutExtension(drawingSheet.modelName) + ".pdf";

                                PrintOrientationEnum sheetOrientation = new PrintOrientationEnum();
                                switch (sh.Orientation)
                                {
                                    case PageOrientationTypeEnum.kLandscapePageOrientation:
                                        drawingSheet.orientation = PrintOrientationEnum.kLandscapeOrientation;
                                        break;
                                    case PageOrientationTypeEnum.kDefaultPageOrientation:
                                        drawingSheet.orientation = PrintOrientationEnum.kDefaultOrientation;
                                        break;
                                    case PageOrientationTypeEnum.kPortraitPageOrientation:
                                        drawingSheet.orientation = PrintOrientationEnum.kPortraitOrientation;
                                        break;
                                }

                                drawingSheets.Add(drawingSheet);

                                if (drawingSheet.modelName.EndsWith(".ipt") || drawingSheet.modelName.EndsWith(".iam"))
                                {
                                    int index = drawingSheet.modelName.LastIndexOf('.');
                                    drawingSheet.modelName = index == -1 ? drawingSheet.modelName : drawingSheet.modelName.Substring(0, index);
                                }

                                idwFileToPrint.sheetNames.Add(drawingSheet.modelName);
                                pageCount++;

                                try
                                {
                                    if (System.IO.File.Exists(drawingSheet.pdfName))
                                    {
                                        if (CheckIfFileIsBeingUsed(drawingSheet.pdfName))
                                        {
                                            // if file is in use, can't delete it.
                                            errMessage += "File is in use, cannot delete: " + drawingSheet.pdfName + "\r\n";
                                            return false;
                                        }
                                        System.IO.File.Delete(drawingSheet.pdfName);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    errMessage += "Error deleting existing PDF: " + drawingSheet.pdfName + " - " + ex.Message + "\r\n";
                                    return false;
                                }
                            }
                        }
                    }
                    logMessage += "Sheet Names All Read When Printing " + idwFileToPrint.idwName + "\r\n";
                    logMessage += "Sheet count: " + drawingSheets.Count + "\r\n";
                    Log.Information("Sheet Names All Read When Printing " + idwFileToPrint.idwName);

                    string printer = pdfPrinterName;
                    string pdfFileName = "";

                    try
                    {
                        logMessage += "Initializing PrintManager...\r\n";
                        ApprenticeDrawingPrintManager pMgr;
                        drgDoc = (ApprenticeServerDrawingDocument)oApprentice.Document;
                        pMgr = (ApprenticeDrawingPrintManager)drgDoc.PrintManager;
                        logMessage += "Setting printer to: " + printer + "\r\n";
                        pMgr.Printer = printer;
                        logMessage += "Printer set successfully.\r\n";
                        int actualSheetIndex = 1;
                        int modifiedSheetIndex = 1;
                        int missingSheetsCount = 0;

                        foreach (DrawingSheet drawingSheet in drawingSheets)
                        {
                            string modelName;
                            {
                                //modelName = sh.DrawingViews[1].ReferencedDocumentDescriptor.DisplayName;
                                modelName = drawingSheet.modelName;

                                // this doesn't work right on files with special characters.
                                //modelName = Path.GetFileNameWithoutExtension(modelName);

                                if (modelName.EndsWith(".ipt") || modelName.EndsWith(".iam"))
                                {
                                    int index = modelName.LastIndexOf('.');
                                    modelName = index == -1 ? modelName : modelName.Substring(0, index);

                                }

                                string newName = "";

                                logMessage += "Sheet " + actualSheetIndex + ": " + modelName + " - Setting print options...\r\n";
                                pMgr.Orientation = drawingSheet.orientation;

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

                                logMessage += "Calling PrintToFile: " + pdfFileName + "\r\n";
                                try
                                {
                                    pMgr.PrintToFile(pdfFileName);
                                }
                                catch (Exception printEx)
                                {
                                    logMessage += "PrintToFile FAILED for sheet " + actualSheetIndex + " (" + modelName + "): " + printEx.ToString() + "\r\n";
                                    Log.Error("PrintToFile FAILED for sheet " + actualSheetIndex + " (" + modelName + "): " + printEx.ToString());
                                    actualSheetIndex++;
                                    modifiedSheetIndex++;
                                    continue;
                                }

                                if (!System.IO.File.Exists(pdfFileName))
                                {
                                    logMessage += "PDF file for " + pdfFileName + " could not be generated.\r\n";
                                    Log.Warning("PDF file for " + pdfFileName + " could not be generated.");
                                    continue;
                                }

                                // Bullzip produces PostScript, not PDF. Convert PS to PDF using Ghostscript.
                                {
                                    string psFileName = pdfFileName + ".ps";
                                    if (System.IO.File.Exists(psFileName))
                                        System.IO.File.Delete(psFileName);
                                    System.IO.File.Move(pdfFileName, psFileName);
                                    logMessage += "Converting PS to PDF: " + psFileName + "\r\n";

                                    try
                                    {
                                        GhostscriptRunner.PsToPdf(psFileName, pdfFileName);
                                    }
                                    catch (Exception gsEx)
                                    {
                                        logMessage += "Ghostscript conversion failed: " + gsEx.Message + "\r\n";
                                        Log.Error("Ghostscript conversion failed: " + gsEx.Message);
                                        actualSheetIndex++;
                                        modifiedSheetIndex++;
                                        continue;
                                    }

                                    if (System.IO.File.Exists(psFileName))
                                        System.IO.File.Delete(psFileName);

                                    logMessage += "PS to PDF conversion successful: " + pdfFileName + "\r\n";
                                }

                                if (System.IO.File.Exists(pdfFileName))
                                {
                                    logMessage += "PDF file generated for " + pdfFileName + "\r\n";
                                    Log.Information("PDF file generated for " + pdfFileName);
                                }
                                else
                                {
                                    logMessage += "PDF file for " + pdfFileName + " could not be generated after conversion.\r\n";
                                    Log.Warning("PDF file for " + pdfFileName + " could not be generated after conversion.");
                                    continue;
                                }

                                if (assemblyFileNameList != null)
                                {
                                    if (assemblyFileNameList.Count > 1)   // combine multiple assembly drawings into one pdf file
                                    {
                                        // Wait for printer driver to finish writing the PDF files
                                        logMessage += "Waiting for PDF files to be ready for merge...\r\n";
                                        PdfDocument inputDocument1 = new PdfDocument();
                                        PdfDocument inputDocument2 = new PdfDocument();

                                        if (System.IO.File.Exists(assemblyFileNameList[0]))
                                        {
                                            inputDocument1 = WaitAndOpenPdf(assemblyFileNameList[0], ref logMessage);
                                        }

                                        if (System.IO.File.Exists(assemblyFileNameList[1]))
                                        {
                                            inputDocument2 = WaitAndOpenPdf(assemblyFileNameList[1], ref logMessage);
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


                                // double check to make sure file got generated and saved properly.
                                if (!System.IO.File.Exists(pdfFileName))
                                {
                                    logMessage += "No PDF Generated for " + pdfFileName + "\r\n";
                                    Log.Warning("No PDF Generated for " + pdfFileName);
                                }
                                else
                                {
                                    logMessage += "PDF Generated for " + pdfFileName + "\r\n";
                                    Log.Information("PDF Generated for " + pdfFileName);
                                }

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errMessage += "PDF Generation Error in printToPDF\r\n";
                        errMessage += ex.ToString() + "\r\n";
                        Log.Error("PDF Generation Error in printToPDF");
                        Log.Error(ex.ToString());
                        return false;
                    }
                }

                catch (Exception ex)
                {
                    errMessage += "IDW File Read Error in printToPDF\r\n";
                    errMessage += ex.Message + "\r\n";
                    Log.Error("IDW File Read Error in printToPDF");
                    Log.Error(ex.Message);
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
                    Log.Information(@" " + "\r\n" + @" " + "Count of files: " + filesToDelete.Count() + @" " + "\r\n" + @" ");


                    foreach (string f in filesToDelete)
                    {
                        //logMessage += "File to delete: " + f + @" " + "\r\n" + @" ";
                        Log.Information("File to delete: " + f + @" " + "\r\n" + @" ");
                        if (System.IO.File.Exists(f))
                        {
                            System.IO.File.Delete(f);
                            //logMessage += "Deleted File " + f + @" " + "\r\n" + @" ";
                            Log.Information("Deleted File " + f + @" " + "\r\n" + @" ");
                        }
                    }
                }
                else
                {
                    //logMessage += @" " + "\r\n" + @" " + "No File Found to Delete for " + fileName + @" " + "\r\n" + @" ";
                    Log.Information(@" " + "\r\n" + @" " + "No File Found to Delete for " + fileName + @" " + "\r\n" + @" ");
                }

                return true;
            }
            catch (Exception)
            {
                errMessage += "Cannot Delete File " + fileName + @" " + "\r\n" + @" ";
                return false;
            }
        }


        PdfDocument WaitAndOpenPdf(string filePath, ref string logMessage)
        {
            int maxRetries = 10;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    return PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
                }
                catch (InvalidOperationException ex)
                {
                    // Log the file header to identify what format the printer actually produced
                    if (i == 0)
                    {
                        try
                        {
                            byte[] header = new byte[20];
                            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                fs.Read(header, 0, header.Length);
                            }
                            string headerText = System.Text.Encoding.ASCII.GetString(header);
                            long fileSize = new FileInfo(filePath).Length;
                            logMessage += "File header: [" + headerText + "] Size: " + fileSize + " bytes\r\n";
                            logMessage += "PdfSharp error: " + ex.Message + "\r\n";
                        }
                        catch { }
                    }
                    logMessage += "PDF not ready yet (" + filePath + "), waiting... attempt " + (i + 1) + "/" + maxRetries + "\r\n";
                    Log.Warning("PDF not ready yet (" + filePath + "), attempt " + (i + 1));
                    Thread.Sleep(1000);
                }
            }
            throw new InvalidOperationException("PDF file not valid after " + maxRetries + " attempts: " + filePath);
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

        struct DrawingSheet
        {
            public string modelName;
            public string pdfName;
            public PrintOrientationEnum orientation;
        };
    }

    
    
        


}

    
