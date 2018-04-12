/*=====================================================================
  
  This file is part of the Autodesk Vault API Code Samples.

  Copyright (C) Autodesk Inc.  All rights reserved.

THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
PARTICULAR PURPOSE.
=====================================================================*/


using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

using Autodesk.Connectivity.Extensibility.Framework;
using Autodesk.Connectivity.Explorer.Extensibility;
using Autodesk.Connectivity.WebServices;
using Autodesk.Connectivity.WebServicesTools;
using ADSK = Autodesk.Connectivity.WebServices;


[assembly: AssemblyCompany("Horst Welding")]
[assembly: AssemblyProduct("JobProcessorPrintPDF")]
[assembly: AssemblyDescription("PDFs an idw file")]
[assembly: ApiVersion("11.0")]
[assembly: ExtensionId("525ae223-2486-4e02-98aa-77a617b47604")]

namespace JobProcessorPrintPDF
{
    public class QueuePrintPDFCommandExtension : IExplorerExtension
    {
        public void QueuePrintPDFCommandHandler(object s, CommandItemEventArgs e)
        {
            // Queue a job
            //
            const string PrintJobTypeName = "Horst.File.PrintPDF";
            const string PrintJob_EntityId = "EntityId";  // this was changed from FileMasterID to EntityID


            foreach (ISelection vaultObj in e.Context.CurrentSelectionSet)
            {
                JobParam[] paramList = new JobParam[3];

                JobParam entityIdParam = new JobParam();
                entityIdParam.Name = PrintJob_EntityId;
                entityIdParam.Val = vaultObj.Id.ToString();
                paramList[0] = entityIdParam;

                JobParam fileNameParam = new JobParam();
                fileNameParam.Name = "EntityClassId";
                fileNameParam.Val = "USER-REQUESTED " + vaultObj.Label;
                paramList[1] = fileNameParam;

                JobParam lifeCycleTransitionParam = new JobParam();
                lifeCycleTransitionParam.Name = "LifeCycleTransitionId";
                lifeCycleTransitionParam.Val = "88";
                paramList[2] = lifeCycleTransitionParam;

                // Add the job to the queue
                //
                e.Context.Application.Connection.WebServiceManager.JobService.AddJob(
                    PrintJobTypeName, String.Format("Print PDF - {0}", fileNameParam.Val),
                    paramList, 100);
            }
        }

        public void QueuePrintPDFFolderCommandHandler(object s, CommandItemEventArgs e)
        {
            using (StreamWriter debugFile = new StreamWriter(@"C:\Users\lorne\Documents\Pdfs\debug10.txt", true))
            {
                foreach (ISelection vaultObj in e.Context.CurrentSelectionSet)
                {
                    //ISelection vaultObj = null;
                    //vaultObj = (ISelection)e.Context.CurrentSelectionSet;

                    debugFile.WriteLine(vaultObj.Id.ToString());
                    Folder rootFolder = e.Context.Application.Connection.WebServiceManager.DocumentService.GetFolderById(vaultObj.Id);
                    debugFile.WriteLine(rootFolder.ToString());
                    VaultFoldertoPDF(rootFolder, e.Context.Application.Connection.WebServiceManager);
                }
            }

            
        }

        private void VaultFoldertoPDF(Folder folder,WebServiceManager mgr)
        {
            const string PrintJobTypeName = "Horst.File.PrintPDF";
            const string PrintJob_EntityId = "EntityId";

            if (folder.Cloaked)
                return;

            ADSK.File[] files = mgr.DocumentService.GetLatestFilesByFolderId(
                folder.Id, true);
            if (files != null)
            {
                foreach (ADSK.File file in files)
                {
                    if (file.Cloaked)
                        continue;

                    if (!file.Name.EndsWith(".idw"))
                        continue;

                    if (!(file.FileLfCyc.LfCycStateName == "Released"))     // only process released idws
                        continue;

                    JobParam[] paramList = new JobParam[3];

                    JobParam entityIdParam = new JobParam();
                    entityIdParam.Name = PrintJob_EntityId;
                    entityIdParam.Val = file.MasterId.ToString();
                    paramList[0] = entityIdParam;

                    JobParam fileNameParam = new JobParam();
                    fileNameParam.Name = "EntityClassId";
                    fileNameParam.Val = "USER-REQUESTED " + file.Name;
                    paramList[1] = fileNameParam;

                    JobParam lifeCycleTransitionParam = new JobParam();
                    lifeCycleTransitionParam.Name = "LifeCycleTransitionId";
                    lifeCycleTransitionParam.Val = "88";
                    paramList[2] = lifeCycleTransitionParam;

                    // Add the job to the queue
                    //
                    mgr.JobService.AddJob(
                        PrintJobTypeName, String.Format("Print PDF - {0}", fileNameParam.Val),
                        paramList, 100);
                }
            }

            Folder[] subFolders = mgr.DocumentService.GetFoldersByParentId(folder.Id, false);
            if (subFolders != null)
            {
                foreach (Folder subFolder in subFolders)
                {
                    VaultFoldertoPDF(subFolder,mgr);
                }
            }
        }

        public string ResourceCollectionName()
        {
            return String.Empty;
        }

        public IEnumerable<CommandSite> CommandSites()
        {
            List<CommandSite> sites = new List<CommandSite>();

            // Describe user history command item
            //
            CommandItem printPDFJobCmdItem = new CommandItem("Command.PrintPDFJob", "Print PDF Job");
            printPDFJobCmdItem.NavigationTypes = new SelectionTypeId[] { SelectionTypeId.File };
            printPDFJobCmdItem.MultiSelectEnabled = false;
            printPDFJobCmdItem.Execute += QueuePrintPDFCommandHandler;

            // deploy user history command on file context menu
            //
            CommandSite printPDFContextMenu = new CommandSite("Menu.FileContextMenu", "Print PDF Job");
            printPDFContextMenu.Location = CommandSiteLocation.FileContextMenu;
            printPDFContextMenu.DeployAsPulldownMenu = false;
            printPDFContextMenu.AddCommand(printPDFJobCmdItem);
            sites.Add(printPDFContextMenu);


            CommandItem printPDFFolderCmdItem = new CommandItem("Command.PrintPDFFolder", "Print PDF Folder");
            printPDFFolderCmdItem.NavigationTypes = new SelectionTypeId[] { SelectionTypeId.Folder };
            printPDFFolderCmdItem.MultiSelectEnabled = false;
            printPDFFolderCmdItem.Execute += QueuePrintPDFFolderCommandHandler;

            
            CommandSite printPDFFolderContextMenu = new CommandSite("Menu.FileContextMenu", "Print PDF Folder");
            printPDFFolderContextMenu.Location = CommandSiteLocation.FolderContextMenu;
            printPDFFolderContextMenu.DeployAsPulldownMenu = false;
            printPDFFolderContextMenu.AddCommand(printPDFFolderCmdItem);
            sites.Add(printPDFFolderContextMenu);
            
            return sites;
        }

        public IEnumerable<DetailPaneTab> DetailTabs()
        {
            return null;
        }
       
        public void OnLogOn(IApplication application)
        {
            // NoOp;
        }

        public void OnLogOff(IApplication application)
        {
            // NoOp;
        }

        public void OnStartup(IApplication application)
        {
            // NoOp;
        }

        public void OnShutdown(IApplication application)
        {
            // NoOp;
        }

        public IEnumerable<string> HiddenCommands()
        {
            return null;
        }

        public IEnumerable<CustomEntityHandler> CustomEntityHandlers()
        {
            return null;
        }
    }
}
