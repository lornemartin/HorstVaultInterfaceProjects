using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadanMaster.Models
{
    public class Order
    {
        [Key]
        public int ID { get; set; }
        public string OrderNumber { get; set; }
        public string ScheduleName { get; set; }
        public string BatchName { get; set; }
        public DateTime EntryDate { get; set; }
        public DateTime DueDate { get; set; }
        public bool IsComplete { get; set; }
        public bool IsBatch { get; set; }
        public DateTime? DateCompleted { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; }
    }
}
