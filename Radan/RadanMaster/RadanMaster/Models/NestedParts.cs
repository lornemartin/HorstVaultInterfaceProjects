using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadanMaster.Models
{
    public class NestedParts
    {
        [Key]
        public int ID { get; set; }
        public int Qty { get; set; }
        public OrderItem OrderItem { get; set; }
    }
}
