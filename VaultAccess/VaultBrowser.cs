/*=====================================================================
  
  This file is part of the Autodesk Vault API Code Samples.

  Copyright (C) Autodesk Inc.  All rights reserved.

THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
PARTICULAR PURPOSE.
=====================================================================*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using ACW = Autodesk.Connectivity.WebServices;
using Framework = Autodesk.DataManagement.Client.Framework;
using Vault = Autodesk.DataManagement.Client.Framework.Vault;
using Forms = Autodesk.DataManagement.Client.Framework.Vault.Forms;
using Autodesk.DataManagement.Client.Framework.Vault.Forms.Currency;

namespace VaultAccess
{
    public partial class VaultBrowser : Form
    {
        #region Member Variables

        private Vault.Currency.Connections.Connection m_conn = null;
        private Vault.Forms.Models.BrowseVaultNavigationModel m_model = null;
        

        private List<Framework.Forms.Controls.GridLayout> m_availableLayouts = new List<Framework.Forms.Controls.GridLayout>();
        private List<ToolStripMenuItem> m_viewButtons = new List<ToolStripMenuItem>();

        private Func<Vault.Currency.Entities.IEntity, bool> m_filterCanDisplayEntity;

        public Vault.Currency.Entities.FileIteration m_selectedItem { get; set; }
        public Vault.Currency.Entities.Folder m_currentFolder { get; set; }
        #endregion

        #region Constructors and Initialization Methods

        public VaultBrowser(Vault.Currency.Connections.Connection conn, Vault.Currency.Entities.Folder startingFolder, string fileTypeFilterText, string fileTypeFilterCmd)
        {
            InitializeComponent();

            m_conn = conn;
            m_currentFolder = startingFolder;

            fileName_multiPartTextBox.EditMode = Framework.Forms.Controls.MultiPartTextBoxControl.EditModeOption.ReadOnly;
            
            //create some filetype filters, borrowing from the Select Entity dialog
            Forms.Settings.SelectEntitySettings.EntityRegularExpressionFilter filter1 = new Forms.Settings.SelectEntitySettings.EntityRegularExpressionFilter("All Files (*.*)", ".+", Vault.Currency.Entities.EntityClassIds.Files);
            fileType_comboBox.Items.Add(filter1);
            /*
            Forms.Settings.SelectEntitySettings.EntityRegularExpressionFilter filter2 = new Forms.Settings.SelectEntitySettings.EntityRegularExpressionFilter("Text Files (*.txt)", ".+txt", Vault.Currency.Entities.EntityClassIds.Files);
            fileType_comboBox.Items.Add(filter2);
            Forms.Settings.SelectEntitySettings.EntityRegularExpressionFilter filter3 = new Forms.Settings.SelectEntitySettings.EntityRegularExpressionFilter("Pictures (*.jpg, *.png, *.gif)", ".+jpg|.+png|.+gif", Vault.Currency.Entities.EntityClassIds.Files);
            fileType_comboBox.Items.Add(filter3);
            Forms.Settings.SelectEntitySettings.EntityRegularExpressionFilter filter4 = new Forms.Settings.SelectEntitySettings.EntityRegularExpressionFilter("Project Files (*.ipj)", ".+ipj", Vault.Currency.Entities.EntityClassIds.Files);
            fileType_comboBox.Items.Add(filter4);
            */

            Forms.Settings.SelectEntitySettings.EntityRegularExpressionFilter filter2 = new Forms.Settings.SelectEntitySettings.EntityRegularExpressionFilter(fileTypeFilterText, fileTypeFilterCmd, Vault.Currency.Entities.EntityClassIds.Files);
            fileType_comboBox.Items.Add(filter2);

            fileType_comboBox.SelectedItem = filter2;
            m_filterCanDisplayEntity = filter2.CanDisplayEntity;
        }

        /// <summary>
        /// Prepares the app to browse a vault by creating the inital set of columns, creating the browse model, connecting all the controls to it,
        /// and navigating to the root of the vault.
        /// </summary>
        private void initalizeGrid()
        {
            Vault.Currency.Properties.PropertyDefinitionDictionary propDefs = m_conn.PropertyManager.GetPropertyDefinitions(null, null, Vault.Currency.Properties.PropertyDefinitionFilter.IncludeAll);

            Vault.Forms.Controls.VaultBrowserControl.Configuration initialConfig = new Vault.Forms.Controls.VaultBrowserControl.Configuration(m_conn, "VaultAccess", propDefs);

            initialConfig.AddInitialColumn(Vault.Currency.Properties.PropertyDefinitionIds.Client.EntityIcon);
            initialConfig.AddInitialColumn(Vault.Currency.Properties.PropertyDefinitionIds.Client.VaultStatus);
            initialConfig.AddInitialColumn(Vault.Currency.Properties.PropertyDefinitionIds.Server.EntityName);
            initialConfig.AddInitialColumn(Vault.Currency.Properties.PropertyDefinitionIds.Server.CheckInDate);
            initialConfig.AddInitialColumn(Vault.Currency.Properties.PropertyDefinitionIds.Server.Comment);
            initialConfig.AddInitialColumn(Vault.Currency.Properties.PropertyDefinitionIds.Server.ThumbnailSystem);
            initialConfig.AddInitialSortCriteria(Vault.Currency.Properties.PropertyDefinitionIds.Server.EntityName, true);

            initialConfig.AddInitialQuickListColumn(Vault.Currency.Properties.PropertyDefinitionIds.Client.EntityIcon);
            initialConfig.AddInitialQuickListColumn(Vault.Currency.Properties.PropertyDefinitionIds.Client.VaultStatus);
            initialConfig.AddInitialQuickListColumn(Vault.Currency.Properties.PropertyDefinitionIds.Server.EntityName);
            initialConfig.AddInitialQuickListColumn(Vault.Currency.Properties.PropertyDefinitionIds.Server.CheckInDate);
            initialConfig.AddInitialQuickListColumn(Vault.Currency.Properties.PropertyDefinitionIds.Server.Comment);
            initialConfig.AddInitialQuickListColumn(Vault.Currency.Properties.PropertyDefinitionIds.Server.ThumbnailSystem);

            m_model = new Forms.Models.BrowseVaultNavigationModel(m_conn, true, true);

            m_model.ParentChanged += new EventHandler(m_model_ParentChanged);
            m_model.SelectedContentChanged += new EventHandler<Forms.Currency.SelectionChangedEventArgs>(m_model_SelectedContentChanged);

            vaultBrowserControl1.SetDataSource(initialConfig, m_model);
            vaultBrowserControl1.OptionsCustomizations.CanDisplayEntityHandler = canDisplayEntity;
            vaultBrowserControl1.OptionsBehavior.MultiSelect = false;
            vaultBrowserControl1.OptionsBehavior.AllowOverrideSelections = false;

            vaultNavigationPathComboboxControl1.SetDataSource(m_conn, m_model, null);

            //Vault.Currency.Entities.IEntity iEntity;


            if (m_currentFolder == null) m_currentFolder = m_conn.FolderManager.RootFolder;
            m_model.Navigate(m_currentFolder, Forms.Currency.NavigationContext.NewContext);

            //m_model.Navigate(m_conn.FolderManager.GetFoldersByIds(ids), Forms.Currency.NavigationContext.NewContext);
        }

        #endregion

        #region Event Handlers

        #region Form Events

        private void VaultBrowser_Shown(object sender, EventArgs e)
        {
            //save each available layout of the browser control as well as generate a button to use in the switch view dropdown
            foreach (Framework.Forms.Controls.GridLayout layout in vaultBrowserControl1.AvailableLayouts)
            {
                m_availableLayouts.Add(layout);
                ToolStripMenuItem item = new ToolStripMenuItem(layout.Name);
                item.Tag = layout;
                item.CheckOnClick = true;
                item.Click += new EventHandler(switchViewDropdown_itemClick);
                switchView_toolStripSplitButton.DropDownItems.Add(item);
                m_viewButtons.Add(item);
            }

            //if(m_conn == null) m_conn = Vault.Forms.Library.Login(null);
            controlStates(m_conn != null);
            if (m_conn != null)
                initalizeGrid();

            vaultBrowserControl1.CurrentLayout = m_availableLayouts[1];  // set default view to list view
        }

        private void VaultBrowser_FormClosed(object sender, FormClosedEventArgs e)
        {
            //we need to be sure to release all our connections when the app closes
            //Vault.Library.ConnectionManager.CloseAllConnections();
        }

        #endregion

        #region BrowserVaultNavigationModel Events

        void m_model_SelectedContentChanged(object sender, Forms.Currency.SelectionChangedEventArgs e)
        {
            //when the selected content changes, we need to update the filename field to reflect the selected entities
            List<Vault.Currency.Entities.IEntity> selectedEntities = new List<Vault.Currency.Entities.IEntity>(e.SelectedEntities);

            bool fileSelected = false;
            List<string> selectedEntityNames = new List<string>();
            foreach (Vault.Currency.Entities.IEntity entity in selectedEntities)
            {
                if (entity is Vault.Currency.Entities.FileIteration)
                    fileSelected = true;
                selectedEntityNames.Add(entity.EntityName);
            }
            fileName_multiPartTextBox.Parts = selectedEntityNames;

            UpdateAssociationsTreeView();
        }

        void m_model_ParentChanged(object sender, EventArgs e)
        {
            navigateBack_toolStripButton.Enabled = m_model.CanMoveBack;
            navigateUp_toolStripButton.Enabled = m_model.CanMoveUp;
        }

        #endregion

        #region ToolStripButton Events

        private void navigateBack_toolStripButton_Click(object sender, EventArgs e)
        {
            m_model.MoveBack();
        }

        private void navigateUp_toolStripButton_Click(object sender, EventArgs e)
        {
            m_model.MoveUp();
        }

        private void switchView_toolStripSplitButton_ButtonClick(object sender, EventArgs e)
        {
            //cycle through the list of available layouts when the switch view button is pressed without using the dropdown
            int setIdx = (m_availableLayouts.IndexOf(vaultBrowserControl1.CurrentLayout) + 1) % m_availableLayouts.Count;
            vaultBrowserControl1.CurrentLayout = m_availableLayouts[setIdx];
        }

        private void switchView_toolStripSplitButton_DropDownOpening(object sender, EventArgs e)
        {
            //Check the currenly visible view in the menu
            foreach (ToolStripMenuItem button in m_viewButtons)
            {
                button.Checked = button.Tag.Equals(vaultBrowserControl1.CurrentLayout);
            }
        }

        #endregion

        #region MenuItem Events

        private void login_toolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_conn = Vault.Forms.Library.Login(null);
            controlStates(m_conn != null);
            if (m_conn != null)
                initalizeGrid();
        }

        private void logout_toolStripMenuItem_Click(object sender, EventArgs e)
        {
            //logout presents the user the option to log in again, so we need to be sure to handle that case
            m_conn = Vault.Forms.Library.Logout(m_conn, null);
            vaultBrowserControl1.SetDataSource(null, null);
            fileName_multiPartTextBox.Parts = new List<string>();
            controlStates(m_conn != null);
            if (m_conn != null)
                initalizeGrid();
        }

        private void m_addFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Vault.Currency.Entities.Folder parent = m_model.Parent as Vault.Currency.Entities.Folder;
            if (parent != null)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string[] filePaths = openFileDialog.FileNames;
                    foreach (string filePath in filePaths)
                    {
                        AddFileCommand.Execute(filePath, parent, m_conn);
                    }
                    m_model.Reload();
                }
            }
        }

        private void m_advancedFindToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AdvancedSearch();
        }

        #endregion

        void switchViewDropdown_itemClick(object sender, EventArgs e)
        {
            //switch to the exact layout that was chosen with the switch view dropdown menu
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            vaultBrowserControl1.CurrentLayout = item.Tag as Framework.Forms.Controls.GridLayout;
        }



        private void fileType_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            //when a new filetype filter is selected, we need to update the saved filter function then tell the grid to update
            Forms.Settings.SelectEntitySettings.EntityFilter filter = fileType_comboBox.SelectedItem as Forms.Settings.SelectEntitySettings.EntityFilter;
            m_filterCanDisplayEntity = filter.CanDisplayEntity;
            vaultBrowserControl1.ReEvaluateCustomFilters();
        }

        #endregion

        #region Implemenation Methods

        /// <summary>
        /// Set the enabled/disabled states of all the controls in the app based on if we have an active connection or not.
        /// </summary>
        /// <param name="activeConnection">True if there is an active connection.</param>
        private void controlStates(bool activeConnection)
        {
            vaultNavigationPathComboboxControl1.Enabled = activeConnection;
            switchView_toolStripSplitButton.Enabled = activeConnection;
            vaultBrowserControl1.Enabled = activeConnection;
            fileType_comboBox.Enabled = activeConnection;
            m_advancedFindToolStripMenuItem.Enabled = activeConnection;

            //navigate up and back are normally handled by the model (m_model_ParentChanged), but we need to specifically disable them when we log out
            if (activeConnection == false)
            {
                navigateBack_toolStripButton.Enabled = false;
                navigateUp_toolStripButton.Enabled = false;
            }
        }

        /// <summary>
        /// Wrapper between the filetype filters and the CanDisplayEntity deleagate on the Vault Browser control.
        /// </summary>
        /// <param name="entity">The entity to run the filter against.</param>
        /// <returns>True if the entity can be displayed.</returns>
        private bool canDisplayEntity(Vault.Currency.Entities.IEntity entity)
        {
            if (m_filterCanDisplayEntity != null)
            {
                if (!m_filterCanDisplayEntity(entity))
                {
                    return false;
                }
            }

            return true;
        }

        

        private void AdvancedSearch()
        {
            FinderForm finderForm = new FinderForm(m_conn);
            finderForm.Show();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            Vault.Currency.Entities.FileIteration file = m_model.SelectedContent.FirstOrDefault() as Vault.Currency.Entities.FileIteration;
            if (file != null)
            {
                m_selectedItem = file;
                m_currentFolder = new Autodesk.DataManagement.Client.Framework.Vault.Currency.Entities.Folder(m_conn, file.Parent);
            }


        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        /// <summary>
        /// List all children for a file.
        /// </summary>
        private void UpdateAssociationsTreeView()
        {
            m_associationsTreeView.Nodes.Clear();

            Vault.Currency.Entities.FileIteration selectedFile = m_model.SelectedContent.FirstOrDefault() as Vault.Currency.Entities.FileIteration;
            if (selectedFile == null)
                return;

            this.m_associationsTreeView.BeginUpdate();

            // get all parent and child information for the file
            ACW.FileAssocArray[] associationArrays = m_conn.WebServiceManager.DocumentService.GetFileAssociationsByIds(
                new long[] { selectedFile.EntityIterationId },
                ACW.FileAssociationTypeEnum.None, false,        // parent associations
                ACW.FileAssociationTypeEnum.Dependency, true,   // child associations
                false, true);

            if (associationArrays != null && associationArrays.Length > 0 &&
                associationArrays[0].FileAssocs != null && associationArrays[0].FileAssocs.Length > 0)
            {
                ACW.FileAssoc[] associations = associationArrays[0].FileAssocs;
                m_associationsTreeView.ShowLines = true;

                // organize the return values by the parent file
                Dictionary<long, List<Vault.Currency.Entities.FileIteration>> associationsByFile = new Dictionary<long, List<Vault.Currency.Entities.FileIteration>>();
                foreach (ACW.FileAssoc association in associations)
                {
                    ACW.File parent = association.ParFile;
                    if (associationsByFile.ContainsKey(parent.Id))
                    {
                        // parent is already in the hashtable, add an new child entry
                        List<Vault.Currency.Entities.FileIteration> list = associationsByFile[parent.Id];
                        list.Add(new Vault.Currency.Entities.FileIteration(m_conn, association.CldFile));
                    }
                    else
                    {
                        // add the parent to the hashtable.
                        List<Vault.Currency.Entities.FileIteration> list = new List<Vault.Currency.Entities.FileIteration>();
                        list.Add(new Vault.Currency.Entities.FileIteration(m_conn, association.CldFile));
                        associationsByFile.Add(parent.Id, list);
                    }
                }

                // construct the tree
                if (associationsByFile.ContainsKey(selectedFile.EntityIterationId))
                {
                    TreeNode rootNode = new TreeNode(selectedFile.EntityName);
                    rootNode.Tag = selectedFile;
                    m_associationsTreeView.Nodes.Add(rootNode);
                    AddChildAssociation(rootNode, associationsByFile);
                }
            }
            else
            {
                m_associationsTreeView.ShowLines = false;
                m_associationsTreeView.Nodes.Add("<< no children >>");
            }

            m_associationsTreeView.EndUpdate();
        }

        /// <summary>
        /// Add tree node for the association tree.
        /// </summary>
        /// <param name="parentNode">Node to add to</param>
        private void AddChildAssociation(TreeNode parentNode,
            Dictionary<long, List<Vault.Currency.Entities.FileIteration>> associationsByFile)
        {
            // get the File object for the Node
            Vault.Currency.Entities.FileIteration parentFile = (Vault.Currency.Entities.FileIteration)parentNode.Tag;

            // if associations exist, create a Node for each one
            if (associationsByFile.ContainsKey(parentFile.EntityIterationId))
            {
                List<Vault.Currency.Entities.FileIteration> list = associationsByFile[parentFile.EntityIterationId];
                foreach(Vault.Currency.Entities.FileIteration childFile in list)
                {
                    TreeNode childNode = new TreeNode(childFile.EntityName);
                    childNode.Tag = childFile;
                    parentNode.Nodes.Add(childNode);

                    // add all of the Nodes for the children's children
                    AddChildAssociation(childNode, associationsByFile);
                }
            }
        }




        #endregion

        
    }
}
