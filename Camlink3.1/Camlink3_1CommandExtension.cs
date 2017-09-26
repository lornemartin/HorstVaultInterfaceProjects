using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;



using Autodesk.Connectivity.Explorer.Extensibility;
using Autodesk.Connectivity.WebServices;
using Autodesk.Connectivity.WebServicesTools;
using VDF = Autodesk.DataManagement.Client.Framework;
using ACW = Autodesk.Connectivity.WebServices;
using ACWT = Autodesk.Connectivity.WebServicesTools;
using Framework = Autodesk.DataManagement.Client.Framework;
using Vault = Autodesk.DataManagement.Client.Framework.Vault;
using Forms = Autodesk.DataManagement.Client.Framework.Vault.Forms;

// These 5 assembly attributes must be specified or your extension will not load. 
[assembly: AssemblyCompany("Horst Welding")]
[assembly: AssemblyProduct("Camlink3_1CommandExtension")]
[assembly: AssemblyDescription("Export")]

// The extension ID needs to be unique for each extension.  
// Make sure to generate your own ID when writing your own extension. 
[assembly: Autodesk.Connectivity.Extensibility.Framework.ExtensionId("f87843c1-f332-4ba8-ad77-8ddb952d2332")]                                                                   

// This number gets incremented for each Vault release.
[assembly: Autodesk.Connectivity.Extensibility.Framework.ApiVersion("10.0")]

namespace Camlink3_1
{
    /// <summary>
    /// This class implements the IExtension interface, which means it tells Vault Explorer what 
    /// commands and custom tabs are provided by this extension.
    /// </summary>
    public class Camlink3_1CommandExtension : IExplorerExtension
    {
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
            // Create the Hello World command object.
            CommandItem Camlink3_1CmdItem = new CommandItem("Camlink3_1Command", "Export...")
            {
                // this command is active when a File is selected
                NavigationTypes = new SelectionTypeId[] { SelectionTypeId.File, SelectionTypeId.FileVersion },

                // this command is not active if there are multiple entities selected
                MultiSelectEnabled = true
            };

            // The Camlink3_1CommandHandler function is called when the custom command is executed.
            Camlink3_1CmdItem.Execute += Camlink3_1CommandHandler;


            // Create a command site to hook the command to the right-click menu for Files.
            CommandSite fileContextCmdSite = new CommandSite("Camlink3_1Command.FileContextMenu", "Camlink Menu")
            {
                Location = CommandSiteLocation.FileContextMenu,
                DeployAsPulldownMenu = false
            };
            fileContextCmdSite.AddCommand(Camlink3_1CmdItem);

            // Now the custom command is available in 2 places.

            // Gather the sites in a List.
            List<CommandSite> sites = new List<CommandSite>();
            sites.Add(fileContextCmdSite);

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

        /// <summary>
        /// This is the function that is called whenever the custom command is executed.
        /// </summary>
        /// <param name="s">The sender object.  Usually not used.</param>
        /// <param name="e">The event args.  Provides additional information about the environment.</param>
        void Camlink3_1CommandHandler(object s, CommandItemEventArgs e)
        {
            try
            {
                VDF.Vault.Currency.Connections.Connection connection = e.Context.Application.Connection;

                // The Context part of the event args tells us information about what is selected.
                // Run some checks to make sure that the selection is valid.
                if (e.Context.CurrentSelectionSet.Count() == 0)
                    MessageBox.Show("Nothing is selected");
                else
                {
                    Screen screen = Screen.FromPoint(Cursor.Position);
                    ExportToSym exportForm = new ExportToSym(e.Context.CurrentSelectionSet, connection);
                    exportForm.StartPosition = FormStartPosition.Manual;
                    exportForm.Left = screen.Bounds.Location.X + 250;
                    exportForm.Top = screen.Bounds.Location.Y + 150;
                    exportForm.ShowDialog();
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

        void Execute(VDF.Vault.Currency.Entities.FileIteration file, VDF.Vault.Currency.Connections.Connection connection)
        {

            string filePath = Path.Combine(System.IO.Path.GetTempPath(), file.EntityName);

            if (System.IO.File.Exists(filePath))
                System.IO.File.SetAttributes(filePath, FileAttributes.Normal);

            //determine if the file already exists
            if (System.IO.File.Exists(filePath))
            {
                //we'll try to delete the file so we can get the latest copy
                try
                {
                    System.IO.File.Delete(filePath);

                    //remove the file from the collection of downloaded files that need to be removed when the application exits
                    if (m_downloadedFiles.Contains(filePath))
                        m_downloadedFiles.Remove(filePath);
                }
                catch (System.IO.IOException)
                {
                    throw new Exception("The file you are attempting to open already exists and can not be overwritten. This file may currently be open, try closing any application you are using to view this file and try opening the file again.");
                }
            }

            downloadFile(connection, file, Path.GetDirectoryName(filePath));
            m_downloadedFiles.Add(filePath);

            //Create a new ProcessStartInfo structure.
            ProcessStartInfo pInfo = new ProcessStartInfo();
            //Set the file name member.
            pInfo.Arguments = "/high";
            pInfo.FileName = filePath;
            //UseShellExecute is true by default. It is set here for illustration.
            pInfo.UseShellExecute = true;
            Process p = Process.Start(pInfo);

            System.IO.File.SetAttributes(filePath, FileAttributes.ReadOnly);
        }

        void downloadFile(VDF.Vault.Currency.Connections.Connection connection, VDF.Vault.Currency.Entities.FileIteration file, string folderPath)
        {
            VDF.Vault.Settings.AcquireFilesSettings settings = new VDF.Vault.Settings.AcquireFilesSettings(connection);
            settings.AddEntityToAcquire(file);

            if (file.EntityName.EndsWith(".iam"))
            {
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeChildren = true;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeLibraryContents = true;
                // this forces all the files to go into one folder, it doesn't replicate the vault's folder structure..
                // this is needed so that Inventor view knows where to find the file
                settings.OrganizeFilesRelativeToCommonVaultRoot = false;
            }
            else
            {
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeChildren = false;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeLibraryContents = false;
                settings.OptionsRelationshipGathering.FileRelationshipSettings.IncludeChildren = false;
            }

            settings.LocalPath = new VDF.Currency.FolderPathAbsolute(folderPath);
            connection.FileManager.AcquireFiles(settings);
        }
    }
}
