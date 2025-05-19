using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

using PrintPDF;

using ACJE = Autodesk.Connectivity.JobProcessor.Extensibility;
using ACW = Autodesk.Connectivity.WebServices;
using ACWT = Autodesk.Connectivity.WebServicesTools;
using VDF = Autodesk.DataManagement.Client.Framework;
using Inventor;


using Autodesk.Connectivity.Extensibility.Framework;
using Autodesk.Connectivity.Explorer.Extensibility;
using Autodesk.Connectivity.WebServices;
using Autodesk.Connectivity.WebServicesTools;


namespace PrintPDFTest
{
    public partial class Form1 : Form
    {
        private string TargetFolder { get; set; }
        private string PDFPath { get; set; }
        private string pdfPrinterName { get; set; }
        private string psToPdfProgName { get; set; }
        private string ghostScriptWorkingFolder { get; set; }

        // this is what determines the pdf file naming convention, either by name, or by ERP Number
        //private const string VaultSearchEntity = "33da3ae9-2966-47a1-a049-7e57ace691a3";     // Vault ERPNumber
        private const string VaultSearchEntity = "Name";            // Vault Name


        private string idwName { get; set; }

        public Form1()
        {
            TargetFolder = System.IO.Path.GetTempPath();
            PDFPath = @"C:\TempPDF\";
            pdfPrinterName = @"Microsoft Print To PDF";

            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "idw | *.idw";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                idwName = openFileDialog.FileName;
                testPrint(idwName);
            }
            

            
        }

        private void testPrint(string fileName)
        {

            PrintObject printOb = new PrintObject();
            string errMsg = "";
            string logMsg = "";
            if (printOb.printToPDF(fileName, PDFPath, pdfPrinterName, ref errMsg, ref logMsg))
            {
                Console.WriteLine("Successfully printed " + fileName + " to PDF\n\r", ACJE.MessageType.eInformation);
            }
            else
            {
                Console.WriteLine("Error printing " + fileName + " to PDF. \n\r" + errMsg, ACJE.MessageType.eError);
            }
        }
    }
}   

