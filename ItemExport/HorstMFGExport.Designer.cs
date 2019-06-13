namespace ItemExport
{
    partial class HorstMFGExport
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
            this.productsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.gridControl1 = new DevExpress.XtraGrid.GridControl();
            this.gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colPartNumber = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colDescription = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colMaterial_ID = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colCategoryName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colThickness = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colStructuralCode = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colIsStock = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colPlantID = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colRequiresPDF = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colOperations = new DevExpress.XtraGrid.Columns.GridColumn();
            this.btnExport = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.txtBoxOrderNumber = new System.Windows.Forms.TextBox();
            this.txtBoxQty = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.productsBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // gridControl1
            // 
            this.gridControl1.DataSource = this.productsBindingSource;
            this.gridControl1.Location = new System.Drawing.Point(31, 12);
            this.gridControl1.MainView = this.gridView1;
            this.gridControl1.Name = "gridControl1";
            this.gridControl1.Size = new System.Drawing.Size(1208, 510);
            this.gridControl1.TabIndex = 0;
            this.gridControl1.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridView1});
            // 
            // gridView1
            // 
            this.gridView1.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colPartNumber,
            this.colDescription,
            this.colMaterial_ID,
            this.colCategoryName,
            this.colThickness,
            this.colStructuralCode,
            this.colIsStock,
            this.colPlantID,
            this.colRequiresPDF,
            this.colOperations});
            this.gridView1.GridControl = this.gridControl1;
            this.gridView1.Name = "gridView1";
            this.gridView1.SortInfo.AddRange(new DevExpress.XtraGrid.Columns.GridColumnSortInfo[] {
            new DevExpress.XtraGrid.Columns.GridColumnSortInfo(this.colMaterial_ID, DevExpress.Data.ColumnSortOrder.Ascending)});
            // 
            // colPartNumber
            // 
            this.colPartNumber.FieldName = "PartNumber";
            this.colPartNumber.Name = "colPartNumber";
            this.colPartNumber.Visible = true;
            this.colPartNumber.VisibleIndex = 0;
            this.colPartNumber.Width = 245;
            // 
            // colDescription
            // 
            this.colDescription.FieldName = "Description";
            this.colDescription.Name = "colDescription";
            this.colDescription.Visible = true;
            this.colDescription.VisibleIndex = 1;
            this.colDescription.Width = 240;
            // 
            // colMaterial_ID
            // 
            this.colMaterial_ID.FieldName = "Material.Name";
            this.colMaterial_ID.Name = "colMaterial_ID";
            this.colMaterial_ID.Visible = true;
            this.colMaterial_ID.VisibleIndex = 5;
            this.colMaterial_ID.Width = 137;
            // 
            // colCategoryName
            // 
            this.colCategoryName.FieldName = "CategoryName";
            this.colCategoryName.Name = "colCategoryName";
            this.colCategoryName.Visible = true;
            this.colCategoryName.VisibleIndex = 2;
            this.colCategoryName.Width = 144;
            // 
            // colThickness
            // 
            this.colThickness.FieldName = "Thickness";
            this.colThickness.Name = "colThickness";
            this.colThickness.Visible = true;
            this.colThickness.VisibleIndex = 6;
            this.colThickness.Width = 167;
            // 
            // colStructuralCode
            // 
            this.colStructuralCode.FieldName = "StructuralCode";
            this.colStructuralCode.Name = "colStructuralCode";
            this.colStructuralCode.Visible = true;
            this.colStructuralCode.VisibleIndex = 7;
            this.colStructuralCode.Width = 167;
            // 
            // colIsStock
            // 
            this.colIsStock.FieldName = "IsStock";
            this.colIsStock.Name = "colIsStock";
            this.colIsStock.Visible = true;
            this.colIsStock.VisibleIndex = 3;
            this.colIsStock.Width = 44;
            // 
            // colPlantID
            // 
            this.colPlantID.FieldName = "PlantID";
            this.colPlantID.Name = "colPlantID";
            this.colPlantID.Visible = true;
            this.colPlantID.VisibleIndex = 9;
            this.colPlantID.Width = 167;
            // 
            // colRequiresPDF
            // 
            this.colRequiresPDF.FieldName = "RequiresPDF";
            this.colRequiresPDF.Name = "colRequiresPDF";
            this.colRequiresPDF.Visible = true;
            this.colRequiresPDF.VisibleIndex = 4;
            this.colRequiresPDF.Width = 49;
            // 
            // colOperations
            // 
            this.colOperations.FieldName = "Operations[0].Name";
            this.colOperations.Name = "colOperations";
            this.colOperations.Visible = true;
            this.colOperations.VisibleIndex = 8;
            this.colOperations.Width = 189;
            // 
            // btnExport
            // 
            this.btnExport.Location = new System.Drawing.Point(1164, 574);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(75, 23);
            this.btnExport.TabIndex = 1;
            this.btnExport.Text = "Export";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(1067, 574);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // txtBoxOrderNumber
            // 
            this.txtBoxOrderNumber.Location = new System.Drawing.Point(404, 574);
            this.txtBoxOrderNumber.Name = "txtBoxOrderNumber";
            this.txtBoxOrderNumber.Size = new System.Drawing.Size(183, 20);
            this.txtBoxOrderNumber.TabIndex = 2;
            this.txtBoxOrderNumber.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // txtBoxQty
            // 
            this.txtBoxQty.Location = new System.Drawing.Point(663, 574);
            this.txtBoxQty.Name = "txtBoxQty";
            this.txtBoxQty.Size = new System.Drawing.Size(100, 20);
            this.txtBoxQty.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(325, 577);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(73, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Order Number";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(611, 577);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(46, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Quantity";
            // 
            // HorstMFGExport
            // 
            this.AcceptButton = this.btnExport;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(1284, 649);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtBoxQty);
            this.Controls.Add(this.txtBoxOrderNumber);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.gridControl1);
            this.Name = "HorstMFGExport";
            this.Text = "HorstMFGExport";
            this.Load += new System.EventHandler(this.HorstMFGExport_Load);
            ((System.ComponentModel.ISupportInitialize)(this.productsBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.BindingSource productsBindingSource;
        private DevExpress.XtraGrid.GridControl gridControl1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private DevExpress.XtraGrid.Columns.GridColumn colPartNumber;
        private DevExpress.XtraGrid.Columns.GridColumn colDescription;
        private DevExpress.XtraGrid.Columns.GridColumn colIsStock;
        private DevExpress.XtraGrid.Columns.GridColumn colMaterial_ID;
        private DevExpress.XtraGrid.Columns.GridColumn colCategoryName;
        private DevExpress.XtraGrid.Columns.GridColumn colThickness;
        private DevExpress.XtraGrid.Columns.GridColumn colStructuralCode;
        private DevExpress.XtraGrid.Columns.GridColumn colPlantID;
        private DevExpress.XtraGrid.Columns.GridColumn colRequiresPDF;
        private DevExpress.XtraGrid.Columns.GridColumn colOperations;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.TextBox txtBoxOrderNumber;
        private System.Windows.Forms.TextBox txtBoxQty;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
    }
}