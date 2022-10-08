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
using Autodesk.Connectivity.WebServices;
using System.Collections;


// These 5 assembly attributes must be specified or your extension will not load. 
[assembly: AssemblyCompany("Horst Welding")]
[assembly: AssemblyProduct("VaultItemExportCommandExtension")]
[assembly: AssemblyDescription("Vault Item Export")]

// The extension ID needs to be unique for each extension.  
// Make sure to generate your own ID when writing your own extension. 
[assembly: Autodesk.Connectivity.Extensibility.Framework.ExtensionId("E7E8E0D2-EEBE-44A8-98B5-0D487A087FA4")]                                                                   

// This number gets incremented for each Vault release.
[assembly: Autodesk.Connectivity.Extensibility.Framework.ApiVersion("16.0")]


namespace ItemExport
{

    /// <summary>
    /// This class implements the IExtension interface, which means it tells Vault Explorer what 
    /// commands and custom tabs are provided by this extension.
    /// </summary>
    public class ItemExportCommandExtension : IExplorerExtension
    {
        private string IvFullPath;  // variable to read properties from settings file

        private static List<string> m_downloadedFiles = new List<string>();

        private List<Framework.Forms.Controls.GridLayout> m_availableLayouts = new List<Framework.Forms.Controls.GridLayout>();
        private List<ToolStripMenuItem> m_viewButtons = new List<ToolStripMenuItem>();

        private bool okToProcess = true;

        #region IExtension Members

