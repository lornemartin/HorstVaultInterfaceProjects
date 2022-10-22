using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadanMaster.Models
{
    public class OrderItemOperationPerformed
    {

        public int ID { get; set; }
        public int qtyRequired { get; set; }
        public int qtyDone { get; set; }

        public OrderItemOperation orderItemOperation { get; set; }
        public int orderOperationID { get; set; }

        public OperationPerformed operationPerformed { get; set; }
        public int operationPerformedID { get; set; }
    }
}
