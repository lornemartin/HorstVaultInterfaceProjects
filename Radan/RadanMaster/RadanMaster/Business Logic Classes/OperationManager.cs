using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace RadanMaster
{
    public class OperationManager
    {
        Models.OrderItemOperation OrderItemOp { get; set; }                         // the order item operation in focus
        Models.User currentUser { get; set; }                                       // the currently logged in user
        List<Models.OrderItemOperation> associatedOrderItemOps { get; set; }        // a list of order item operations that are linked to the same part as the original order item operation
        List<Models.OrderItemOperationPerformed> itemOpsPerformed { get; set; }     // a list of order item operations performed that are linked to the selected associated order item operation
        List<Models.OperationPerformed> opsPerformed { get; set; }                  // a list of operations performed that are linked to the part in the original order item operation
        List<Models.OrderItemOperation> unFinishedOrderItemOps { get; set; }        // a list of operations in associatedOrderItemOps that still have outstanding ops to perform
        List<Models.OrderItemOperation> overBatchOrderItemOps { get; set; }         // a list of operations in associatedOrderItemOps that are not assigned to any batch item

        /// <summary>
        /// constructor 
        /// </summary>
        /// <param name="orderItemOp"></param>
        /// <param name="curUser"></param>
        public OperationManager(Models.OrderItemOperation orderItemOp, Models.User curUser)
        {
            currentUser = curUser;
            OrderItemOp = orderItemOp;
            RefreshDataStructures();
        }

        /// <summary>
        /// Refresh the data structures from the database
        /// </summary>
        public void RefreshDataStructures()
        {
            // first find all the associated orderItemOperations
            associatedOrderItemOps = Globals.dbContext.OrderItemOperations.Where(o => o.orderItem.PartID == OrderItemOp.orderItem.PartID)
                                                                          .Where(o => o.operationID == OrderItemOp.operationID).ToList();

            // then find all the associated orderItemOperations that have not been applied to an order item yet
            overBatchOrderItemOps = Globals.dbContext.OrderItemOperations.Where(o => o.orderItemID == null).Where(o => o.operation.PartID == OrderItemOp.operation.PartID)
                                                                                                           .Where(o => o.qtyRequired == 0).ToList();

            // now combine the two lists into one.
            associatedOrderItemOps.AddRange(overBatchOrderItemOps);

            //opsPerformed = Globals.dbContext.OperationPerformeds.Where(op => op.OrderItemOperationsPerformed.FirstOrDefault().orderItemOperation.operationID == OrderItemOp.operationID).ToList();
            opsPerformed = Globals.dbContext.OperationPerformeds.Where(op => op.OrderItemOperationsPerformed.FirstOrDefault().orderItemOperation.operation.PartID == OrderItemOp.operation.PartID).ToList();
        }

        /// <summary>
        /// are there any unassigned operationPerformeds that could be applied to this orderItem?
        /// </summary>
        /// <param name="qtyOverBatchItemOps"></param>
        /// <param name="qtyUnAssignedOrderItemOps"></param>
        /// <param name="opName"></param>
        /// <param name="partName"></param>
        /// <returns></returns>
        public bool HasUnassignedOperationsPerformed(out int qtyOverBatchItemOps, out int qtyUnAssignedOrderItemOps, out string opName, out string partName)
        {
            opName = OrderItemOp.operation.Name;
            partName = OrderItemOp.operation.Part.FileName;

            qtyOverBatchItemOps = 0;
            if (overBatchOrderItemOps != null)
                qtyOverBatchItemOps = overBatchOrderItemOps.Sum(x => x.qtyDone);

            qtyUnAssignedOrderItemOps = 0;
            if (associatedOrderItemOps != null)
            {
                qtyUnAssignedOrderItemOps = associatedOrderItemOps.Where(x => x.qtyDone < x.qtyRequired).Sum(x => x.qtyRequired - x.qtyDone);
            }

            if (qtyOverBatchItemOps > 0 && qtyUnAssignedOrderItemOps > 0)
                return true;
            else
                return false;
        }

        /// <summary>
        /// If we have both orderItemOperations that are not completed, and orderItemOperations that were previously overproduced,
        /// this function will attempt to fill all the unfinished items with the over produced items
        /// </summary>
        public void AssignUnfinishedOperations()
        {
            // First fill all unFinishedOps with overOps
            foreach (Models.OrderItemOperation unFinishedOp in unFinishedOrderItemOps.ToList())
            {
                foreach (Models.OrderItemOperation overOp in overBatchOrderItemOps.ToList())
                {
                    int unFinishedOpQtyToFill = unFinishedOp.qtyRequired - unFinishedOp.qtyDone;
                    int overOpQtyRemaining = overOp.qtyDone;

                    if (unFinishedOpQtyToFill >= overOp.qtyDone)
                    {
                        // this is the simplest, we just adjust the unFinishedOp, and remove the overOp
                        unFinishedOp.qtyDone += overOp.qtyDone;
                        unFinishedOp.OrderItemOperationPerformeds.Add(overOp.OrderItemOperationPerformeds.FirstOrDefault());

                        Globals.dbContext.OrderItemOperations.Remove(overOp);
                        // move on to next overOp
                    }
                    else             // overOp qty is more than will fit into unFinishedOp
                    {
                        unFinishedOp.qtyDone = unFinishedOp.qtyRequired;
                        overOp.qtyDone = unFinishedOpQtyToFill;

                        // create a new 
                        Models.OrderItemOperationPerformed newOrderItemOpPerformed = new Models.OrderItemOperationPerformed();


                    }
                }
            }

            // then fill any remaining overOps with unFinishedOps

            // .....refresh overOps.....

            foreach (Models.OrderItemOperation overOp in overBatchOrderItemOps.ToList())
            {
                unFinishedOrderItemOps = Globals.dbContext.OrderItemOperations.Where(o => o.orderItem.PartID == OrderItemOp.orderItem.PartID)
                                                                  .Where(o => o.operationID == OrderItemOp.operationID)
                                                                  .Where(o => o.qtyDone < o.qtyRequired).ToList();

                // I have not tested this yet with multiple unFinishedOps, more code will be needed
                foreach (Models.OrderItemOperation unFinishedOp in unFinishedOrderItemOps.ToList())
                {
                    if ((unFinishedOp.qtyRequired - unFinishedOp.qtyDone) >= (overOp.qtyDone)) // if overOp 'fits inside' unFinishedOp
                    {
                        // create a new OrderItemOperationPerformed
                        Models.OrderItemOperationPerformed newItemOpPerformed = new Models.OrderItemOperationPerformed();
                        newItemOpPerformed.qtyDone = overOp.qtyDone;
                        newItemOpPerformed.opPerformed = opsPerformed.LastOrDefault();
                        newItemOpPerformed.orderItemOperation = unFinishedOp;
                        //newItemOpPerformed.orderItemOperation.OrderItemOperationPerformeds.Add(unFinishedOp.OrderItemOperationPerformeds.FirstOrDefault());
                        unFinishedOp.OrderItemOperationPerformeds.Add(newItemOpPerformed);
                        Globals.dbContext.OrderItemOperationPerformeds.Add(newItemOpPerformed);

                        // remove the orderItemOpPerformed associated with this overOp
                        overOp.OrderItemOperationPerformeds.ToList().Remove(overOp.OrderItemOperationPerformeds.FirstOrDefault());

                        // modify unFinished op with new quantities
                        unFinishedOp.qtyDone += overOp.qtyDone;

                        // remove overOp 
                        Globals.dbContext.OrderItemOperations.Remove(overOp);


                    }
                    else        // overOp.QtyDone is greater than unFinishedOp.QtyRequired - QtyDone
                    {
                        // create a new OrderItemOperationPerformed
                        Models.OrderItemOperationPerformed newItemOpPerformed = new Models.OrderItemOperationPerformed();
                        newItemOpPerformed.qtyDone = overOp.qtyDone;
                        newItemOpPerformed.opPerformed = opsPerformed.LastOrDefault();
                        unFinishedOp.OrderItemOperationPerformeds.Add(newItemOpPerformed);
                        Globals.dbContext.OrderItemOperationPerformeds.Add(newItemOpPerformed);

                        // modify unFinished op with new quantities
                        unFinishedOp.qtyDone = unFinishedOp.qtyRequired;

                        // modify overOp with new quantities
                        overOp.qtyDone -= unFinishedOp.qtyRequired;
                    }
                }


            }
            Globals.dbContext.SaveChanges();
        }

        /// <summary>
        /// return all the orderItemOperationPerformeds linked to the orderItemOperation passed in
        /// </summary>
        /// <returns></returns>
        public List<Models.OrderItemOperationPerformed> GetAssociatedOrderItemOperationPerformeds(Models.OrderItemOperation orderItemOp)
        {
            return Globals.dbContext.OrderItemOperationPerformeds.Where(op => op.orderItemOperation.ID == orderItemOp.ID).ToList();
        }

        /// <summary>
        /// record an operation completed
        /// </summary>
        /// <param name="qty"></param>
        /// <returns></returns>
        public void RecordOperationCompleted(int qty)
        {
            Models.OrderItemOperation orderItemOp = Globals.dbContext.OrderItemOperations.Where(o => o.ID == OrderItemOp.ID).FirstOrDefault();

            int qtyDone = qty;
            Models.OrderItemOperationPerformed itemOpPerformed = new Models.OrderItemOperationPerformed();

            Models.OperationPerformed opPerformed = new Models.OperationPerformed();
            opPerformed.qtyDone = qtyDone;
            opPerformed.timePerformed = DateTime.Now;
            opPerformed.usr = currentUser;
            opPerformed.OrderItemOperationsPerformed = new List<Models.OrderItemOperationPerformed>();

            int totalQtyLeftToDo = qtyDone;
            // assign operations to order items
            foreach (Models.OrderItemOperation associatedOrderItemOp in associatedOrderItemOps)
            {
                if (associatedOrderItemOp.qtyDone < associatedOrderItemOp.qtyRequired)
                {
                    int itemQtyLeftToDo = associatedOrderItemOp.qtyRequired - associatedOrderItemOp.qtyDone;
                    Models.OrderItemOperationPerformed orderItemOpPerformed = new Models.OrderItemOperationPerformed();
                    if (totalQtyLeftToDo <= itemQtyLeftToDo)
                    {
                        orderItemOpPerformed = new Models.OrderItemOperationPerformed();
                        orderItemOpPerformed.qtyDone = totalQtyLeftToDo;
                        associatedOrderItemOp.qtyDone += totalQtyLeftToDo;
                        associatedOrderItemOp.OrderItemOperationPerformeds.Add(orderItemOpPerformed);
                        opPerformed.OrderItemOperationsPerformed.Add(orderItemOpPerformed);
                        totalQtyLeftToDo -= totalQtyLeftToDo;
                        break;
                    }
                    else
                    {
                        orderItemOpPerformed = new Models.OrderItemOperationPerformed();
                        orderItemOpPerformed.qtyDone = itemQtyLeftToDo;
                        associatedOrderItemOp.qtyDone = associatedOrderItemOp.qtyRequired;
                        associatedOrderItemOp.OrderItemOperationPerformeds.Add(orderItemOpPerformed);
                        opPerformed.OrderItemOperationsPerformed.Add(orderItemOpPerformed);
                        totalQtyLeftToDo -= itemQtyLeftToDo;
                    }
                }
            }

            // if we've filled all the order items, we need to do something with the rest...
            if (totalQtyLeftToDo > 0)
            {
                Models.Operation overBatchOperation = new Models.Operation();
                overBatchOperation.Location = orderItemOp.operation.Location;
                overBatchOperation.Name = orderItemOp.operation.Name;
                overBatchOperation.Part = orderItemOp.operation.Part;
                Globals.dbContext.Operations.Add(overBatchOperation);

                Models.OrderItemOperation overBatchItemOperation = new Models.OrderItemOperation();
                overBatchItemOperation.operation = overBatchOperation;
                overBatchItemOperation.orderItem = null;
                overBatchItemOperation.qtyDone = totalQtyLeftToDo;
                overBatchItemOperation.qtyRequired = 0;
                Globals.dbContext.OrderItemOperations.Add(overBatchItemOperation);

                Models.OrderItemOperationPerformed overBatchItemOperatonPerformed = new Models.OrderItemOperationPerformed();
                overBatchItemOperatonPerformed.orderItemOperation = overBatchItemOperation;
                overBatchItemOperatonPerformed.qtyDone = totalQtyLeftToDo;
                overBatchItemOperatonPerformed.opPerformed = opPerformed;
                Globals.dbContext.OrderItemOperationPerformeds.Add(overBatchItemOperatonPerformed);

                opPerformed.OrderItemOperationsPerformed.Add(overBatchItemOperatonPerformed);
                overBatchOperation.OrderItemOperations.Add(overBatchItemOperation);
            }

            Globals.dbContext.OperationPerformeds.Add(opPerformed);
            Globals.dbContext.SaveChanges();
        }

        /// <summary>
        /// Get all the order items associated with the orderItem in focus
        /// </summary>
        /// <returns></returns>
        public List<Models.OrderItemOperation> GetAssociatedOrderItems()
        {
            return associatedOrderItemOps;  
        }

        /// <summary>
        /// get all the oeprations performed associated with the orderItem in focus
        /// </summary>
        /// <returns></returns>
        public List<Models.OperationPerformed> GetOperationsPerformed()
        {
            return opsPerformed;
        }

        /// <summary>
        /// returns a stream of the PDF file attached to the orderItem part
        /// </summary>
        /// <returns></returns>
        public Stream GetPDFStream()
        {
            int partIndex = OrderItemOp.operation.Part.ID;
            Models.Part prt = Globals.dbContext.Parts.FirstOrDefault(p => p.ID == partIndex);
            if (prt.Files.Count > 0)
            {
                int fileIndex = prt.Files.FirstOrDefault().FileId;
                Models.File file = Globals.dbContext.Files.FirstOrDefault(f => f.FileId == fileIndex);
                Stream stream = new MemoryStream(file.Content);
                return stream;
            }
            return null;
        }

        


    }


}
