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
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

using Autodesk.Connectivity.Explorer.Extensibility;
using VDF = Autodesk.DataManagement.Client.Framework;
using Framework = Autodesk.DataManagement.Client.Framework;
using Vault = Autodesk.DataManagement.Client.Framework.Vault;



// These 5 assembly attributes must be specified or your extension will not load. 
[assembly: AssemblyCompany("Horst Welding")]
[assembly: AssemblyProduct("VaultViewCommandExtension")]
[assembly: AssemblyDescription("Vault File Viewer")]

// The extension ID needs to be unique for each extension.  
// Make sure to generate your own ID when writing your own extension. 
[assembly: Autodesk.Connectivity.Extensibility.Framework.ExtensionId("689e45ae-b49b-4fcb-954f-181bc48cdf25")]                                                                   

// This number gets incremented for each Vault release.
[assembly: Autodesk.Connectivity.Extensibility.Framework.ApiVersion("17.0")]


namespace VaultView
{

    /// <summary>
    /// This class implements the IExtension interface, which means it tells Vault Explorer what 
    /// commands and custom tabs are provided by this extension.
    /// </summary>
    public class VaultViewCommandExtension : IExplorerExtension
    {
        private string IvFullPath;  // variable to read properties from settings file

        private static List<string> m_downloadedFiles = new List<string>();

        private List<Framework.Forms.Controls.GridLayout> m_availableLayouts = new List<Framework.Forms.Controls.GridLayout>();
        private List<ToolStripMenuItem> m_viewButtons = new List<ToolStripMenuItem>();

        #region IExtension Members

        /// <summary>
        /// This function tells Vault Explorer what custom commands this extension provides.
        /// Part of the IExtension interface.
        /// </summary>
        /// <returns>A collection of CommandSites, which are collections of custom commands.</returns>
        public IEnumerable<CommandSite> CommandSites()
        {
            // Create the DirectView command object.
            CommandItem VaultViewCmdItem = new CommandItem("VaultViewCommand", "D&irect View...")
            {
                // this command is active when a File is selected
                NavigationTypes = new SelectionTypeId[] { SelectionTypeId.File, SelectionTypeId.FileVersion },

                // this command is not active if there are multiple entities selected
                MultiSelectEnabled = false
            };

            // create the selectViewerApp command
            CommandItem SelectViewerAppCmdItem = new CommandItem("SelectViewerAppCommand", "Select DirectView Viewer App...")
            {
                // not sure what options should go in here...
            };
            

            // The VaultViewCommandHandler function is called when the custom command is executed.
            VaultViewCmdItem.Execute += HelloWorldCommandHandler;
            SelectViewerAppCmdItem.Execute += SelectViewerAppCommandHandler;


            // Create a command site to hook the DirectView command to the right-click menu for Files.
            CommandSite fileContextCmdSite = new CommandSite("VaultViewCommand.FileContextMenu", "Vault View Menu")
            {
                Location = CommandSiteLocation.FileContextMenu,
                DeployAsPulldownMenu = false
            };
            fileContextCmdSite.AddCommand(VaultViewCmdItem);

            CommandSite toolsCommandMenuSite = new CommandSite("SelectViewerAppCommand", "Select DirectView Viewer App...")
            {
                Location = CommandSiteLocation.ToolsMenu,
                DeployAsPulldownMenu = false
            };
            toolsCommandMenuSite.AddCommand(SelectViewerAppCmdItem);
            

            // Gather the sites in a List.
            List<CommandSite> sites = new List<CommandSite>();
            sites.Add(fileContextCmdSite);
            sites.Add(toolsCommandMenuSite);

            // Return the list of CommandSites.
            return sites;
        }

        /// <summary>
        /// This function tells Vault Explorer what custom tabs this extension provides.
        /// Part of the IExtension interface.
        /// </summary>
        /// <returns>A collection of DetailTabs, each object represents a custom tab.</returns>
        public IEnumerable<DetailPaneTab> DetailTabs()
        {
            // Create a DetailPaneTab list to return from method
            List<DetailPaneTab> fileTabs = new List<DetailPaneTab>();

            // Create Selection Info tab for Files
            DetailPaneTab filePropertyTab = new DetailPaneTab("File.Tab.PropertyGrid",
                                                        "Selection Info",
                                                        SelectionTypeId.File,
                                                        typeof(MyCustomTabControl));

            // The propertyTab_SelectionChanged is called whenever our tab is active and the selection changes in the 
            // main grid.
            filePropertyTab.SelectionChanged += propertyTab_SelectionChanged;
            fileTabs.Add(filePropertyTab);

            // Create Selection Info tab for Folders
            DetailPaneTab folderPropertyTab = new DetailPaneTab("Folder.Tab.PropertyGrid",
                                                        "Selection Info",
                                                        SelectionTypeId.Folder,
                                                        typeof(MyCustomTabControl));
            folderPropertyTab.SelectionChanged += propertyTab_SelectionChanged;
            fileTabs.Add(folderPropertyTab);

            // Create Selection Info tab for Items
            DetailPaneTab itemPropertyTab = new DetailPaneTab("Item.Tab.PropertyGrid",
                                                        "Selection Info",
                                                        SelectionTypeId.Item,
                                                        typeof(MyCustomTabControl));
            itemPropertyTab.SelectionChanged += propertyTab_SelectionChanged;
            fileTabs.Add(itemPropertyTab);

            // Create Selection Info tab for Change Orders
            DetailPaneTab coPropertyTab = new DetailPaneTab("Co.Tab.PropertyGrid",
                                                        "Selection Info",
                                                        SelectionTypeId.ChangeOrder,
                                                        typeof(MyCustomTabControl));
            coPropertyTab.SelectionChanged += propertyTab_SelectionChanged;
            fileTabs.Add(coPropertyTab);

            // Return tabs
            return fileTabs;
        }

