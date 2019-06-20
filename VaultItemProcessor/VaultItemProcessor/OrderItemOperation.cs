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
    
    public partial class OrderItemOperation
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public OrderItemOperation()
        {
            this.OrderItemOperationPerformeds = new HashSet<OrderItemOperationPerformed>();
        }
    
        public int ID { get; set; }
        public int qtyRequired { get; set; }
        public int qtyDone { get; set; }
        public Nullable<int> operationID { get; set; }
        public Nullable<int> orderItemID { get; set; }
    
        public virtual Operation Operation { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<OrderItemOperationPerformed> OrderItemOperationPerformeds { get; set; }
        public virtual OrderItem OrderItem { get; set; }
    }
}
