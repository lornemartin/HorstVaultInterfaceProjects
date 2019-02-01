namespace VaultItemProcessor
{
    partial class Form1
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
            DevExpress.XtraSplashScreen.SplashScreenManager splashScreenManager1 = new DevExpress.XtraSplashScreen.SplashScreenManager(this, null, true, true);
            DevExpress.XtraTreeList.StyleFormatConditions.TreeListFormatRule treeListFormatRule1 = new DevExpress.XtraTreeList.StyleFormatConditions.TreeListFormatRule();
            DevExpress.XtraEditors.FormatConditionRuleExpression formatConditionRuleExpression1 = new DevExpress.XtraEditors.FormatConditionRuleExpression();
            DevExpress.XtraTreeList.StyleFormatConditions.TreeListFormatRule treeListFormatRule2 = new DevExpress.XtraTreeList.StyleFormatConditions.TreeListFormatRule();
            DevExpress.XtraEditors.FormatConditionRuleExpression formatConditionRuleExpression2 = new DevExpress.XtraEditors.FormatConditionRuleExpression();
            DevExpress.XtraTreeList.StyleFormatConditions.TreeListFormatRule treeListFormatRule3 = new DevExpress.XtraTreeList.StyleFormatConditions.TreeListFormatRule();
            DevExpress.XtraEditors.FormatConditionRuleExpression formatConditionRuleExpression3 = new DevExpress.XtraEditors.FormatConditionRuleExpression();
            DevExpress.XtraTreeList.StyleFormatConditions.TreeListFormatRule treeListFormatRule4 = new DevExpress.XtraTreeList.StyleFormatConditions.TreeListFormatRule();
            DevExpress.XtraEditors.FormatConditionRuleExpression formatConditionRuleExpression4 = new DevExpress.XtraEditors.FormatConditionRuleExpression();
            DevExpress.XtraTreeList.StyleFormatConditions.TreeListFormatRule treeListFormatRule5 = new DevExpress.XtraTreeList.StyleFormatConditions.TreeListFormatRule();
            DevExpress.XtraEditors.FormatConditionRuleExpression formatConditionRuleExpression5 = new DevExpress.XtraEditors.FormatConditionRuleExpression();
            DevExpress.XtraTreeList.StyleFormatConditions.TreeListFormatRule treeListFormatRule6 = new DevExpress.XtraTreeList.StyleFormatConditions.TreeListFormatRule();
            DevExpress.XtraEditors.FormatConditionRuleExpression formatConditionRuleExpression6 = new DevExpress.XtraEditors.FormatConditionRuleExpression();
            DevExpress.XtraTreeList.StyleFormatConditions.TreeListFormatRule treeListFormatRule7 = new DevExpress.XtraTreeList.StyleFormatConditions.TreeListFormatRule();
            DevExpress.XtraEditors.FormatConditionRuleExpression formatConditionRuleExpression7 = new DevExpress.XtraEditors.FormatConditionRuleExpression();
            DevExpress.XtraTreeList.StyleFormatConditions.TreeListFormatRule treeListFormatRule8 = new DevExpress.XtraTreeList.StyleFormatConditions.TreeListFormatRule();
            DevExpress.XtraEditors.FormatConditionRuleExpression formatConditionRuleExpression8 = new DevExpress.XtraEditors.FormatConditionRuleExpression();
            DevExpress.XtraTreeList.StyleFormatConditions.TreeListFormatRule treeListFormatRule9 = new DevExpress.XtraTreeList.StyleFormatConditions.TreeListFormatRule();
            DevExpress.XtraEditors.FormatConditionRuleExpression formatConditionRuleExpression9 = new DevExpress.XtraEditors.FormatConditionRuleExpression();
            DevExpress.XtraTreeList.StyleFormatConditions.TreeListFormatRule treeListFormatRule10 = new DevExpress.XtraTreeList.StyleFormatConditions.TreeListFormatRule();
            DevExpress.XtraEditors.FormatConditionRuleExpression formatConditionRuleExpression10 = new DevExpress.XtraEditors.FormatConditionRuleExpression();
            DevExpress.XtraTreeList.StyleFormatConditions.TreeListFormatRule treeListFormatRule11 = new DevExpress.XtraTreeList.StyleFormatConditions.TreeListFormatRule();
            DevExpress.XtraEditors.FormatConditionRuleExpression formatConditionRuleExpression11 = new DevExpress.XtraEditors.FormatConditionRuleExpression();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.colThickness = new DevExpress.XtraTreeList.Columns.TreeListColumn();
            this.repositoryItemTextEdit1 = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.Material = new DevExpress.XtraTreeList.Columns.TreeListColumn();
            this.colStructCode = new DevExpress.XtraTreeList.Columns.TreeListColumn();
            this.Operations = new DevExpress.XtraTreeList.Columns.TreeListColumn();
            this.exportTreeList = new DevExpress.XtraTreeList.TreeList();
            this.Parent = new DevExpress.XtraTreeList.Columns.TreeListColumn();
            this.Number = new DevExpress.XtraTreeList.Columns.TreeListColumn();
            this.Title = new DevExpress.XtraTreeList.Columns.TreeListColumn();
            this.ItemDescription = new DevExpress.XtraTreeList.Columns.TreeListColumn();
            this.Category = new DevExpress.XtraTreeList.Columns.TreeListColumn();
            this.Qty = new DevExpress.XtraTreeList.Columns.TreeListColumn();
            this.colIsStock = new DevExpress.XtraTreeList.Columns.TreeListColumn();
            this.repositoryItemCheckEdit1 = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            this.colPlantID = new DevExpress.XtraTreeList.Columns.TreeListColumn();
            this.HasPdf = new DevExpress.XtraTreeList.Columns.TreeListColumn();
            this.requiresPDF = new DevExpress.XtraTreeList.Columns.TreeListColumn();
            this.btnProcess = new System.Windows.Forms.Button();
            this.btnLoad = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.spinEditOrderQty = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtBoxOrderNumber = new System.Windows.Forms.TextBox();
            this.pdfViewer1 = new DevExpress.XtraPdfViewer.PdfViewer();
            this.groupBoxOutput = new System.Windows.Forms.GroupBox();
            this.button1 = new System.Windows.Forms.Button();
            this.btnRemoveBatchItem = new System.Windows.Forms.Button();
            this.btnGroupSawDrawings3 = new System.Windows.Forms.Button();
            this.btnRemoveOrder = new System.Windows.Forms.Button();
            this.btnFinalize = new System.Windows.Forms.Button();
            this.btnSelectOutputFolder = new System.Windows.Forms.Button();
            this.outputFolderlbl = new System.Windows.Forms.Label();
            this.textBoxOutputFolder = new System.Windows.Forms.TextBox();
            this.groupBoxInput = new System.Windows.Forms.GroupBox();
            this.btnProcessBatch = new System.Windows.Forms.Button();
            this.folderBrowserDialogOutputFolderSelect = new System.Windows.Forms.FolderBrowserDialog();
            this.behaviorManager1 = new DevExpress.Utils.Behaviors.BehaviorManager(this.components);
            this.btnOdoo = new System.Windows.Forms.Button();
            this.colNotes = new DevExpress.XtraTreeList.Columns.TreeListColumn();
            this.radioGroup1 = new DevExpress.XtraEditors.RadioGroup();
            this.btnHorstMFG = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemTextEdit1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.exportTreeList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemCheckEdit1)).BeginInit();
            this.statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.spinEditOrderQty)).BeginInit();
            this.groupBoxOutput.SuspendLayout();
            this.groupBoxInput.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.behaviorManager1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.radioGroup1.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // splashScreenManager1
            // 
            splashScreenManager1.ClosingDelay = 500;
            // 
            // colThickness
            // 
            this.colThickness.Caption = "Thickness";
            this.colThickness.ColumnEdit = this.repositoryItemTextEdit1;
            this.colThickness.FieldName = "MaterialThickness";
            this.colThickness.Name = "colThickness";
            this.colThickness.OptionsColumn.AllowSort = false;
            this.colThickness.Visible = true;
            this.colThickness.VisibleIndex = 5;
            this.colThickness.Width = 30;
            // 
            // repositoryItemTextEdit1
            // 
            this.repositoryItemTextEdit1.AutoHeight = false;
            this.repositoryItemTextEdit1.Name = "repositoryItemTextEdit1";
            // 
            // Material
            // 
            this.Material.Caption = "Material";
            this.Material.FieldName = "Material";
            this.Material.Name = "Material";
            this.Material.OptionsColumn.AllowSort = false;
            this.Material.Visible = true;
            this.Material.VisibleIndex = 4;
            // 
            // colStructCode
            // 
            this.colStructCode.Caption = "Structural Code";
            this.colStructCode.ColumnEdit = this.repositoryItemTextEdit1;
            this.colStructCode.FieldName = "StructCode";
            this.colStructCode.Name = "colStructCode";
            this.colStructCode.OptionsColumn.AllowSort = false;
            this.colStructCode.Visible = true;
            this.colStructCode.VisibleIndex = 6;
            this.colStructCode.Width = 59;
            // 
            // Operations
            // 
            this.Operations.Caption = "Operations";
            this.Operations.FieldName = "Operations";
            this.Operations.Name = "Operations";
            this.Operations.OptionsColumn.AllowSort = false;
            this.Operations.Visible = true;
            this.Operations.VisibleIndex = 7;
            this.Operations.Width = 60;
            // 
            // exportTreeList
            // 
            this.exportTreeList.Columns.AddRange(new DevExpress.XtraTreeList.Columns.TreeListColumn[] {
            this.Parent,
            this.Number,
            this.Title,
            this.ItemDescription,
            this.Category,
            this.Material,
            this.colThickness,
            this.colStructCode,
            this.Operations,
            this.Qty,
            this.colIsStock,
            this.colPlantID,
            this.HasPdf,
            this.requiresPDF,
            this.colNotes});
            this.exportTreeList.Cursor = System.Windows.Forms.Cursors.SizeWE;
            this.exportTreeList.DataSource = null;
            treeListFormatRule1.ApplyToRow = true;
            treeListFormatRule1.Name = "Format0";
            formatConditionRuleExpression1.Appearance.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            formatConditionRuleExpression1.Appearance.Options.UseFont = true;
            formatConditionRuleExpression1.Expression = "[HasPdf] = True";
            treeListFormatRule1.Rule = formatConditionRuleExpression1;
            treeListFormatRule2.Column = this.colThickness;
            treeListFormatRule2.ColumnApplyTo = this.colThickness;
            treeListFormatRule2.Name = "CheckForMatThickness";
            formatConditionRuleExpression2.Appearance.BackColor = System.Drawing.Color.Red;
            formatConditionRuleExpression2.Appearance.ForeColor = System.Drawing.Color.Transparent;
            formatConditionRuleExpression2.Appearance.Options.UseBackColor = true;
            formatConditionRuleExpression2.Appearance.Options.UseForeColor = true;
            formatConditionRuleExpression2.Expression = "[Operations] = \'Laser\' And [MaterialThickness] = \'\'";
            treeListFormatRule2.Rule = formatConditionRuleExpression2;
            treeListFormatRule3.Column = this.Material;
            treeListFormatRule3.ColumnApplyTo = this.Material;
            treeListFormatRule3.Name = "CheckForMatType";
            formatConditionRuleExpression3.Appearance.BackColor = System.Drawing.Color.Red;
            formatConditionRuleExpression3.Appearance.ForeColor = System.Drawing.Color.Transparent;
            formatConditionRuleExpression3.Appearance.Options.UseBackColor = true;
            formatConditionRuleExpression3.Appearance.Options.UseForeColor = true;
            formatConditionRuleExpression3.Expression = "[Operations] = \'Laser\' And [Material] = \'\'";
            treeListFormatRule3.Rule = formatConditionRuleExpression3;
            treeListFormatRule4.Column = this.colStructCode;
            treeListFormatRule4.ColumnApplyTo = this.colStructCode;
            treeListFormatRule4.Name = "CheckForStructSpecs";
            formatConditionRuleExpression4.Appearance.BackColor = System.Drawing.Color.Red;
            formatConditionRuleExpression4.Appearance.ForeColor = System.Drawing.Color.Transparent;
            formatConditionRuleExpression4.Appearance.Options.UseBackColor = true;
            formatConditionRuleExpression4.Appearance.Options.UseForeColor = true;
            formatConditionRuleExpression4.Expression = "([Operations] = \'Bandsaw\' Or [Operations] = \'Iron Worker\' Or [Operations] = \'Mach" +
    "ine Shop\') And [StructCode] = \'\'";
            treeListFormatRule4.Rule = formatConditionRuleExpression4;
            treeListFormatRule5.Column = this.Operations;
            treeListFormatRule5.ColumnApplyTo = this.Operations;
            treeListFormatRule5.Name = "CheckForOperations";
            formatConditionRuleExpression5.Appearance.BackColor = System.Drawing.Color.Red;
            formatConditionRuleExpression5.Appearance.ForeColor = System.Drawing.Color.Transparent;
            formatConditionRuleExpression5.Appearance.Options.UseBackColor = true;
            formatConditionRuleExpression5.Appearance.Options.UseForeColor = true;
            formatConditionRuleExpression5.Expression = "[Operations] = \'\'";
            treeListFormatRule5.Rule = formatConditionRuleExpression5;
            treeListFormatRule6.Column = this.Operations;
            treeListFormatRule6.ColumnApplyTo = this.Operations;
            treeListFormatRule6.Name = "CheckForEmptyPartOps";
            formatConditionRuleExpression6.Appearance.BackColor = System.Drawing.Color.Red;
            formatConditionRuleExpression6.Appearance.ForeColor = System.Drawing.Color.Black;
            formatConditionRuleExpression6.Appearance.Options.UseBackColor = true;
            formatConditionRuleExpression6.Appearance.Options.UseForeColor = true;
            formatConditionRuleExpression6.Expression = "[Category] = \'Part\' And [Operations] = \'N/A\'";
            treeListFormatRule6.Rule = formatConditionRuleExpression6;
            treeListFormatRule7.ApplyToRow = true;
            treeListFormatRule7.Name = "CheckForPlant2";
            formatConditionRuleExpression7.Appearance.BackColor = System.Drawing.SystemColors.Info;
            formatConditionRuleExpression7.Appearance.BackColor2 = System.Drawing.SystemColors.Info;
            formatConditionRuleExpression7.Appearance.BorderColor = System.Drawing.Color.White;
            formatConditionRuleExpression7.Appearance.Options.UseBackColor = true;
            formatConditionRuleExpression7.Appearance.Options.UseBorderColor = true;
            formatConditionRuleExpression7.Expression = "[PlantID] = \'Plant 2\'";
            treeListFormatRule7.Rule = formatConditionRuleExpression7;
            treeListFormatRule8.ApplyToRow = true;
            treeListFormatRule8.Name = "hasPDFItalics";
            formatConditionRuleExpression8.Appearance.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Italic);
            formatConditionRuleExpression8.Appearance.Options.UseFont = true;
            formatConditionRuleExpression8.Expression = "[HasPdf] = False";
            treeListFormatRule8.Rule = formatConditionRuleExpression8;
            treeListFormatRule9.ApplyToRow = true;
            treeListFormatRule9.Name = "noPDFWarning";
            formatConditionRuleExpression9.Appearance.BackColor = System.Drawing.Color.Red;
            formatConditionRuleExpression9.Appearance.BackColor2 = System.Drawing.Color.White;
            formatConditionRuleExpression9.Appearance.BorderColor = System.Drawing.Color.Red;
            formatConditionRuleExpression9.Appearance.Options.HighPriority = true;
            formatConditionRuleExpression9.Appearance.Options.UseBackColor = true;
            formatConditionRuleExpression9.Appearance.Options.UseBorderColor = true;
            formatConditionRuleExpression9.Expression = "[HasPdf] = False And [RequiresPdf] = True";
            treeListFormatRule9.Rule = formatConditionRuleExpression9;
            treeListFormatRule10.ApplyToRow = true;
            treeListFormatRule10.Name = "CheckForPlant1and2";
            formatConditionRuleExpression10.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            formatConditionRuleExpression10.Appearance.BackColor2 = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            formatConditionRuleExpression10.Appearance.Options.UseBackColor = true;
            formatConditionRuleExpression10.Expression = "[PlantID] = \'Plant 1&2\'";
            treeListFormatRule10.Rule = formatConditionRuleExpression10;
            treeListFormatRule11.ApplyToRow = true;
            treeListFormatRule11.Name = "CheckForCheck";
            formatConditionRuleExpression11.Appearance.BackColor = System.Drawing.Color.Chartreuse;
            formatConditionRuleExpression11.Appearance.FontStyleDelta = System.Drawing.FontStyle.Underline;
            formatConditionRuleExpression11.Appearance.Options.HighPriority = true;
            formatConditionRuleExpression11.Appearance.Options.UseBackColor = true;
            formatConditionRuleExpression11.Appearance.Options.UseFont = true;
            formatConditionRuleExpression11.Expression = "[Notes] = \'Check\' Or [Notes] = \'check\' Or [Notes] = \'CHECK\'";
            treeListFormatRule11.Rule = formatConditionRuleExpression11;
            this.exportTreeList.FormatRules.Add(treeListFormatRule1);
            this.exportTreeList.FormatRules.Add(treeListFormatRule2);
            this.exportTreeList.FormatRules.Add(treeListFormatRule3);
            this.exportTreeList.FormatRules.Add(treeListFormatRule4);
            this.exportTreeList.FormatRules.Add(treeListFormatRule5);
            this.exportTreeList.FormatRules.Add(treeListFormatRule6);
            this.exportTreeList.FormatRules.Add(treeListFormatRule7);
            this.exportTreeList.FormatRules.Add(treeListFormatRule8);
            this.exportTreeList.FormatRules.Add(treeListFormatRule9);
            this.exportTreeList.FormatRules.Add(treeListFormatRule10);
            this.exportTreeList.FormatRules.Add(treeListFormatRule11);
            this.exportTreeList.KeyFieldName = "";
            this.exportTreeList.Location = new System.Drawing.Point(52, 46);
            this.exportTreeList.Name = "exportTreeList";
            this.exportTreeList.OptionsBehavior.PopulateServiceColumns = true;
            this.exportTreeList.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.exportTreeList.OptionsSelection.EnableAppearanceFocusedRow = false;
            this.exportTreeList.ParentFieldName = "";
            this.exportTreeList.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.repositoryItemTextEdit1,
            this.repositoryItemCheckEdit1});
            this.exportTreeList.Size = new System.Drawing.Size(843, 416);
            this.exportTreeList.TabIndex = 0;
            this.exportTreeList.CompareNodeValues += new DevExpress.XtraTreeList.CompareNodeValuesEventHandler(this.exportTreeList_CompareNodeValues);
            this.exportTreeList.PopupMenuShowing += new DevExpress.XtraTreeList.PopupMenuShowingEventHandler(this.exportTreeList_PopupMenuShowing);
            this.exportTreeList.MouseDown += new System.Windows.Forms.MouseEventHandler(this.exportTreeList_MouseDown);
            // 
            // Parent
            // 
            this.Parent.Caption = "Parent";
            this.Parent.FieldName = "Parent";
            this.Parent.Name = "Parent";
            this.Parent.Visible = true;
            this.Parent.VisibleIndex = 11;
            // 
            // Number
            // 
            this.Number.Caption = "Number";
            this.Number.FieldName = "Number";
            this.Number.Name = "Number";
            this.Number.OptionsColumn.AllowSort = false;
            this.Number.Visible = true;
            this.Number.VisibleIndex = 0;
            this.Number.Width = 150;
            // 
            // Title
            // 
            this.Title.Caption = "Title";
            this.Title.FieldName = "Title";
            this.Title.Name = "Title";
            this.Title.Width = 188;
            // 
            // ItemDescription
            // 
            this.ItemDescription.Caption = "Item Description";
            this.ItemDescription.FieldName = "ItemDescription";
            this.ItemDescription.Name = "ItemDescription";
            this.ItemDescription.OptionsColumn.AllowSort = false;
            this.ItemDescription.Visible = true;
            this.ItemDescription.VisibleIndex = 1;
            this.ItemDescription.Width = 150;
            // 
            // Category
            // 
            this.Category.Caption = "Category";
            this.Category.FieldName = "Category";
            this.Category.Name = "Category";
            this.Category.OptionsColumn.AllowSort = false;
            this.Category.Visible = true;
            this.Category.VisibleIndex = 2;
            this.Category.Width = 35;
            // 
            // Qty
            // 
            this.Qty.Caption = "Qty";
            this.Qty.FieldName = "Qty";
            this.Qty.MinWidth = 16;
            this.Qty.Name = "Qty";
            this.Qty.OptionsColumn.AllowSort = false;
            this.Qty.Visible = true;
            this.Qty.VisibleIndex = 3;
            this.Qty.Width = 16;
            // 
            // colIsStock
            // 
            this.colIsStock.Caption = "Stock";
            this.colIsStock.ColumnEdit = this.repositoryItemCheckEdit1;
            this.colIsStock.FieldName = "IsStock";
            this.colIsStock.MinWidth = 16;
            this.colIsStock.Name = "colIsStock";
            this.colIsStock.OptionsColumn.AllowSize = false;
            this.colIsStock.Visible = true;
            this.colIsStock.VisibleIndex = 8;
            this.colIsStock.Width = 16;
            // 
            // repositoryItemCheckEdit1
            // 
            this.repositoryItemCheckEdit1.AutoHeight = false;
            this.repositoryItemCheckEdit1.Name = "repositoryItemCheckEdit1";
            // 
            // colPlantID
            // 
            this.colPlantID.Caption = "Plant";
            this.colPlantID.FieldName = "PlantID";
            this.colPlantID.Name = "colPlantID";
            this.colPlantID.Visible = true;
            this.colPlantID.VisibleIndex = 9;
            // 
            // HasPdf
            // 
            this.HasPdf.Caption = "HasPdf";
            this.HasPdf.FieldName = "HasPdf";
            this.HasPdf.Name = "HasPdf";
            // 
            // requiresPDF
            // 
            this.requiresPDF.Caption = "requiresPDF";
            this.requiresPDF.FieldName = "RequiresPdf";
            this.requiresPDF.Name = "requiresPDF";
            this.requiresPDF.Visible = true;
            this.requiresPDF.VisibleIndex = 10;
            this.requiresPDF.Width = 25;
            // 
            // btnProcess
            // 
            this.btnProcess.Enabled = false;
            this.btnProcess.Location = new System.Drawing.Point(458, 27);
            this.btnProcess.Name = "btnProcess";
            this.btnProcess.Size = new System.Drawing.Size(89, 23);
            this.btnProcess.TabIndex = 1;
            this.btnProcess.Text = "Process Order";
            this.btnProcess.UseVisualStyleBackColor = true;
            this.btnProcess.Click += new System.EventHandler(this.btnProcess_Click);
            // 
            // btnLoad
            // 
            this.btnLoad.Location = new System.Drawing.Point(19, 19);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(75, 23);
            this.btnLoad.TabIndex = 1;
            this.btnLoad.Text = "Load...";
            this.btnLoad.UseVisualStyleBackColor = true;
            this.btnLoad.Click += new System.EventHandler(this.btnLoad_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 676);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1417, 22);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(48, 17);
            this.toolStripStatusLabel1.Text = "Ready...";
            // 
            // spinEditOrderQty
            // 
            this.spinEditOrderQty.Location = new System.Drawing.Point(106, 27);
            this.spinEditOrderQty.Maximum = new decimal(new int[] {
            9999999,
            0,
            0,
            0});
            this.spinEditOrderQty.Name = "spinEditOrderQty";
            this.spinEditOrderQty.Size = new System.Drawing.Size(65, 20);
            this.spinEditOrderQty.TabIndex = 3;
            this.spinEditOrderQty.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(17, 32);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(83, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Number of Units";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(182, 31);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(46, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Order #:";
            // 
            // txtBoxOrderNumber
            // 
            this.txtBoxOrderNumber.Location = new System.Drawing.Point(235, 27);
            this.txtBoxOrderNumber.Name = "txtBoxOrderNumber";
            this.txtBoxOrderNumber.Size = new System.Drawing.Size(203, 20);
            this.txtBoxOrderNumber.TabIndex = 5;
            this.txtBoxOrderNumber.Text = "Order Number";
            this.txtBoxOrderNumber.DoubleClick += new System.EventHandler(this.btnProcess_Click);
            // 
            // pdfViewer1
            // 
            this.pdfViewer1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pdfViewer1.Location = new System.Drawing.Point(925, 46);
            this.pdfViewer1.Name = "pdfViewer1";
            this.pdfViewer1.Size = new System.Drawing.Size(466, 416);
            this.pdfViewer1.TabIndex = 6;
            this.pdfViewer1.ZoomMode = DevExpress.XtraPdfViewer.PdfZoomMode.PageLevel;
            // 
            // groupBoxOutput
            // 
            this.groupBoxOutput.Controls.Add(this.button1);
            this.groupBoxOutput.Controls.Add(this.btnRemoveBatchItem);
            this.groupBoxOutput.Controls.Add(this.btnGroupSawDrawings3);
            this.groupBoxOutput.Controls.Add(this.btnRemoveOrder);
            this.groupBoxOutput.Controls.Add(this.btnFinalize);
            this.groupBoxOutput.Controls.Add(this.btnSelectOutputFolder);
            this.groupBoxOutput.Controls.Add(this.outputFolderlbl);
            this.groupBoxOutput.Controls.Add(this.textBoxOutputFolder);
            this.groupBoxOutput.Controls.Add(this.label1);
            this.groupBoxOutput.Controls.Add(this.txtBoxOrderNumber);
            this.groupBoxOutput.Controls.Add(this.btnProcess);
            this.groupBoxOutput.Controls.Add(this.label2);
            this.groupBoxOutput.Controls.Add(this.spinEditOrderQty);
            this.groupBoxOutput.Location = new System.Drawing.Point(327, 492);
            this.groupBoxOutput.Name = "groupBoxOutput";
            this.groupBoxOutput.Size = new System.Drawing.Size(563, 181);
            this.groupBoxOutput.TabIndex = 7;
            this.groupBoxOutput.TabStop = false;
            this.groupBoxOutput.Text = "Output Data";
            this.groupBoxOutput.Enter += new System.EventHandler(this.groupBoxOutput_Enter);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(334, 88);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(104, 51);
            this.button1.TabIndex = 10;
            this.button1.Text = "Group Saw Drawings Double Sided";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // btnRemoveBatchItem
            // 
            this.btnRemoveBatchItem.Location = new System.Drawing.Point(20, 146);
            this.btnRemoveBatchItem.Name = "btnRemoveBatchItem";
            this.btnRemoveBatchItem.Size = new System.Drawing.Size(151, 23);
            this.btnRemoveBatchItem.TabIndex = 10;
            this.btnRemoveBatchItem.Text = "Remove Batch Item...";
            this.btnRemoveBatchItem.UseVisualStyleBackColor = true;
            this.btnRemoveBatchItem.Click += new System.EventHandler(this.btnRemoveBatchItem_Click);
            // 
            // btnGroupSawDrawings3
            // 
            this.btnGroupSawDrawings3.Location = new System.Drawing.Point(185, 88);
            this.btnGroupSawDrawings3.Name = "btnGroupSawDrawings3";
            this.btnGroupSawDrawings3.Size = new System.Drawing.Size(121, 51);
            this.btnGroupSawDrawings3.TabIndex = 12;
            this.btnGroupSawDrawings3.Text = "Group Saw Drawings";
            this.btnGroupSawDrawings3.UseVisualStyleBackColor = true;
            this.btnGroupSawDrawings3.Click += new System.EventHandler(this.btnGroupSawDrawings3_Click);
            // 
            // btnRemoveOrder
            // 
            this.btnRemoveOrder.Location = new System.Drawing.Point(20, 117);
            this.btnRemoveOrder.Name = "btnRemoveOrder";
            this.btnRemoveOrder.Size = new System.Drawing.Size(151, 23);
            this.btnRemoveOrder.TabIndex = 10;
            this.btnRemoveOrder.Text = "Remove Order Item...";
            this.btnRemoveOrder.UseVisualStyleBackColor = true;
            this.btnRemoveOrder.Click += new System.EventHandler(this.btnRemoveOrder_Click);
            // 
            // btnFinalize
            // 
            this.btnFinalize.Location = new System.Drawing.Point(20, 88);
            this.btnFinalize.Name = "btnFinalize";
            this.btnFinalize.Size = new System.Drawing.Size(151, 23);
            this.btnFinalize.TabIndex = 9;
            this.btnFinalize.Text = "Finalize Schedule";
            this.btnFinalize.UseVisualStyleBackColor = true;
            this.btnFinalize.Click += new System.EventHandler(this.btnFinalize_Click);
            // 
            // btnSelectOutputFolder
            // 
            this.btnSelectOutputFolder.Location = new System.Drawing.Point(444, 58);
            this.btnSelectOutputFolder.Name = "btnSelectOutputFolder";
            this.btnSelectOutputFolder.Size = new System.Drawing.Size(23, 23);
            this.btnSelectOutputFolder.TabIndex = 8;
            this.btnSelectOutputFolder.Text = "...";
            this.btnSelectOutputFolder.UseVisualStyleBackColor = true;
            this.btnSelectOutputFolder.Click += new System.EventHandler(this.btnSelectOutputFolder_Click);
            // 
            // outputFolderlbl
            // 
            this.outputFolderlbl.AutoSize = true;
            this.outputFolderlbl.Location = new System.Drawing.Point(17, 62);
            this.outputFolderlbl.Name = "outputFolderlbl";
            this.outputFolderlbl.Size = new System.Drawing.Size(71, 13);
            this.outputFolderlbl.TabIndex = 7;
            this.outputFolderlbl.Text = "Output Folder";
            // 
            // textBoxOutputFolder
            // 
            this.textBoxOutputFolder.Location = new System.Drawing.Point(106, 60);
            this.textBoxOutputFolder.Name = "textBoxOutputFolder";
            this.textBoxOutputFolder.Size = new System.Drawing.Size(332, 20);
            this.textBoxOutputFolder.TabIndex = 6;
            // 
            // groupBoxInput
            // 
            this.groupBoxInput.Controls.Add(this.btnProcessBatch);
            this.groupBoxInput.Controls.Add(this.btnLoad);
            this.groupBoxInput.Location = new System.Drawing.Point(52, 492);
            this.groupBoxInput.Name = "groupBoxInput";
            this.groupBoxInput.Size = new System.Drawing.Size(228, 100);
            this.groupBoxInput.TabIndex = 8;
            this.groupBoxInput.TabStop = false;
            this.groupBoxInput.Text = "Input Data";
            // 
            // btnProcessBatch
            // 
            this.btnProcessBatch.Location = new System.Drawing.Point(19, 52);
            this.btnProcessBatch.Name = "btnProcessBatch";
            this.btnProcessBatch.Size = new System.Drawing.Size(100, 23);
            this.btnProcessBatch.TabIndex = 2;
            this.btnProcessBatch.Text = "Process Batch...";
            this.btnProcessBatch.UseVisualStyleBackColor = true;
            this.btnProcessBatch.Click += new System.EventHandler(this.btnProcessBatch_Click);
            // 
            // btnOdoo
            // 
            this.btnOdoo.Location = new System.Drawing.Point(1290, 609);
            this.btnOdoo.Name = "btnOdoo";
            this.btnOdoo.Size = new System.Drawing.Size(75, 23);
            this.btnOdoo.TabIndex = 9;
            this.btnOdoo.Text = "Odoo";
            this.btnOdoo.UseVisualStyleBackColor = true;
            this.btnOdoo.Click += new System.EventHandler(this.btnOdoo_Click);
            // 
            // btnHorstMFG
            // 
            this.btnHorstMFG.Location = new System.Drawing.Point(1258, 492);
            this.btnHorstMFG.Name = "btnHorstMFG";
            this.btnHorstMFG.Size = new System.Drawing.Size(107, 23);
            this.btnHorstMFG.TabIndex = 10;
            this.btnHorstMFG.Text = "ProductionMaster";
            this.btnHorstMFG.UseVisualStyleBackColor = true;
            this.btnHorstMFG.Click += new System.EventHandler(this.btnHorstMFG_Click);
            // 
            // colNotes
            // 
            this.colNotes.Caption = "Notes";
            this.colNotes.FieldName = "Notes";
            this.colNotes.Name = "colNotes";
            this.colNotes.OptionsColumn.AllowEdit = false;
            // 
            // radioGroup1
            // 
            this.radioGroup1.Location = new System.Drawing.Point(52, 603);
            this.radioGroup1.Name = "radioGroup1";
            this.radioGroup1.Properties.Columns = 1;
            this.radioGroup1.Properties.Items.AddRange(new DevExpress.XtraEditors.Controls.RadioGroupItem[] {
            new DevExpress.XtraEditors.Controls.RadioGroupItem(false, "Mark all items as stock", true, "Mark all items as stock"),
            new DevExpress.XtraEditors.Controls.RadioGroupItem(false, "Mark All Items as Make To Order")});
            this.radioGroup1.Size = new System.Drawing.Size(228, 61);
            this.radioGroup1.TabIndex = 11;
            this.radioGroup1.SelectedIndexChanged += new System.EventHandler(this.radioGroup1_SelectedIndexChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1417, 698);
            this.Controls.Add(this.radioGroup1);
            this.Controls.Add(this.btnOdoo);
            this.Controls.Add(this.groupBoxInput);
            this.Controls.Add(this.pdfViewer1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.exportTreeList);
            this.Controls.Add(this.groupBoxOutput);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "Vault Item Processor 2018";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemTextEdit1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.exportTreeList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemCheckEdit1)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.spinEditOrderQty)).EndInit();
            this.groupBoxOutput.ResumeLayout(false);
            this.groupBoxOutput.PerformLayout();
            this.groupBoxInput.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.behaviorManager1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.radioGroup1.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private DevExpress.XtraTreeList.TreeList exportTreeList;
        private DevExpress.XtraTreeList.Columns.TreeListColumn Title;
        private DevExpress.XtraTreeList.Columns.TreeListColumn ItemDescription;
        private DevExpress.XtraTreeList.Columns.TreeListColumn Category;
        private DevExpress.XtraTreeList.Columns.TreeListColumn Material;
        private DevExpress.XtraTreeList.Columns.TreeListColumn Qty;
        private System.Windows.Forms.Button btnProcess;
        private DevExpress.XtraTreeList.Columns.TreeListColumn Number;
        private DevExpress.XtraTreeList.Columns.TreeListColumn Parent;
        private System.Windows.Forms.Button btnLoad;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private DevExpress.XtraTreeList.Columns.TreeListColumn HasPdf;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown spinEditOrderQty;
        private System.Windows.Forms.TextBox txtBoxOrderNumber;
        private System.Windows.Forms.Label label2;
        private DevExpress.XtraPdfViewer.PdfViewer pdfViewer1;
        private DevExpress.XtraTreeList.Columns.TreeListColumn colThickness;
        private DevExpress.XtraTreeList.Columns.TreeListColumn Operations;
        private System.Windows.Forms.GroupBox groupBoxInput;
        private System.Windows.Forms.GroupBox groupBoxOutput;
        private System.Windows.Forms.Button btnSelectOutputFolder;
        private System.Windows.Forms.Label outputFolderlbl;
        private System.Windows.Forms.TextBox textBoxOutputFolder;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialogOutputFolderSelect;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit repositoryItemTextEdit1;
        private System.Windows.Forms.Button btnFinalize;
        private System.Windows.Forms.Button btnRemoveOrder;
        private DevExpress.Utils.Behaviors.BehaviorManager behaviorManager1;
        private DevExpress.XtraTreeList.Columns.TreeListColumn colStructCode;
        private DevExpress.XtraTreeList.Columns.TreeListColumn colIsStock;
        private DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit repositoryItemCheckEdit1;
        private DevExpress.XtraTreeList.Columns.TreeListColumn colPlantID;
        private System.Windows.Forms.Button btnRemoveBatchItem;
        private DevExpress.XtraTreeList.Columns.TreeListColumn requiresPDF;
        private System.Windows.Forms.Button btnProcessBatch;
        private System.Windows.Forms.Button btnOdoo;
        private System.Windows.Forms.Button btnGroupSawDrawings3;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button btnHorstMFG;
        private DevExpress.XtraTreeList.Columns.TreeListColumn colNotes;
        private DevExpress.XtraEditors.RadioGroup radioGroup1;
        //private DevExpress.XtraSplashScreen.SplashScreenManager splashScreenManager1;
        //private DevExpress.XtraSplashScreen.SplashScreenManager splashScreenManager2;
    }
}

