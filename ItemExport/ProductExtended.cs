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
            List<ProductProduct> searchPpList = db.ProductProducts.Where((pr1 => pr1.ParentProductID == 1)).ToList();

            if (searchPp == null)
            {
                searchPp = new ProductProduct();
            }

            searchPp.Product = this;
            searchPp.Product1 = p;
            searchPp.Qty = qty;
        }

    }
}