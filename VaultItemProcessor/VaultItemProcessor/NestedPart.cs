//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace VaultItemProcessor
{
    using System;
    using System.Collections.Generic;
    
    public partial class NestedPart
    {
        public int ID { get; set; }
        public int Qty { get; set; }
        public Nullable<int> OrderItem_ID { get; set; }
        public Nullable<int> Nest_ID { get; set; }
    
        public virtual Nest Nest { get; set; }
        public virtual OrderItem OrderItem { get; set; }
    }
}
