using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadanMaster.Models
{
    public class RadanID
    {
        [Key]
        public int ID { get; set; }
        public int RadanIDNumber { get; set; }
        [Required]
        public OrderItem OrderItem { get; set; }
        public int OrderItemID { get; set; }
    }
}
