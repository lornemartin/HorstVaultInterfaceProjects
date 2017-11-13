using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Autodesk.Connectivity.WebServices;
using System.Net;
using System.Net.Mail;
using System.Data;
using System.ComponentModel;
using Autodesk.Connectivity.WebServicesTools;
using VDF = Autodesk.DataManagement.Client.Framework;

/*
December 29 2015

This code has been tested with the new vault.
it has been run once in simulation mode with the new vault and all looked good.

However I have to revert the following three file references back to the old versions for now so I could connect 
to the 2015 vault again for now:

-Autodesk.Connectivity.WebServices
-Autodesk.DataManagement.Client.Framework
-Autodesk.DataManagement.Client.Framework.Vault

Once we are ready to go live with Vault 2016, these 3 file references will have to be updated 
again to the 2016 versions.  Also obviously the task scheduler entry will have to be updated with the new
vault credentials

February 18 2016. - Updated references and re-compiled. 


*/
namespace ConsoleApplication1
{
    class Program
    {

        static VDF.Vault.Currency.Connections.Connection connection;
        static System.Collections.Specialized.StringCollection log = new System.Collections.Specialized.StringCollection();

        static System.IO.StreamWriter debugFile;

        static string symFolderName;
        static string pdfFolderName;
        static string debugFileName;
        static string vaultUsername;
        static string vaultPassword;
        static string vaultServer;
        static string vaultVault;
        static string simulateRun;
        static Boolean simulate;

        static void Main(string[] args)
        {
            // Specify the arguements on the command line, or in 
            // Visual Studio in the Project > Properties > Debug pane.
            if (args.Length < 7)
            {
                Console.WriteLine("Usage:  ReviewSymFiles <sym Folder Name> <debug file> <vault User Name> <vault password> <vault Server> <vault Vault> <Simulate Run Y/N> <PDF Folder Name>");
                //Console.ReadKey(); 
                Environment.Exit(1);
            }
           
            symFolderName = args[0];
            debugFileName = symFolderName + "\\" + args[1];
            vaultUsername = args[2];
            vaultPassword = args[3];
            vaultServer = args[4];
            vaultVault = args[5];
            simulateRun = args[6];
             pdfFolderName = args[7];
            if (simulateRun == "Y") simulate = true;
            
            if (!InitServices())
            {
                Environment.Exit(1);
            }
            InitFiles();

            

            SyncSymFiles(symFolderName);

            if (pdfFolderName != "") SyncPdfFiles(pdfFolderName);

            //Console.WriteLine("Press any key");
            //Console.ReadKey();

            CloseFiles();
        }

        private static void CloseFiles()
        {
            debugFile.WriteLine("Successfully synchronized sym files " + DateTime.Now);
            debugFile.Close();
        }

        public static void InitFiles()
        {
            debugFile = System.IO.File.AppendText(debugFileName);
            debugFile.WriteLine("************************************************");
            debugFile.WriteLine(DateTime.Now);
            debugFile.WriteLine("Syncing to Vault :" + vaultServer);
            if(simulate)
            {
                debugFile.WriteLine("***Simulated Run - NO FILES MODIFIED***");
            }
            debugFile.WriteLine("************************************************");
        }

        public static Boolean InitServices()
        {
            VDF.Vault.Results.LogInResult results = VDF.Vault.Library.ConnectionManager.LogIn(
                vaultServer,vaultVault,vaultUsername,vaultPassword, VDF.Vault.Currency.Connections.AuthenticationFlags.Standard, null
                );

            if (!results.Success)
                return false;

            connection = results.Connection;
            return true;
        }

