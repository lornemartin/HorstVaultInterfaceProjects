using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using System.Data.Entity;
using DevExpress.XtraGrid.Views.Grid;

namespace RadanMaster
{

    public partial class AddOperationCompleted : DevExpress.XtraEditors.XtraForm
    {
        Models.User currentUser { get; set; }
        RadanMaster.DAL.RadanMasterContext dbContext { get; set; }
        Models.OrderItemOperation orderItemOperation { get; set; }
        Models.Part part { get; set; }

        public AddOperationCompleted(Models.User curUser, Models.OrderItemOperation op, Models.Part prt, RadanMaster.DAL.RadanMasterContext ctx)
        {
            InitializeComponent();
            currentUser = curUser;
            part = prt;
            //dbContext = new RadanMaster.DAL.RadanMasterContext();
            dbContext = ctx;

            dbContext.Parts.Load();
            dbContext.Operations.Load();
            dbContext.OrderItemOperations.Load();
            dbContext.OperationPerformeds.Load();
            dbContext.OrderItemOperationPerformeds.Load();

            textEditUser.Text = currentUser.UserName;
            textEditItemName.Text = prt.FileName;
            textEditOperation.Text = op.operation.Name;

            List<Models.OrderItemOperation> assignableOps = new List<Models.OrderItemOperation>();
            assignableOps = dbContext.OrderItemOperations.Where(o => o.operationID == op.operationID).ToList();

            //assignableOps = dbContext.OrderItemOperations.ToList();
            gridControlAssignableOps.DataSource = null;
            gridControlAssignableOps.DataSource = assignableOps;
        }

        private void gridViewAssignableOps_CustomUnboundColumnData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
        {
            GridView view = sender as GridView;
            if (view == null) return;
            int rowIndex = e.ListSourceRowIndex;
            int opID = Convert.ToInt32(view.GetListSourceRowCellValue(rowIndex, "ID"));

            Models.OrderItemOperation op = dbContext.OrderItemOperations.Where(o => o.ID == opID).FirstOrDefault();

            List<Models.OrderItem> openOrderItems = dbContext.OrderItems.Where(o => o.IsComplete == false).ToList();
            Models.OrderItem orderItem = new Models.OrderItem();

            foreach(Models.OrderItem oi in openOrderItems)
            {
                if (oi.orderItemOps.Contains(op))
                    orderItem = oi;
            }

            Models.Order order = dbContext.Orders.Where(o => o.ID == orderItem.OrderID).FirstOrDefault();

            if(order!=null)
                e.Value = order.BatchName + " --- " + orderItem.ProductName;
        }

        private void textEditQty_EditValueChanged(object sender, EventArgs e)
        {
            int qty = 0;
            bool readyToAllocate = false;

            if (textEditQty.Text != "")
            {
                int.TryParse(textEditQty.Text, out qty);
                if(qty > 0)
                {
                    readyToAllocate = true;
                }

            }
            btnAllocate.Enabled = readyToAllocate;
        }

        private void btnAllocate_Click(object sender, EventArgs e)
        {
            List<Models.OrderItem> openOrderItems = dbContext.OrderItems.Where(o => o.IsComplete == false)
                                                                        .Where(o => o.Part.ID == part.ID).ToList();

            int.TryParse(textEditQty.Text, out int qtyToAllocate);
            int qtyAllocatedRunningTotal = 0;

            Models.OperationPerformed opPerformed = new Models.OperationPerformed();
            opPerformed.qtyDone = qtyToAllocate;
            opPerformed.timePerformed = DateTime.Now;
            opPerformed.usr = currentUser;
            opPerformed.Notes = textEditNotes.Text;
            opPerformed.OrderItemOperationPerformeds = new List<Models.OrderItemOperationPerformed>();

            foreach (Models.OrderItem orderItem in openOrderItems.ToList())
            {
                if (orderItem.QtyRequired <= qtyToAllocate - qtyAllocatedRunningTotal)
                {
                    orderItem.QtyNested = orderItem.QtyRequired;
                    qtyAllocatedRunningTotal += orderItem.QtyRequired;

                    Models.OrderItemOperationPerformed orderItemOpPerformed = new Models.OrderItemOperationPerformed();
                    orderItemOpPerformed.qtyDone = orderItem.QtyRequired;
                    orderItemOpPerformed.orderItemOperation = orderItemOperation;
                    orderItemOpPerformed.operationPerformed = opPerformed;
                    dbContext.OrderItemOperationPerformeds.Add(orderItemOpPerformed);

                    opPerformed.OrderItemOperationPerformeds.Add(orderItemOpPerformed);
                }
            }

            dbContext.OperationPerformeds.Add(opPerformed);

            dbContext.SaveChanges();
        }
    }
}