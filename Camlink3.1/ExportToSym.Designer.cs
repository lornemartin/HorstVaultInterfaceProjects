namespace Camlink3_1
{
    partial class ExportToSym
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
            this.picBoxIpt = new System.Windows.Forms.PictureBox();
            this.picBoxSym = new System.Windows.Forms.PictureBox();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.progressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnConvertAll = new System.Windows.Forms.Button();
            this.txtBoxProject = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.btnProjectBrowse = new System.Windows.Forms.Button();
            this.btnAddToProject = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.txtBoxSymFolder = new System.Windows.Forms.TextBox();
            this.lblSymFolderName = new System.Windows.Forms.Label();
            this.btnSymFolderBrowse = new System.Windows.Forms.Button();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.txtBoxSymFolder2 = new System.Windows.Forms.TextBox();
            this.btnBrowseForSym2 = new System.Windows.Forms.Button();
            this.objectListView1 = new BrightIdeasSoftware.ObjectListView();
            this.olvClmQuantity = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.olvClmName = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.olvClmDesc = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.olvClmThickness = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.olvClmType = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.checkBoxSecondarySymFolder = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.picBoxIpt)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picBoxSym)).BeginInit();
            this.statusStrip.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.objectListView1)).BeginInit();
            this.SuspendLayout();
            // 
            // picBoxIpt
            // 
            this.picBoxIpt.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picBoxIpt.Location = new System.Drawing.Point(761, 12);
            this.picBoxIpt.Name = "picBoxIpt";
            this.picBoxIpt.Size = new System.Drawing.Size(115, 115);
            this.picBoxIpt.TabIndex = 1;
            this.picBoxIpt.TabStop = false;
            // 
            // picBoxSym
            // 
            this.picBoxSym.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picBoxSym.Location = new System.Drawing.Point(761, 153);
            this.picBoxSym.Name = "picBoxSym";
            this.picBoxSym.Size = new System.Drawing.Size(115, 115);
            this.picBoxSym.TabIndex = 1;
            this.picBoxSym.TabStop = false;
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.progressBar,
            this.toolStripStatusLabel});
            this.statusStrip.Location = new System.Drawing.Point(0, 479);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(896, 22);
            this.statusStrip.TabIndex = 4;
            this.statusStrip.Text = "statusStrip1";
            // 
            // progressBar
            // 
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(100, 16);
            // 
            // toolStripStatusLabel
            // 
            this.toolStripStatusLabel.Name = "toolStripStatusLabel";
            this.toolStripStatusLabel.Size = new System.Drawing.Size(0, 17);
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnOK.Location = new System.Drawing.Point(761, 399);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(106, 61);
            this.btnOK.TabIndex = 5;
            this.btnOK.Text = "Ok";
            this.btnOK.UseVisualStyleBackColor = true;
            // 
            // btnConvertAll
            // 
            this.btnConvertAll.Location = new System.Drawing.Point(761, 308);
            this.btnConvertAll.Name = "btnConvertAll";
            this.btnConvertAll.Size = new System.Drawing.Size(106, 57);
            this.btnConvertAll.TabIndex = 6;
            this.btnConvertAll.Text = "Convert All";
            this.btnConvertAll.UseVisualStyleBackColor = true;
            this.btnConvertAll.Click += new System.EventHandler(this.btnConvertAll_Click);
            // 
            // txtBoxProject
            // 
            this.txtBoxProject.Location = new System.Drawing.Point(97, 28);
            this.txtBoxProject.Name = "txtBoxProject";
            this.txtBoxProject.Size = new System.Drawing.Size(272, 20);
            this.txtBoxProject.TabIndex = 7;
            this.txtBoxProject.TextChanged += new System.EventHandler(this.txtBoxProject_TextChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(31, 35);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(40, 13);
            this.label2.TabIndex = 8;
            this.label2.Text = "Project";
            // 
            // btnProjectBrowse
            // 
            this.btnProjectBrowse.Location = new System.Drawing.Point(110, 81);
            this.btnProjectBrowse.Name = "btnProjectBrowse";
            this.btnProjectBrowse.Size = new System.Drawing.Size(115, 42);
            this.btnProjectBrowse.TabIndex = 9;
            this.btnProjectBrowse.Text = "Browse for Project...";
            this.btnProjectBrowse.UseVisualStyleBackColor = true;
            this.btnProjectBrowse.Click += new System.EventHandler(this.btnProjectBrowse_Click);
            // 
            // btnAddToProject
            // 
            this.btnAddToProject.Location = new System.Drawing.Point(254, 81);
            this.btnAddToProject.Name = "btnAddToProject";
            this.btnAddToProject.Size = new System.Drawing.Size(115, 42);
            this.btnAddToProject.TabIndex = 9;
            this.btnAddToProject.Text = "Add To Project";
            this.btnAddToProject.UseVisualStyleBackColor = true;
            this.btnAddToProject.Click += new System.EventHandler(this.btnAddToProject_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnAddToProject);
            this.groupBox1.Controls.Add(this.btnProjectBrowse);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.txtBoxProject);
            this.groupBox1.Location = new System.Drawing.Point(26, 295);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(441, 166);
            this.groupBox1.TabIndex = 12;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Projects";
            // 
            // txtBoxSymFolder
            // 
            this.txtBoxSymFolder.Location = new System.Drawing.Point(498, 314);
            this.txtBoxSymFolder.Name = "txtBoxSymFolder";
            this.txtBoxSymFolder.Size = new System.Drawing.Size(241, 20);
            this.txtBoxSymFolder.TabIndex = 13;
            // 
            // lblSymFolderName
            // 
            this.lblSymFolderName.AutoSize = true;
            this.lblSymFolderName.Location = new System.Drawing.Point(498, 295);
            this.lblSymFolderName.Name = "lblSymFolderName";
            this.lblSymFolderName.Size = new System.Drawing.Size(127, 13);
            this.lblSymFolderName.TabIndex = 14;
            this.lblSymFolderName.Text = "Primary Sym Folder Name";
            // 
            // btnSymFolderBrowse
            // 
            this.btnSymFolderBrowse.Location = new System.Drawing.Point(646, 341);
            this.btnSymFolderBrowse.Name = "btnSymFolderBrowse";
            this.btnSymFolderBrowse.Size = new System.Drawing.Size(93, 23);
            this.btnSymFolderBrowse.TabIndex = 15;
            this.btnSymFolderBrowse.Text = "Browse...";
            this.btnSymFolderBrowse.UseVisualStyleBackColor = true;
            this.btnSymFolderBrowse.Click += new System.EventHandler(this.btnSymFolderBrowse_Click);
            // 
            // txtBoxSymFolder2
            // 
            this.txtBoxSymFolder2.Location = new System.Drawing.Point(498, 410);
            this.txtBoxSymFolder2.Name = "txtBoxSymFolder2";
            this.txtBoxSymFolder2.Size = new System.Drawing.Size(241, 20);
            this.txtBoxSymFolder2.TabIndex = 13;
            // 
            // btnBrowseForSym2
            // 
            this.btnBrowseForSym2.Location = new System.Drawing.Point(646, 437);
            this.btnBrowseForSym2.Name = "btnBrowseForSym2";
            this.btnBrowseForSym2.Size = new System.Drawing.Size(93, 23);
            this.btnBrowseForSym2.TabIndex = 15;
            this.btnBrowseForSym2.Text = "Browse...";
            this.btnBrowseForSym2.UseVisualStyleBackColor = true;
            this.btnBrowseForSym2.Click += new System.EventHandler(this.btnBrowseForSym2_Click);
            // 
            // objectListView1
            // 
            this.objectListView1.AllColumns.Add(this.olvClmQuantity);
            this.objectListView1.AllColumns.Add(this.olvClmName);
            this.objectListView1.AllColumns.Add(this.olvClmDesc);
            this.objectListView1.AllColumns.Add(this.olvClmThickness);
            this.objectListView1.AllColumns.Add(this.olvClmType);
            this.objectListView1.CellEditActivation = BrightIdeasSoftware.ObjectListView.CellEditActivateMode.DoubleClick;
            this.objectListView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.olvClmQuantity,
            this.olvClmName,
            this.olvClmDesc,
            this.olvClmThickness,
            this.olvClmType});
            this.objectListView1.FullRowSelect = true;
            this.objectListView1.Location = new System.Drawing.Point(26, 12);
            this.objectListView1.MultiSelect = false;
            this.objectListView1.Name = "objectListView1";
            this.objectListView1.Size = new System.Drawing.Size(713, 256);
            this.objectListView1.TabIndex = 11;
            this.objectListView1.UseCompatibleStateImageBehavior = false;
            this.objectListView1.View = System.Windows.Forms.View.Details;
            this.objectListView1.FormatRow += new System.EventHandler<BrightIdeasSoftware.FormatRowEventArgs>(this.objectListView1_FormatRow);
            this.objectListView1.SelectionChanged += new System.EventHandler(this.objectListView1_SelectionChanged);
            this.objectListView1.DoubleClick += new System.EventHandler(this.objectListView1_DoubleClick);
            // 
            // olvClmQuantity
            // 
            this.olvClmQuantity.AspectName = "qty";
            this.olvClmQuantity.Sortable = false;
            this.olvClmQuantity.Text = "Qty";
            this.olvClmQuantity.Width = 43;
            // 
            // olvClmName
            // 
            this.olvClmName.AspectName = "name";
            this.olvClmName.IsEditable = false;
            this.olvClmName.Sortable = false;
            this.olvClmName.Text = "Name";
            this.olvClmName.Width = 144;
            // 
            // olvClmDesc
            // 
            this.olvClmDesc.AspectName = "desc";
            this.olvClmDesc.IsEditable = false;
            this.olvClmDesc.Sortable = false;
            this.olvClmDesc.Text = "Description";
            this.olvClmDesc.Width = 244;
            // 
            // olvClmThickness
            // 
            this.olvClmThickness.AspectName = "thickness";
            this.olvClmThickness.Sortable = false;
            this.olvClmThickness.Text = "Thickness";
            this.olvClmThickness.Width = 85;
            // 
            // olvClmType
            // 
            this.olvClmType.AspectName = "materialType";
            this.olvClmType.Sortable = false;
            this.olvClmType.Text = "Material Type";
            this.olvClmType.Width = 194;
            // 
            // checkBoxSecondarySymFolder
            // 
            this.checkBoxSecondarySymFolder.AutoSize = true;
            this.checkBoxSecondarySymFolder.Location = new System.Drawing.Point(501, 387);
            this.checkBoxSecondarySymFolder.Name = "checkBoxSecondarySymFolder";
            this.checkBoxSecondarySymFolder.Size = new System.Drawing.Size(163, 17);
            this.checkBoxSecondarySymFolder.TabIndex = 16;
            this.checkBoxSecondarySymFolder.Text = "Secondary Sym Folder Name";
            this.checkBoxSecondarySymFolder.UseVisualStyleBackColor = true;
            this.checkBoxSecondarySymFolder.CheckedChanged += new System.EventHandler(this.checkBoxSecondarySymFolder_CheckedChanged);
            // 
            // ExportToSym
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnOK;
            this.ClientSize = new System.Drawing.Size(896, 501);
            this.Controls.Add(this.checkBoxSecondarySymFolder);
            this.Controls.Add(this.btnBrowseForSym2);
            this.Controls.Add(this.btnSymFolderBrowse);
            this.Controls.Add(this.txtBoxSymFolder2);
            this.Controls.Add(this.lblSymFolderName);
            this.Controls.Add(this.txtBoxSymFolder);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.objectListView1);
            this.Controls.Add(this.btnConvertAll);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.picBoxSym);
            this.Controls.Add(this.picBoxIpt);
            this.Name = "ExportToSym";
            this.Text = "ExportToSym";
            ((System.ComponentModel.ISupportInitialize)(this.picBoxIpt)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picBoxSym)).EndInit();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.objectListView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox picBoxIpt;
        private System.Windows.Forms.PictureBox picBoxSym;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripProgressBar progressBar;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnConvertAll;
        private System.Windows.Forms.TextBox txtBoxProject;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnProjectBrowse;
        private System.Windows.Forms.Button btnAddToProject;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private BrightIdeasSoftware.ObjectListView objectListView1;
        private BrightIdeasSoftware.OLVColumn olvClmName;
        private BrightIdeasSoftware.OLVColumn olvClmDesc;
        private BrightIdeasSoftware.OLVColumn olvClmQuantity;
        private BrightIdeasSoftware.OLVColumn olvClmThickness;
        private BrightIdeasSoftware.OLVColumn olvClmType;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox txtBoxSymFolder;
        private System.Windows.Forms.Label lblSymFolderName;
        private System.Windows.Forms.Button btnSymFolderBrowse;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.TextBox txtBoxSymFolder2;
        private System.Windows.Forms.Button btnBrowseForSym2;
        private System.Windows.Forms.CheckBox checkBoxSecondarySymFolder;
    }
}