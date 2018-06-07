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
            this.gridControl1 = new DevExpress.XtraGrid.GridControl();
            this.gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colDescription = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colCategoryName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colMaterial = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colNotes = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colOperations = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colPlantID = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colRequiresPDF = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colStructuralCode = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colThickness = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colPartNumber = new DevExpress.XtraGrid.Columns.GridColumn();
            this.c__MigrationHistoryBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.productsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.gridControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.c__MigrationHistoryBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.productsBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // gridControl1
            // 
            this.gridControl1.DataSource = this.productsBindingSource;
            this.gridControl1.Location = new System.Drawing.Point(49, 34);
            this.gridControl1.MainView = this.gridView1;
            this.gridControl1.Name = "gridControl1";
            this.gridControl1.Size = new System.Drawing.Size(1188, 424);
            this.gridControl1.TabIndex = 0;
            this.gridControl1.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridView1});
            // 
            // gridView1
            // 
            this.gridView1.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colPartNumber,
            this.colDescription,
            this.colCategoryName,
            this.colMaterial,
            this.colNotes,
            this.colOperations,
            this.colPlantID,
            this.colRequiresPDF,
            this.colStructuralCode,
            this.colThickness});
            this.gridView1.GridControl = this.gridControl1;
            this.gridView1.Name = "gridView1";
            // 
            // colDescription
            // 
            this.colDescription.FieldName = "Description";
            this.colDescription.Name = "colDescription";
            this.colDescription.Visible = true;
            this.colDescription.VisibleIndex = 0;
            // 
            // colCategoryName
            // 
            this.colCategoryName.FieldName = "CategoryName";
            this.colCategoryName.Name = "colCategoryName";
            this.colCategoryName.Visible = true;
            this.colCategoryName.VisibleIndex = 1;
            // 
            // colMaterial
            // 
            this.colMaterial.FieldName = "Material";
            this.colMaterial.Name = "colMaterial";
            this.colMaterial.Visible = true;
            this.colMaterial.VisibleIndex = 2;
            // 
            // colNotes
            // 
            this.colNotes.FieldName = "Notes";
            this.colNotes.Name = "colNotes";
            this.colNotes.Visible = true;
            this.colNotes.VisibleIndex = 3;
            // 
            // colOperations
            // 
            this.colOperations.FieldName = "Operations";
            this.colOperations.Name = "colOperations";
            this.colOperations.Visible = true;
            this.colOperations.VisibleIndex = 4;
            // 
            // colPlantID
            // 
            this.colPlantID.FieldName = "PlantID";
            this.colPlantID.Name = "colPlantID";
            this.colPlantID.Visible = true;
            this.colPlantID.VisibleIndex = 5;
            // 
            // colRequiresPDF
            // 
            this.colRequiresPDF.FieldName = "RequiresPDF";
            this.colRequiresPDF.Name = "colRequiresPDF";
            this.colRequiresPDF.Visible = true;
            this.colRequiresPDF.VisibleIndex = 6;
            // 
            // colStructuralCode
            // 
            this.colStructuralCode.FieldName = "StructuralCode";
            this.colStructuralCode.Name = "colStructuralCode";
            this.colStructuralCode.Visible = true;
            this.colStructuralCode.VisibleIndex = 7;
            // 
            // colThickness
            // 
            this.colThickness.FieldName = "Thickness";
            this.colThickness.Name = "colThickness";
            this.colThickness.Visible = true;
            this.colThickness.VisibleIndex = 8;
            // 
            // colPartNumber
            // 
            this.colPartNumber.FieldName = "PartNumber";
            this.colPartNumber.Name = "colPartNumber";
            this.colPartNumber.Visible = true;
            this.colPartNumber.VisibleIndex = 9;
            // 
            // c__MigrationHistoryBindingSource
            // 
            this.c__MigrationHistoryBindingSource.DataSource = typeof(ItemExport.C__MigrationHistory);
            // 
            // productsBindingSource
            // 
            this.productsBindingSource.DataSource = typeof(ItemExport.Product);
            // 
            // HorstMFGExport
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1284, 562);
            this.Controls.Add(this.gridControl1);
            this.Name = "HorstMFGExport";
            this.Text = "HorstMFGExport";
            ((System.ComponentModel.ISupportInitialize)(this.gridControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.c__MigrationHistoryBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.productsBindingSource)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraGrid.GridControl gridControl1;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private DevExpress.XtraGrid.Columns.GridColumn colDescription;
        private DevExpress.XtraGrid.Columns.GridColumn colCategoryName;
        private DevExpress.XtraGrid.Columns.GridColumn colMaterial;
        private DevExpress.XtraGrid.Columns.GridColumn colNotes;
        private DevExpress.XtraGrid.Columns.GridColumn colOperations;
        private DevExpress.XtraGrid.Columns.GridColumn colPlantID;
        private DevExpress.XtraGrid.Columns.GridColumn colRequiresPDF;
        private DevExpress.XtraGrid.Columns.GridColumn colStructuralCode;
        private DevExpress.XtraGrid.Columns.GridColumn colThickness;
        private DevExpress.XtraGrid.Columns.GridColumn colPartNumber;
        private System.Windows.Forms.BindingSource c__MigrationHistoryBindingSource;
        private System.Windows.Forms.BindingSource productsBindingSource;
    }
}