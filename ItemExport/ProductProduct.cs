//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ItemExport
{
    using System;
    using System.Collections.Generic;
    
    public partial class ProductProduct
    {
        public int ParentProductID { get; set; }
        public int ChildProductID { get; set; }
        public int Qty { get; set; }
    
        public virtual Product Product { get; set; }
        public virtual Product Product1 { get; set; }
    }
}