        /// <summary>
        /// This function tells Vault Explorer what custom commands this extension provides.
        /// Part of the IExtension interface.
        /// </summary>
        /// <returns>A collection of CommandSites, which are collections of custom commands.</returns>
        public IEnumerable<CommandSite> CommandSites()
        {
            // Create the Hello World command object.
            CommandItem ItemExportCmdItem = new CommandItem("VaultViewCommand", "E&xport Item...")
            {
                // this command is active when a File is selected
                NavigationTypes = new SelectionTypeId[] { SelectionTypeId.Item },

                // this command is not active if there are multiple entities selected
                MultiSelectEnabled = false
            };

            CommandItem BomExportCmdItem = new CommandItem("BomItemExportCommand", "Export Item &Direct")
            {
                NavigationTypes = new SelectionTypeId[] { SelectionTypeId.Bom},
                MultiSelectEnabled = false
            };

            CommandItem BomExportPreviewCmdItem = new CommandItem("BomItemExportPreviewCommand", "Export Item &Preview")
            {
                NavigationTypes = new SelectionTypeId[] { SelectionTypeId.Bom },
                MultiSelectEnabled = false
            };

            CommandItem BomExportOdooCmdItem = new CommandItem("BomItemExportOdooCommand", "Export Item &Odoo")
            {
                NavigationTypes = new SelectionTypeId[] { SelectionTypeId.Bom },
                MultiSelectEnabled = false
            };

            // The VaultViewCommandHandler function is called when the custom command is executed.
            ItemExportCmdItem.Execute += ItemExportCommandHandler;
            BomExportCmdItem.Execute += BomItemExportCommandHandler;
            BomExportPreviewCmdItem.Execute += BomItemExportPreviewCommandHandler;
            BomExportOdooCmdItem.Execute += BomItemExportOdooCommandHandler;


            // Create a command site to hook the command to the right-click menu for Files.
            CommandSite itemContextCmdSite = new CommandSite("ItemExportCommand.ItemContextMenu", "Item Export Menu")
            {
                Location = CommandSiteLocation.ItemContextMenu,
                DeployAsPulldownMenu = false
            };
            itemContextCmdSite.AddCommand(ItemExportCmdItem);

            CommandSite bomExportCmdSite = new CommandSite("BomItemExportCommand", "Export Item Direct")
            {
                Location = CommandSiteLocation.ItemBomToolbar,
                DeployAsPulldownMenu = false
            };
            bomExportCmdSite.AddCommand(BomExportCmdItem);

            CommandSite bomExportPreviewCmdSite = new CommandSite("BomItemExportPreviewCommand", "Export Item Preview")
            {
                Location = CommandSiteLocation.ItemBomToolbar,
                DeployAsPulldownMenu = false
            };
            bomExportCmdSite.AddCommand(BomExportPreviewCmdItem);

            CommandSite bomExportOdooCmdSite = new CommandSite("BomItemExportOdooCommand", "Export Item Odoo")
            {
                Location = CommandSiteLocation.ItemBomToolbar,
                DeployAsPulldownMenu = false
            };
            bomExportCmdSite.AddCommand(BomExportOdooCmdItem);

            // Now the custom command is available in 2 places.

            // Gather the sites in a List.
            List<CommandSite> sites = new List<CommandSite>();
            sites.Add(itemContextCmdSite);
            sites.Add(bomExportCmdSite);

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

           //// Create Selection Info tab for Files
           //DetailPaneTab filePropertyTab = new DetailPaneTab("File.Tab.PropertyGrid",
           //                                            "Selection Info",
           //                                            SelectionTypeId.File,
           //                                            typeof(MyCustomTabControl));

           // //The propertyTab_SelectionChanged is called whenever our tab is active and the selection changes in the

           // //main grid.
           //filePropertyTab.SelectionChanged += propertyTab_SelectionChanged;
           // fileTabs.Add(filePropertyTab);

           // //Create Selection Info tab for Folders
           //DetailPaneTab folderPropertyTab = new DetailPaneTab("Folder.Tab.PropertyGrid",
           //                                            "Selection Info",
           //                                            SelectionTypeId.Folder,
           //                                            typeof(MyCustomTabControl));
           //folderPropertyTab.SelectionChanged += propertyTab_SelectionChanged;
           // fileTabs.Add(folderPropertyTab);

           // //Create Selection Info tab for Items
           //DetailPaneTab itemPropertyTab = new DetailPaneTab("Item.Tab.PropertyGrid",
           //                                            "Selection Info",
           //                                            SelectionTypeId.Item,
           //                                            typeof(MyCustomTabControl));
           //itemPropertyTab.SelectionChanged += propertyTab_SelectionChanged;
           // fileTabs.Add(itemPropertyTab);

           //// Create Selection Info tab for Change Orders

           //DetailPaneTab coPropertyTab = new DetailPaneTab("Co.Tab.PropertyGrid",
           //                                            "Selection Info",
           //                                            SelectionTypeId.ChangeOrder,
           //                                            typeof(MyCustomTabControl));
           // coPropertyTab.SelectionChanged += propertyTab_SelectionChanged;
           // fileTabs.Add(coPropertyTab);

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
        void ItemExportCommandHandler(object s, CommandItemEventArgs e)
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
                    Autodesk.Connectivity.WebServices.Item selectedItem = null;
                    if (selection.TypeId == SelectionTypeId.Item)
                    {
                        // our ISelection.Id is really a File.MasterId
                        selectedItem = connection.WebServiceManager.ItemService.GetItemsByIds(new long[] { selection.Id }).First();
                    }
                    //else if (selection.TypeId == SelectionTypeId.FileVersion)
                    //{
                    //    // our ISelection.Id is really a File.Id
                    //    selectedItem = connection.WebServiceManager.DocumentService.GetFileById(selection.Id);
                    //}

                    if (selectedItem == null)
                    {
                        MessageBox.Show("Selection is not an item.");
                    }
                    else
                    {
                        // this is the message we hope to see
                        //MessageBox.Show(String.Format("Hello World! The file size is: {0} bytes",
                        //selectedFile.FileSize));
                    }

                    VDF.Vault.Settings.AcquireFilesSettings settings = new VDF.Vault.Settings.AcquireFilesSettings(connection);

                    //VDF.Vault.Currency.Entities.FileIteration selFiles = new Vault.Currency.Entities.FileIteration(connection, selectedFile);


                    //Vault.Currency.Entities.FileIteration file = selFiles;

                    UpdateItem(selectedItem, connection);
                    okToProcess = true;
                    Execute(selectedItem, connection, okToProcess);
                }
            }
            catch (Exception ex)
            {
                // If something goes wrong, we don't want the exception to bubble up to Vault Explorer.
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        void BomItemExportCommandHandler(object s, CommandItemEventArgs e)
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
                    Autodesk.Connectivity.WebServices.Item selectedItem = null;
                    if (selection.TypeId == SelectionTypeId.Bom)
                    {
                        // our ISelection.Id is really a File.MasterId
                        selectedItem = connection.WebServiceManager.ItemService.GetItemsByIds(new long[] { selection.Id }).First();
                    }

                    if (selectedItem == null)
                    {
                        MessageBox.Show("Selection is not an item.");
                    }
                    else
                    {
                        VDF.Vault.Settings.AcquireFilesSettings settings = new VDF.Vault.Settings.AcquireFilesSettings(connection);

                        ItemAssoc[] subItemAssociations = GetChildItems(selectedItem, connection);

                        // first update the top level item
                        UpdateItem(selectedItem, connection);

                        // then also all the child items
                        ItemService itemSvc = connection.WebServiceManager.ItemService;
                        foreach (ItemAssoc subItemAssoc in subItemAssociations)
                        {
                            long subID = subItemAssoc.CldItemID;

                            Item subItem = itemSvc.GetItemsByIds(new long[] { subID })[0];

                            if (subItem != null)
                                UpdateItem(subItem, connection);
                        }

                        // attempt to update all items at once, i didn't get it to work yet...
                        //Item[] itemArray = new Item[subItemAssociations.Count() + 1];
                        //itemArray[0] = selectedItem;

                        //int index = 1;
                        //ItemService itemSvc = connection.WebServiceManager.ItemService;
                        //foreach (ItemAssoc assoc in subItemAssociations)
                        //{
                        //    long subID = assoc.CldItemID;
                        //    Item subItem = itemSvc.GetItemsByIds(new long[] { subID })[0];
                        //    itemArray[index] = subItem;
                        //    index++;
                        //}

                        //UpdateItems(itemArray, connection);
                        okToProcess = true;
                        Execute(selectedItem, connection, okToProcess);
                    }
                }
            }
            catch (Exception ex)
            {
                // If something goes wrong, we don't want the exception to bubble up to Vault Explorer.
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        void BomItemExportPreviewCommandHandler(object s, CommandItemEventArgs e)
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
                    Autodesk.Connectivity.WebServices.Item selectedItem = null;
                    if (selection.TypeId == SelectionTypeId.Bom)
                    {
                        // our ISelection.Id is really a File.MasterId
                        selectedItem = connection.WebServiceManager.ItemService.GetItemsByIds(new long[] { selection.Id }).First();
                    }
                    //else if (selection.TypeId == SelectionTypeId.FileVersion)
                    //{
                    //    // our ISelection.Id is really a File.Id
                    //    selectedItem = connection.WebServiceManager.DocumentService.GetFileById(selection.Id);
                    //}

                    if (selectedItem == null)
                    {
                        MessageBox.Show("Selection is not an item.");
                    }
                    else
                    {
                        // this is the message we hope to see
                        //MessageBox.Show(String.Format("Hello World! The file size is: {0} bytes",
                        //selectedFile.FileSize));
                    }

                    VDF.Vault.Settings.AcquireFilesSettings settings = new VDF.Vault.Settings.AcquireFilesSettings(connection);

                    //VDF.Vault.Currency.Entities.FileIteration selFiles = new Vault.Currency.Entities.FileIteration(connection, selectedFile);


                    //Vault.Currency.Entities.FileIteration file = selFiles;
                    okToProcess = false;
                    Execute(selectedItem, connection, okToProcess);
                }
            }
            catch (Exception ex)
            {
                // If something goes wrong, we don't want the exception to bubble up to Vault Explorer.
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        void BomItemExportOdooCommandHandler(object s, CommandItemEventArgs e)
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
                    Autodesk.Connectivity.WebServices.Item selectedItem = null;
                    if (selection.TypeId == SelectionTypeId.Bom)
                    {
                        // our ISelection.Id is really a File.MasterId
                        selectedItem = connection.WebServiceManager.ItemService.GetItemsByIds(new long[] { selection.Id }).First();
                    }
                    //else if (selection.TypeId == SelectionTypeId.FileVersion)
                    //{
                    //    // our ISelection.Id is really a File.Id
                    //    selectedItem = connection.WebServiceManager.DocumentService.GetFileById(selection.Id);
                    //}

                    if (selectedItem == null)
                    {
                        MessageBox.Show("Selection is not an item.");
                    }
                    else
                    {
                        // this is the message we hope to see
                        //MessageBox.Show(String.Format("Hello World! The file size is: {0} bytes",
                        //selectedFile.FileSize));
                    }

                    VDF.Vault.Settings.AcquireFilesSettings settings = new VDF.Vault.Settings.AcquireFilesSettings(connection);

                    //VDF.Vault.Currency.Entities.FileIteration selFiles = new Vault.Currency.Entities.FileIteration(connection, selectedFile);


                    //Vault.Currency.Entities.FileIteration file = selFiles;
                    okToProcess = false;
                    ExecuteOdoo(selectedItem, connection, okToProcess);
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

        ItemAssoc [] GetChildItems(Autodesk.Connectivity.WebServices.Item item, VDF.Vault.Currency.Connections.Connection connection)
        {
            // returns all the direct children of an item.
            try
            {
                if (item == null)
                {
                    MessageBox.Show("Select an Item first");
                    return null;
                }

                ItemService itemSvc = connection.WebServiceManager.ItemService;

                // change second parameter to true to return all children recursively
                return itemSvc.GetItemBOMAssociationsByItemIds(new long[] { item.Id }, false);
            }
            catch(Exception)
            {
                return null;
            }
        }

        // this function is not used
        void UpdateItems(Autodesk.Connectivity.WebServices.Item []itemArray , VDF.Vault.Currency.Connections.Connection connection)
        {
            long[] itemRevisionIds = new long[itemArray.Count()];
            //itemRevisionIds[0] = item.RevId;

            int index = 0;
            foreach(Item item in itemArray)
            {
                itemRevisionIds[index] = item.Id;
                index++;
            }

            Item[] itemsToCommit = new Item[0];
            long[] itemsToCommit_Ids = new long[0];
            try
            {
                ItemService itemSvc = connection.WebServiceManager.ItemService;

                // doesn't seem to be necessary to put items into edit state to update them
                //itemSvc.EditItems(itemRevisionIds);     

                itemSvc.UpdatePromoteComponents(itemRevisionIds, ItemAssignAll.Yes, false);

                DateTime now = DateTime.Now;

                GetPromoteOrderResults compO = itemSvc.GetPromoteComponentOrder(out now);

                if (compO.PrimaryArray != null)     // the only time this should happen anymore is if an item has children with no associations, e.g. manually created top level assemblies
                {
                    ArrayList compOrder = new ArrayList(compO.PrimaryArray);
                    ArrayList subSet = new ArrayList();
                    int setSize = 100;
                    int setNum = 0;

                    //get full sets
                    while (setNum < compOrder.Count / setSize)
                    {
                        subSet = compOrder.GetRange
                                      (setNum * setSize, setSize);
                        itemSvc.PromoteComponents
                      (now, (long[])subSet.ToArray(typeof(long)));
                        setNum++;
                    }

                    //get remaining set
                    if (compOrder.Count % setSize > 0)
                    {
                        subSet = compOrder.GetRange
                    (setNum * setSize, compOrder.Count % setSize);
                        itemSvc.PromoteComponents
                      (now, (long[])subSet.ToArray(typeof(long)));
                    }

                    ItemsAndFiles result = itemSvc.
                                 GetPromoteComponentsResults(now);
                    Item[] items = null;
                    items = result.ItemRevArray;
                    int[] statusArray = result.StatusArray;
                    // loop through the Items in the ItemRevArray
                    for (int i = 0; i < items.Length; i++)
                    {
                        // see if the item in the ItemRevArray
                        //has been updated (not equal to 1)
                        if (statusArray[i] != 1)
                        {
                            //change the size of the array
                            Array.Resize(ref itemsToCommit,
                                        itemsToCommit.Length + 1);
                            //add the updated item to the array
                            //that will be committed
                            itemsToCommit[itemsToCommit.Length - 1]
                                                          = items[i];
                        }


                    }
                    //commit the updated items             
                    itemSvc.UpdateAndCommitItems(itemsToCommit);
                    // Testing catch - this could cause error
                    // as items contains Items that may
                    // not need to be updated
                    // itemSvc.UpdateAndCommitItems(items);
                }
            }
            catch
            {
                // get the items that need to be undone
                for (int i = 0; i < itemsToCommit.Length; i++)
                {
                    // change the size of the array
                    // of Ids (long)
                    Array.Resize(ref itemsToCommit_Ids,
                               itemsToCommit_Ids.Length + 1);
                    //Add the id to the array that will be
                    // used in UndoEditItems()     
                    itemsToCommit_Ids
                        [itemsToCommit_Ids.Length - 1]
                                       = itemsToCommit[i].Id;
                }
                connection.WebServiceManager.ItemService.
                            UndoEditItems(itemsToCommit_Ids);

            }
        }

        void UpdateItem(Autodesk.Connectivity.WebServices.Item item, VDF.Vault.Currency.Connections.Connection connection)
        //*************************************************************************************
        // make sure this function gets updated in the VaultAccess Project if changes are made.
        //*************************************************************************************
        {

            if (item == null)
            {
                MessageBox.Show("Select an Item first");
                return;
            }

            long[] itemRevisionIds = new long[1];
            itemRevisionIds[0] = item.RevId;

            Item[] itemsToCommit = new Item[0];
            long[] itemsToCommit_Ids = new long[0];
            try
            {
                ItemService itemSvc = connection.WebServiceManager.ItemService;

                // put item into edit state so that timedate stamp gets updated.
                List<Item> topLevelItems = new List<Item>();
                topLevelItems = itemSvc.EditItems(itemRevisionIds).ToList();
                itemSvc.UpdateAndCommitItems(topLevelItems.ToArray());


                itemSvc.UpdatePromoteComponents(itemRevisionIds,ItemAssignAll.Default,false);

                DateTime now = DateTime.Now;

                GetPromoteOrderResults compO = itemSvc.GetPromoteComponentOrder(out now);

                if (compO.PrimaryArray != null)     // the only time this should happen anymore is if an item has children with no associations, e.g. manually created top level assemblies
                {
                    ArrayList compOrder = new ArrayList(compO.PrimaryArray);
                    ArrayList subSet = new ArrayList();
                    int setSize = 100;
                    int setNum = 0;

                    //get full sets
                    while (setNum < compOrder.Count / setSize)
                    {
                        subSet = compOrder.GetRange
                                      (setNum * setSize, setSize);
                        itemSvc.PromoteComponents
                      (now, (long[])subSet.ToArray(typeof(long)));
                        setNum++;
                    }

                    //get remaining set
                    if (compOrder.Count % setSize > 0)
                    {
                        subSet = compOrder.GetRange
                    (setNum * setSize, compOrder.Count % setSize);
                        itemSvc.PromoteComponents
                      (now, (long[])subSet.ToArray(typeof(long)));
                    }

                    ItemsAndFiles result = itemSvc.
                                 GetPromoteComponentsResults(now);
                    Item[] items = null;
                    items = result.ItemRevArray;
                    int[] statusArray = result.StatusArray;
                    // loop through the Items in the ItemRevArray
                    for (int i = 0; i < items.Length; i++)
                    {
                        // see if the item in the ItemRevArray
                        //has been updated (not equal to 1)
                        if (statusArray[i] != 1)
                        {
                            //change the size of the array
                            Array.Resize(ref itemsToCommit,
                                        itemsToCommit.Length + 1);
                            //add the updated item to the array
                            //that will be committed
                            itemsToCommit[itemsToCommit.Length - 1]
                                                          = items[i];
                        }
                    }
                    // commit the updated items

                    itemSvc.UpdateAndCommitItems(itemsToCommit);
                    // Testing catch - this could cause error
                    // as items contains Items that may
                    // not need to be updated
                    // itemSvc.UpdateAndCommitItems(items);
                }
            }
            catch
            {
                MessageBox.Show("There was a problem with updating this item.  Please manually update it and try again.");

                // get the items that need to be undone
                for (int i = 0; i < itemsToCommit.Length; i++)
                {
                    // change the size of the array
                    // of Ids (long)
                    Array.Resize(ref itemsToCommit_Ids,
                               itemsToCommit_Ids.Length + 1);
                    //Add the id to the array that will be
                    // used in UndoEditItems()     
                    itemsToCommit_Ids
                        [itemsToCommit_Ids.Length - 1]
                                       = itemsToCommit[i].Id;
                }
                connection.WebServiceManager.ItemService.
                            UndoEditItems(itemsToCommit_Ids);

            }
        }

        void Execute(Autodesk.Connectivity.WebServices.Item item, VDF.Vault.Currency.Connections.Connection connection, bool okToProcess)
        //*************************************************************************************
        // make sure this function gets updated in the VaultAccess Project if changes are made.
        //*************************************************************************************
        {
            string processFileName = AppSettings.Get("VaultExportFilePath").ToString() + "Process.txt";
            using (StreamWriter writer = new StreamWriter(processFileName))
            {
                if (okToProcess == false)
                    writer.WriteLine("false");
                else
                    writer.WriteLine("true");
            }

            string filename = (string)AppSettings.Get("ExportFile");

            PackageService packageSvc = connection.WebServiceManager.PackageService;

            // export to CSV file
            PkgItemsAndBOM pkgBom = packageSvc.GetLatestPackageDataByItemIds(new long[] { item.Id }, BOMTyp.Latest);

            // Create a mapping between Item properties and columns in the CSV file
            MapPair parentPair = new MapPair();
            parentPair.ToName = "Parent";
            parentPair.FromName = "BOMStructure-41FF056B-8EEF-47E2-8F9E-490BC0C52C71";

            MapPair numberPair = new MapPair();
            numberPair.ToName = "Number";
            numberPair.FromName = "Number";

            MapPair titlePair = new MapPair();
            titlePair.ToName = "Title (Item,CO)";
            titlePair.FromName = "Title(Item,CO)";

            MapPair descriptionPair = new MapPair();
            descriptionPair.ToName = "Item Description";
            descriptionPair.FromName = "Description(Item,CO)";

            MapPair categoryNamePair = new MapPair();
            categoryNamePair.ToName = "CategoryName";
            categoryNamePair.FromName = "CategoryName";

            MapPair thicknessPair = new MapPair();
            thicknessPair.ToName = "Thickness";
            thicknessPair.FromName = "7c5169ad-9081-4aa7-b1a3-4670edae0b8c";

            MapPair materialPair = new MapPair();
            materialPair.ToName = "Material";
            materialPair.FromName = "Material";

            MapPair operationsPair = new MapPair();
            operationsPair.ToName = "Operations";
            operationsPair.FromName = "794d5b7d-49b5-49ba-938a-7e341a7ff8e4";

            MapPair quantityPair = new MapPair();
            quantityPair.ToName = "Quantity";
            quantityPair.FromName = "Quantity-41FF056B-8EEF-47E2-8F9E-490BC0C52C71";

            MapPair structCodePair = new MapPair();
            structCodePair.ToName = "Structural Code";
            structCodePair.FromName = "e3811c7a-a3ee-4f67-b34e-cbc892640616";

            MapPair plantIDPair = new MapPair();
            plantIDPair.ToName = "Plant ID";
            plantIDPair.FromName = "eff195ae-da71-4929-b3df-2d6fd1e25f53";

            MapPair isStockPair = new MapPair();
            isStockPair.ToName = "Is Stock";
            isStockPair.FromName = "f78c17cd-86d1-4728-96b5-8001fb58b67f";

            MapPair requiresPDFPair = new MapPair();
            requiresPDFPair.ToName = "Requires PDF";
            requiresPDFPair.FromName = "6df4ae8b-fbd9-4e62-b801-a46097d4f9c5";

            MapPair commentPair = new MapPair();
            commentPair.ToName = "Comment";
            commentPair.FromName = "Comment";

            MapPair modDatePair = new MapPair();
            modDatePair.ToName = "Date Modified";
            modDatePair.FromName = "ModDate";

            MapPair statePair = new MapPair();
            statePair.ToName = "State";
            statePair.FromName = "State";

            MapPair stockNamePair = new MapPair();
            stockNamePair.ToName = "Stock Name";
            stockNamePair.FromName = "a42ca550-c503-4835-99dd-8c4d4ff6dbaf";

            MapPair keywordsPair = new MapPair();
            keywordsPair.ToName = "Keywords";
            keywordsPair.FromName = "Keywords";

            MapPair notesPair = new MapPair();
            notesPair.ToName = "Notes";
            notesPair.FromName = "0d012a5c-cc28-443c-b44e-735372eee117";

            MapPair revisionNumberPair = new MapPair();
            revisionNumberPair.ToName = "Revision";
            revisionNumberPair.FromName = "Revision";
            

            FileNameAndURL fileNameAndUrl = packageSvc.ExportToPackage(pkgBom, FileFormat.TDL_LEVEL,
                    new MapPair[] { parentPair, numberPair, titlePair, descriptionPair,categoryNamePair, thicknessPair,
                                    materialPair,operationsPair,quantityPair,structCodePair,plantIDPair,isStockPair,requiresPDFPair,
                                    commentPair,modDatePair,statePair,stockNamePair,keywordsPair,notesPair,revisionNumberPair});

            long currentByte = 0;
            long partSize = connection.PartSizeInBytes;
            using (FileStream fs = new FileStream(filename, FileMode.Create))
            {
                while (currentByte < fileNameAndUrl.FileSize)
                {
                    long lastByte = currentByte + partSize < fileNameAndUrl.FileSize ? currentByte + partSize : fileNameAndUrl.FileSize;
                    byte[] contents = packageSvc.DownloadPackagePart(fileNameAndUrl.Name, currentByte, lastByte);
                    fs.Write(contents, 0, (int)(lastByte - currentByte));
                    currentByte += partSize;
                }
            }

            // create a list to hold all the IDs of the BOM items
            List<long> idList = new List<long>();

            // create a dictionary to match up the exported ids with the exported item numbers
            Dictionary<string, long> bomDict = new Dictionary<string, long>();

            // loop through all the bomItems and extract the IDs along with the item numbers
            foreach(var v in pkgBom.PkgItemArray)
            {
                idList.Add(v.ID);
                bomDict.Add(v.ItemNum, v.ID);
            }

            // now create a list to store all the BOM items in
            List<BOMComp> bomList = new List<BOMComp>();

            // we now need to replace the item number with the primary file name field.  I can't figure out how to map it above, so we have to open the text file,
            // search through it line by line, and query the vault for the primary file name link of of each item number.  We then save the text file again with the
            // primary file name now in place of the item number....

            List<string> lineList = new List<string>(); // create a list of lines to save the file text in.

            using (StreamReader reader = System.IO.File.OpenText(filename))
            {
                string line;
                line = reader.ReadLine();       // first line is header, just save it the the list, don't process it
                lineList.Add(line);

                int lineNum = 1;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Replace("\"", "");

                    string[] items = line.Split('\t');
                    string origItemNumber = items[1];

                    // download all the bom items into a list in one API call
                    bomList = connection.WebServiceManager.ItemService.GetPrimaryComponentsByItemIds(idList.ToArray()).ToList();

                    // create another dictionary to match up the IDs with the bomComps
                    Dictionary<long, BOMComp> bomDict2 = new Dictionary<long, BOMComp>();

                    // populate the second dictionary
                    int index = 0;
                    foreach(long l in idList)
                    {
                        bomDict2.Add(l, bomList[index]);
                        index++;
                    }

                    long searchID = 0;
                    bomDict.TryGetValue(origItemNumber, out searchID);

                    BOMComp searchbomComp = new BOMComp();
                    bomDict2.TryGetValue(searchID, out searchbomComp);

                    long primaryLinkID = searchbomComp.XRefId;  // get the id of the primary linked file
                    if (primaryLinkID != -1)    // if we have a primary file name, use it
                    {
                        // we could likely speed this up some more if we grouped this all into one call rather than one by one, but code would get messier yet...
                        Autodesk.Connectivity.WebServices.File primaryLinkFile = connection.WebServiceManager.DocumentService.GetFileById(primaryLinkID);
                        string primaryLinkName = primaryLinkFile.Name;

                        items[1] = primaryLinkName;

                        string newItemString = "";

                        foreach(string s in items)
                        {
                            newItemString += s + "\t";
                        }
                        lineList.Add(newItemString);
                    }
                    else        // otherwise just use the item number, for example top level items will fall into this category
                    {
                        lineList.Add(line);
                    }

                    lineNum++;
                }
            }

            using (StreamWriter writer = new StreamWriter(filename,false))
            {
                foreach(string s in lineList)
                {
                    writer.WriteLine(s);
                }
            }
        }

        void ExecuteOdoo(Autodesk.Connectivity.WebServices.Item item, VDF.Vault.Currency.Connections.Connection connection, bool okToProcess)
        
        {
            string processFileName = AppSettings.Get("VaultExportFilePath").ToString() + "Process.txt";
            using (StreamWriter writer = new StreamWriter(processFileName))
            {
                if (okToProcess == false)
                    writer.WriteLine("false");
                else
                    writer.WriteLine("true");
            }

            //string filename = (string)AppSettings.Get("ExportFile");
            string filename1 = @"C:\Users\lorne\Desktop\Vault Export\odoo1.csv";
            string filename2 = @"C:\Users\lorne\Desktop\Vault Export\odoo2.csv";

            PackageService packageSvc = connection.WebServiceManager.PackageService;

            // export to CSV file
            PkgItemsAndBOM pkgBom = packageSvc.GetLatestPackageDataByItemIds(new long[] { item.Id }, BOMTyp.Latest);

            // Create a mapping between Item properties and columns in the CSV file
            MapPair parentPair = new MapPair();
            parentPair.ToName = "product";
            parentPair.FromName = "BOMStructure-41FF056B-8EEF-47E2-8F9E-490BC0C52C71";

            MapPair numberPair = new MapPair();
            numberPair.ToName = "name";
            numberPair.FromName = "Number";

            MapPair titlePair = new MapPair();
            titlePair.ToName = "Title (Item,CO)";
            titlePair.FromName = "Title(Item,CO)";

            MapPair descriptionPair = new MapPair();
            descriptionPair.ToName = "description";
            descriptionPair.FromName = "Description(Item,CO)";

            MapPair categoryNamePair = new MapPair();
            categoryNamePair.ToName = "CategoryName";
            categoryNamePair.FromName = "CategoryName";

            MapPair thicknessPair = new MapPair();
            thicknessPair.ToName = "Thickness";
            thicknessPair.FromName = "7c5169ad-9081-4aa7-b1a3-4670edae0b8c";

            MapPair materialPair = new MapPair();
            materialPair.ToName = "Material";
            materialPair.FromName = "Material";

            MapPair operationsPair = new MapPair();
            operationsPair.ToName = "Operations";
            operationsPair.FromName = "794d5b7d-49b5-49ba-938a-7e341a7ff8e4";

            MapPair quantityPair = new MapPair();
            quantityPair.ToName = "bom_line_ids/product_qty";
            quantityPair.FromName = "Quantity-41FF056B-8EEF-47E2-8F9E-490BC0C52C71";

            MapPair structCodePair = new MapPair();
            structCodePair.ToName = "Structural Code";
            structCodePair.FromName = "e3811c7a-a3ee-4f67-b34e-cbc892640616";

            MapPair plantIDPair = new MapPair();
            plantIDPair.ToName = "Plant ID";
            plantIDPair.FromName = "eff195ae-da71-4929-b3df-2d6fd1e25f53";

            MapPair isStockPair = new MapPair();
            isStockPair.ToName = "Is Stock";
            isStockPair.FromName = "f78c17cd-86d1-4728-96b5-8001fb58b67f";

            MapPair requiresPDFPair = new MapPair();
            requiresPDFPair.ToName = "Requires PDF";
            requiresPDFPair.FromName = "6df4ae8b-fbd9-4e62-b801-a46097d4f9c5";

            MapPair commentPair = new MapPair();
            commentPair.ToName = "Comment";
            commentPair.FromName = "Comment";

            MapPair modDatePair = new MapPair();
            modDatePair.ToName = "Date Modified";
            modDatePair.FromName = "ModDate";

            MapPair statePair = new MapPair();
            statePair.ToName = "State";
            statePair.FromName = "State";

            MapPair stockNamePair = new MapPair();
            stockNamePair.ToName = "Stock Name";
            stockNamePair.FromName = "a42ca550-c503-4835-99dd-8c4d4ff6dbaf";

            MapPair keywordsPair = new MapPair();
            keywordsPair.ToName = "Keywords";
            keywordsPair.FromName = "Keywords";

            MapPair notesPair = new MapPair();
            notesPair.ToName = "Notes";
            notesPair.FromName = "0d012a5c-cc28-443c-b44e-735372eee117";

            FileNameAndURL fileNameAndUrl = packageSvc.ExportToPackage(pkgBom, FileFormat.TDL_PARENT,
                    new MapPair[] { numberPair, descriptionPair });

            // write parts to file1
            long currentByte = 0;
            long partSize = connection.PartSizeInBytes;
            using (FileStream fs = new FileStream(filename1, FileMode.Create))
            {
                while (currentByte < fileNameAndUrl.FileSize)
                {
                    long lastByte = currentByte + partSize < fileNameAndUrl.FileSize ? currentByte + partSize : fileNameAndUrl.FileSize;
                    byte[] contents = packageSvc.DownloadPackagePart(fileNameAndUrl.Name, currentByte, lastByte);
                    fs.Write(contents, 0, (int)(lastByte - currentByte));
                    currentByte += partSize;
                }
            }

            // write bom to file2

            MapPair bomNumberPair = new MapPair();
            bomNumberPair.ToName = "bom_line_ids/product_id";
            bomNumberPair.FromName = "Number";

            fileNameAndUrl = packageSvc.ExportToPackage(pkgBom, FileFormat.TDL_PARENT,
                    new MapPair[] { parentPair, bomNumberPair, quantityPair });

            currentByte = 0;
            partSize = connection.PartSizeInBytes;
            using (FileStream fs = new FileStream(filename2, FileMode.Create))
            {
                while (currentByte < fileNameAndUrl.FileSize)
                {
                    long lastByte = currentByte + partSize < fileNameAndUrl.FileSize ? currentByte + partSize : fileNameAndUrl.FileSize;
                    byte[] contents = packageSvc.DownloadPackagePart(fileNameAndUrl.Name, currentByte, lastByte);
                    fs.Write(contents, 0, (int)(lastByte - currentByte));
                    currentByte += partSize;
                }
            }



        }

        private static void downloadFile(VDF.Vault.Currency.Connections.Connection connection, VDF.Vault.Currency.Entities.FileIteration file, string folderPath)
        //*************************************************************************************
        // make sure this function gets updated in the VaultAccess Project if changes are made.
        //*************************************************************************************
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
