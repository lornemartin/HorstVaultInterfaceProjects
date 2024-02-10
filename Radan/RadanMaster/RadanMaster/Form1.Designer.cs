namespace RadanMaster
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
            DevExpress.XtraGrid.GridLevelNode gridLevelNode1 = new DevExpress.XtraGrid.GridLevelNode();
            DevExpress.XtraGrid.GridFormatRule gridFormatRule1 = new DevExpress.XtraGrid.GridFormatRule();
            DevExpress.XtraEditors.FormatConditionRuleValue formatConditionRuleValue1 = new DevExpress.XtraEditors.FormatConditionRuleValue();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.colIsInProject = new DevExpress.XtraGrid.Columns.GridColumn();
            this.gridControlItems = new DevExpress.XtraGrid.GridControl();
            this.orderItemsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.gridViewItems = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colID = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colQtyRequired = new DevExpress.XtraGrid.Columns.GridColumn();
            this.repositoryItemSpinEdit1 = new DevExpress.XtraEditors.Repository.RepositoryItemSpinEdit();
            this.colQtyNested = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colQtyRemaining = new DevExpress.XtraGrid.Columns.GridColumn();
            this.gridColumnThumb2 = new DevExpress.XtraGrid.Columns.GridColumn();
            this.repositoryItemPictureEdit1 = new DevExpress.XtraEditors.Repository.RepositoryItemPictureEdit();
            this.partNameCol = new DevExpress.XtraGrid.Columns.GridColumn();
            this.PartDescCol = new DevExpress.XtraGrid.Columns.GridColumn();
            this.thicknessCol = new DevExpress.XtraGrid.Columns.GridColumn();
            this.materialCol = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colIsComplete = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colOrderNum = new DevExpress.XtraGrid.Columns.GridColumn();
            this.gridColSchedName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.gridColBatchName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.IsBatch = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colDateEntered = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colNotes = new DevExpress.XtraGrid.Columns.GridColumn();
            this.repositoryItemMemoEdit1 = new DevExpress.XtraEditors.Repository.RepositoryItemMemoEdit();
            this.colOrderIsComplete = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colHasBends = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colDateCompleted = new DevExpress.XtraGrid.Columns.GridColumn();
            this.defaultLookAndFeel1 = new DevExpress.LookAndFeel.DefaultLookAndFeel(this.components);
            this.ribbonControl1 = new DevExpress.XtraBars.Ribbon.RibbonControl();
            this.skinRibbonGalleryBarItem1 = new DevExpress.XtraBars.SkinRibbonGalleryBarItem();
            this.barButtonItemAdd = new DevExpress.XtraBars.BarButtonItem();
            this.barButtonItemImport = new DevExpress.XtraBars.BarButtonItem();
            this.barEditRadanProject = new DevExpress.XtraBars.BarEditItem();
            this.barButtonSendSelectionToRadan = new DevExpress.XtraBars.BarButtonItem();
            this.barButtonGroup1 = new DevExpress.XtraBars.BarButtonGroup();
            this.barButtonBrowseRadanProject = new DevExpress.XtraBars.BarButtonItem();
            this.barStaticItem1 = new DevExpress.XtraBars.BarStaticItem();
            this.barEditItem1 = new DevExpress.XtraBars.BarEditItem();
            this.repositoryItemTextEdit2 = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.barEditRadanProjectBrowse = new DevExpress.XtraBars.BarEditItem();
            this.repositoryItemTextEdit1 = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.barButtonUpdateFromRadan = new DevExpress.XtraBars.BarButtonItem();
            this.barButtonRetrieveSelectionFromRadan = new DevExpress.XtraBars.BarButtonItem();
            this.barButtonNewRadanProject = new DevExpress.XtraBars.BarButtonItem();
            this.barToggleSwitchShowBatches = new DevExpress.XtraBars.BarToggleSwitchItem();
            this.barToggleSwitchShowOrders = new DevExpress.XtraBars.BarToggleSwitchItem();
            this.barToggleShowComplete = new DevExpress.XtraBars.BarToggleSwitchItem();
            this.barToggleShowRadan = new DevExpress.XtraBars.BarToggleSwitchItem();
            this.barToggleSwitchGroup1 = new DevExpress.XtraBars.BarToggleSwitchItem();
            this.barToggleSwitchGroup2 = new DevExpress.XtraBars.BarToggleSwitchItem();
            this.barButtonItem1 = new DevExpress.XtraBars.BarButtonItem();
            this.barButtonRetrieveAll = new DevExpress.XtraBars.BarButtonItem();
            this.barToggleSwitchShowCompletedOrders = new DevExpress.XtraBars.BarToggleSwitchItem();
            this.barButtonItemConnectToRadan = new DevExpress.XtraBars.BarButtonItem();
            this.barCheckItemShowAllCompletedOrders = new DevExpress.XtraBars.BarCheckItem();
            this.barCheckItemShowCompletedOrdersFromLastDayOnly = new DevExpress.XtraBars.BarCheckItem();
            this.barEditNumOfDays = new DevExpress.XtraBars.BarEditItem();
            this.repositoryItemSpinEdit4 = new DevExpress.XtraEditors.Repository.RepositoryItemSpinEdit();
            this.ribbonPage1 = new DevExpress.XtraBars.Ribbon.RibbonPage();
            this.rpgSkins = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            this.ribbonPageGroup1 = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            this.ribbonPageGroup2 = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            this.rbnPageFilters = new DevExpress.XtraBars.Ribbon.RibbonPage();
            this.ribbonPageQuickFilters = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            this.ribbonPageQuickSorts = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            this.ribbonPageCompletedOrders = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            this.repositoryItemTextEdit3 = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.repositoryItemTimeSpanEdit1 = new DevExpress.XtraEditors.Repository.RepositoryItemTimeSpanEdit();
            this.repositoryItemSpinEdit2 = new DevExpress.XtraEditors.Repository.RepositoryItemSpinEdit();
            this.repositoryItemSpinEdit3 = new DevExpress.XtraEditors.Repository.RepositoryItemSpinEdit();
            this.openFileDialogImport = new System.Windows.Forms.OpenFileDialog();
            this.openFileDialogAddItem = new System.Windows.Forms.OpenFileDialog();
            this.openFileDialogProject = new System.Windows.Forms.OpenFileDialog();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabelMain = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.behaviorManager1 = new DevExpress.Utils.Behaviors.BehaviorManager(this.components);
            this.gridControlNests = new DevExpress.XtraGrid.GridControl();
            this.gridViewNests = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.colID1 = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colThumbnail = new DevExpress.XtraGrid.Columns.GridColumn();
            this.repositoryItemPictureEdit2 = new DevExpress.XtraEditors.Repository.RepositoryItemPictureEdit();
            this.colnestName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colnestPath = new DevExpress.XtraGrid.Columns.GridColumn();
            this.colQtyOnNest = new DevExpress.XtraGrid.Columns.GridColumn();
            this.nestsBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.splitContainerControl1 = new DevExpress.XtraEditors.SplitContainerControl();
            this.progressPanel1 = new DevExpress.XtraWaitForm.ProgressPanel();
            this.displayItemBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.entityCommand1 = new System.Data.Entity.Core.EntityClient.EntityCommand();
            ((System.ComponentModel.ISupportInitialize)(this.gridControlItems)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.orderItemsBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewItems)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemSpinEdit1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemPictureEdit1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemMemoEdit1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ribbonControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemTextEdit2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemTextEdit1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemSpinEdit4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemTextEdit3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemTimeSpanEdit1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemSpinEdit2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemSpinEdit3)).BeginInit();
            this.statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.behaviorManager1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridControlNests)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewNests)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemPictureEdit2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nestsBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1)).BeginInit();
            this.splitContainerControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.displayItemBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // splashScreenManager1
            // 
            splashScreenManager1.ClosingDelay = 500;
            // 
            // colIsInProject
            // 
            this.colIsInProject.Caption = "In Radan Project?";
            this.colIsInProject.FieldName = "IsInProject";
            this.colIsInProject.Name = "colIsInProject";
            this.colIsInProject.OptionsColumn.AllowEdit = false;
            this.colIsInProject.Visible = true;
            this.colIsInProject.VisibleIndex = 15;
            this.colIsInProject.Width = 33;
            // 
            // gridControlItems
            // 
            this.gridControlItems.AllowDrop = true;
            this.gridControlItems.DataSource = this.orderItemsBindingSource;
            this.gridControlItems.Dock = System.Windows.Forms.DockStyle.Fill;
            gridLevelNode1.RelationName = "Level1";
            this.gridControlItems.LevelTree.Nodes.AddRange(new DevExpress.XtraGrid.GridLevelNode[] {
            gridLevelNode1});
            this.gridControlItems.Location = new System.Drawing.Point(0, 0);
            this.gridControlItems.MainView = this.gridViewItems;
            this.gridControlItems.Name = "gridControlItems";
            this.gridControlItems.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.repositoryItemPictureEdit1,
            this.repositoryItemSpinEdit1,
            this.repositoryItemMemoEdit1});
            this.gridControlItems.Size = new System.Drawing.Size(1600, 571);
            this.gridControlItems.TabIndex = 0;
            this.gridControlItems.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridViewItems});
            this.gridControlItems.DragDrop += new System.Windows.Forms.DragEventHandler(this.gridControlItems_DragDrop);
            this.gridControlItems.DragOver += new System.Windows.Forms.DragEventHandler(this.gridControlItems_DragOver);
            this.gridControlItems.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.gridControlItems_MouseDoubleClick);
            // 
            // gridViewItems
            // 
            this.gridViewItems.Appearance.FocusedRow.BorderColor = System.Drawing.Color.Red;
            this.gridViewItems.Appearance.FocusedRow.Options.UseBorderColor = true;
            this.gridViewItems.Appearance.SelectedRow.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            this.gridViewItems.Appearance.SelectedRow.Options.UseBackColor = true;
            this.gridViewItems.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colID,
            this.colQtyRequired,
            this.colQtyNested,
            this.colQtyRemaining,
            this.gridColumnThumb2,
            this.partNameCol,
            this.PartDescCol,
            this.thicknessCol,
            this.materialCol,
            this.colIsComplete,
            this.colOrderNum,
            this.gridColSchedName,
            this.gridColBatchName,
            this.IsBatch,
            this.colDateEntered,
            this.colIsInProject,
            this.colNotes,
            this.colOrderIsComplete,
            this.colHasBends,
            this.colDateCompleted});
            gridFormatRule1.ApplyToRow = true;
            gridFormatRule1.Column = this.colIsInProject;
            gridFormatRule1.Name = "frmtIsInProject";
            formatConditionRuleValue1.Appearance.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            formatConditionRuleValue1.Appearance.Options.UseFont = true;
            formatConditionRuleValue1.Condition = DevExpress.XtraEditors.FormatCondition.Equal;
            formatConditionRuleValue1.Value1 = true;
            gridFormatRule1.Rule = formatConditionRuleValue1;
            this.gridViewItems.FormatRules.Add(gridFormatRule1);
            this.gridViewItems.GridControl = this.gridControlItems;
            this.gridViewItems.Name = "gridViewItems";
            this.gridViewItems.OptionsEditForm.ShowOnF2Key = DevExpress.Utils.DefaultBoolean.True;
            this.gridViewItems.OptionsLayout.StoreAppearance = true;
            this.gridViewItems.OptionsSelection.MultiSelect = true;
            this.gridViewItems.OptionsView.RowAutoHeight = true;
            this.gridViewItems.RowClick += new DevExpress.XtraGrid.Views.Grid.RowClickEventHandler(this.gridViewItems_RowClick);
            this.gridViewItems.CustomDrawGroupRow += new DevExpress.XtraGrid.Views.Base.RowObjectCustomDrawEventHandler(this.gridViewItems_CustomDrawGroupRow);
            this.gridViewItems.RowCellStyle += new DevExpress.XtraGrid.Views.Grid.RowCellStyleEventHandler(this.gridViewItems_RowCellStyle);
            this.gridViewItems.PopupMenuShowing += new DevExpress.XtraGrid.Views.Grid.PopupMenuShowingEventHandler(this.gridViewItems_PopupMenuShowing);
            this.gridViewItems.HiddenEditor += new System.EventHandler(this.gridViewItems_HiddenEditor);
            this.gridViewItems.RowDeleted += new DevExpress.Data.RowDeletedEventHandler(this.gridView1_RowDeleted);
            this.gridViewItems.RowUpdated += new DevExpress.XtraGrid.Views.Base.RowObjectEventHandler(this.gridView1_RowUpdated);
            this.gridViewItems.CustomUnboundColumnData += new DevExpress.XtraGrid.Views.Base.CustomColumnDataEventHandler(this.gridViewItems_CustomUnboundColumnData);
            this.gridViewItems.KeyDown += new System.Windows.Forms.KeyEventHandler(this.gridView1_KeyDown);
            // 
            // colID
            // 
            this.colID.FieldName = "ID";
            this.colID.Name = "colID";
            // 
            // colQtyRequired
            // 
            this.colQtyRequired.ColumnEdit = this.repositoryItemSpinEdit1;
            this.colQtyRequired.FieldName = "QtyRequired";
            this.colQtyRequired.Name = "colQtyRequired";
            this.colQtyRequired.Visible = true;
            this.colQtyRequired.VisibleIndex = 0;
            this.colQtyRequired.Width = 50;
            // 
            // repositoryItemSpinEdit1
            // 
            this.repositoryItemSpinEdit1.AutoHeight = false;
            this.repositoryItemSpinEdit1.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.repositoryItemSpinEdit1.Name = "repositoryItemSpinEdit1";
            // 
            // colQtyNested
            // 
            this.colQtyNested.Caption = "Qty Nested";
            this.colQtyNested.FieldName = "calcQtyNested";
            this.colQtyNested.Name = "colQtyNested";
            this.colQtyNested.OptionsColumn.AllowEdit = false;
            this.colQtyNested.OptionsColumn.ReadOnly = true;
            this.colQtyNested.UnboundType = DevExpress.Data.UnboundColumnType.Integer;
            this.colQtyNested.Visible = true;
            this.colQtyNested.VisibleIndex = 1;
            this.colQtyNested.Width = 49;
            // 
            // colQtyRemaining
            // 
            this.colQtyRemaining.Caption = "Qty Remaining";
            this.colQtyRemaining.FieldName = "calcRemaining";
            this.colQtyRemaining.Name = "colQtyRemaining";
            this.colQtyRemaining.OptionsColumn.AllowEdit = false;
            this.colQtyRemaining.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            this.colQtyRemaining.UnboundType = DevExpress.Data.UnboundColumnType.Integer;
            this.colQtyRemaining.Visible = true;
            this.colQtyRemaining.VisibleIndex = 2;
            // 
            // gridColumnThumb2
            // 
            this.gridColumnThumb2.Caption = "Thumbnail";
            this.gridColumnThumb2.ColumnEdit = this.repositoryItemPictureEdit1;
            this.gridColumnThumb2.FieldName = "Part.Thumbnail";
            this.gridColumnThumb2.Name = "gridColumnThumb2";
            this.gridColumnThumb2.OptionsColumn.AllowEdit = false;
            this.gridColumnThumb2.Visible = true;
            this.gridColumnThumb2.VisibleIndex = 3;
            this.gridColumnThumb2.Width = 62;
            // 
            // repositoryItemPictureEdit1
            // 
            this.repositoryItemPictureEdit1.Name = "repositoryItemPictureEdit1";
            this.repositoryItemPictureEdit1.PictureStoreMode = DevExpress.XtraEditors.Controls.PictureStoreMode.ByteArray;
            this.repositoryItemPictureEdit1.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Zoom;
            // 
            // partNameCol
            // 
            this.partNameCol.Caption = "Part Name";
            this.partNameCol.FieldName = "Part.FileName";
            this.partNameCol.Name = "partNameCol";
            this.partNameCol.OptionsColumn.AllowEdit = false;
            this.partNameCol.Visible = true;
            this.partNameCol.VisibleIndex = 4;
            this.partNameCol.Width = 245;
            // 
            // PartDescCol
            // 
            this.PartDescCol.Caption = "Description";
            this.PartDescCol.FieldName = "Part.Description";
            this.PartDescCol.Name = "PartDescCol";
            this.PartDescCol.OptionsColumn.AllowEdit = false;
            this.PartDescCol.Visible = true;
            this.PartDescCol.VisibleIndex = 6;
            this.PartDescCol.Width = 86;
            // 
            // thicknessCol
            // 
            this.thicknessCol.Caption = "Thickness";
            this.thicknessCol.FieldName = "Part.Thickness";
            this.thicknessCol.Name = "thicknessCol";
            this.thicknessCol.Visible = true;
            this.thicknessCol.VisibleIndex = 5;
            this.thicknessCol.Width = 86;
            // 
            // materialCol
            // 
            this.materialCol.Caption = "Material";
            this.materialCol.FieldName = "Part.Material";
            this.materialCol.Name = "materialCol";
            this.materialCol.Visible = true;
            this.materialCol.VisibleIndex = 7;
            this.materialCol.Width = 86;
            // 
            // colIsComplete
            // 
            this.colIsComplete.FieldName = "IsComplete";
            this.colIsComplete.Name = "colIsComplete";
            this.colIsComplete.Visible = true;
            this.colIsComplete.VisibleIndex = 8;
            this.colIsComplete.Width = 61;
            // 
            // colOrderNum
            // 
            this.colOrderNum.Caption = "Order Number";
            this.colOrderNum.FieldName = "Order.OrderNumber";
            this.colOrderNum.Name = "colOrderNum";
            this.colOrderNum.OptionsColumn.AllowEdit = false;
            this.colOrderNum.Visible = true;
            this.colOrderNum.VisibleIndex = 12;
            this.colOrderNum.Width = 122;
            // 
            // gridColSchedName
            // 
            this.gridColSchedName.Caption = "Schedule";
            this.gridColSchedName.FieldName = "Order.ScheduleName";
            this.gridColSchedName.Name = "gridColSchedName";
            this.gridColSchedName.OptionsColumn.AllowEdit = false;
            this.gridColSchedName.Visible = true;
            this.gridColSchedName.VisibleIndex = 10;
            this.gridColSchedName.Width = 163;
            // 
            // gridColBatchName
            // 
            this.gridColBatchName.Caption = "Batch Name";
            this.gridColBatchName.FieldName = "Order.BatchName";
            this.gridColBatchName.Name = "gridColBatchName";
            this.gridColBatchName.OptionsColumn.AllowEdit = false;
            this.gridColBatchName.Visible = true;
            this.gridColBatchName.VisibleIndex = 11;
            this.gridColBatchName.Width = 158;
            // 
            // IsBatch
            // 
            this.IsBatch.Caption = "Is Batch";
            this.IsBatch.FieldName = "Order.IsBatch";
            this.IsBatch.Name = "IsBatch";
            this.IsBatch.OptionsColumn.AllowEdit = false;
            this.IsBatch.Visible = true;
            this.IsBatch.VisibleIndex = 9;
            this.IsBatch.Width = 47;
            // 
            // colDateEntered
            // 
            this.colDateEntered.Caption = "Date Entered";
            this.colDateEntered.DisplayFormat.FormatString = "g";
            this.colDateEntered.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.colDateEntered.FieldName = "Order.EntryDate";
            this.colDateEntered.Name = "colDateEntered";
            this.colDateEntered.Visible = true;
            this.colDateEntered.VisibleIndex = 13;
            this.colDateEntered.Width = 96;
            // 
            // colNotes
            // 
            this.colNotes.Caption = "Notes";
            this.colNotes.ColumnEdit = this.repositoryItemMemoEdit1;
            this.colNotes.FieldName = "Notes";
            this.colNotes.Name = "colNotes";
            this.colNotes.Visible = true;
            this.colNotes.VisibleIndex = 14;
            this.colNotes.Width = 238;
            // 
            // repositoryItemMemoEdit1
            // 
            this.repositoryItemMemoEdit1.AcceptsReturn = false;
            this.repositoryItemMemoEdit1.Name = "repositoryItemMemoEdit1";
            // 
            // colOrderIsComplete
            // 
            this.colOrderIsComplete.Caption = "Order Is Complete";
            this.colOrderIsComplete.FieldName = "Order.IsComplete";
            this.colOrderIsComplete.Name = "colOrderIsComplete";
            this.colOrderIsComplete.Visible = true;
            this.colOrderIsComplete.VisibleIndex = 16;
            // 
            // colHasBends
            // 
            this.colHasBends.Caption = "Has Bends";
            this.colHasBends.FieldName = "Part.HasBends";
            this.colHasBends.Name = "colHasBends";
            this.colHasBends.Visible = true;
            this.colHasBends.VisibleIndex = 17;
            // 
            // colDateCompleted
            // 
            this.colDateCompleted.Caption = "Date Completed";
            this.colDateCompleted.FieldName = "Order.DateCompleted";
            this.colDateCompleted.Name = "colDateCompleted";
            this.colDateCompleted.Visible = true;
            this.colDateCompleted.VisibleIndex = 18;
            // 
            // ribbonControl1
            // 
            this.ribbonControl1.ExpandCollapseItem.Id = 0;
            this.ribbonControl1.Items.AddRange(new DevExpress.XtraBars.BarItem[] {
            this.ribbonControl1.ExpandCollapseItem,
            this.ribbonControl1.SearchEditItem,
            this.skinRibbonGalleryBarItem1,
            this.barButtonItemAdd,
            this.barButtonItemImport,
            this.barEditRadanProject,
            this.barButtonSendSelectionToRadan,
            this.barButtonGroup1,
            this.barButtonBrowseRadanProject,
            this.barStaticItem1,
            this.barEditItem1,
            this.barEditRadanProjectBrowse,
            this.barButtonUpdateFromRadan,
            this.barButtonRetrieveSelectionFromRadan,
            this.barButtonNewRadanProject,
            this.barToggleSwitchShowBatches,
            this.barToggleSwitchShowOrders,
            this.barToggleShowComplete,
            this.barToggleShowRadan,
            this.barToggleSwitchGroup1,
            this.barToggleSwitchGroup2,
            this.barButtonItem1,
            this.barButtonRetrieveAll,
            this.barToggleSwitchShowCompletedOrders,
            this.barButtonItemConnectToRadan,
            this.barCheckItemShowAllCompletedOrders,
            this.barCheckItemShowCompletedOrdersFromLastDayOnly,
            this.barEditNumOfDays});
            this.ribbonControl1.Location = new System.Drawing.Point(0, 0);
            this.ribbonControl1.MaxItemId = 11;
            this.ribbonControl1.Name = "ribbonControl1";
            this.ribbonControl1.Pages.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPage[] {
            this.ribbonPage1,
            this.rbnPageFilters});
            this.ribbonControl1.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.repositoryItemTextEdit1,
            this.repositoryItemTextEdit2,
            this.repositoryItemTextEdit3,
            this.repositoryItemTimeSpanEdit1,
            this.repositoryItemSpinEdit2,
            this.repositoryItemSpinEdit3,
            this.repositoryItemSpinEdit4});
            this.ribbonControl1.Size = new System.Drawing.Size(1600, 158);
            // 
            // skinRibbonGalleryBarItem1
            // 
            this.skinRibbonGalleryBarItem1.Caption = "skinRibbonGalleryBarItem1";
            this.skinRibbonGalleryBarItem1.Id = 1;
            this.skinRibbonGalleryBarItem1.Name = "skinRibbonGalleryBarItem1";
            // 
            // barButtonItemAdd
            // 
            this.barButtonItemAdd.Caption = "Add...";
            this.barButtonItemAdd.Hint = "Add an item to the list";
            this.barButtonItemAdd.Id = 2;
            this.barButtonItemAdd.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("barButtonItemAdd.ImageOptions.Image")));
            this.barButtonItemAdd.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("barButtonItemAdd.ImageOptions.LargeImage")));
            this.barButtonItemAdd.Name = "barButtonItemAdd";
            this.barButtonItemAdd.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            this.barButtonItemAdd.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barButtonItemAdd_ItemClick);
            // 
            // barButtonItemImport
            // 
            this.barButtonItemImport.Caption = "Import...";
            this.barButtonItemImport.Hint = "Import a list of items from an xml file.";
            this.barButtonItemImport.Id = 3;
            this.barButtonItemImport.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("barButtonItemImport.ImageOptions.Image")));
            this.barButtonItemImport.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("barButtonItemImport.ImageOptions.LargeImage")));
            this.barButtonItemImport.Name = "barButtonItemImport";
            this.barButtonItemImport.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barButtonItemImport_ItemClick);
            // 
            // barEditRadanProject
            // 
            this.barEditRadanProject.Edit = null;
            this.barEditRadanProject.EditHeight = 50;
            this.barEditRadanProject.EditValue = "Select Radan Project File";
            this.barEditRadanProject.EditWidth = 30;
            this.barEditRadanProject.Id = 9;
            this.barEditRadanProject.Name = "barEditRadanProject";
            this.barEditRadanProject.VisibleWhenVertical = true;
            // 
            // barButtonSendSelectionToRadan
            // 
            this.barButtonSendSelectionToRadan.Caption = "Send Selection";
            this.barButtonSendSelectionToRadan.Hint = "Send the selected items to the Radan project";
            this.barButtonSendSelectionToRadan.Id = 5;
            this.barButtonSendSelectionToRadan.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("barButtonSendSelectionToRadan.ImageOptions.Image")));
            this.barButtonSendSelectionToRadan.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("barButtonSendSelectionToRadan.ImageOptions.LargeImage")));
            this.barButtonSendSelectionToRadan.LargeWidth = 80;
            this.barButtonSendSelectionToRadan.Name = "barButtonSendSelectionToRadan";
            this.barButtonSendSelectionToRadan.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barButtonSendSelectionToRadan_ItemClick);
            // 
            // barButtonGroup1
            // 
            this.barButtonGroup1.Caption = "barButtonGroup1";
            this.barButtonGroup1.Id = 6;
            this.barButtonGroup1.Name = "barButtonGroup1";
            // 
            // barButtonBrowseRadanProject
            // 
            this.barButtonBrowseRadanProject.Caption = "Browse";
            this.barButtonBrowseRadanProject.Hint = "Browse For Radan Project";
            this.barButtonBrowseRadanProject.Id = 7;
            this.barButtonBrowseRadanProject.Name = "barButtonBrowseRadanProject";
            this.barButtonBrowseRadanProject.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barButtonBrowseRadanProject_ItemClick);
            // 
            // barStaticItem1
            // 
            this.barStaticItem1.Caption = "barStaticItem1";
            this.barStaticItem1.Id = 8;
            this.barStaticItem1.Name = "barStaticItem1";
            // 
            // barEditItem1
            // 
            this.barEditItem1.Caption = "barEditItem1";
            this.barEditItem1.Edit = this.repositoryItemTextEdit2;
            this.barEditItem1.Id = 10;
            this.barEditItem1.Name = "barEditItem1";
            // 
            // repositoryItemTextEdit2
            // 
            this.repositoryItemTextEdit2.AutoHeight = false;
            this.repositoryItemTextEdit2.Name = "repositoryItemTextEdit2";
            // 
            // barEditRadanProjectBrowse
            // 
            this.barEditRadanProjectBrowse.Caption = "Radan Project";
            this.barEditRadanProjectBrowse.Edit = this.repositoryItemTextEdit1;
            this.barEditRadanProjectBrowse.EditWidth = 300;
            this.barEditRadanProjectBrowse.Id = 11;
            this.barEditRadanProjectBrowse.Name = "barEditRadanProjectBrowse";
            this.barEditRadanProjectBrowse.EditValueChanged += new System.EventHandler(this.barEditRadanProjectBrowse_EditValueChanged);
            // 
            // repositoryItemTextEdit1
            // 
            this.repositoryItemTextEdit1.AutoHeight = false;
            this.repositoryItemTextEdit1.Name = "repositoryItemTextEdit1";
            // 
            // barButtonUpdateFromRadan
            // 
            this.barButtonUpdateFromRadan.Caption = "Update From Radan";
            this.barButtonUpdateFromRadan.Hint = "Update the master list from the Radan project";
            this.barButtonUpdateFromRadan.Id = 12;
            this.barButtonUpdateFromRadan.ImageOptions.Image = global::RadanMaster.Properties.Resources.refreshallpivottable_16x16;
            this.barButtonUpdateFromRadan.ImageOptions.LargeImage = global::RadanMaster.Properties.Resources.refreshallpivottable_32x321;
            this.barButtonUpdateFromRadan.LargeWidth = 80;
            this.barButtonUpdateFromRadan.Name = "barButtonUpdateFromRadan";
            this.barButtonUpdateFromRadan.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barButtonUpdateFromRadan_ItemClick);
            // 
            // barButtonRetrieveSelectionFromRadan
            // 
            this.barButtonRetrieveSelectionFromRadan.Caption = "Retrieve Selection";
            this.barButtonRetrieveSelectionFromRadan.Hint = "Retrieve the selected items From the Radan project";
            this.barButtonRetrieveSelectionFromRadan.Id = 13;
            this.barButtonRetrieveSelectionFromRadan.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("barButtonRetrieveSelectionFromRadan.ImageOptions.Image")));
            this.barButtonRetrieveSelectionFromRadan.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("barButtonRetrieveSelectionFromRadan.ImageOptions.LargeImage")));
            this.barButtonRetrieveSelectionFromRadan.LargeWidth = 80;
            this.barButtonRetrieveSelectionFromRadan.Name = "barButtonRetrieveSelectionFromRadan";
            this.barButtonRetrieveSelectionFromRadan.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barButtonRetrieveSelectionFromRadan_ItemClick);
            // 
            // barButtonNewRadanProject
            // 
            this.barButtonNewRadanProject.Caption = "New Radan Project";
            this.barButtonNewRadanProject.Hint = "Create New Radan Project";
            this.barButtonNewRadanProject.Id = 14;
            this.barButtonNewRadanProject.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("barButtonNewRadanProject.ImageOptions.Image")));
            this.barButtonNewRadanProject.Name = "barButtonNewRadanProject";
            this.barButtonNewRadanProject.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            this.barButtonNewRadanProject.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barButtonItem1_ItemClick);
            // 
            // barToggleSwitchShowBatches
            // 
            this.barToggleSwitchShowBatches.BindableChecked = true;
            this.barToggleSwitchShowBatches.Caption = "Show Batches";
            this.barToggleSwitchShowBatches.Checked = true;
            this.barToggleSwitchShowBatches.Id = 15;
            this.barToggleSwitchShowBatches.Name = "barToggleSwitchShowBatches";
            this.barToggleSwitchShowBatches.CheckedChanged += new DevExpress.XtraBars.ItemClickEventHandler(this.barToggleSwitchShowBatches_CheckedChanged);
            // 
            // barToggleSwitchShowOrders
            // 
            this.barToggleSwitchShowOrders.BindableChecked = true;
            this.barToggleSwitchShowOrders.Caption = "Show Orders";
            this.barToggleSwitchShowOrders.Checked = true;
            this.barToggleSwitchShowOrders.Id = 17;
            this.barToggleSwitchShowOrders.Name = "barToggleSwitchShowOrders";
            this.barToggleSwitchShowOrders.CheckedChanged += new DevExpress.XtraBars.ItemClickEventHandler(this.barToggleSwitchShowOrders_CheckedChanged);
            // 
            // barToggleShowComplete
            // 
            this.barToggleShowComplete.BindableChecked = true;
            this.barToggleShowComplete.Caption = "Show Complete";
            this.barToggleShowComplete.Checked = true;
            this.barToggleShowComplete.Id = 18;
            this.barToggleShowComplete.Name = "barToggleShowComplete";
            this.barToggleShowComplete.CheckedChanged += new DevExpress.XtraBars.ItemClickEventHandler(this.barToggleShowComplete_CheckedChanged);
            // 
            // barToggleShowRadan
            // 
            this.barToggleShowRadan.Caption = "Show Only Items In Radan";
            this.barToggleShowRadan.Id = 19;
            this.barToggleShowRadan.Name = "barToggleShowRadan";
            this.barToggleShowRadan.CheckedChanged += new DevExpress.XtraBars.ItemClickEventHandler(this.barToggleShowRadan_CheckedChanged);
            // 
            // barToggleSwitchGroup1
            // 
            this.barToggleSwitchGroup1.Caption = "Group By Batch and Thickness";
            this.barToggleSwitchGroup1.Id = 20;
            this.barToggleSwitchGroup1.Name = "barToggleSwitchGroup1";
            this.barToggleSwitchGroup1.CheckedChanged += new DevExpress.XtraBars.ItemClickEventHandler(this.barToggleSwitchGroup1_CheckedChanged);
            // 
            // barToggleSwitchGroup2
            // 
            this.barToggleSwitchGroup2.Caption = "Group By Schedule and Thickness";
            this.barToggleSwitchGroup2.Id = 21;
            this.barToggleSwitchGroup2.Name = "barToggleSwitchGroup2";
            this.barToggleSwitchGroup2.CheckedChanged += new DevExpress.XtraBars.ItemClickEventHandler(this.barToggleSwitchGroup2_CheckedChanged);
            // 
            // barButtonItem1
            // 
            this.barButtonItem1.Caption = "Update Symbol";
            this.barButtonItem1.Id = 22;
            this.barButtonItem1.Name = "barButtonItem1";
            // 
            // barButtonRetrieveAll
            // 
            this.barButtonRetrieveAll.Caption = "Retrieve All";
            this.barButtonRetrieveAll.Hint = "Retrieve all unNested parts from the Radan project";
            this.barButtonRetrieveAll.Id = 1;
            this.barButtonRetrieveAll.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("barButtonRetrieveAll.ImageOptions.Image")));
            this.barButtonRetrieveAll.Name = "barButtonRetrieveAll";
            this.barButtonRetrieveAll.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            this.barButtonRetrieveAll.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barButtonRetrieveAll_ItemClick);
            // 
            // barToggleSwitchShowCompletedOrders
            // 
            this.barToggleSwitchShowCompletedOrders.BindableChecked = true;
            this.barToggleSwitchShowCompletedOrders.Caption = "Show Completed Orders";
            this.barToggleSwitchShowCompletedOrders.Checked = true;
            this.barToggleSwitchShowCompletedOrders.Hint = "Turn this toggle on to show all completed orders and schedules";
            this.barToggleSwitchShowCompletedOrders.Id = 3;
            this.barToggleSwitchShowCompletedOrders.Name = "barToggleSwitchShowCompletedOrders";
            this.barToggleSwitchShowCompletedOrders.CheckedChanged += new DevExpress.XtraBars.ItemClickEventHandler(this.barToggleSwitchShowCompletedOrders_CheckedChanged);
            // 
            // barButtonItemConnectToRadan
            // 
            this.barButtonItemConnectToRadan.Caption = "Connect To Radan";
            this.barButtonItemConnectToRadan.Hint = "Connect To Running Instance of Radan";
            this.barButtonItemConnectToRadan.Id = 4;
            this.barButtonItemConnectToRadan.ImageOptions.Image = global::RadanMaster.Properties.Resources.radan;
            this.barButtonItemConnectToRadan.Name = "barButtonItemConnectToRadan";
            this.barButtonItemConnectToRadan.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonItemStyles.Large;
            this.barButtonItemConnectToRadan.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barButtonItemConnectToRadan_ItemClick);
            // 
            // barCheckItemShowAllCompletedOrders
            // 
            this.barCheckItemShowAllCompletedOrders.BindableChecked = true;
            this.barCheckItemShowAllCompletedOrders.Caption = "Show All Completed Orders";
            this.barCheckItemShowAllCompletedOrders.CheckBoxVisibility = DevExpress.XtraBars.CheckBoxVisibility.BeforeText;
            this.barCheckItemShowAllCompletedOrders.Checked = true;
            this.barCheckItemShowAllCompletedOrders.CheckStyle = DevExpress.XtraBars.BarCheckStyles.Radio;
            this.barCheckItemShowAllCompletedOrders.Id = 5;
            this.barCheckItemShowAllCompletedOrders.Name = "barCheckItemShowAllCompletedOrders";
            this.barCheckItemShowAllCompletedOrders.CheckedChanged += new DevExpress.XtraBars.ItemClickEventHandler(this.barCheckItemShowAllCompletedOrders_CheckedChanged);
            // 
            // barCheckItemShowCompletedOrdersFromLastDayOnly
            // 
            this.barCheckItemShowCompletedOrdersFromLastDayOnly.Caption = "Show Completed Orders From Last N Days";
            this.barCheckItemShowCompletedOrdersFromLastDayOnly.CheckBoxVisibility = DevExpress.XtraBars.CheckBoxVisibility.BeforeText;
            this.barCheckItemShowCompletedOrdersFromLastDayOnly.CheckStyle = DevExpress.XtraBars.BarCheckStyles.Radio;
            this.barCheckItemShowCompletedOrdersFromLastDayOnly.Id = 6;
            this.barCheckItemShowCompletedOrdersFromLastDayOnly.Name = "barCheckItemShowCompletedOrdersFromLastDayOnly";
            this.barCheckItemShowCompletedOrdersFromLastDayOnly.CheckedChanged += new DevExpress.XtraBars.ItemClickEventHandler(this.barCheckItemShowCompletedOrdersFromLastDayOnly_CheckedChanged);
            // 
            // barEditNumOfDays
            // 
            this.barEditNumOfDays.Caption = "Number of days";
            this.barEditNumOfDays.Edit = this.repositoryItemSpinEdit4;
            this.barEditNumOfDays.EditValue = 1;
            this.barEditNumOfDays.EditWidth = 100;
            this.barEditNumOfDays.Id = 8;
            this.barEditNumOfDays.ItemClickFireMode = DevExpress.XtraBars.BarItemEventFireMode.Immediate;
            this.barEditNumOfDays.Name = "barEditNumOfDays";
            this.barEditNumOfDays.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barEditNumOfDays_ItemClick);
            // 
            // repositoryItemSpinEdit4
            // 
            this.repositoryItemSpinEdit4.AutoHeight = false;
            this.repositoryItemSpinEdit4.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.repositoryItemSpinEdit4.IsFloatValue = false;
            this.repositoryItemSpinEdit4.Mask.EditMask = "N00";
            this.repositoryItemSpinEdit4.MaxValue = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.repositoryItemSpinEdit4.Name = "repositoryItemSpinEdit4";
            this.repositoryItemSpinEdit4.EditValueChanged += new System.EventHandler(this.repositoryItemSpinEdit4_EditValueChanged);
            // 
            // ribbonPage1
            // 
            this.ribbonPage1.Groups.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPageGroup[] {
            this.rpgSkins,
            this.ribbonPageGroup1,
            this.ribbonPageGroup2});
            this.ribbonPage1.Name = "ribbonPage1";
            this.ribbonPage1.Text = "Home";
            // 
            // rpgSkins
            // 
            this.rpgSkins.Enabled = false;
            this.rpgSkins.ItemLinks.Add(this.skinRibbonGalleryBarItem1);
            this.rpgSkins.Name = "rpgSkins";
            this.rpgSkins.Text = "Skins";
            this.rpgSkins.Visible = false;
            // 
            // ribbonPageGroup1
            // 
            this.ribbonPageGroup1.ItemLinks.Add(this.barButtonItemAdd);
            this.ribbonPageGroup1.ItemLinks.Add(this.barButtonItemImport);
            this.ribbonPageGroup1.ItemsLayout = DevExpress.XtraBars.Ribbon.RibbonPageGroupItemsLayout.OneRow;
            this.ribbonPageGroup1.Name = "ribbonPageGroup1";
            this.ribbonPageGroup1.Text = "Items";
            // 
            // ribbonPageGroup2
            // 
            this.ribbonPageGroup2.ItemLinks.Add(this.barEditRadanProject);
            this.ribbonPageGroup2.ItemLinks.Add(this.barEditRadanProjectBrowse);
            this.ribbonPageGroup2.ItemLinks.Add(this.barButtonBrowseRadanProject);
            this.ribbonPageGroup2.ItemLinks.Add(this.barButtonSendSelectionToRadan);
            this.ribbonPageGroup2.ItemLinks.Add(this.barButtonGroup1);
            this.ribbonPageGroup2.ItemLinks.Add(this.barButtonRetrieveSelectionFromRadan);
            this.ribbonPageGroup2.ItemLinks.Add(this.barButtonUpdateFromRadan);
            this.ribbonPageGroup2.ItemLinks.Add(this.barButtonNewRadanProject);
            this.ribbonPageGroup2.ItemLinks.Add(this.barButtonRetrieveAll);
            this.ribbonPageGroup2.ItemLinks.Add(this.barButtonItemConnectToRadan);
            this.ribbonPageGroup2.ItemsLayout = DevExpress.XtraBars.Ribbon.RibbonPageGroupItemsLayout.OneRow;
            this.ribbonPageGroup2.Name = "ribbonPageGroup2";
            this.ribbonPageGroup2.Text = "Radan";
            // 
            // rbnPageFilters
            // 
            this.rbnPageFilters.Groups.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPageGroup[] {
            this.ribbonPageQuickFilters,
            this.ribbonPageQuickSorts,
            this.ribbonPageCompletedOrders});
            this.rbnPageFilters.Name = "rbnPageFilters";
            this.rbnPageFilters.Text = "Quick Filters & Groups";
            // 
            // ribbonPageQuickFilters
            // 
            this.ribbonPageQuickFilters.ItemLinks.Add(this.barToggleSwitchShowBatches);
            this.ribbonPageQuickFilters.ItemLinks.Add(this.barToggleSwitchShowOrders);
            this.ribbonPageQuickFilters.ItemLinks.Add(this.barToggleShowComplete);
            this.ribbonPageQuickFilters.ItemLinks.Add(this.barToggleShowRadan);
            this.ribbonPageQuickFilters.ItemsLayout = DevExpress.XtraBars.Ribbon.RibbonPageGroupItemsLayout.TwoRows;
            this.ribbonPageQuickFilters.Name = "ribbonPageQuickFilters";
            this.ribbonPageQuickFilters.Text = "Quick Filters";
            // 
            // ribbonPageQuickSorts
            // 
            this.ribbonPageQuickSorts.ItemLinks.Add(this.barToggleSwitchGroup1);
            this.ribbonPageQuickSorts.ItemLinks.Add(this.barToggleSwitchGroup2);
            this.ribbonPageQuickSorts.Name = "ribbonPageQuickSorts";
            this.ribbonPageQuickSorts.Text = "Quick Groups";
            // 
            // ribbonPageCompletedOrders
            // 
            this.ribbonPageCompletedOrders.ItemLinks.Add(this.barCheckItemShowAllCompletedOrders);
            this.ribbonPageCompletedOrders.ItemLinks.Add(this.barCheckItemShowCompletedOrdersFromLastDayOnly);
            this.ribbonPageCompletedOrders.ItemLinks.Add(this.barEditNumOfDays);
            this.ribbonPageCompletedOrders.Name = "ribbonPageCompletedOrders";
            this.ribbonPageCompletedOrders.Text = "Completed Orders";
            // 
            // repositoryItemTextEdit3
            // 
            this.repositoryItemTextEdit3.AutoHeight = false;
            this.repositoryItemTextEdit3.Name = "repositoryItemTextEdit3";
            // 
            // repositoryItemTimeSpanEdit1
            // 
            this.repositoryItemTimeSpanEdit1.AllowEditHours = false;
            this.repositoryItemTimeSpanEdit1.AllowEditMinutes = false;
            this.repositoryItemTimeSpanEdit1.AllowEditSeconds = false;
            this.repositoryItemTimeSpanEdit1.AutoHeight = false;
            this.repositoryItemTimeSpanEdit1.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.repositoryItemTimeSpanEdit1.Name = "repositoryItemTimeSpanEdit1";
            this.repositoryItemTimeSpanEdit1.TimeEditStyle = DevExpress.XtraEditors.Repository.TimeEditStyle.SpinButtons;
            // 
            // repositoryItemSpinEdit2
            // 
            this.repositoryItemSpinEdit2.AutoHeight = false;
            this.repositoryItemSpinEdit2.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.repositoryItemSpinEdit2.EditValueChangedFiringMode = DevExpress.XtraEditors.Controls.EditValueChangedFiringMode.Default;
            this.repositoryItemSpinEdit2.IsFloatValue = false;
            this.repositoryItemSpinEdit2.Mask.EditMask = "N00";
            this.repositoryItemSpinEdit2.MaxValue = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.repositoryItemSpinEdit2.Name = "repositoryItemSpinEdit2";
            // 
            // repositoryItemSpinEdit3
            // 
            this.repositoryItemSpinEdit3.AutoHeight = false;
            this.repositoryItemSpinEdit3.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.repositoryItemSpinEdit3.Name = "repositoryItemSpinEdit3";
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabelMain,
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 717);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1600, 22);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabelMain
            // 
            this.toolStripStatusLabelMain.Name = "toolStripStatusLabelMain";
            this.toolStripStatusLabelMain.Size = new System.Drawing.Size(0, 17);
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(118, 17);
            this.toolStripStatusLabel1.Text = "toolStripStatusLabel1";
            // 
            // gridControlNests
            // 
            this.gridControlNests.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridControlNests.Location = new System.Drawing.Point(0, 0);
            this.gridControlNests.MainView = this.gridViewNests;
            this.gridControlNests.MenuManager = this.ribbonControl1;
            this.gridControlNests.Name = "gridControlNests";
            this.gridControlNests.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.repositoryItemPictureEdit2});
            this.gridControlNests.Size = new System.Drawing.Size(0, 0);
            this.gridControlNests.TabIndex = 4;
            this.gridControlNests.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridViewNests});
            // 
            // gridViewNests
            // 
            this.gridViewNests.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.colID1,
            this.colThumbnail,
            this.colnestName,
            this.colnestPath,
            this.colQtyOnNest});
            this.gridViewNests.GridControl = this.gridControlNests;
            this.gridViewNests.Name = "gridViewNests";
            this.gridViewNests.OptionsView.RowAutoHeight = true;
            this.gridViewNests.PopupMenuShowing += new DevExpress.XtraGrid.Views.Grid.PopupMenuShowingEventHandler(this.gridViewNests_PopupMenuShowing);
            // 
            // colID1
            // 
            this.colID1.FieldName = "ID";
            this.colID1.Name = "colID1";
            // 
            // colThumbnail
            // 
            this.colThumbnail.Caption = "Thumbnail";
            this.colThumbnail.ColumnEdit = this.repositoryItemPictureEdit2;
            this.colThumbnail.FieldName = "Thumbnail";
            this.colThumbnail.Name = "colThumbnail";
            this.colThumbnail.Visible = true;
            this.colThumbnail.VisibleIndex = 0;
            // 
            // repositoryItemPictureEdit2
            // 
            this.repositoryItemPictureEdit2.Name = "repositoryItemPictureEdit2";
            this.repositoryItemPictureEdit2.ShowZoomSubMenu = DevExpress.Utils.DefaultBoolean.True;
            this.repositoryItemPictureEdit2.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Zoom;
            this.repositoryItemPictureEdit2.ZoomPercent = 300D;
            // 
            // colnestName
            // 
            this.colnestName.FieldName = "NestName";
            this.colnestName.Name = "colnestName";
            this.colnestName.Visible = true;
            this.colnestName.VisibleIndex = 1;
            // 
            // colnestPath
            // 
            this.colnestPath.FieldName = "NestPath";
            this.colnestPath.Name = "colnestPath";
            this.colnestPath.Visible = true;
            this.colnestPath.VisibleIndex = 2;
            // 
            // colQtyOnNest
            // 
            this.colQtyOnNest.AppearanceCell.Options.UseTextOptions = true;
            this.colQtyOnNest.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.colQtyOnNest.Caption = "Qty On Nest";
            this.colQtyOnNest.FieldName = "QtyOnNest";
            this.colQtyOnNest.Name = "colQtyOnNest";
            this.colQtyOnNest.OptionsColumn.AllowEdit = false;
            this.colQtyOnNest.UnboundType = DevExpress.Data.UnboundColumnType.Integer;
            this.colQtyOnNest.Visible = true;
            this.colQtyOnNest.VisibleIndex = 3;
            // 
            // nestsBindingSource
            // 
            this.nestsBindingSource.Filter = "nestName = \'P1000\'";
            // 
            // splitContainerControl1
            // 
            this.splitContainerControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerControl1.Horizontal = false;
            this.splitContainerControl1.Location = new System.Drawing.Point(0, 158);
            this.splitContainerControl1.Name = "splitContainerControl1";
            this.splitContainerControl1.Panel1.Controls.Add(this.progressPanel1);
            this.splitContainerControl1.Panel1.Controls.Add(this.gridControlItems);
            this.splitContainerControl1.Panel1.Text = "Panel1";
            this.splitContainerControl1.Panel2.Controls.Add(this.gridControlNests);
            this.splitContainerControl1.Panel2.Text = "Panel2";
            this.splitContainerControl1.Size = new System.Drawing.Size(1600, 581);
            this.splitContainerControl1.SplitterPosition = 591;
            this.splitContainerControl1.TabIndex = 5;
            // 
            // progressPanel1
            // 
            this.progressPanel1.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.progressPanel1.Appearance.Options.UseBackColor = true;
            this.progressPanel1.Location = new System.Drawing.Point(935, 105);
            this.progressPanel1.Name = "progressPanel1";
            this.progressPanel1.Size = new System.Drawing.Size(246, 66);
            this.progressPanel1.TabIndex = 1;
            this.progressPanel1.Text = "progressPanel1";
            // 
            // entityCommand1
            // 
            this.entityCommand1.CommandTimeout = 0;
            this.entityCommand1.CommandTree = null;
            this.entityCommand1.Connection = null;
            this.entityCommand1.EnablePlanCaching = true;
            this.entityCommand1.Transaction = null;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1600, 739);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.splitContainerControl1);
            this.Controls.Add(this.ribbonControl1);
            this.IconOptions.Icon = ((System.Drawing.Icon)(resources.GetObject("Form1.IconOptions.Icon")));
            this.Name = "Form1";
            this.Ribbon = this.ribbonControl1;
            this.Text = "RadanMaster 2.4.0.0";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.gridControlItems)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.orderItemsBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewItems)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemSpinEdit1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemPictureEdit1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemMemoEdit1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ribbonControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemTextEdit2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemTextEdit1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemSpinEdit4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemTextEdit3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemTimeSpanEdit1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemSpinEdit2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemSpinEdit3)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.behaviorManager1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridControlNests)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewNests)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemPictureEdit2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nestsBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1)).EndInit();
            this.splitContainerControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.displayItemBindingSource)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DevExpress.XtraGrid.GridControl gridControlItems;
        private DevExpress.XtraGrid.Views.Grid.GridView gridViewItems;
        private DevExpress.LookAndFeel.DefaultLookAndFeel defaultLookAndFeel1;
        private DevExpress.XtraBars.Ribbon.RibbonControl ribbonControl1;
        private DevExpress.XtraBars.Ribbon.RibbonPage ribbonPage1;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup rpgSkins;
        private DevExpress.XtraBars.SkinRibbonGalleryBarItem skinRibbonGalleryBarItem1;
        private DevExpress.XtraGrid.Columns.GridColumn colIsComplete;
        private DevExpress.XtraGrid.Columns.GridColumn colQtyRequired;
        private DevExpress.XtraGrid.Columns.GridColumn colOrderNum;
        private DevExpress.XtraGrid.Columns.GridColumn colID;
        private System.Windows.Forms.BindingSource orderItemsBindingSource;
        private DevExpress.XtraGrid.Columns.GridColumn partNameCol;
        private DevExpress.XtraGrid.Columns.GridColumn thicknessCol;
        private DevExpress.XtraGrid.Columns.GridColumn PartDescCol;
        private DevExpress.XtraGrid.Columns.GridColumn materialCol;
        private DevExpress.XtraGrid.Columns.GridColumn colQtyNested;
        private System.Windows.Forms.OpenFileDialog openFileDialogImport;
        private System.Windows.Forms.OpenFileDialog openFileDialogAddItem;
        private DevExpress.XtraGrid.Columns.GridColumn IsBatch;
        private System.Windows.Forms.OpenFileDialog openFileDialogProject;
        private DevExpress.XtraBars.BarButtonItem barButtonItemAdd;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup ribbonPageGroup1;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup ribbonPageGroup2;
        private DevExpress.XtraBars.BarButtonItem barButtonItemImport;
        private DevExpress.XtraBars.BarEditItem barEditRadanProject;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit repositoryItemTextEdit1;
        private DevExpress.XtraBars.BarButtonItem barButtonSendSelectionToRadan;
        private DevExpress.XtraBars.BarButtonGroup barButtonGroup1;
        private DevExpress.XtraBars.BarButtonItem barButtonBrowseRadanProject;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelMain;
        private DevExpress.XtraBars.BarStaticItem barStaticItem1;
        private DevExpress.XtraBars.BarEditItem barEditItem1;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit repositoryItemTextEdit2;
        private DevExpress.XtraBars.BarEditItem barEditRadanProjectBrowse;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit repositoryItemTextEdit3;
        private DevExpress.XtraBars.BarButtonItem barButtonUpdateFromRadan;
        private DevExpress.XtraGrid.Columns.GridColumn colDateEntered;
        private DevExpress.Utils.Behaviors.BehaviorManager behaviorManager1;
        private DevExpress.XtraGrid.Columns.GridColumn gridColSchedName;
        private DevExpress.XtraGrid.Columns.GridColumn gridColBatchName;
        private DevExpress.XtraEditors.Repository.RepositoryItemPictureEdit repositoryItemPictureEdit1;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumnThumb2;
        private DevExpress.XtraEditors.Repository.RepositoryItemSpinEdit repositoryItemSpinEdit1;
        private DevExpress.XtraGrid.Columns.GridColumn colIsInProject;
        private DevExpress.XtraBars.BarButtonItem barButtonRetrieveSelectionFromRadan;
        private DevExpress.XtraGrid.GridControl gridControlNests;
        private DevExpress.XtraGrid.Views.Grid.GridView gridViewNests;
        private DevExpress.XtraEditors.SplitContainerControl splitContainerControl1;
        private System.Windows.Forms.BindingSource nestsBindingSource;
        private DevExpress.XtraGrid.Columns.GridColumn colID1;
        private DevExpress.XtraGrid.Columns.GridColumn colnestName;
        private DevExpress.XtraGrid.Columns.GridColumn colnestPath;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private DevExpress.XtraGrid.Columns.GridColumn colQtyOnNest;
        private DevExpress.XtraBars.BarButtonItem barButtonNewRadanProject;
        private DevExpress.XtraBars.BarToggleSwitchItem barToggleSwitchShowBatches;
        private DevExpress.XtraBars.Ribbon.RibbonPage rbnPageFilters;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup ribbonPageQuickFilters;
        private DevExpress.XtraBars.BarToggleSwitchItem barToggleSwitchShowOrders;
        private DevExpress.XtraBars.BarToggleSwitchItem barToggleShowComplete;
        private DevExpress.XtraBars.BarToggleSwitchItem barToggleShowRadan;
        private DevExpress.XtraBars.BarToggleSwitchItem barToggleSwitchGroup1;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup ribbonPageQuickSorts;
        private DevExpress.XtraWaitForm.ProgressPanel progressPanel1;
        private DevExpress.XtraBars.BarToggleSwitchItem barToggleSwitchGroup2;
        private DevExpress.XtraGrid.Columns.GridColumn colNotes;
        private DevExpress.XtraEditors.Repository.RepositoryItemMemoEdit repositoryItemMemoEdit1;
        private DevExpress.XtraBars.BarButtonItem barButtonItem1;
        private DevExpress.XtraBars.BarButtonItem barButtonRetrieveAll;
        private DevExpress.XtraGrid.Columns.GridColumn colOrderIsComplete;
        private DevExpress.XtraBars.BarToggleSwitchItem barToggleSwitchShowCompletedOrders;
        private System.Windows.Forms.BindingSource displayItemBindingSource;
        private DevExpress.XtraGrid.Columns.GridColumn colHasBends;
        private DevExpress.XtraGrid.Columns.GridColumn colQtyRemaining;
        private DevExpress.XtraBars.BarButtonItem barButtonItemConnectToRadan;
        private DevExpress.XtraGrid.Columns.GridColumn colThumbnail;
        private DevExpress.XtraEditors.Repository.RepositoryItemPictureEdit repositoryItemPictureEdit2;
        private DevExpress.XtraBars.BarCheckItem barCheckItemShowAllCompletedOrders;
        private DevExpress.XtraBars.BarCheckItem barCheckItemShowCompletedOrdersFromLastDayOnly;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup ribbonPageCompletedOrders;
        private DevExpress.XtraGrid.Columns.GridColumn colDateCompleted;
        private DevExpress.XtraEditors.Repository.RepositoryItemTimeSpanEdit repositoryItemTimeSpanEdit1;
        private DevExpress.XtraBars.BarEditItem barEditNumOfDays;
        private DevExpress.XtraEditors.Repository.RepositoryItemSpinEdit repositoryItemSpinEdit2;
        private DevExpress.XtraEditors.Repository.RepositoryItemSpinEdit repositoryItemSpinEdit3;
        private DevExpress.XtraEditors.Repository.RepositoryItemSpinEdit repositoryItemSpinEdit4;
        private System.Data.Entity.Core.EntityClient.EntityCommand entityCommand1;
    }
}