       public static void SyncSymFiles(string symRoot)
       {
            // this function traverses through all the sym files in the symRoot folder.
            // for each file it calls the SearchVaultWorkspace function to search for the corresponding ipt file

            // Data structure to hold names of subfolders to be
            // examined for files.

           List<string>[] mailLists = new List<string>[3];
           List<string> outdatedList = new List<string>();
           List<string> missingList = new List<string>();
           List<string> speculationList = new List<string>();
          

            Stack<string> dirs = new Stack<string>(20);

            if (!System.IO.Directory.Exists(symRoot))
            {
                throw new ArgumentException();
            }
            dirs.Push(symRoot);

            while (dirs.Count > 0)
            {
                string currentDir = dirs.Pop();
                string[] subDirs;
                try
                {
                    subDirs = System.IO.Directory.GetDirectories(currentDir);
                }
                // An UnauthorizedAccessException exception will be thrown if we do not have
                // discovery permission on a folder or file. It may or may not be acceptable 
                // to ignore the exception and continue enumerating the remaining files and 
                // folders. It is also possible (but unlikely) that a DirectoryNotFound exception 
                // will be raised. This will happen if currentDir has been deleted by
                // another application or thread after our call to Directory.Exists. The 
                // choice of which exceptions to catch depends entirely on the specific task 
                // you are intending to perform and also on how much you know with certainty 
                // about the systems on which this code will run.
                catch (UnauthorizedAccessException e)
                {                    
                    Console.WriteLine(e.Message);
                    continue;
                }
                catch (System.IO.DirectoryNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }

                string[] files = null;
                try
                {
                    files = System.IO.Directory.GetFiles(currentDir, "*.sym");
                }

                catch (UnauthorizedAccessException e)
                {

                    Console.WriteLine(e.Message);
                    continue;
                }

                catch (System.IO.DirectoryNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }

                foreach (string file in files)
                {
                    try
                    {
                        System.IO.FileInfo symFile = new System.IO.FileInfo(file);

                        FileFolder iptFile = new FileFolder();

                        iptFile = SearchVault(symFile);

                        if (iptFile != null)
                        {
                            VDF.Vault.Currency.Entities.FileIteration searchFile =
                                new Autodesk.DataManagement.Client.Framework.Vault.Currency.Entities.FileIteration(connection, iptFile.File);

                            string comment = GetVaultCheckOutComment(searchFile, connection);

                            if (comment != "IM")
                            {


                                // we found a matching ipt file

                                //DateTime symDate = symFile.CreationTime;
                                DateTime symDate = symFile.LastWriteTime;
                                DateTime iptDate = iptFile.File.ModDate;
                                TimeSpan diff = symDate - iptDate;
                                if (diff.TotalSeconds < 0)  // ipt file has been modified since sym was created.
                                {
                                    // sym file is out of date
                                    debugFile.WriteLine(symFile.Name + " needs to be updated. It is older than its corresponding ipt file in the vault");
                                    debugFile.WriteLine("It will be moved to the non-controlled folder\n");
                                    outdatedList.Add(symFile.Name);
                                    Console.WriteLine("Oudated File Found");
                                    DirectoryInfo symFolder = new DirectoryInfo(symRoot);
                                    DirectoryInfo symParentFolder = symFolder.Parent;
                                    DirectoryInfo symFolderNonControlled = new DirectoryInfo(symParentFolder.FullName + "\\Vault Sym Files - non controlled");

                                    if (!simulate)
                                    {
                                        if (!symFolderNonControlled.Exists)
                                        {
                                            symFolderNonControlled.Create();
                                        }

                                        string nonControlledFileName = (symFolderNonControlled.FullName + "\\" + symFile.Name);
                                        FileInfo nonControlledFile = new FileInfo(nonControlledFileName);
                                        if (nonControlledFile.Exists) nonControlledFile.Delete();
                                        symFile.MoveTo(nonControlledFile.FullName);
                                        nonControlledFile.LastWriteTime = DateTime.Now;

                                        if (!nonControlledFile.Exists)
                                        {
                                            debugFile.WriteLine("Error in copying file");
                                        }
                                    }
                                }
                                else
                                {
                                    //file could still have been checked out and back in again without being modifed.
                                    //check for this yet, and flag it for speculation
                                    iptDate = iptFile.File.CkInDate;
                                    diff = symDate - iptDate;
                                    if (diff.TotalSeconds < 0)
                                    {
                                       speculationList.Add(symFile.Name);
                                        symFile.CreationTime = DateTime.Now;        // bump up the sym file timestamp so we won't flag it again tomorrow
                                    }
                                    int z = 10;//  this is  test line
                                }
                            }
                            else
                            {
                                // only item master properties changed, so we don't flag this file as out of date
                                debugFile.WriteLine("Only Item Master Properties Changed. " + symFile.Name);
                                debugFile.WriteLine(" It will not be moved to the non-controlled folder\n");
                            }
                            }
                            else
                            {
                                // no matching ipt file found, it is not currently in the vault
                                DirectoryInfo symFolder = new DirectoryInfo(symRoot);
                                DirectoryInfo symParentFolder = symFolder.Parent;
                                DirectoryInfo symFolderNonControlled = new DirectoryInfo(symParentFolder.FullName + "\\Vault Sym Files - non controlled");
                                if (!symFolderNonControlled.Exists)
                                {
                                    symFolderNonControlled.Create();
                                }

                                string nonControlledFileName = (symFolderNonControlled.FullName + "\\" + symFile.Name);
                                FileInfo nonControlledFile = new FileInfo(nonControlledFileName);

                                if (!simulate)
                                {
                                    if (nonControlledFile.Exists) nonControlledFile.Delete();
                                    symFile.MoveTo(nonControlledFile.FullName);
                                    nonControlledFile.LastWriteTime = DateTime.Now;
                                    if (!nonControlledFile.Exists)
                                    {
                                        debugFile.WriteLine("Error in copying file");
                                    }
                                }

                                debugFile.WriteLine("No ipt file found for " + symFile.Name);
                                debugFile.WriteLine(" It will be moved to the non-controlled folder\n");
                                missingList.Add(symFile.Name);
                            }

                        Console.WriteLine(Path.GetFileName(symFile.Name) + " is up to date");
                    }
                    catch (System.IO.FileNotFoundException e)
                    {
                        // If file was deleted by a separate application
                        //  or thread since the call to TravrerseSymTree()
                        // then just continue.
                        Console.WriteLine(e.Message);
                        continue;
                    }
                }

                // Push the subdirectories onto the stack for traversal.
                // This could also be done before handing the files.
                foreach (string str in subDirs)
                    dirs.Push(str);
            }

            mailLists[0] = outdatedList;
            mailLists[1] = missingList;
            mailLists[2] = speculationList;

            sendMail(mailLists);
        }

