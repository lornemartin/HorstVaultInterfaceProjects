using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PrintPDF;

namespace PrintPdfTest2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string fileName = @"C:\Vault Workspace\Designs\Main\Blades\SB1000\SB1000(Assembly).idw";
            //string fileName = @"C:\Vault Workspace\Designs\Main\Blades\Dirt Blades\Grader Blade\GB-Parts.idw";
            //string fileName = @"C:\Vault Workspace\Designs\Main\Blades\Direct Mount Frames\SB4000 Direct Mounts\John Deere Direct Mounts\JD 6000 Series\JD6000 with front pump\DM JD6000 with pump Drawings.idw";
            string PDFPath = @"C:\TempPDF\";
            string pdfPrinterName = "Microsoft Print To Pdf";

            PrintObject printOb = new PrintObject();
            string errMsg = "";
            string logMsg = "";
            if (printOb.printToPDF(fileName, PDFPath, pdfPrinterName, ref errMsg, ref logMsg))
            {
                MessageBox.Show("Success");
            }
            else
            {
                MessageBox.Show("Error");
            }
        }
    }
}
