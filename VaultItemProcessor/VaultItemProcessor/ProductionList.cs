using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;
using PdfSharp;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using PdfSharp.Pdf.IO;
using System.Drawing;
using PdfSharp.Drawing.Layout;
using DevExpress.XtraEditors.Controls;
using DevExpress.DataAccess;
using DevExpress.DataAccess.ObjectBinding;

namespace VaultItemProcessor
{
    [System.ComponentModel.DisplayName("ProductionList")]
    [HighlightedClass]
    public class ProductionListDataSource
    {
        public string XmlFileName { get; set; }
        public string PdfInputPath { get; set; }
        public List<ProductionListProduct> productList { get; set; }
        public bool Finalized { get; set; }
        public int currentIndex { get; set; }

        // The HighlightedMember attribute highlights a constructor.
        [HighlightedMember]
        
        public ProductionListDataSource()
        {
            //XmlFileName = xmlFilePath + "ProductionList.xml";
            XmlFileName = (AppSettings.Get("ExportFilePath").ToString()) + @"ProductionList.xml";
            PdfInputPath = (AppSettings.Get("PdfPath").ToString());
            productList = new List<ProductionListProduct>();
            Finalized = false;
            currentIndex = 0;
        }




        public void AddProduct(ProductionListProduct prod)
        {
            if (productList.Count > 0)
            {
                int maxId = productList.Max(p => p.ID);
                prod.ID = maxId + 1;
                currentIndex = prod.ID;
            }
            else
                prod.ID = 0;

            productList.Add(prod);

            SaveToFile();
        }

        public ProductionListProduct GetPrev()
        {
            if (currentIndex >= 1)
            {
                currentIndex--;
                return productList.ElementAt(currentIndex);
            }
            else
            {
                return null;
            }
        }

        public ProductionListProduct GetNext()
        {
            if (currentIndex < productList.Count())
            {
                currentIndex++;
                return productList.ElementAt(currentIndex);
            }
            else
            {
                return null;
            }
        }

        // The HighlightedMember attribute highlights a data member.
        [HighlightedMember]
        public IEnumerable<ProductionListProduct> GetProductionList()
        {
            Load();
            return this.productList;
        }
        public bool SaveToFile()
        {
            try
            {
                XmlSerializer xs = new XmlSerializer(typeof(ProductionListDataSource));
                TextWriter tw = new StreamWriter(XmlFileName, false);
                xs.Serialize(tw, this);
                tw.Close();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public ProductionListDataSource Load()
        {
            try
            {
                XmlSerializer xs = new XmlSerializer(typeof(ProductionListDataSource));
                TextReader tr = new StreamReader(XmlFileName, false);

                ProductionListDataSource pList = (ProductionListDataSource) xs.Deserialize(tr);
                //productList = (ProductionListProduct) xs.Deserialize(tr);
                tr.Close();
                productList = pList.productList;
                return pList;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        //public void FinalizeData()
        //{
        //    Finalized = true;
        //    SaveToFile();

        //}

        //public bool IsFinalized()
        //{
        //    return Finalized;
        //}
    }



    

}
