namespace VaultAccess
{
    partial class VaultBrowser
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VaultBrowser));
            this.vaultBrowserControl1 = new Autodesk.DataManagement.Client.Framework.Vault.Forms.Controls.VaultBrowserControl();
            this.vaultNavigationPathComboboxControl1 = new Autodesk.DataManagement.Client.Framework.Vault.Forms.Controls.VaultNavigationPathComboboxControl();
            this.lookIn_label = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.actionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.m_advancedFindToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileName_multiPartTextBox = new Autodesk.DataManagement.Client.Framework.Forms.Controls.MultiPartTextBoxControl();
            this.fileName_label = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.navigateBack_toolStripButton = new System.Windows.Forms.ToolStripButton();
            this.navigateUp_toolStripButton = new System.Windows.Forms.ToolStripButton();
            this.switchView_toolStripSplitButton = new System.Windows.Forms.ToolStripSplitButton();
            this.revision_label = new System.Windows.Forms.Label();
            this.fileType_comboBox = new System.Windows.Forms.ComboBox();
            this.m_tabControl = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.m_associationsTreeView = new System.Windows.Forms.TreeView();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.menuStrip1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.m_tabControl.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.SuspendLayout();
            // 
            // vaultBrowserControl1
            // 
            resources.ApplyResources(this.vaultBrowserControl1, "vaultBrowserControl1");
            // 
            // vaultNavigationPathComboboxControl1
            // 
            resources.ApplyResources(this.vaultNavigationPathComboboxControl1, "vaultNavigationPathComboboxControl1");
            this.vaultNavigationPathComboboxControl1.Name = "vaultNavigationPathComboboxControl1";
            // 
            // lookIn_label
            // 
            resources.ApplyResources(this.lookIn_label, "lookIn_label");
            this.lookIn_label.Name = "lookIn_label";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.actionsToolStripMenuItem});
            resources.ApplyResources(this.menuStrip1, "menuStrip1");
            this.menuStrip1.Name = "menuStrip1";
            // 
            // actionsToolStripMenuItem
            // 
            this.actionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_advancedFindToolStripMenuItem});
            this.actionsToolStripMenuItem.Name = "actionsToolStripMenuItem";
            resources.ApplyResources(this.actionsToolStripMenuItem, "actionsToolStripMenuItem");
            // 
            // m_advancedFindToolStripMenuItem
            // 
            resources.ApplyResources(this.m_advancedFindToolStripMenuItem, "m_advancedFindToolStripMenuItem");
            this.m_advancedFindToolStripMenuItem.Name = "m_advancedFindToolStripMenuItem";
            this.m_advancedFindToolStripMenuItem.Click += new System.EventHandler(this.m_advancedFindToolStripMenuItem_Click);
            // 
            // fileName_multiPartTextBox
            // 
            resources.ApplyResources(this.fileName_multiPartTextBox, "fileName_multiPartTextBox");
            this.tableLayoutPanel1.SetColumnSpan(this.fileName_multiPartTextBox, 2);
            this.fileName_multiPartTextBox.EditMode = Autodesk.DataManagement.Client.Framework.Forms.Controls.MultiPartTextBoxControl.EditModeOption.FullEdit;
            this.fileName_multiPartTextBox.Name = "fileName_multiPartTextBox";
            this.fileName_multiPartTextBox.Parts = ((System.Collections.Generic.IEnumerable<string>)(resources.GetObject("fileName_multiPartTextBox.Parts")));
            // 
            // fileName_label
            // 
            resources.ApplyResources(this.fileName_label, "fileName_label");
            this.fileName_label.Name = "fileName_label";
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.lookIn_label, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.vaultNavigationPathComboboxControl1, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.toolStrip1, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.fileName_label, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.fileName_multiPartTextBox, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.revision_label, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.fileType_comboBox, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.m_tabControl, 0, 4);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // toolStrip1
            // 
            this.toolStrip1.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(this.toolStrip1, "toolStrip1");
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.navigateBack_toolStripButton,
            this.navigateUp_toolStripButton,
            this.switchView_toolStripSplitButton});
            this.toolStrip1.Name = "toolStrip1";
            // 
            // navigateBack_toolStripButton
            // 
            this.navigateBack_toolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.navigateBack_toolStripButton.Image = global::VaultAccess.Resource.Back_16;
            resources.ApplyResources(this.navigateBack_toolStripButton, "navigateBack_toolStripButton");
            this.navigateBack_toolStripButton.Name = "navigateBack_toolStripButton";
            this.navigateBack_toolStripButton.Click += new System.EventHandler(this.navigateBack_toolStripButton_Click);
            // 
            // navigateUp_toolStripButton
            // 
            this.navigateUp_toolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.navigateUp_toolStripButton.Image = global::VaultAccess.Resource.uplevel_16;
            resources.ApplyResources(this.navigateUp_toolStripButton, "navigateUp_toolStripButton");
            this.navigateUp_toolStripButton.Name = "navigateUp_toolStripButton";
            this.navigateUp_toolStripButton.Click += new System.EventHandler(this.navigateUp_toolStripButton_Click);
            // 
            // switchView_toolStripSplitButton
            // 
            this.switchView_toolStripSplitButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.switchView_toolStripSplitButton.Image = global::VaultAccess.Resource.ViewOptions_16;
            resources.ApplyResources(this.switchView_toolStripSplitButton, "switchView_toolStripSplitButton");
            this.switchView_toolStripSplitButton.Name = "switchView_toolStripSplitButton";
            this.switchView_toolStripSplitButton.ButtonClick += new System.EventHandler(this.switchView_toolStripSplitButton_ButtonClick);
            this.switchView_toolStripSplitButton.DropDownOpening += new System.EventHandler(this.switchView_toolStripSplitButton_DropDownOpening);
            // 
            // revision_label
            // 
            resources.ApplyResources(this.revision_label, "revision_label");
            this.revision_label.Name = "revision_label";
            // 
            // fileType_comboBox
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.fileType_comboBox, 2);
            resources.ApplyResources(this.fileType_comboBox, "fileType_comboBox");
            this.fileType_comboBox.FormattingEnabled = true;
            this.fileType_comboBox.Name = "fileType_comboBox";
            this.fileType_comboBox.SelectedIndexChanged += new System.EventHandler(this.fileType_comboBox_SelectedIndexChanged);
            // 
            // m_tabControl
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.m_tabControl, 3);
            this.m_tabControl.Controls.Add(this.tabPage1);
            resources.ApplyResources(this.m_tabControl, "m_tabControl");
            this.m_tabControl.Name = "m_tabControl";
            this.m_tabControl.SelectedIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.m_associationsTreeView);
            resources.ApplyResources(this.tabPage1, "tabPage1");
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // m_associationsTreeView
            // 
            resources.ApplyResources(this.m_associationsTreeView, "m_associationsTreeView");
            this.m_associationsTreeView.Name = "m_associationsTreeView";
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnOk
            // 
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            resources.ApplyResources(this.btnOk, "btnOk");
            this.btnOk.Name = "btnOk";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // VaultBrowser
            // 
            this.AcceptButton = this.btnOk;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "VaultBrowser";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.VaultBrowser_FormClosed);
            this.Shown += new System.EventHandler(this.VaultBrowser_Shown);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.m_tabControl.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Autodesk.DataManagement.Client.Framework.Vault.Forms.Controls.VaultBrowserControl vaultBrowserControl1;
        private Autodesk.DataManagement.Client.Framework.Vault.Forms.Controls.VaultNavigationPathComboboxControl vaultNavigationPathComboboxControl1;
        private System.Windows.Forms.Label lookIn_label;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private Autodesk.DataManagement.Client.Framework.Forms.Controls.MultiPartTextBoxControl fileName_multiPartTextBox;
        private System.Windows.Forms.Label fileName_label;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label revision_label;
        private System.Windows.Forms.ComboBox fileType_comboBox;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton navigateBack_toolStripButton;
        private System.Windows.Forms.ToolStripButton navigateUp_toolStripButton;
        private System.Windows.Forms.ToolStripSplitButton switchView_toolStripSplitButton;
        private System.Windows.Forms.TabControl m_tabControl;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TreeView m_associationsTreeView;
        private System.Windows.Forms.ToolStripMenuItem actionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem m_advancedFindToolStripMenuItem;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
    }
}

