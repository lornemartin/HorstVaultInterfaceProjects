namespace RadanMaster.AddItemDialog
{
    partial class AddItem
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
            this.comboBoxOrderNum = new System.Windows.Forms.ComboBox();
            this.txtBoxQty = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.comboBoxSchedName = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.comboBoxBatchName = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.chkBoxCheckForBends = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.txtBoxNotes = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.itemNameTxtBox = new System.Windows.Forms.TextBox();
            this.pictureEditThumbnail = new DevExpress.XtraEditors.PictureEdit();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureEditThumbnail.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // comboBoxOrderNum
            // 
            this.comboBoxOrderNum.FormattingEnabled = true;
            this.comboBoxOrderNum.Location = new System.Drawing.Point(170, 152);
            this.comboBoxOrderNum.Name = "comboBoxOrderNum";
            this.comboBoxOrderNum.Size = new System.Drawing.Size(204, 21);
            this.comboBoxOrderNum.TabIndex = 1;
            // 
            // txtBoxQty
            // 
            this.txtBoxQty.AcceptsTab = true;
            this.txtBoxQty.Location = new System.Drawing.Point(170, 110);
            this.txtBoxQty.Name = "txtBoxQty";
            this.txtBoxQty.Size = new System.Drawing.Size(204, 20);
            this.txtBoxQty.TabIndex = 0;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(53, 152);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Order Number";
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(216, 390);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(75, 23);
            this.btnAdd.TabIndex = 6;
            this.btnAdd.Text = "Add";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(100, 390);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 7;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // comboBoxSchedName
            // 
            this.comboBoxSchedName.FormattingEnabled = true;
            this.comboBoxSchedName.Location = new System.Drawing.Point(170, 191);
            this.comboBoxSchedName.Name = "comboBoxSchedName";
            this.comboBoxSchedName.Size = new System.Drawing.Size(204, 21);
            this.comboBoxSchedName.TabIndex = 2;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(53, 191);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(69, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Sched Name";
            // 
            // comboBoxBatchName
            // 
            this.comboBoxBatchName.FormattingEnabled = true;
            this.comboBoxBatchName.Location = new System.Drawing.Point(170, 233);
            this.comboBoxBatchName.Name = "comboBoxBatchName";
            this.comboBoxBatchName.Size = new System.Drawing.Size(204, 21);
            this.comboBoxBatchName.TabIndex = 3;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(53, 233);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(66, 13);
            this.label4.TabIndex = 2;
            this.label4.Text = "Batch Name";
            // 
            // chkBoxCheckForBends
            // 
            this.chkBoxCheckForBends.AutoSize = true;
            this.chkBoxCheckForBends.Location = new System.Drawing.Point(56, 324);
            this.chkBoxCheckForBends.Name = "chkBoxCheckForBends";
            this.chkBoxCheckForBends.Size = new System.Drawing.Size(108, 17);
            this.chkBoxCheckForBends.TabIndex = 5;
            this.chkBoxCheckForBends.Text = "Check For Bends";
            this.chkBoxCheckForBends.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(53, 275);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(35, 13);
            this.label5.TabIndex = 2;
            this.label5.Text = "Notes";
            // 
            // txtBoxNotes
            // 
            this.txtBoxNotes.Location = new System.Drawing.Point(170, 275);
            this.txtBoxNotes.Name = "txtBoxNotes";
            this.txtBoxNotes.Size = new System.Drawing.Size(204, 20);
            this.txtBoxNotes.TabIndex = 4;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(216, 390);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 6;
            this.button1.Text = "Add";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // button2
            // 
            this.button2.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button2.Location = new System.Drawing.Point(100, 390);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 7;
            this.button2.Text = "Cancel";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(56, 324);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(108, 17);
            this.checkBox1.TabIndex = 5;
            this.checkBox1.Text = "Check For Bends";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // itemNameTxtBox
            // 
            this.itemNameTxtBox.AcceptsTab = true;
            this.itemNameTxtBox.Location = new System.Drawing.Point(56, 44);
            this.itemNameTxtBox.Name = "itemNameTxtBox";
            this.itemNameTxtBox.Size = new System.Drawing.Size(318, 20);
            this.itemNameTxtBox.TabIndex = 0;
            // 
            // pictureEditThumbnail
            // 
            this.pictureEditThumbnail.Location = new System.Drawing.Point(435, 44);
            this.pictureEditThumbnail.Name = "pictureEditThumbnail";
            this.pictureEditThumbnail.Properties.ShowCameraMenuItem = DevExpress.XtraEditors.Controls.CameraMenuItemVisibility.Auto;
            this.pictureEditThumbnail.Size = new System.Drawing.Size(230, 202);
            this.pictureEditThumbnail.TabIndex = 8;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(53, 110);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(46, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Quantity";
            // 
            // AddItem
            // 
            this.AcceptButton = this.btnAdd;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(700, 446);
            this.Controls.Add(this.pictureEditThumbnail);
            this.Controls.Add(this.txtBoxNotes);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.chkBoxCheckForBends);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboBoxBatchName);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboBoxSchedName);
            this.Controls.Add(this.itemNameTxtBox);
            this.Controls.Add(this.txtBoxQty);
            this.Controls.Add(this.comboBoxOrderNum);
            this.Name = "AddItem";
            this.Text = "AddItem";
            this.Load += new System.EventHandler(this.AddItem_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureEditThumbnail.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBoxOrderNum;
        private System.Windows.Forms.TextBox txtBoxQty;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.ComboBox comboBoxSchedName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox comboBoxBatchName;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox chkBoxCheckForBends;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtBoxNotes;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.TextBox itemNameTxtBox;
        private DevExpress.XtraEditors.PictureEdit pictureEditThumbnail;
        private System.Windows.Forms.Label label1;
    }
}