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


                    using (StreamWriter writetext = new StreamWriter(myProcess.StartInfo.WorkingDirectory + "arguments.txt"))
                    {
                        writetext.WriteLine(myProcess.StartInfo.Arguments);
                    }

                    int lineCount = System.IO.File.ReadLines(AppSettings.Get("PrintPDFreturnFile").ToString()).Count();

                    // after a successful run of PrintPDF, the file 'return.txt' should contain a 
                    // list of files printed, the number of lines matching the number of sheets in the idw
                    
                    if (!(lineCount == pageCount))
                    // if the drawing set has a sheet with no drawing views on it, for example if a sheet has only a BOM
                    // this test will not be accurate.  The routine will return an error even though the pages all printed ok
                    // but because the page count does not equal the sheet count it will return a false error.

                    // so I decided for now I will still log the error, but we won't return a fail status.
                    {
                        errMessage = System.IO.File.ReadAllText(AppSettings.Get("PrintPDFreturnFile").ToString());
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

    
