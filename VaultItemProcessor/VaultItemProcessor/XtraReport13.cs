using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using System.IO;
using DevExpress.XtraPdfViewer;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System.Collections.Generic;

namespace VaultItemProcessor
{
    public partial class XtraReport13 : DevExpress.XtraReports.UI.XtraReport
    {
        public XtraReport13()
        {
            InitializeComponent();
        }

        //assembly 1 half page
        private void xrPictureBox2_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            if (ReportAssemblies.GetCurrentColumnValue("AssemblyName") != null)
            {
                List<Bitmap> bmpList = (List<Bitmap>) ReportAssemblies.GetCurrentColumnValue("Pages");

                if (bmpList.Count > 0)
                {
                    Bitmap orig = (bmpList[0]);
                    xrPictureBox2.ImageSource = new DevExpress.XtraPrinting.Drawing.ImageSource(RotateBitmap(orig, RotateFlipType.Rotate90FlipNone));
                    xrPictureBox2.BackColor = Color.AliceBlue;
                }
                else
                {
                    xrPictureBox2.ImageSource = null;
                }
            }
        }

        // assembly 1 full page
        private void xrPictureBox5_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            if (ReportAssemblies.GetCurrentColumnValue("AssemblyName") != null)
            {
                List<Bitmap> bmpList = (List<Bitmap>)ReportAssemblies.GetCurrentColumnValue("Pages");

                if (bmpList.Count > 0)
                {
                    xrPictureBox5.ImageSource = new DevExpress.XtraPrinting.Drawing.ImageSource(bmpList[0]);
                    xrPictureBox5.BackColor = Color.AliceBlue;
                }
                else
                {
                    xrPictureBox5.ImageSource = null;
                }
            }
        }

        // assembly two half page
        private void xrPictureBox4_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            if (ReportAssemblies.GetCurrentColumnValue("AssemblyName") != null)
            {
                List<Bitmap> bmpList = (List<Bitmap>)ReportAssemblies.GetCurrentColumnValue("Pages");

                if (bmpList.Count > 0)
                {
                    Bitmap orig = (bmpList[1]);
                    xrPictureBox4.ImageSource = new DevExpress.XtraPrinting.Drawing.ImageSource(RotateBitmap(orig, RotateFlipType.Rotate90FlipNone));
                    xrPictureBox4.BackColor = Color.AliceBlue;
                }
                else
                {
                    xrPictureBox4.ImageSource = null;
                }
            }
        }

        // assembly 2 full page
        private void xrPictureBox6_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            if (ReportAssemblies.GetCurrentColumnValue("AssemblyName") != null)
            {
                List<Bitmap> bmpList = (List<Bitmap>)ReportAssemblies.GetCurrentColumnValue("Pages");

                if (bmpList.Count > 0)
                {
                    xrPictureBox6.ImageSource = new DevExpress.XtraPrinting.Drawing.ImageSource(bmpList[1]);
                    xrPictureBox6.BackColor = Color.AliceBlue;
                }
                else
                {
                    xrPictureBox6.ImageSource = null;
                }
            }
        }

        

        // parts half page
        private void xrPictureBox1_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            if (ReportParts.GetCurrentColumnValue("PartName") != null)
            {
                List<Bitmap> bmpList = (List<Bitmap>)ReportParts.GetCurrentColumnValue("Pages");

                if (bmpList.Count > 0)
                {
                    Bitmap orig = (bmpList[0]);
                    xrPictureBox1.ImageSource = new DevExpress.XtraPrinting.Drawing.ImageSource(RotateBitmap(orig, RotateFlipType.Rotate90FlipNone));
                    xrPictureBox1.BackColor = Color.AliceBlue;
                }
                else
                {
                    xrPictureBox1.ImageSource = null;
                }
            }
        }

        // parts full page
        private void xrPictureBox7_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            if (ReportParts.GetCurrentColumnValue("PartName") != null)
            {
                List<Bitmap> bmpList = (List<Bitmap>)ReportParts.GetCurrentColumnValue("Pages");

                if (bmpList.Count > 0)
                {
                    Bitmap orig = (bmpList[0]);
                    xrPictureBox7.ImageSource = new DevExpress.XtraPrinting.Drawing.ImageSource(orig);
                    xrPictureBox7.BackColor = Color.AliceBlue;
                }
                else
                {
                    xrPictureBox7.ImageSource = null;
                }
            }
        }

        private void Assy2Detail1_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            if (ReportAssemblies.GetCurrentColumnValue("AssemblyName") != null)
            {
                List<Bitmap> bmpList = (List<Bitmap>)ReportAssemblies.GetCurrentColumnValue("Pages");

                if (bmpList.Count < 2)
                {
                    e.Cancel = true;
                }
                
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void Assy2Detail2_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            if (ReportAssemblies.GetCurrentColumnValue("AssemblyName") != null)
            {
                List<Bitmap> bmpList = (List<Bitmap>)ReportAssemblies.GetCurrentColumnValue("Pages");

                if (bmpList.Count < 2)
                {
                    e.Cancel = true;
                }

            }
            else
            {
                e.Cancel = true;
            }
        }

        private void Assy2FullPagePDF_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            if (ReportAssemblies.GetCurrentColumnValue("AssemblyName") != null)
            {
                List<Bitmap> bmpList = (List<Bitmap>)ReportAssemblies.GetCurrentColumnValue("Pages");

                if (bmpList.Count < 2)
                {
                    e.Cancel = true;
                }

            }
            else
            {
                e.Cancel = true;
            }
        }

        private Bitmap RotateBitmap(Image original_image, RotateFlipType rotate_flip_type)
        {
            // Copy the Bitmap.
            Bitmap new_bitmap = new Bitmap(original_image);

            // Rotate and flip.
            new_bitmap.RotateFlip(rotate_flip_type);

            // Return the result.
            return new_bitmap;
        }
    }
}
