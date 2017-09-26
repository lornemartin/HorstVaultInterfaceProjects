using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using InventorApprentice;

using PdfSharp;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Drawing;

namespace PrintPDF
{
    class Program
    {
        static int Main(string[] args)
        {
            // the first parameter passed to this program needs to be the name of the printer to print to.
            // the second one is the path to the folder to print the pdfs to
            // the third one is the path and name of the executable of the postscript to pdf converter
            // the rest of the parameters need to be in sets as follows:
            // 1.  the full path to the idw file
            // 2.  the number of pages in the idw file
            // 3.  a list of filenames (the count matching the number of pages), one for each page in the idw

            // this program writes to a text file called return.txt that is created in the working directory.
            // it outputs each pdf filename on a line by itself as soon as it has successfully written it. 

            string printer = "";
            string outputFolder = "";
            string pdfConverter = "";
            string workingDir = "";
            List<idwFile> idwFiles = new List<idwFile>();

            try
            {
                if (System.IO.File.Exists("return.txt")) System.IO.File.Delete("return.txt");
                idwFile idw = new idwFile();
                idw.sheetNames = new List<string>();

                if (args == null)
                {
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter("return.txt", true))
                    {
                        file.WriteLine("Error: No arguments specified, aborting operation");
                    }
                    Console.WriteLine("No arguments specified, aborting operation\n");
                    return 1;     // printer not specified, nothing to print
                }

                printer = args[0]; // first argument is name of printer to print to
                outputFolder = args[1];
                pdfConverter = args[2];
                workingDir = args[3];

                for (int argIndex = 4; argIndex < args.Count();)    // the rest of the arguments are the pdf names to use for printing the idw sheets
                {
                    idw.idwName = args[argIndex];
                    idw.pageCount = int.Parse(args[argIndex+1]);
                    argIndex+=2;
                    int i = 0;
                    for (i = argIndex; i < (argIndex + idw.pageCount); i++)
                    {
                        idw.sheetNames.Add(args[i]);
                        using (System.IO.StreamWriter file = new System.IO.StreamWriter("callingparams.txt", true))
                        {
                            file.WriteLine(args[i]);
                            if (System.IO.File.Exists(outputFolder + args[i] + ".pdf"))
                            {
                                System.IO.File.Delete(outputFolder + args[i] + ".pdf");  // delete any outdated files
                            }
                        }
                    }
                    argIndex = i;
                    idwFiles.Add(idw);
                    idw.sheetNames = new List<string>();
                }
            }
            catch (Exception ex)
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter("return.txt", true))
                {
                    file.WriteLine("Error: Problems parsing arguments, aborting operation");
                }
                Console.WriteLine("Problems parsing arguments, aborting operation\n");
                return 1;
            }

            try
            {
                foreach (idwFile idw in idwFiles)
                {
                    InventorApprentice.ApprenticeServerComponent oApprentice = new ApprenticeServerComponent();
                    InventorApprentice.ApprenticeServerDrawingDocument drgDoc;
                    InventorApprentice.ApprenticeDrawingPrintManager pMgr;
                    drgDoc = (InventorApprentice.ApprenticeServerDrawingDocument)oApprentice.Document;
                    oApprentice.Open(idw.idwName);
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
                            modelExtension = System.IO.Path.GetExtension(modelName);

                            List<string> assemblyFileNameList = new List<string>();
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
                            pMgr.SetSheetRange(actualSheetIndex,actualSheetIndex);
                            pMgr.PrintRange = PrintRangeEnum.kPrintSheetRange;
                            pMgr.ScaleMode = InventorApprentice.PrintScaleModeEnum.kPrintBestFitScale;

                            //if (more than one matching pdf name)
                            if (idw.sheetNames.Where(x => x.Equals(idw.sheetNames[modifiedSheetIndex - 1])).Count() > 1)
                            {
                                newName = outputFolder + idw.sheetNames[modifiedSheetIndex - 1] + ".pdf";

                                if (System.IO.File.Exists(outputFolder + idw.sheetNames[modifiedSheetIndex - 1] + ".pdf"))
                                {
                                    assemblyFileNameList.Add(newName);
                                    newName = outputFolder + idw.sheetNames[modifiedSheetIndex - 1] + "~" + 1 + ".pdf";
                                    if (System.IO.File.Exists(newName)) System.IO.File.Delete(newName);
                                    System.IO.File.Move(outputFolder + idw.sheetNames[modifiedSheetIndex - 1] + ".pdf", newName);
                                    assemblyFileNameList.Add(newName);
                                }
                            }

                            string psFileName = outputFolder + idw.sheetNames[modifiedSheetIndex - 1] + ".ps";
                            string pdfFileName = outputFolder + idw.sheetNames[modifiedSheetIndex - 1] + ".pdf";

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
                            //startInfo.WorkingDirectory = @"C:\Program Files\gs\gs9.18\bin\";
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

                            using (System.IO.StreamWriter file = new System.IO.StreamWriter("return.txt", true))
                            {
                                file.WriteLine(pdfFileName);
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
                return 0;
            }
            catch (Exception x)
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter("return.txt", true))
                {
                    file.WriteLine("Error: Problems printing pdfs, aborting operation");
                    file.WriteLine(x.ToString());
                }
                Console.WriteLine("Problems printing pdfs, aborting operation\n");
                return 1;
            }
        }

        /*
        backup of main before fixing of multiple pdfs printing
        static int Main(string[] args)
        {
            // the first parameter passed to this program needs to be the name of the printer to print to.
            // the second one is the path to the folder to print the pdfs to
            // the third one is the path and name of the executable of the postscript to pdf converter
            // the rest of the parameters need to be in sets as follows:
            // 1.  the full path to the idw file
            // 2.  the number of pages in the idw file
            // 3.  a list of filenames (the count matching the number of pages), one for each page in the idw

            // this program writes to a text file called return.txt that is created in the working directory.
            // it outputs each pdf filename on a line by itself as soon as it has successfully written it. 

            string printer = "";
            string outputFolder = "";
            string pdfConverter = "";
            List<idwFile> idwFiles = new List<idwFile>();

            try
            {
                if (System.IO.File.Exists("return.txt")) System.IO.File.Delete("return.txt");
                idwFile idw = new idwFile();
                idw.sheetNames = new List<string>();

                if (args == null)
                {
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter("return.txt", true))
                    {
                        file.WriteLine("Error: No arguments specified, aborting operation");
                    }
                    Console.WriteLine("No arguments specified, aborting operation\n");
                    return 1;     // printer not specified, nothing to print
                }

                printer = args[0]; // first argument is name of printer to print to
                outputFolder = args[1];
                pdfConverter = args[2];

                for (int argIndex = 3; argIndex < args.Count(); )    // the rest of the arguments are the pdf names to use for printing the idw sheets
                {
                    idw.idwName = args[argIndex];
                    idw.pageCount = int.Parse(args[argIndex + 1]);
                    argIndex += 2;
                    int i = 0;
                    for (i = argIndex; i < (argIndex + idw.pageCount); i++)
                    {
                        idw.sheetNames.Add(args[i]);
                        using (System.IO.StreamWriter file = new System.IO.StreamWriter("callingparams.txt", true))
                        {
                            file.WriteLine(args[i]);
                        }
                    }
                    argIndex = i;
                    idwFiles.Add(idw);
                    idw.sheetNames = new List<string>();
                }
            }
            catch (Exception ex)
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter("return.txt", true))
                {
                    file.WriteLine("Error: Problems parsing arguments, aborting operation");
                }
                Console.WriteLine("Problems parsing arguments, aborting operation\n");
                return 1;
            }

            try
            {
                foreach (idwFile idw in idwFiles)
                {
                    Inventor.ApprenticeServerComponent oApprentice = new ApprenticeServerComponent();
                    Inventor.ApprenticeServerDrawingDocument drgDoc;
                    Inventor.ApprenticeDrawingPrintManager pMgr;
                    drgDoc = (Inventor.ApprenticeServerDrawingDocument)oApprentice.Document;
                    oApprentice.Open(idw.idwName);
                    drgDoc = (Inventor.ApprenticeServerDrawingDocument)oApprentice.Document;
                    pMgr = (Inventor.ApprenticeDrawingPrintManager)drgDoc.PrintManager;
                    pMgr.Printer = printer;
                    int sheetIndex = 1;
                    Boolean doCombine = true;

                    foreach (Sheet sh in drgDoc.Sheets)
                    {
                        string modelName;
                        string modelExtension;
                        modelName = sh.DrawingViews[1].ReferencedDocumentDescriptor.DisplayName;
                        modelExtension = System.IO.Path.GetExtension(modelName);

                        List<string> assemblyFileNameList = new List<string>();
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
                        pMgr.SetSheetRange(sheetIndex, sheetIndex);
                        pMgr.PrintRange = PrintRangeEnum.kPrintSheetRange;
                        pMgr.ScaleMode = Inventor.PrintScaleModeEnum.kPrintBestFitScale;

                        //if (more than one matching pdf name)
                        if (idw.sheetNames.Where(x => x.Equals(idw.sheetNames[sheetIndex - 1])).Count() > 1)
                        {
                            newName = outputFolder + idw.sheetNames[sheetIndex - 1] + ".pdf";

                            if (!doCombine)
                                if (System.IO.File.Exists(outputFolder + idw.sheetNames[sheetIndex - 1] + ".pdf"))
                                {
                                    System.IO.File.Delete(outputFolder + idw.sheetNames[sheetIndex - 1] + ".pdf");
                                }

                            if (System.IO.File.Exists(outputFolder + idw.sheetNames[sheetIndex - 1] + ".pdf"))
                            {
                                assemblyFileNameList.Add(newName);
                                newName = outputFolder + idw.sheetNames[sheetIndex - 1] + "~" + 1 + ".pdf";
                                System.IO.File.Move(outputFolder + idw.sheetNames[sheetIndex - 1] + ".pdf", newName);
                                assemblyFileNameList.Add(newName);
                            }
                        }
                        else
                        {
                            System.IO.File.Delete(outputFolder + modelName + ".pdf");
                        }

                        string psFileName = outputFolder + idw.sheetNames[sheetIndex - 1] + ".ps";
                        string pdfFileName = outputFolder + idw.sheetNames[sheetIndex - 1] + ".pdf";

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
                        //startInfo.WorkingDirectory = @"C:\Program Files\gs\gs9.14\bin\";
                        startInfo.Arguments = @"""" + psFileName + @"""" + " " + @"""" + pdfFileName + @"""";
                        startInfo.CreateNoWindow = true;
                        oProc.StartInfo = startInfo;
                        oProc.StartInfo.UseShellExecute = false;
                        oProc.Start();
                        oProc.WaitForExit();
                        using (System.IO.StreamWriter file = new System.IO.StreamWriter("return.txt", true))
                        {
                            file.WriteLine(pdfFileName);
                        }

                        System.IO.File.Delete(psFileName);

                        if (assemblyFileNameList != null)
                        {

                            if (assemblyFileNameList.Count > 1)   // combine multiple assembly drawings into one pdf file
                            {
                                // Open the input files
                                PdfDocument inputDocument1 = PdfReader.Open(assemblyFileNameList[0], PdfDocumentOpenMode.Import);
                                PdfDocument inputDocument2 = PdfReader.Open(assemblyFileNameList[1], PdfDocumentOpenMode.Import);

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

                                // Save the document...
                                string filename = assemblyFileNameList[0];
                                outputDocument.Save(filename);

                                // delete the temp file and clear the list
                                System.IO.File.Delete(assemblyFileNameList[1]);
                                assemblyFileNameList.Clear();
                            }
                        }
                        sheetIndex++;
                    }
                }
                return 0;
            }
            catch (Exception)
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter("return.txt", true))
                {
                    file.WriteLine("Error: Problems printing pdfs, aborting operation");
                }
                Console.WriteLine("Problems printing pdfs, aborting operation\n");
                return 1;
            }
        }
        */
        struct idwFile
        {
            public string idwName;
            public int pageCount;
            public List<string> sheetNames;

        };
    }
}