        /// <summary>
        /// This function is called after the user logs in to the Vault Server.
        /// Part of the IExtension interface.
        /// </summary>
        /// <param name="application">Provides information about the running application.</param>
        public void OnLogOn(IApplication application)
        {
        }

        /// <summary>
        /// This function is called after the user is logged out of the Vault Server.
        /// Part of the IExtension interface.
        /// </summary>
        /// <param name="application">Provides information about the running application.</param>
        public void OnLogOff(IApplication application)
        {
        }

        /// <summary>
        /// This function is called before the application is closed.
        /// Part of the IExtension interface.
        /// </summary>
        /// <param name="application">Provides information about the running application.</param>
        public void OnShutdown(IApplication application)
        {
            // Although this function is empty for this project, it's still needs to be defined 
            // because it's part of the IExtension interface.
        }

        /// <summary>
        /// This function is called after the application starts up.
        /// Part of the IExtension interface.
        /// </summary>
        /// <param name="application">Provides information about the running application.</param>
        public void OnStartup(IApplication application)
        {
            // Although this function is empty for this project, it's still needs to be defined 
            // because it's part of the IExtension interface.
        }

        /// <summary>
        /// This function tells Vault Exlorer which default commands should be hidden.
        /// Part of the IExtension interface.
        /// </summary>
        /// <returns>A collection of command names.</returns>
        public IEnumerable<string> HiddenCommands()
        {
            // This extension does not hide any commands.
            return null;
        }

        /// <summary>
        /// This function allows the extension to define special behavior for Custom Entity types.
        /// Part of the IExtension interface.
        /// </summary>
        /// <returns>A collection of CustomEntityHandler objects.  Each object defines special behavior
        /// for a specific Custom Entity type.</returns>
        public IEnumerable<CustomEntityHandler> CustomEntityHandlers()
        {
            // This extension does not provide special Custom Entity behavior.
            return null;
        }

        #endregion

        void SelectViewerAppCommandHandler(object s, CommandItemEventArgs e)
        {
            selectDirectViewerApp();
        }


