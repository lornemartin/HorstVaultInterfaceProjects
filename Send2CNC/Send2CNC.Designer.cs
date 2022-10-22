namespace WindowsFormsApplication1
{
    partial class Send2CNC
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
            this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
            this.btnHyundaiSave2 = new System.Windows.Forms.Button();
            this.btnHyundaiSave = new System.Windows.Forms.Button();
            this.btnSL30Save2 = new System.Windows.Forms.Button();
            this.btnSL30Save = new System.Windows.Forms.Button();
            this.btnEX110Save2 = new System.Windows.Forms.Button();
            this.btnEX110Save = new System.Windows.Forms.Button();
            this.btnTC20Save2 = new System.Windows.Forms.Button();
            this.btnTC20Save = new System.Windows.Forms.Button();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.btnHyundai = new System.Windows.Forms.Button();
            this.btnSaveUSB = new System.Windows.Forms.Button();
            this.btnEX110 = new System.Windows.Forms.Button();
            this.btnTC20 = new System.Windows.Forms.Button();
            this.btnGenerate = new System.Windows.Forms.Button();
            this.ncFileBox = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lblToolNo = new System.Windows.Forms.Label();
            this.txtBoxToolNo = new System.Windows.Forms.TextBox();
            this.chkBoxSafeCheck = new System.Windows.Forms.CheckBox();
            this.OffsetsGroupBox = new System.Windows.Forms.GroupBox();
            this.OffsetTextBox = new System.Windows.Forms.TextBox();
            this.workShiftTextBox = new System.Windows.Forms.TextBox();
            this.offsetLabel = new System.Windows.Forms.Label();
            this.workShiftLabel = new System.Windows.Forms.Label();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileNewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileOpenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileSaveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileExitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.serialPortTC20 = new System.IO.Ports.SerialPort(this.components);
            this.serialPortEX110 = new System.IO.Ports.SerialPort(this.components);
            this.serialPortHaasVF1 = new System.IO.Ports.SerialPort(this.components);
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.toolStripContainer1.ContentPanel.SuspendLayout();
            this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
            this.toolStripContainer1.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.OffsetsGroupBox.SuspendLayout();
            this.menuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStripContainer1
            // 
            // 
            // toolStripContainer1.ContentPanel
            // 
            this.toolStripContainer1.ContentPanel.Controls.Add(this.btnHyundaiSave2);
            this.toolStripContainer1.ContentPanel.Controls.Add(this.btnHyundaiSave);
            this.toolStripContainer1.ContentPanel.Controls.Add(this.btnSL30Save2);
            this.toolStripContainer1.ContentPanel.Controls.Add(this.btnSL30Save);
            this.toolStripContainer1.ContentPanel.Controls.Add(this.btnEX110Save2);
            this.toolStripContainer1.ContentPanel.Controls.Add(this.btnEX110Save);
            this.toolStripContainer1.ContentPanel.Controls.Add(this.btnTC20Save2);
            this.toolStripContainer1.ContentPanel.Controls.Add(this.btnTC20Save);
            this.toolStripContainer1.ContentPanel.Controls.Add(this.statusStrip);
            this.toolStripContainer1.ContentPanel.Controls.Add(this.btnHyundai);
            this.toolStripContainer1.ContentPanel.Controls.Add(this.btnSaveUSB);
            this.toolStripContainer1.ContentPanel.Controls.Add(this.btnEX110);
            this.toolStripContainer1.ContentPanel.Controls.Add(this.btnTC20);
            this.toolStripContainer1.ContentPanel.Controls.Add(this.btnGenerate);
            this.toolStripContainer1.ContentPanel.Controls.Add(this.ncFileBox);
            this.toolStripContainer1.ContentPanel.Controls.Add(this.groupBox1);
            this.toolStripContainer1.ContentPanel.Controls.Add(this.OffsetsGroupBox);
            this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(934, 561);
            this.toolStripContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolStripContainer1.Location = new System.Drawing.Point(0, 0);
            this.toolStripContainer1.Name = "toolStripContainer1";
            this.toolStripContainer1.Size = new System.Drawing.Size(934, 585);
            this.toolStripContainer1.TabIndex = 0;
            this.toolStripContainer1.Text = "toolStripContainer1";
            // 
            // toolStripContainer1.TopToolStripPanel
            // 
            this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.menuStrip);
            // 
            // btnHyundaiSave2
            // 
            this.btnHyundaiSave2.Enabled = false;
            this.btnHyundaiSave2.Location = new System.Drawing.Point(285, 482);
            this.btnHyundaiSave2.Name = "btnHyundaiSave2";
            this.btnHyundaiSave2.Size = new System.Drawing.Size(75, 23);
            this.btnHyundaiSave2.TabIndex = 10;
            this.btnHyundaiSave2.Text = "Save...";
            this.btnHyundaiSave2.UseVisualStyleBackColor = true;
            this.btnHyundaiSave2.Click += new System.EventHandler(this.btnHyundaiSave2_Click);
            // 
            // btnHyundaiSave
            // 
            this.btnHyundaiSave.Enabled = false;
            this.btnHyundaiSave.Location = new System.Drawing.Point(182, 482);
            this.btnHyundaiSave.Name = "btnHyundaiSave";
            this.btnHyundaiSave.Size = new System.Drawing.Size(75, 23);
            this.btnHyundaiSave.TabIndex = 10;
            this.btnHyundaiSave.Text = "Save";
            this.btnHyundaiSave.UseVisualStyleBackColor = true;
            this.btnHyundaiSave.Click += new System.EventHandler(this.btnHyundaiSave_Click);
            // 
            // btnSL30Save2
            // 
            this.btnSL30Save2.Enabled = false;
            this.btnSL30Save2.Location = new System.Drawing.Point(285, 402);
            this.btnSL30Save2.Name = "btnSL30Save2";
            this.btnSL30Save2.Size = new System.Drawing.Size(75, 23);
            this.btnSL30Save2.TabIndex = 10;
            this.btnSL30Save2.Text = "Save...";
            this.btnSL30Save2.UseVisualStyleBackColor = true;
            this.btnSL30Save2.Click += new System.EventHandler(this.btnSL30Save2_Click);
            // 
            // btnSL30Save
            // 
            this.btnSL30Save.Enabled = false;
            this.btnSL30Save.Location = new System.Drawing.Point(182, 402);
            this.btnSL30Save.Name = "btnSL30Save";
            this.btnSL30Save.Size = new System.Drawing.Size(75, 23);
            this.btnSL30Save.TabIndex = 10;
            this.btnSL30Save.Text = "Save";
            this.btnSL30Save.UseVisualStyleBackColor = true;
            this.btnSL30Save.Click += new System.EventHandler(this.btnSL30Save_Click);
            // 
            // btnEX110Save2
            // 
            this.btnEX110Save2.Enabled = false;
            this.btnEX110Save2.Location = new System.Drawing.Point(285, 320);
            this.btnEX110Save2.Name = "btnEX110Save2";
            this.btnEX110Save2.Size = new System.Drawing.Size(75, 23);
            this.btnEX110Save2.TabIndex = 10;
            this.btnEX110Save2.Text = "Save...";
            this.btnEX110Save2.UseVisualStyleBackColor = true;
            this.btnEX110Save2.Click += new System.EventHandler(this.btnEX110Save2_Click);
            // 
            // btnEX110Save
            // 
            this.btnEX110Save.Enabled = false;
            this.btnEX110Save.Location = new System.Drawing.Point(182, 320);
            this.btnEX110Save.Name = "btnEX110Save";
            this.btnEX110Save.Size = new System.Drawing.Size(75, 23);
            this.btnEX110Save.TabIndex = 10;
            this.btnEX110Save.Text = "Save";
            this.btnEX110Save.UseVisualStyleBackColor = true;
            this.btnEX110Save.Click += new System.EventHandler(this.btnEX110Save_Click);
            // 
            // btnTC20Save2
            // 
            this.btnTC20Save2.Enabled = false;
            this.btnTC20Save2.Location = new System.Drawing.Point(285, 244);
            this.btnTC20Save2.Name = "btnTC20Save2";
            this.btnTC20Save2.Size = new System.Drawing.Size(75, 23);
            this.btnTC20Save2.TabIndex = 9;
            this.btnTC20Save2.Text = "Save...";
            this.btnTC20Save2.UseVisualStyleBackColor = true;
            this.btnTC20Save2.Click += new System.EventHandler(this.btnTC20Save2_Click);
            // 
            // btnTC20Save
            // 
            this.btnTC20Save.Enabled = false;
            this.btnTC20Save.Location = new System.Drawing.Point(182, 244);
            this.btnTC20Save.Name = "btnTC20Save";
            this.btnTC20Save.Size = new System.Drawing.Size(75, 23);
            this.btnTC20Save.TabIndex = 9;
            this.btnTC20Save.Text = "Save";
            this.btnTC20Save.UseVisualStyleBackColor = true;
            this.btnTC20Save.Click += new System.EventHandler(this.btnTC20Save_Click);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel});
            this.statusStrip.Location = new System.Drawing.Point(0, 539);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(934, 22);
            this.statusStrip.TabIndex = 7;
            this.statusStrip.Text = "statusStrip";
            // 
            // toolStripStatusLabel
            // 
            this.toolStripStatusLabel.Name = "toolStripStatusLabel";
            this.toolStripStatusLabel.Size = new System.Drawing.Size(0, 17);
            // 
            // btnHyundai
            // 
            this.btnHyundai.Enabled = false;
            this.btnHyundai.Location = new System.Drawing.Point(22, 464);
            this.btnHyundai.Name = "btnHyundai";
            this.btnHyundai.Size = new System.Drawing.Size(144, 58);
            this.btnHyundai.TabIndex = 8;
            this.btnHyundai.Text = "Hyundai";
            this.btnHyundai.UseVisualStyleBackColor = true;
            this.btnHyundai.Click += new System.EventHandler(this.btnHyundai_Click);
            // 
            // btnSaveUSB
            // 
            this.btnSaveUSB.Enabled = false;
            this.btnSaveUSB.Location = new System.Drawing.Point(22, 384);
            this.btnSaveUSB.Name = "btnSaveUSB";
            this.btnSaveUSB.Size = new System.Drawing.Size(144, 58);
            this.btnSaveUSB.TabIndex = 8;
            this.btnSaveUSB.Text = "Save to USB";
            this.btnSaveUSB.UseVisualStyleBackColor = true;
            this.btnSaveUSB.Click += new System.EventHandler(this.btnSaveUSB_Click);
            // 
            // btnEX110
            // 
            this.btnEX110.Enabled = false;
            this.btnEX110.Location = new System.Drawing.Point(22, 302);
            this.btnEX110.Name = "btnEX110";
            this.btnEX110.Size = new System.Drawing.Size(144, 58);
            this.btnEX110.TabIndex = 7;
            this.btnEX110.Text = "EX-110";
            this.btnEX110.UseVisualStyleBackColor = true;
            this.btnEX110.Click += new System.EventHandler(this.btnEX110_Click);
            // 
            // btnTC20
            // 
            this.btnTC20.Enabled = false;
            this.btnTC20.Location = new System.Drawing.Point(22, 226);
            this.btnTC20.Name = "btnTC20";
            this.btnTC20.Size = new System.Drawing.Size(144, 58);
            this.btnTC20.TabIndex = 6;
            this.btnTC20.Text = "TC-20";
            this.btnTC20.UseVisualStyleBackColor = true;
            this.btnTC20.Click += new System.EventHandler(this.btnTC20_Click);
            // 
            // btnGenerate
            // 
            this.btnGenerate.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnGenerate.Location = new System.Drawing.Point(22, 154);
            this.btnGenerate.Name = "btnGenerate";
            this.btnGenerate.Size = new System.Drawing.Size(483, 55);
            this.btnGenerate.TabIndex = 5;
            this.btnGenerate.Text = "Generate Program";
            this.btnGenerate.UseVisualStyleBackColor = true;
            this.btnGenerate.Click += new System.EventHandler(this.btnGenerate_Click);
            // 
            // ncFileBox
            // 
            this.ncFileBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ncFileBox.Location = new System.Drawing.Point(529, 60);
            this.ncFileBox.Multiline = true;
            this.ncFileBox.Name = "ncFileBox";
            this.ncFileBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.ncFileBox.Size = new System.Drawing.Size(393, 476);
            this.ncFileBox.TabIndex = 11;
            this.ncFileBox.WordWrap = false;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lblToolNo);
            this.groupBox1.Controls.Add(this.txtBoxToolNo);
            this.groupBox1.Controls.Add(this.chkBoxSafeCheck);
            this.groupBox1.Location = new System.Drawing.Point(260, 26);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(183, 100);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Bar Feed";
            // 
            // lblToolNo
            // 
            this.lblToolNo.AutoSize = true;
            this.lblToolNo.Enabled = false;
            this.lblToolNo.Location = new System.Drawing.Point(11, 53);
            this.lblToolNo.Name = "lblToolNo";
            this.lblToolNo.Size = new System.Drawing.Size(77, 13);
            this.lblToolNo.TabIndex = 5;
            this.lblToolNo.Text = "Bar Pull Tool #";
            // 
            // txtBoxToolNo
            // 
            this.txtBoxToolNo.Enabled = false;
            this.txtBoxToolNo.Location = new System.Drawing.Point(94, 50);
            this.txtBoxToolNo.Name = "txtBoxToolNo";
            this.txtBoxToolNo.Size = new System.Drawing.Size(62, 20);
            this.txtBoxToolNo.TabIndex = 4;
            this.txtBoxToolNo.Text = "0404";
            // 
            // chkBoxSafeCheck
            // 
            this.chkBoxSafeCheck.AutoSize = true;
            this.chkBoxSafeCheck.Location = new System.Drawing.Point(14, 22);
            this.chkBoxSafeCheck.Name = "chkBoxSafeCheck";
            this.chkBoxSafeCheck.Size = new System.Drawing.Size(129, 17);
            this.chkBoxSafeCheck.TabIndex = 3;
            this.chkBoxSafeCheck.Text = "Bar Pull Safety Check";
            this.chkBoxSafeCheck.UseVisualStyleBackColor = true;
            this.chkBoxSafeCheck.CheckedChanged += new System.EventHandler(this.chkBoxSafeCheck_CheckedChanged);
            // 
            // OffsetsGroupBox
            // 
            this.OffsetsGroupBox.Controls.Add(this.OffsetTextBox);
            this.OffsetsGroupBox.Controls.Add(this.workShiftTextBox);
            this.OffsetsGroupBox.Controls.Add(this.offsetLabel);
            this.OffsetsGroupBox.Controls.Add(this.workShiftLabel);
            this.OffsetsGroupBox.Location = new System.Drawing.Point(27, 26);
            this.OffsetsGroupBox.Name = "OffsetsGroupBox";
            this.OffsetsGroupBox.Size = new System.Drawing.Size(183, 100);
            this.OffsetsGroupBox.TabIndex = 0;
            this.OffsetsGroupBox.TabStop = false;
            this.OffsetsGroupBox.Text = "Offsets";
            // 
            // OffsetTextBox
            // 
            this.OffsetTextBox.Location = new System.Drawing.Point(85, 55);
            this.OffsetTextBox.Name = "OffsetTextBox";
            this.OffsetTextBox.Size = new System.Drawing.Size(80, 20);
            this.OffsetTextBox.TabIndex = 2;
            this.OffsetTextBox.Text = "0.060";
            // 
            // workShiftTextBox
            // 
            this.workShiftTextBox.Location = new System.Drawing.Point(85, 19);
            this.workShiftTextBox.Name = "workShiftTextBox";
            this.workShiftTextBox.Size = new System.Drawing.Size(80, 20);
            this.workShiftTextBox.TabIndex = 1;
            this.workShiftTextBox.Text = "-3.5";
            // 
            // offsetLabel
            // 
            this.offsetLabel.AutoSize = true;
            this.offsetLabel.Location = new System.Drawing.Point(18, 58);
            this.offsetLabel.Name = "offsetLabel";
            this.offsetLabel.Size = new System.Drawing.Size(35, 13);
            this.offsetLabel.TabIndex = 2;
            this.offsetLabel.Text = "Offset";
            // 
            // workShiftLabel
            // 
            this.workShiftLabel.AutoSize = true;
            this.workShiftLabel.Location = new System.Drawing.Point(18, 22);
            this.workShiftLabel.Name = "workShiftLabel";
            this.workShiftLabel.Size = new System.Drawing.Size(52, 13);
            this.workShiftLabel.TabIndex = 2;
            this.workShiftLabel.Text = "Workshift";
            // 
            // menuStrip
            // 
            this.menuStrip.Dock = System.Windows.Forms.DockStyle.None;
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(934, 24);
            this.menuStrip.TabIndex = 0;
            this.menuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileNewToolStripMenuItem,
            this.fileOpenToolStripMenuItem,
            this.fileSaveToolStripMenuItem,
            this.fileExitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // fileNewToolStripMenuItem
            // 
            this.fileNewToolStripMenuItem.Name = "fileNewToolStripMenuItem";
            this.fileNewToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.fileNewToolStripMenuItem.Text = "&New";
            // 
            // fileOpenToolStripMenuItem
            // 
            this.fileOpenToolStripMenuItem.Name = "fileOpenToolStripMenuItem";
            this.fileOpenToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.fileOpenToolStripMenuItem.Text = "&Open";
            // 
            // fileSaveToolStripMenuItem
            // 
            this.fileSaveToolStripMenuItem.Enabled = false;
            this.fileSaveToolStripMenuItem.Name = "fileSaveToolStripMenuItem";
            this.fileSaveToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.fileSaveToolStripMenuItem.Text = "&Save";
            // 
            // fileExitToolStripMenuItem
            // 
            this.fileExitToolStripMenuItem.Name = "fileExitToolStripMenuItem";
            this.fileExitToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.fileExitToolStripMenuItem.Text = "E&xit";
            this.fileExitToolStripMenuItem.Click += new System.EventHandler(this.fileExitToolStripMenuItem_Click);
            // 
            // openFileDialog
            // 
            this.openFileDialog.FileName = "openFileDialog1";
            // 
            // serialPortTC20
            // 
            this.serialPortTC20.BaudRate = 2400;
            this.serialPortTC20.DataBits = 7;
            this.serialPortTC20.Parity = System.IO.Ports.Parity.Even;
            this.serialPortTC20.PortName = "COM7";
            // 
            // serialPortEX110
            // 
            this.serialPortEX110.BaudRate = 2400;
            this.serialPortEX110.DataBits = 7;
            this.serialPortEX110.Parity = System.IO.Ports.Parity.Even;
            // 
            // toolTip1
            // 
            this.toolTip1.Tag = "";
            // 
            // folderBrowserDialog1
            // 
            this.folderBrowserDialog1.RootFolder = System.Environment.SpecialFolder.MyComputer;
            this.folderBrowserDialog1.HelpRequest += new System.EventHandler(this.folderBrowserDialog1_HelpRequest);
            // 
            // Send2CNC
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(934, 585);
            this.Controls.Add(this.toolStripContainer1);
            this.MainMenuStrip = this.menuStrip;
            this.Name = "Send2CNC";
            this.Text = "Send2CNC";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Send2CNC_FormClosing);
            this.toolStripContainer1.ContentPanel.ResumeLayout(false);
            this.toolStripContainer1.ContentPanel.PerformLayout();
            this.toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.PerformLayout();
            this.toolStripContainer1.ResumeLayout(false);
            this.toolStripContainer1.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.OffsetsGroupBox.ResumeLayout(false);
            this.OffsetsGroupBox.PerformLayout();
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ToolStripContainer toolStripContainer1;
        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fileNewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fileOpenToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fileExitToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.GroupBox OffsetsGroupBox;
        private System.Windows.Forms.TextBox OffsetTextBox;
        private System.Windows.Forms.TextBox workShiftTextBox;
        private System.Windows.Forms.Label offsetLabel;
        private System.Windows.Forms.Label workShiftLabel;
        private System.Windows.Forms.ToolStripMenuItem fileSaveToolStripMenuItem;
        private System.Windows.Forms.TextBox ncFileBox;
        private System.Windows.Forms.Button btnGenerate;
        private System.Windows.Forms.Button btnSaveUSB;
        private System.Windows.Forms.Button btnEX110;
        private System.Windows.Forms.Button btnTC20;
        private System.IO.Ports.SerialPort serialPortTC20;
        private System.IO.Ports.SerialPort serialPortEX110;
        private System.IO.Ports.SerialPort serialPortHaasVF1;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
        private System.Windows.Forms.Button btnEX110Save;
        private System.Windows.Forms.Button btnTC20Save;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox chkBoxSafeCheck;
        private System.Windows.Forms.Label lblToolNo;
        private System.Windows.Forms.TextBox txtBoxToolNo;
        private System.Windows.Forms.Button btnSL30Save;
        private System.Windows.Forms.Button btnHyundaiSave;
        private System.Windows.Forms.Button btnHyundai;
        private System.Windows.Forms.Button btnHyundaiSave2;
        private System.Windows.Forms.Button btnSL30Save2;
        private System.Windows.Forms.Button btnEX110Save2;
        private System.Windows.Forms.Button btnTC20Save2;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
    }
}

