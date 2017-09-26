namespace VaultItemProcessor
{
    partial class PrintIDW
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
            this.treeListIdwList = new DevExpress.XtraTreeList.TreeList();
            this.vaultIDCol = new DevExpress.XtraTreeList.Columns.TreeListColumn();
            this.vaultNameCol = new DevExpress.XtraTreeList.Columns.TreeListColumn();
            this.label2 = new System.Windows.Forms.Label();
            this.btnPrint = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.treeListIdwList)).BeginInit();
            this.SuspendLayout();
            // 
            // treeListIdwList
            // 
            this.treeListIdwList.Appearance.FocusedCell.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            this.treeListIdwList.Appearance.FocusedCell.BackColor2 = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(255)))));
            this.treeListIdwList.Appearance.FocusedCell.Options.UseBackColor = true;
            this.treeListIdwList.Appearance.FocusedRow.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            this.treeListIdwList.Appearance.FocusedRow.BackColor2 = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            this.treeListIdwList.Appearance.FocusedRow.Options.UseBackColor = true;
            this.treeListIdwList.Appearance.SelectedRow.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            this.treeListIdwList.Appearance.SelectedRow.Options.UseBackColor = true;
            this.treeListIdwList.Columns.AddRange(new DevExpress.XtraTreeList.Columns.TreeListColumn[] {
            this.vaultIDCol,
            this.vaultNameCol});
            this.treeListIdwList.Location = new System.Drawing.Point(92, 95);
            this.treeListIdwList.Name = "treeListIdwList";
            this.treeListIdwList.OptionsBehavior.Editable = false;
            this.treeListIdwList.OptionsSelection.UseIndicatorForSelection = true;
            this.treeListIdwList.Size = new System.Drawing.Size(400, 200);
            this.treeListIdwList.TabIndex = 0;
            // 
            // vaultIDCol
            // 
            this.vaultIDCol.Caption = "VaultID";
            this.vaultIDCol.FieldName = "EntityIterationID";
            this.vaultIDCol.Name = "vaultIDCol";
            // 
            // vaultNameCol
            // 
            this.vaultNameCol.Caption = "Vault Name";
            this.vaultNameCol.FieldName = "Vault Name";
            this.vaultNameCol.Name = "vaultNameCol";
            this.vaultNameCol.Visible = true;
            this.vaultNameCol.VisibleIndex = 0;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(76, 57);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(247, 20);
            this.label2.TabIndex = 2;
            this.label2.Text = "Please Select the File to Print";
            // 
            // btnPrint
            // 
            this.btnPrint.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnPrint.Location = new System.Drawing.Point(372, 351);
            this.btnPrint.Name = "btnPrint";
            this.btnPrint.Size = new System.Drawing.Size(107, 23);
            this.btnPrint.TabIndex = 3;
            this.btnPrint.Text = "Print Selected";
            this.btnPrint.UseVisualStyleBackColor = true;
            this.btnPrint.Click += new System.EventHandler(this.btnPrint_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(280, 351);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // PrintIDW
            // 
            this.AcceptButton = this.btnPrint;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(603, 444);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnPrint);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.treeListIdwList);
            this.Name = "PrintIDW";
            this.Text = "PrintIDW";
            ((System.ComponentModel.ISupportInitialize)(this.treeListIdwList)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DevExpress.XtraTreeList.TreeList treeListIdwList;
        private DevExpress.XtraTreeList.Columns.TreeListColumn vaultIDCol;
        private DevExpress.XtraTreeList.Columns.TreeListColumn vaultNameCol;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnPrint;
        private System.Windows.Forms.Button btnCancel;
    }
}