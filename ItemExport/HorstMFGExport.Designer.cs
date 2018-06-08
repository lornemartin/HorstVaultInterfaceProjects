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
            ((System.ComponentModel.ISupportInitialize)(this.productsBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // productsBindingSource
            // 
            this.productsBindingSource.DataSource = typeof(ItemExport.Product);
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
            // HorstMFGExport
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1284, 562);
            this.Controls.Add(this.gridControl1);
            this.Name = "HorstMFGExport";
            this.Text = "HorstMFGExport";
            ((System.ComponentModel.ISupportInitialize)(this.productsBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).EndInit();
            this.ResumeLayout(false);

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
    }
}