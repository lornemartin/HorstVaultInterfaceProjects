using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RadanMaster.Models
{
    public class OrderItem
    {
        [Key]
        public int ID { get; set; }
        public Order Order { get; set; }
        public int OrderID { get; set; }
        public bool IsComplete { get; set; }
        public int QtyRequired { get; set; }
        public int QtyNested { get; set; }
        public bool IsInProject { get; set; }
        public string Notes { get; set; }

        public Part Part { get; set; }
        public int PartID { get; set; }

        public RadanID RadanID {get; set; }
        public int RadanIDNumber { get; set; }

        public virtual ICollection<Nest> AssociatedNests { get; set; }
    }
}