        /// <summary>
        /// This is the function that is called whenever the custom command is executed.
        /// </summary>
        /// <param name="s">The sender object.  Usually not used.</param>
        /// <param name="e">The event args.  Provides additional information about the environment.</param>
        void HelloWorldCommandHandler(object s, CommandItemEventArgs e)
        {
            try
            {
                VDF.Vault.Currency.Connections.Connection connection = e.Context.Application.Connection;

                // The Context part of the event args tells us information about what is selected.
                // Run some checks to make sure that the selection is valid.
                if (e.Context.CurrentSelectionSet.Count() == 0)
                    MessageBox.Show("Nothing is selected");
                else if (e.Context.CurrentSelectionSet.Count() > 1)
                    MessageBox.Show("This function does not support multiple selections");
                else
                {

                    // we only have one item selected, which is the expected behavior
                    ISelection selection = e.Context.CurrentSelectionSet.First();

                    // Look of the File object.  How we do this depends on what is selected.
                    Autodesk.Connectivity.WebServices.File selectedFile = null;
                    if (selection.TypeId == SelectionTypeId.File)
                    {
                        // our ISelection.Id is really a File.MasterId
                        selectedFile = connection.WebServiceManager.DocumentService.GetLatestFileByMasterId(selection.Id);
                    }
                    else if (selection.TypeId == SelectionTypeId.FileVersion)
                    {
                        // our ISelection.Id is really a File.Id
                        selectedFile = connection.WebServiceManager.DocumentService.GetFileById(selection.Id);
                    }

                    if (selectedFile == null)
                    {
                        MessageBox.Show("Selection is not a file.");
                    }
                    else
                    {
                        // this is the message we hope to see
                        //MessageBox.Show(String.Format("Hello World! The file size is: {0} bytes",
                        //selectedFile.FileSize));
                    }

                    VDF.Vault.Settings.AcquireFilesSettings settings = new VDF.Vault.Settings.AcquireFilesSettings(connection);

                    VDF.Vault.Currency.Entities.FileIteration selFiles = new Vault.Currency.Entities.FileIteration(connection, selectedFile);


                    Vault.Currency.Entities.FileIteration file = selFiles;

                    if (file.EntityName.EndsWith(".iam") ||
                        file.EntityName.EndsWith(".ipt") ||
                        file.EntityName.EndsWith(".idw") &&
                        file != null)
                        Execute(file, connection);
                }
            }
            catch (Exception ex)
            {
                // If something goes wrong, we don't want the exception to bubble up to Vault Explorer.
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        /// <summary>
        /// This function is called whenever our custom tab is active and the selection has changed in the main grid.
        /// </summary>
        /// <param name="sender">The sender object.  Usually not used.</param>
        /// <param name="e">The event args.  Provides additional information about the environment.</param>
        void propertyTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // The event args has our custom tab object.  We need to cast it to our type.
                MyCustomTabControl tabControl = e.Context.UserControl as MyCustomTabControl;

                // Send selection to the tab so that it can display the object.
                tabControl.SetSelectedObject(e.Context.SelectedObject);
            }
            catch (Exception ex)
            {
                // If something goes wrong, we don't want the exception to bubble up to Vault Explorer.
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        Boolean selectDirectViewerApp()
        {
            try
            {
                DialogResult result;
                OpenFileDialog openFileDialog1 = new OpenFileDialog();
                openFileDialog1.Filter = "exe files|*.exe";
                openFileDialog1.Title = "Please select the Inventor Viewer Application";

                result = openFileDialog1.ShowDialog(); // Show the dialog.
                if (result == DialogResult.OK) // Test result.
                {
                    string f = openFileDialog1.FileName;
                    IvFullPath = f;
                }
                // ensure the user selects a valid viewer application

                if (result == DialogResult.OK)
                {
                    AppSettings.Set("Iv_FullPath", IvFullPath);
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        void Execute(VDF.Vault.Currency.Entities.FileIteration file, VDF.Vault.Currency.Connections.Connection connection)
        {
            VaultAccess.VaultAccess va = new VaultAccess.VaultAccess(connection, @"\\HLAFILESRVR2\ENGCOMMON\PDF Drawing Files\");

            if (System.IO.File.Exists(AppSettings.SettingsFilePath))
            {
                IvFullPath = (string)AppSettings.Get("Iv_FullPath");

                if (!System.IO.File.Exists(IvFullPath))
                {
                    string Path1 = @"C:\Program Files\Autodesk\Inventor View 2023\Bin\InventorView.exe";
                    string Path2 = @"C:\Program Files\Autodesk\Inventor2023\Bin\InventorView.exe";
                    if (System.IO.File.Exists(Path1))
                    {
                        IvFullPath = Path1;
                        AppSettings.Set("Iv_FullPath", Path1);
                    }
                    else if (System.IO.File.Exists(Path2))
                    {
                        IvFullPath = Path2;
                        AppSettings.Set("Iv_FullPath", Path2);
                    }
                    else
                    {
                        selectDirectViewerApp();
                    }
                }
            }

            string filePath = Path.Combine(System.IO.Path.GetTempPath() + "\\DirectView\\", file.EntityName);

            if (file.EntityName.EndsWith(".iam"))
            {
                va.downloadFileAndAssociations(file, Path.GetDirectoryName(filePath));
            }
            else if (file.EntityName.EndsWith(".idw"))
            {
                va.downloadFile(file, Path.GetDirectoryName(filePath));
                //if (va.CheckForOutOfDateSheets(filePath))
                //    MessageBox.Show("Cannot view this file, there are drawing sheet(s) that are out of date");

            }
            else 
            {
                va.downloadFile(file, Path.GetDirectoryName(filePath));
            }

            if (!File.Exists(filePath))
            {
                DialogResult downloadResult = MessageBox.Show("File is not finished downloading.  Did you want to try again?", "File Download", MessageBoxButtons.YesNoCancel);

                if (downloadResult == DialogResult.No || downloadResult == DialogResult.Cancel)
                    return;
            }

            Process p = new Process();

            p.StartInfo.WorkingDirectory = Path.GetDirectoryName(IvFullPath);

            p.StartInfo.FileName = IvFullPath;

            p.StartInfo.Arguments = "\"" + filePath + "\"";

            p.StartInfo.CreateNoWindow = false;
            p.Start();

            System.IO.File.SetAttributes(filePath, FileAttributes.ReadOnly);


        }
        
    }   

    
}
