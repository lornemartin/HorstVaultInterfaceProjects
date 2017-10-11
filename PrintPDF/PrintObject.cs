using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InventorApprentice;
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


namespace PrintPDF
{
    public class PrintObject
    {

        // this routine was coded to use the ERP names instead of the part names
        // it needs to have a list of ERP names passed to it, each one matching its corresponding sheet in the IDW.
        //  the routine actually doesn't care whether it gets passed Vault Names or Epicor Numbers, 
        //  it'll just name the pdfs accordingly
        public Boolean printToPDFNew(string idw, System.Collections.Generic.Dictionary<VDF.Vault.Currency.Entities.IEntity,
                                                      Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties.PropertyValue>
                                                      propDict, string outputFolder, ref string errMessage, ref string logMessage)
        {
            {
                try
                {
                    

                    InventorApprentice.ApprenticeServerComponent oApprentice = new ApprenticeServerComponent();
                    InventorApprentice.ApprenticeServerDrawingDocument drgDoc;
                    drgDoc = (InventorApprentice.ApprenticeServerDrawingDocument)oApprentice.Document;
                    oApprentice.Open(idw);
                    drgDoc = (InventorApprentice.ApprenticeServerDrawingDocument)oApprentice.Document;
                    int pageCount = 1;
                    List<string> assemblyFileNameList = new List<string>();
                    
                    idwFile idwObject = new idwFile();
                    idwObject.sheetNames = new List<string>();
                    idwObject.idwName = idw;
                    idwObject.pageCount = drgDoc.Sheets.Count;

                    foreach (Sheet sh in drgDoc.Sheets)
                    {
                        if (sh.DrawingViews.Count > 0)
                        {
                            string modelName;
                            string modelExtension;
                            modelName = sh.DrawingViews[1].ReferencedDocumentDescriptor.DisplayName;
                            modelExtension = System.IO.Path.GetExtension(modelName);

                            bool matchFound = false;
                            foreach (KeyValuePair<VDF.Vault.Currency.Entities.IEntity,
                                     VDF.Vault.Currency.Properties.PropertyValue> pair in propDict)
                            {
                                if (pair.Key.EntityMasterId != 0)
                                {
                                    if (pair.Key.EntityName.ToString() == modelName)
                                    {

                                        modelName = System.IO.Path.GetFileNameWithoutExtension(pair.Value.Value.ToString());
                                        //logMessage+= "\nModel Name: " + modelName + "\n\r";
                                        matchFound = true;
                                        idwObject.sheetNames.Add(modelName);
                                        break;
                                    }
                                }
                            }

                            if (!matchFound)
                            {
                                logMessage += @" " + "\r\n" + @" " + "No corresponding model found for " + modelName + @" " + "\r\n" + @" ";
                                idwObject.sheetNames.Add("unmatchedfile");
                                pageCount++;
                                //continue;
                            }
                            else
                            {
                                pageCount++;
                            }

                        }
                    }

                    string debugFileName = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\PrintPDFCommandLine\debug.txt";
                    using (StreamWriter writetext = new StreamWriter(debugFileName))
                    {
                        writetext.WriteLine(AppSettings.Get("PrintPDFWorkingFolder").ToString());
                    }

                    Process myProcess = new Process();
                    myProcess.StartInfo.UseShellExecute = false;
                    myProcess.StartInfo.WorkingDirectory = AppSettings.Get("PrintPDFWorkingFolder").ToString();
                    myProcess.StartInfo.FileName = AppSettings.Get("PrintPDFExecutable").ToString();
                    myProcess.StartInfo.Arguments = @"""" + AppSettings.Get("PrintPDFPrinter").ToString() + @"""" + " " +
                                                    @"""" + outputFolder + @"""" + " " +
                                                    @"""" + AppSettings.Get("PrintPDFPS2PDF").ToString() + @"""" + " " +
                                                    @"""" + AppSettings.Get("GhostScriptWorkingFolder").ToString() + @"""" + " " +
                                                    @"""" + idw + @"""" + " " +
                                                    (pageCount-1) + " ";
                    foreach(string sheetName in idwObject.sheetNames)
                    {
                        myProcess.StartInfo.Arguments += " " + @"""" + sheetName + @"""";
                    }
                    myProcess.StartInfo.CreateNoWindow = true;
                    myProcess.Start();
                    myProcess.WaitForExit();

                    string argumentsFileName = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\PrintPDFCommandLine\arguments.txt";

                    using (StreamWriter writetext = new StreamWriter(argumentsFileName))
                    {
                        writetext.WriteLine(myProcess.StartInfo.Arguments);
                    }

                    string returnFileName = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\PrintPDFCommandLine\" + AppSettings.Get("PrintPDFreturnFile").ToString();
                    int lineCount = System.IO.File.ReadLines(returnFileName).Count();

                    // after a successful run of PrintPDF, the file 'return.txt' should contain a 
                    // list of files printed, the number of lines matching the number of sheets in the idw
                    
                    if (!(lineCount == pageCount))
                    // if the drawing set has a sheet with no drawing views on it, for example if a sheet has only a BOM
                    // this test will not be accurate.  The routine will return an error even though the pages all printed ok
                    // but because the page count does not equal the sheet count it will return a false error.

                    // so I decided for now I will still log the error, but we won't return a fail status.
                    {
                        errMessage = System.IO.File.ReadAllText(returnFileName);
                    }
                    else
                    {
                        logMessage += idw + " printed successfully";
                    }

                    errMessage = logMessage;
                    return true;
                }
                catch (Exception ex)
                {
                    errMessage += "Unknown Error in printToPDF\r\n";
                    errMessage += ex.Message + "\r\n";
                    return false;
                }
            }
        }

