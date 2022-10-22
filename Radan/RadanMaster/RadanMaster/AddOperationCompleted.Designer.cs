namespace RadanMaster
{
    partial class AddOperationCompleted
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.textEditUser = new DevExpress.XtraEditors.TextEdit();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textEditQty = new DevExpress.XtraEditors.TextEdit();
            this.label4 = new System.Windows.Forms.Label();
            this.textEditNotes = new DevExpress.XtraEditors.MemoEdit();
            this.btnAllocate = new System.Windows.Forms.Button();
            this.buttonConfirm = new System.Windows.Forms.Button();
            this.radanMaster2DataSet = new RadanMaster.RadanMaster2DataSet();
            this.gridControlAssignableOps = new DevExpress.XtraGrid.GridControl();
            this.orderItemOperationsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.gridViewAssignableOps = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colID = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colqtyRequired = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colqtyDone = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colPartName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.coloperationID = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colOperationPerformeds = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colProductName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.textEditItemName = new DevExpress.XtraEditors.TextEdit();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.textEditOperation = new DevExpress.XtraEditors.TextEdit();
            this.labelControl2 = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)(this.textEditUser.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.textEditQty.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.textEditNotes.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radanMaster2DataSet)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridControlAssignableOps)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.orderItemOperationsBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewAssignableOps)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.textEditItemName.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.textEditOperation.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // textEditUser
            // 
            this.textEditUser.Location = new System.Drawing.Point(85, 77);
            this.textEditUser.Name = "textEditUser";
            this.textEditUser.Size = new System.Drawing.Size(144, 20);
            this.textEditUser.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(32, 80);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "User";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(29, 131);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Notes";
            // 
            // textEditQty
            // 
            this.textEditQty.Location = new System.Drawing.Point(337, 77);
            this.textEditQty.Name = "textEditQty";
            this.textEditQty.Size = new System.Drawing.Size(170, 20);
            this.textEditQty.TabIndex = 1;
            this.textEditQty.EditValueChanged += new System.EventHandler(this.textEditQty_EditValueChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(293, 84);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(25, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "Qty";
            // 
            // textEditNotes
            // 
            this.textEditNotes.Location = new System.Drawing.Point(70, 128);
            this.textEditNotes.Name = "textEditNotes";
            this.textEditNotes.Size = new System.Drawing.Size(448, 116);
            this.textEditNotes.TabIndex = 1;
            // 
            // btnAllocate
            // 
            this.btnAllocate.Enabled = false;
            this.btnAllocate.Location = new System.Drawing.Point(555, 69);
            this.btnAllocate.Name = "btnAllocate";
            this.btnAllocate.Size = new System.Drawing.Size(123, 75);
            this.btnAllocate.TabIndex = 6;
            this.btnAllocate.Text = "Allocate";
            this.btnAllocate.UseVisualStyleBackColor = true;
            this.btnAllocate.Click += new System.EventHandler(this.btnAllocate_Click);
            // 
            // buttonConfirm
            // 
            this.buttonConfirm.Enabled = false;
            this.buttonConfirm.Location = new System.Drawing.Point(699, 69);
            this.buttonConfirm.Name = "buttonConfirm";
            this.buttonConfirm.Size = new System.Drawing.Size(111, 75);
            this.buttonConfirm.TabIndex = 7;
            this.buttonConfirm.Text = "Confirm";
            this.buttonConfirm.UseVisualStyleBackColor = true;
            // 
            // radanMaster2DataSet
            // 
            this.radanMaster2DataSet.DataSetName = "RadanMaster2DataSet";
            this.radanMaster2DataSet.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema;
            // 
            // gridControlAssignableOps
            // 
            this.gridControlAssignableOps.DataSource = this.orderItemOperationsBindingSource;
            this.gridControlAssignableOps.Location = new System.Drawing.Point(70, 350);
            this.gridControlAssignableOps.MainView = this.gridViewAssignableOps;
            this.gridControlAssignableOps.Name = "gridControlAssignableOps";
            this.gridControlAssignableOps.Size = new System.Drawing.Size(751, 279);
            this.gridControlAssignableOps.TabIndex = 8;
            this.gridControlAssignableOps.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridViewAssignableOps});
            // 
            // orderItemOperationsBindingSource
            // 
            this.orderItemOperationsBindingSource.DataSource = typeof(RadanMaster.Models.OrderItemOperation);
            // 
            // gridViewAssignableOps
            // 
            this.gridViewAssignableOps.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colID,
            this.colqtyRequired,
            this.colqtyDone,
            this.colPartName,
            this.colProductName,
            this.coloperationID,
            this.colOperationPerformeds});
            this.gridViewAssignableOps.GridControl = this.gridControlAssignableOps;
            this.gridViewAssignableOps.Name = "gridViewAssignableOps";
            this.gridViewAssignableOps.CustomUnboundColumnData += new DevExpress.XtraGrid.Views.Base.CustomColumnDataEventHandler(this.gridViewAssignableOps_CustomUnboundColumnData);
            // 
            // colID
            // 
            this.colID.FieldName = "ID";
            this.colID.Name = "colID";
            // 
            // colqtyRequired
            // 
            this.colqtyRequired.FieldName = "qtyRequired";
            this.colqtyRequired.Name = "colqtyRequired";
            this.colqtyRequired.Visible = true;
            this.colqtyRequired.VisibleIndex = 0;
            this.colqtyRequired.Width = 100;
            // 
            // colqtyDone
            // 
            this.colqtyDone.FieldName = "qtyDone";
            this.colqtyDone.Name = "colqtyDone";
            this.colqtyDone.Visible = true;
            this.colqtyDone.VisibleIndex = 1;
            this.colqtyDone.Width = 106;
            // 
            // colPartName
            // 
            this.colPartName.FieldName = "operation.Part.FileName";
            this.colPartName.Name = "colPartName";
            this.colPartName.Width = 870;
            // 
            // coloperationID
            // 
            this.coloperationID.Caption = "OperationID";
            this.coloperationID.FieldName = "operationID";
            this.coloperationID.Name = "coloperationID";
            // 
            // colOperationPerformeds
            // 
            this.colOperationPerformeds.FieldName = "OperationPerformeds";
            this.colOperationPerformeds.Name = "colOperationPerformeds";
            // 
            // colProductName
            // 
            this.colProductName.Caption = "Product";
            this.colProductName.FieldName = "ProductandBatch";
            this.colProductName.Name = "colProductName";
            this.colProductName.UnboundType = DevExpress.Data.UnboundColumnType.String;
            this.colProductName.Visible = true;
            this.colProductName.VisibleIndex = 2;
            this.colProductName.Width = 431;
            // 
            // textEditItemName
            // 
            this.textEditItemName.Enabled = false;
            this.textEditItemName.Location = new System.Drawing.Point(113, 28);
            this.textEditItemName.Name = "textEditItemName";
            this.textEditItemName.Size = new System.Drawing.Size(192, 20);
            this.textEditItemName.TabIndex = 9;
            // 
            // labelControl1
            // 
            this.labelControl1.Location = new System.Drawing.Point(57, 31);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(37, 13);
            this.labelControl1.TabIndex = 10;
            this.labelControl1.Text = "Product";
            // 
            // textEditOperation
            // 
            this.textEditOperation.Enabled = false;
            this.textEditOperation.Location = new System.Drawing.Point(444, 28);
            this.textEditOperation.Name = "textEditOperation";
            this.textEditOperation.Size = new System.Drawing.Size(192, 20);
            this.textEditOperation.TabIndex = 9;
            // 
            // labelControl2
            // 
            this.labelControl2.Location = new System.Drawing.Point(388, 31);
            this.labelControl2.Name = "labelControl2";
            this.labelControl2.Size = new System.Drawing.Size(48, 13);
            this.labelControl2.TabIndex = 10;
            this.labelControl2.Text = "Operation";
            // 
            // AddOperationCompleted
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(891, 704);
            this.Controls.Add(this.labelControl2);
            this.Controls.Add(this.textEditOperation);
            this.Controls.Add(this.labelControl1);
            this.Controls.Add(this.textEditItemName);
            this.Controls.Add(this.gridControlAssignableOps);
            this.Controls.Add(this.buttonConfirm);
            this.Controls.Add(this.btnAllocate);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textEditQty);
            this.Controls.Add(this.textEditUser);
            this.Controls.Add(this.textEditNotes);
            this.Name = "AddOperationCompleted";
            this.Text = "AddOperationCompleted";
            ((System.ComponentModel.ISupportInitialize)(this.textEditUser.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.textEditQty.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.textEditNotes.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radanMaster2DataSet)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridControlAssignableOps)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.orderItemOperationsBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewAssignableOps)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.textEditItemName.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.textEditOperation.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DevExpress.XtraEditors.TextEdit textEditUser;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private DevExpress.XtraEditors.TextEdit textEditQty;
        private System.Windows.Forms.Label label4;
        private DevExpress.XtraEditors.MemoEdit textEditNotes;
        private System.Windows.Forms.Button btnAllocate;
        private System.Windows.Forms.Button buttonConfirm;
        private RadanMaster2DataSet radanMaster2DataSet;
        private DevExpress.XtraGrid.GridControl gridControlAssignableOps;
        private DevExpress.XtraGrid.Views.Grid.GridView gridViewAssignableOps;
        private System.Windows.Forms.BindingSource orderItemOperationsBindingSource;
        private DevExpress.XtraGrid.Columns.GridColumn colID;
        private DevExpress.XtraGrid.Columns.GridColumn colqtyRequired;
        private DevExpress.XtraGrid.Columns.GridColumn colqtyDone;
        private DevExpress.XtraGrid.Columns.GridColumn colPartName;
        private DevExpress.XtraGrid.Columns.GridColumn coloperationID;
        private DevExpress.XtraGrid.Columns.GridColumn colOperationPerformeds;
        private DevExpress.XtraGrid.Columns.GridColumn colProductName;
        private DevExpress.XtraEditors.TextEdit textEditItemName;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.TextEdit textEditOperation;
        private DevExpress.XtraEditors.LabelControl labelControl2;
    }
}