        public static void SyncPdfFiles(string pdfFolder)
        {
            // this function traverses through all the pdf files in the pdfFolder folder.
            // for each file it calls the SearchVaultWorkspace function to search for the corresponding ipt file

            // Data structure to hold names of subfolders to be
            // examined for files.
            
            List<string>[] mailLists = new List<string>[3];
            List<string> missingList = new List<string>();
            List<string> outdatedList = new List<string>();
            int fileCount = 0;

            Stack<string> dirs = new Stack<string>(20);

            if (!System.IO.Directory.Exists(pdfFolder))
            {
                throw new ArgumentException();
            }
            
            dirs.Push(pdfFolder);

            while (dirs.Count > 0)
            {
                string currentDir = dirs.Pop();
                string[] subDirs;
                try
                {
                    subDirs = System.IO.Directory.GetDirectories(currentDir);
                }
                // An UnauthorizedAccessException exception will be thrown if we do not have
                // discovery permission on a folder or file. It may or may not be acceptable 
                // to ignore the exception and continue enumerating the remaining files and 
                // folders. It is also possible (but unlikely) that a DirectoryNotFound exception 
                // will be raised. This will happen if currentDir has been deleted by
                // another application or thread after our call to Directory.Exists. The 
                // choice of which exceptions to catch depends entirely on the specific task 
                // you are intending to perform and also on how much you know with certainty 
                // about the systems on which this code will run.
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                catch (System.IO.DirectoryNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }

                string[] files = null;
                try
                {
                    files = System.IO.Directory.GetFiles(currentDir, "*.pdf");
                }

                catch (UnauthorizedAccessException e)
                {

                    Console.WriteLine(e.Message);
                    continue;
                }

                catch (System.IO.DirectoryNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }



                //foreach (string file in files)
                //{
                //    try
                //    {
                //        System.IO.FileInfo pdfFile = new System.IO.FileInfo(file);

                //        FileFolder iptFile = new FileFolder();

                //        iptFile = SearchVaultPDF(pdfFile);

                //        Console.Write("File Number " + fileCount++ + ":     Searching for " + System.IO.Path.GetFileName(pdfFile.Name) + " ");

                //        if (iptFile == null)
                //        {
                //            // no matching ipt file found, it is not currently in the vault

                //            if (!simulate)
                //            {
                //                pdfFile.Delete();
                //            }

                //            Console.Write("No file found\n");

                //            debugFile.WriteLine("No ipt file found for " + pdfFile.Name);
                //            debugFile.WriteLine(" It will be deleted\n");
                //            missingList.Add(pdfFile.Name);
                //        }
                //        else
                //        {
                //            Console.Write("Found it\n");
                //        }


                //    }
                //    catch (System.IO.FileNotFoundException e)
                //    {
                //        // If file was deleted by a separate application
                //        //  or thread since the call to TravrerseSymTree()
                //        // then just continue.
                //        Console.WriteLine(e.Message);
                //        continue;
                //    }
                //  }






                foreach (string file in files)
                {
                    try
                    {
                        System.IO.FileInfo pdfFile = new System.IO.FileInfo(file);

                        FileFolder iptFile = new FileFolder();

                        iptFile = SearchVaultPDF(pdfFile);

                        if (iptFile != null)
                        {
                            VDF.Vault.Currency.Entities.FileIteration searchFile =
                                new Autodesk.DataManagement.Client.Framework.Vault.Currency.Entities.FileIteration(connection, iptFile.File);

                            string comment = GetVaultCheckOutComment(searchFile, connection);

                            if (comment != "IM")
                            {
                                // we found a matching ipt file

                                //DateTime symDate = symFile.CreationTime;
                                DateTime symDate = pdfFile.LastWriteTime;
                                DateTime iptDate = iptFile.File.ModDate;
                                TimeSpan diff = symDate - iptDate;
                                if (diff.TotalSeconds < 0)  // ipt file has been modified since sym was created.
                                {
                                    // sym file is out of date
                                    debugFile.WriteLine(pdfFile.Name + " needs to be deleted. It is older than its corresponding ipt file in the vault");
                                    outdatedList.Add(pdfFile.Name);
                                    Console.WriteLine("Oudated File Found");
                                    

                                    if (!simulate)
                                    {
                                        outdatedList.Add(pdfFile.Name);
                                        pdfFile.Delete();
                                    }
                                }
                            }
                            else
                            {
                                // only item master properties changed, so we don't flag this file as out of date
                                debugFile.WriteLine("Only Item Master Properties Changed. " + pdfFile.Name);
                                debugFile.WriteLine(" It will not be deleted");
                            }
                        }
                        else
                        {
                            // no matching ipt file found, it is not currently in the vault

                            if (!simulate)
                            {
                                missingList.Add(pdfFile.Name);
                                pdfFile.Delete();
                            }

                            debugFile.WriteLine("No ipt file found for " + pdfFile.Name);
                            debugFile.WriteLine(" It will be deleted\n");
                            missingList.Add(pdfFile.Name);
                        }

                        Console.WriteLine(Path.GetFileName(pdfFile.Name) + " is up to date");
                    }
                    catch (System.IO.FileNotFoundException e)
                    {
                        // If file was deleted by a separate application
                        //  or thread since the call to TravrerseSymTree()
                        // then just continue.
                        Console.WriteLine(e.Message);
                        continue;
                    }
                }
























                // Push the subdirectories onto the stack for traversal.
                // This could also be done before handing the files.
                foreach (string str in subDirs)
                    dirs.Push(str);
            }

            mailLists[0] = missingList;
            mailLists[1] = outdatedList;

            debugFile.WriteLine(fileCount + " PDF Files Processed.");
            sendPDFMail(mailLists);
        }

