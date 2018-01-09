using System;
using System.Collections.Generic;
using System.Reflection;

using Autodesk.Connectivity.Extensibility.Framework;
using Autodesk.Connectivity.Explorer.Extensibility;
using Autodesk.Connectivity.WebServices;
using Autodesk.Connectivity.WebServicesTools;


[assembly: AssemblyCompany("Horst Welding")]
[assembly: AssemblyProduct("JobProcessorUpdateFile")]
[assembly: AssemblyDescription("Updates PDF files")]
[assembly: ApiVersion("11.0")]
[assembly: ExtensionId("780d59d0-cacb-45c6-add7-8f0059219e09")]

namespace JobProcessorFileUpdate
{
    public class QueueFileUpdateCommandExtension : IExplorerExtension
    {

        public void QueueFileUpdateCommandHandler(object s, CommandItemEventArgs e)
        {
            // Queue a job
            //
            const string PublishJobTypeName = "Horst.File.FileUpdate";
            const string PublishJob_FileMasterId = "FileMasterId";
            const string PublishJob_FileName = "FileName";

            foreach (ISelection vaultObj in e.Context.CurrentSelectionSet)
            {
                JobParam[] paramList = new JobParam[2];
                JobParam masterIdParam = new JobParam();
                masterIdParam.Name = PublishJob_FileMasterId;
                masterIdParam.Val = vaultObj.Id.ToString();
                paramList[0] = masterIdParam;

                JobParam fileNameParam = new JobParam();
                fileNameParam.Name = PublishJob_FileName;
                fileNameParam.Val = vaultObj.Label;
                paramList[1] = fileNameParam;

                // Add the job to the queue
                //
                e.Context.Application.Connection.WebServiceManager.JobService.AddJob(
                    PublishJobTypeName, String.Format("File Update - {0}", fileNameParam.Val),
                    paramList, 10);
                
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
            CommandItem FileUpdateJobCmdItem = new CommandItem("Command.FileUpdateJob", "File Update");
            FileUpdateJobCmdItem.NavigationTypes = new SelectionTypeId[] { SelectionTypeId.File };
            FileUpdateJobCmdItem.MultiSelectEnabled = false;
            FileUpdateJobCmdItem.Execute += QueueFileUpdateCommandHandler;

            // deploy user history command on file context menu
            //
            CommandSite FileUpdateContextMenu = new CommandSite("Menu.FileContextMenu", "File Update");
            FileUpdateContextMenu.Location = CommandSiteLocation.FileContextMenu;
            FileUpdateContextMenu.DeployAsPulldownMenu = false;
            FileUpdateContextMenu.AddCommand(FileUpdateJobCmdItem);
            sites.Add(FileUpdateContextMenu);

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
