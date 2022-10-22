using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadanMaster.Models
{
    public class Nest
    {
        [Key]
        public int ID { get; set; }
        public string nestName { get; set; }
        public string nestPath { get; set; }
        public byte[] Thumbnail { get; set; }


        public virtual ICollection<OrderItem> OrderItems { get; set; }
        public virtual ICollection<NestedParts> NestedParts { get; set; }
    }
}