        private static Boolean sendPDFMail(List<string>[] l)
        {
            // set up some variable for sending the notification emails.
            var fromAddress = new MailAddress("lrn.martin@gmail.com", "Automatic Updater");
            var toAddress = new MailAddress("lorne@horstwelding.com", "To Name");
            const string fromPassword = "hDr47IFHVRbw";
            string subject = "PDF File Updates";
            string body = null;

            if (simulate) subject = "Simulated PDF File Updates -- NO FILES MOVED";

            if (l[0] != null)
            {
                body = "Syncing to Vault :" + vaultServer;
                body += "----------------------------------------------\n";
                body += "Missing Files deleted:\n";
                body += "----------------------------------------------\n";
                foreach (string s in l[0])
                {
                    body += s;
                    body += "\n";
                }
            }

            if (l[1] != null)
            {
                body = "Syncing to Vault :" + vaultServer;
                body += "----------------------------------------------\n";
                body += "Out of Date Files deleted:\n";
                body += "----------------------------------------------\n";
                foreach (string s in l[1])
                {
                    body += s;
                    body += "\n";
                }
            }

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };
            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body
            })
            {
                if (l != null)
                {
                    smtp.Send(message);
                }
            }

            return true;
        }

        public static Boolean sendMail(List<string>[] l )
       {
           // set up some variable for sending the notification emails.
           var fromAddress = new MailAddress("lrn.martin@gmail.com", "Automatic Updater");
           var toAddress = new MailAddress("lorne@horstwelding.com", "To Name");
           MailAddress bcc = new MailAddress("henryt@horstwelding.com");
           const string fromPassword = "hDr47IFHVRbw";
           string subject = "Sym File Updates";
           string body = null;

            if (simulate) subject = "Simulated Sym File Updates -- NO FILES MOVED";

           if (l[0] != null)
           {
               body = "Syncing to Vault :" + vaultServer;
               body += "----------------------------------------------\n";
               body += "Modified Files moved to non-controlled folder:\n";
               body += "----------------------------------------------\n";
               foreach (string s in l[0])
               {
                   body += s;
                   body += "\n";
               }
           }
           if (l[1] != null)
           {

               body += "\n";
               body += "----------------------------------------------\n";
               body += "Missing Files moved to non-controlled folder:\n";
               body += "----------------------------------------------\n";
               foreach (string s in l[1])
               {
                   body += s;
                   body += "\n";
               }
           }

           if (l[2] != null)
           {
               body += "\n";
               body += "-------------------------------------------------------------\n";
               body += "Files that have been checked out and back in but not modified\n";
               body += "They will not be moved to the non-controlled folder\n";
               body += "-------------------------------------------------------------\n";
               foreach (string s in l[2])
               {
                   body += s;
                   body += "\n";
               }
           }

           var smtp = new SmtpClient
           {
               Host = "smtp.gmail.com",
               Port = 587,
               EnableSsl = true,
               DeliveryMethod = SmtpDeliveryMethod.Network,
               UseDefaultCredentials = false,
               Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
           };
           using (var message = new MailMessage(fromAddress, toAddress)
           {
               Subject = subject,
               Body = body
           })
           {
               if (l != null)
               {
                   message.Bcc.Add(bcc);
                   smtp.Send(message);
               }
           }

           return true;
       }

        public static FileFolder SearchVault(System.IO.FileInfo symFile)
       {
           // search the vault for the specified file
           // find the Filename property

           //string fname = getIptFileName(symFile.Name);
           string fname = (Path.GetFileNameWithoutExtension(symFile.Name) + ".ipt");

           Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties.PropertyDefinition propDef = connection.PropertyManager.GetPropertyDefinitionBySystemName("ClientFileName");

           if (propDef == null)
               throw new Exception("Error looking up FileName property");

           SrchCond condition = new SrchCond();

           condition.SrchTxt = fname;
           condition.SrchOper = 3;  //exact match
           condition.PropDefId = propDef.Id;
           condition.PropTyp = PropertySearchType.SingleProperty;

           List<FileFolder> resultList = new List<FileFolder>();
           SrchStatus status = null;
           string bookmark = string.Empty;
           

           while (status == null || resultList.Count < status.TotalHits)
           {
               //FileFolder[] results = docSrv.FindFileFoldersBySearchConditions(
               FileFolder[] results = connection.WebServiceManager.DocumentService.FindFileFoldersBySearchConditions(
                   new SrchCond[] { condition }, null, null, true, true, ref bookmark, out status);


               if (results != null)
               {
                   resultList.AddRange(results);
                   return resultList[0];
               }
               else
               {
                   return null;
               }
           }
           return null;
       }

        public static FileFolder SearchVaultPDF(System.IO.FileInfo pdfFile)
        {
            // search the vault for the specified file
            // find the Filename property

            //string fname = getIptFileName(symFile.Name);
            string fname = (Path.GetFileNameWithoutExtension(pdfFile.Name) + ".ipt");

            Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties.PropertyDefinition propDef = connection.PropertyManager.GetPropertyDefinitionBySystemName("ClientFileName");

            if (propDef == null)
                throw new Exception("Error looking up FileName property");

            SrchCond condition = new SrchCond();

            condition.SrchTxt = fname;
            condition.SrchOper = 3;  //exact match
            condition.PropDefId = propDef.Id;
            condition.PropTyp = PropertySearchType.SingleProperty;

            List<FileFolder> resultList = new List<FileFolder>();
            SrchStatus status = null;
            string bookmark = string.Empty;


            while (status == null || resultList.Count < status.TotalHits)
            {
                //FileFolder[] results = docSrv.FindFileFoldersBySearchConditions(
                FileFolder[] results = connection.WebServiceManager.DocumentService.FindFileFoldersBySearchConditions(
                    new SrchCond[] { condition }, null, null, true, true, ref bookmark, out status);


                if (results != null)
                {
                    resultList.AddRange(results);
                    return resultList[0];
                }
                else
                {
                    // search for assemblies yet too.
                    fname = (Path.GetFileNameWithoutExtension(pdfFile.Name) + ".iam");

                    propDef = connection.PropertyManager.GetPropertyDefinitionBySystemName("ClientFileName");

                    if (propDef == null)
                        throw new Exception("Error looking up FileName property");

                    condition = new SrchCond();

                    condition.SrchTxt = fname;
                    condition.SrchOper = 3;  //exact match
                    condition.PropDefId = propDef.Id;
                    condition.PropTyp = PropertySearchType.SingleProperty;

                    resultList = new List<FileFolder>();
                    status = null;
                    bookmark = string.Empty;


                    while (status == null || resultList.Count < status.TotalHits)
                    {
                        //FileFolder[] results = docSrv.FindFileFoldersBySearchConditions(
                        results = connection.WebServiceManager.DocumentService.FindFileFoldersBySearchConditions(
                            new SrchCond[] { condition }, null, null, true, true, ref bookmark, out status);


                        if (results != null)
                        {
                            resultList.AddRange(results);
                            return resultList[0];
                        }
                        else
                        {
                            return null;
                        }
                    }
                    return null;
                }
            }
            return null;
        }

        public static string GetVaultCheckOutComment(VDF.Vault.Currency.Entities.FileIteration selectedFile, VDF.Vault.Currency.Connections.Connection connection)
        {
            // a list of dictionaries of the vault properties of the associated files
            List<Dictionary<string, string>> vaultPropDictList = new List<Dictionary<string, string>>();

            // function that gets properties requires a list so we'll define a list and add the selected file to it...
            List<VDF.Vault.Currency.Entities.FileIteration> fileList = new List<VDF.Vault.Currency.Entities.FileIteration>();
            fileList.Add(selectedFile);

            // define a dictionary to hold all the properties pertaining to a vault entity
            VDF.Vault.Currency.Properties.PropertyDefinitionDictionary allPropDefDict = new VDF.Vault.Currency.Properties.PropertyDefinitionDictionary();
            allPropDefDict = connection.PropertyManager.GetPropertyDefinitions(VDF.Vault.Currency.Entities.EntityClassIds.Files, null, VDF.Vault.Currency.Properties.PropertyDefinitionFilter.IncludeAll);

            // define a list of only the properties that we are concerned about.
            List<VDF.Vault.Currency.Properties.PropertyDefinition> filteredPropDefList =
                    new List<VDF.Vault.Currency.Properties.PropertyDefinition>();

            List<string> propNames = new List<string> { "Comment" };

            // copy only definitions in propNames list from allPropDefDict to filteredPropDefList
            foreach (string s in propNames)
            {
                VDF.Vault.Currency.Properties.PropertyDefinition propDefs =
                    new Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties.PropertyDefinition();
                propDefs = allPropDefDict[s];

                filteredPropDefList.Add(propDefs);
            }

            // the following line should return a list of properties
            VDF.Vault.Currency.Properties.PropertyValues propValues = new Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties.PropertyValues();
            propValues = connection.PropertyManager.GetPropertyValues(fileList, filteredPropDefList, null);

            VDF.Vault.Currency.Properties.PropertyDefinition def = new VDF.Vault.Currency.Properties.PropertyDefinition(propNames[0]);
            VDF.Vault.Currency.Properties.PropertyValue val;

            Dictionary<VDF.Vault.Currency.Properties.PropertyDefinition, VDF.Vault.Currency.Properties.PropertyValue> d = propValues.GetValues(selectedFile);
            d.TryGetValue(def, out val);

            string key = null;
            if (def != null)
            {
                key = (def.SystemName == null) ? null : def.SystemName;

                string value = null;
                if (val != null)
                {
                    value = (val.Value == null) ? "" : val.Value.ToString();
                    return value;
                }
                else
                    value = "";
            }

            return null;

        }
    }
}


