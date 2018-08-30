using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace ItemExport
{
    public partial class Product
    {
        public void AddChildProduct(HorstMFGEntities db, Product p, int qty)
        {
            Product parentProduct = this;
            ProductProduct searchPp = db.ProductProducts.Where(pr1 => pr1.ParentProductID == parentProduct.ID).Where(pr2 => pr2.Product.ID == p.ID).FirstOrDefault();

            if (searchPp == null)
            {
                searchPp = new ProductProduct();
            }

            searchPp.Product1 = this;
            searchPp.Product = p;
            searchPp.Qty = qty;

            db.ProductProducts.Add(searchPp);
        }

    }
}