        // this is an attempt to get rid of the extra command line step for printing pdfs.  
        // it also uses the display names inside the idw for the pdf names rather than getting it from a Vault property.
        public Boolean printToPDF(string idw, string outputFolder, ref string errMessage, ref string logMessage)
        {
            {
                try
                {
                    InventorApprentice.ApprenticeServerComponent oApprentice = new ApprenticeServerComponent();
                    InventorApprentice.ApprenticeServerDrawingDocument drgDoc;
                    oApprentice.Open(idw);
                    drgDoc = (InventorApprentice.ApprenticeServerDrawingDocument)oApprentice.Document;
                    int pageCount = 1;
                    List<string> assemblyFileNameList = new List<string>();

                    idwFile idwFileToPrint = new idwFile();
                    idwFileToPrint.sheetNames = new List<string>();
                    idwFileToPrint.idwName = idw;
                    idwFileToPrint.pageCount = drgDoc.Sheets.Count;

                    foreach (Sheet sh in drgDoc.Sheets)
                    {
                        if (sh.DrawingViews.Count > 0)
                        {
                            string modelName;
                            string modelExtension;
                            modelName = sh.DrawingViews[1].ReferencedDocumentDescriptor.DisplayName;
                            modelName = Path.GetFileNameWithoutExtension(modelName);
                            modelExtension = System.IO.Path.GetExtension(modelName);

                            idwFileToPrint.sheetNames.Add(modelName);
                            pageCount++;

                        }
                    }

                    string printer = AppSettings.Get("PrintPDFPrinter").ToString();
                    string pdfConverter = AppSettings.Get("PrintPDFPS2PDF").ToString();
                    string workingDir = AppSettings.Get("GhostScriptWorkingFolder").ToString();

                    try
                    {
                        InventorApprentice.ApprenticeDrawingPrintManager pMgr;
                        drgDoc = (InventorApprentice.ApprenticeServerDrawingDocument)oApprentice.Document;
                        pMgr = (InventorApprentice.ApprenticeDrawingPrintManager)drgDoc.PrintManager;
                        pMgr.Printer = printer;
                        int actualSheetIndex = 1;
                        int modifiedSheetIndex = 1;

                        foreach (Sheet sh in drgDoc.Sheets)
                        {
                            string modelName;
                            string modelExtension;
                            if (sh.DrawingViews.Count > 0)  // added to make sure sheet has at least one drawing view
                            {
                                modelName = sh.DrawingViews[1].ReferencedDocumentDescriptor.DisplayName;
                                modelName = Path.GetFileNameWithoutExtension(modelName);
                                modelExtension = System.IO.Path.GetExtension(modelName);

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
                                pMgr.SetSheetRange(actualSheetIndex, actualSheetIndex);
                                pMgr.PrintRange = PrintRangeEnum.kPrintSheetRange;
                                pMgr.ScaleMode = InventorApprentice.PrintScaleModeEnum.kPrintBestFitScale;


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

                                string psFileName = outputFolder + idwFileToPrint.sheetNames[modifiedSheetIndex - 1] + ".ps";
                                string pdfFileName = outputFolder + idwFileToPrint.sheetNames[modifiedSheetIndex - 1] + ".pdf";

                                // for some reason if a ps filename contains a comma it doesn't want to print.
                                // we'll replace it with a tilde.
                                if (psFileName.Contains(","))
                                {
                                    psFileName = psFileName.Replace(',', '~');
                                }

                                if (psFileName.Contains("°"))
                                {
                                    psFileName = psFileName.Replace('°', '~');
                                }

                                pMgr.PrintToFile(psFileName);

                                // notice:
                                // gs doesn't seem to be able to handle the degree symbol
                                // all filenames with a degree symbol will lose it when run through this script

                                Process oProc = new Process();
                                // need the full path to the program if we want to set UseShellExecute to false
                                ProcessStartInfo startInfo = new ProcessStartInfo(pdfConverter);
                                startInfo.WorkingDirectory = workingDir;
                                startInfo.Arguments = @"""" + psFileName + @"""" + " " + @"""" + pdfFileName + @"""";
                                startInfo.CreateNoWindow = true;
                                oProc.StartInfo = startInfo;
                                oProc.StartInfo.UseShellExecute = false;
                                oProc.Start();
                                oProc.WaitForExit();


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

                                System.IO.File.Delete(psFileName);
                                actualSheetIndex++;
                                modifiedSheetIndex++;
                            }
                            else
                            {
                                actualSheetIndex++;   // still need to increment sheet index, even if no drawing view was found
                                                      // on current sheet...
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errMessage += "PDF Generation Error in printToPDF\r\n";
                        errMessage += ex.Message + "\r\n";
                        return false;
                    }
                }

                catch (Exception ex)
                {
                    errMessage += "IDW File Read Error in printToPDF\r\n";
                    errMessage += ex.Message + "\r\n";
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
                    logMessage += @" " + "\r\n" + @" " + "Count of files: " + filesToDelete.Count() + @" " + "\r\n" + @" ";

                    foreach (string f in filesToDelete)
                    {
                        logMessage += "File to delete: " + f + @" " + "\r\n" + @" ";
                        if (System.IO.File.Exists(f))
                        {
                            System.IO.File.Delete(f);
                            logMessage += "Deleted File " + f + @" " + "\r\n" + @" ";
                        }
                    }
                }
                else
                {
                    logMessage += @" " + "\r\n" + @" " + "No File Found to Delete for " + fileName + @" " + "\r\n" + @" ";
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

    
