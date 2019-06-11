using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Autodesk.Connectivity.Explorer.Extensibility;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;

using Autodesk.Connectivity.WebServices;
using Autodesk.Connectivity.WebServicesTools;
using VDF = Autodesk.DataManagement.Client.Framework;
using ACW = Autodesk.Connectivity.WebServices;
using ACWT = Autodesk.Connectivity.WebServicesTools;
using Framework = Autodesk.DataManagement.Client.Framework;
using Vault = Autodesk.DataManagement.Client.Framework.Vault;
using Forms = Autodesk.DataManagement.Client.Framework.Vault.Forms;
using ADSK = Autodesk.Connectivity.WebServices;
using Inventor;


using RadanInterface2;
using RadProject;
using BrightIdeasSoftware;
using Autodesk.DataManagement.Client.Framework.Vault.Currency.Properties;

namespace Camlink3_1
{
    public partial class ExportToSym : Form
    {
        private IEnumerable <Autodesk.Connectivity.Explorer.Extensibility.ISelection> selectionSet { get; set; }
        private List<Autodesk.Connectivity.WebServices.File> selectedFiles { get; set; }
        private List<PartToImport> PartsToImport { get; set; }
        private VDF.Vault.Currency.Connections.Connection connection { get; set; }
        private VDF.Vault.Currency.Properties.PropertyDefinitionDictionary propDefs { get; set; }
        private RadanInterface radInterface { get; set; }

        // read properties from settings file
        private string symFolderPrimary = Properties.Settings.Default["symFolder"].ToString();
        private string symFolderSecondary = Properties.Settings.Default["symFolder2"].ToString();
        private string extensionFolder = Properties.Settings.Default["extensionFolder"].ToString();
        private string useSecondarySymLocation = Properties.Settings.Default["useSecondarySymLocation"].ToString();

        #region objectListView delegates
        private void objectListView1_FormatRow(object sender, BrightIdeasSoftware.FormatRowEventArgs e)
        {
            PartToImport part = (PartToImport)e.Model;
            if (!System.IO.File.Exists(symFolderPrimary + part.name + ".sym"))
                e.Item.ForeColor = System.Drawing.Color.Gray;
        }
        #endregion

        #region constructors
        public ExportToSym()
        {
            InitializeComponent();
        }

        public ExportToSym(IEnumerable <Autodesk.Connectivity.Explorer.Extensibility.ISelection> selection, VDF.Vault.Currency.Connections.Connection conn)
        {
            InitializeComponent();
            selectionSet = selection;
            connection = conn;

            radInterface = new RadanInterface();
            radInterface.Initialize();

            selectedFiles = new List<ADSK.File>();

            // structure to hold list of files plus some extra attributes
            PartsToImport = new List<PartToImport>();       
            
            propDefs = new Vault.Currency.Properties.PropertyDefinitionDictionary();
            propDefs =
              connection.PropertyManager.GetPropertyDefinitions(
                VDF.Vault.Currency.Entities.EntityClassIds.Files,
                null,
                VDF.Vault.Currency.Properties.PropertyDefinitionFilter.IncludeUserDefined
              );

            PopulateListBox();

            txtBoxProject.Text = Properties.Settings.Default["radanProject"].ToString();
            txtBoxSymFolder.Text = symFolderPrimary;
            txtBoxSymFolder2.Text = symFolderSecondary;

            if (Properties.Settings.Default["useSecondarySymLocation"].ToString() == "yes")
            {
                checkBoxSecondarySymFolder.Checked = true;
                txtBoxSymFolder2.Enabled = true;
                btnBrowseForSym2.Enabled = true;
            }
            else
            {
                checkBoxSecondarySymFolder.Checked = false;
                txtBoxSymFolder2.Enabled = false;
                btnBrowseForSym2.Enabled = false;
            }
        }

        #endregion
        
        private void PopulateListBox()
        {
            PartsToImport.Clear();

            foreach (ISelection selection in selectionSet)
            {
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
                
                if (selectedFile != null)
                {
                    if (selectedFile.Name.EndsWith(".ipt"))
                    {
                        PartToImport part = new PartToImport();
                        part.name = selectedFile.Name.Replace(".ipt", "");

                        VDF.Vault.Currency.Entities.FileIteration fileIter = new Vault.Currency.Entities.FileIteration(connection, selectedFile);
                        part.desc = GetERPDescriptionProperty(fileIter);

                        part.thickness = radInterface.GetThicknessFromSym(symFolderPrimary + part.name + ".sym");
                        part.materialType = radInterface.GetMaterialTypeFromSym(symFolderPrimary + part.name + ".sym");
                        part.qty = 0;

                        PartsToImport.Add(part);
                        selectedFiles.Add(selectedFile);
                    }
                }
            }
            
            if(PartsToImport.Count > 0)
                objectListView1.SetObjects(PartsToImport);
        }

        private bool ConvertFile(string NameOfPartToConvert, ref string ErrorMessage)
        {
            toolStripStatusLabel.Text = "";
            progressBar.Value = 0;
            progressBar.PerformStep();
            string selectedItemIptName = NameOfPartToConvert + ".ipt";
            string errorMessage = "";
            string materialName = "";
            string partThickness = "";
            string topPattern = "";
            string partName = "";
            string partDescription = "";
            string partUnits = "in";
            Boolean overWriteConfirm = true;
            string openProjectName = "";
            string openNestName = "";

            try
            {
                openProjectName = radInterface.getOpenProjectName(ref errorMessage);

                string symFileNamePrimary = symFolderPrimary + NameOfPartToConvert + ".sym";
                string symFileNameSecondary = symFolderSecondary + NameOfPartToConvert + ".sym";

                if (System.IO.File.Exists(symFileNamePrimary))
                {
                    DialogResult result;
                    MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                    result = MessageBox.Show("The sym file already exists. Do you want to overwrite it?", "Confirm Overwrite", buttons);
                    if (result == DialogResult.Yes)
                    {
                        overWriteConfirm = true;
                    }
                    else
                    {
                        overWriteConfirm = false;
                        System.IO.File.SetLastWriteTimeUtc(symFileNamePrimary, DateTime.UtcNow);
                        if (System.IO.File.Exists(symFileNameSecondary))
                            System.IO.File.SetLastWriteTimeUtc(symFileNameSecondary, DateTime.UtcNow);
                    }
                }

                if (overWriteConfirm == true)
                {

                    Autodesk.Connectivity.WebServices.File webServicesFile = selectedFiles.FirstOrDefault(sel => sel.Name == selectedItemIptName);
                    VDF.Vault.Currency.Entities.FileIteration fileIter = new Vault.Currency.Entities.FileIteration(connection, webServicesFile);
                    PartToImport modifiedPart = new PartToImport();
                    string filePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), fileIter.EntityName);

                    PartToImport partToModify = PartsToImport.FirstOrDefault(sel => sel.name == NameOfPartToConvert);
                    int index = objectListView1.IndexOf(partToModify);
                    modifiedPart.name = PartsToImport[index].name;

                    partDescription = GetERPDescriptionProperty(fileIter);
                    modifiedPart.desc = partDescription;

                    // remove old ipt from temp folder to ensure we will use the lastest version
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.SetAttributes(filePath, FileAttributes.Normal);
                    System.IO.File.Delete(filePath);

                    radInterface.SaveNest(ref errorMessage);
                    openNestName = radInterface.getOpenNestName(ref errorMessage);

                    toolStripStatusLabel.Text = "Downloading File...";
                    DownloadFile(fileIter, filePath);
                    progressBar.PerformStep();

                    toolStripStatusLabel.Text = "Saving Radan Nest...";

                    if (!radInterface.SaveNest(ref errorMessage))
                    {
                        ErrorMessage = errorMessage;
                        toolStripStatusLabel.Text = errorMessage;
                        return false;
                    }
                    else
                        progressBar.PerformStep();

                    toolStripStatusLabel.Text = "Opening 3D File...";
                    if (!radInterface.Open3DFileInRadan(filePath, "", ref errorMessage))
                    {
                        ErrorMessage = errorMessage;
                        toolStripStatusLabel.Text = errorMessage;
                        return false;
                    }
                    else progressBar.PerformStep();

                    toolStripStatusLabel.Text = "Unfolding 3D File...";
                    if (!radInterface.UnfoldActive3DFile(ref partName, ref materialName, ref partThickness, ref topPattern, ref errorMessage))
                    {
                        ErrorMessage = errorMessage;
                        toolStripStatusLabel.Text = errorMessage;
                        return false;
                    }
                    else progressBar.PerformStep();

                    toolStripStatusLabel.Text = "Saving Sym File...";
                    if (!radInterface.SavePart(topPattern, symFileNamePrimary, ref errorMessage))
                    {
                        ErrorMessage = errorMessage;
                        toolStripStatusLabel.Text = errorMessage;
                        return false;
                    }
                    else progressBar.PerformStep();

                    toolStripStatusLabel.Text = "Setting Radan Attributes...";

                    // set up a list of strings containing all the Inventor material names.
                    List<string> materialNameList = new List<string>();
                    materialNameList.Add("Steel, Mild");
                    materialNameList.Add("44W");
                    materialNameList.Add("Rubber");
                    materialNameList.Add("1060 Carbon Steel");
                    materialNameList.Add("Steel, Mild");
                    materialNameList.Add("T-100,Steel");

                    // if the material name we read from Radan does not match a name on the list, default it to 'Steel, Mild'
                    if (!materialNameList.Contains(materialName))
                        materialName = "Steel, Mild";

                    if (!radInterface.InsertAttributes(symFileNamePrimary, materialName, partThickness, partUnits, partDescription, ref errorMessage))
                    {
                        ErrorMessage = errorMessage;
                        toolStripStatusLabel.Text = errorMessage;
                        return false;
                    }
                    else progressBar.PerformStep();

                    if (checkBoxSecondarySymFolder.Checked)
                    {
                        // delete the local file if it already exists
                        if (System.IO.File.Exists(symFileNameSecondary))
                            System.IO.File.Delete(symFileNameSecondary);
                        // and then copy the server version to the local
                        System.IO.File.Copy(symFileNamePrimary, symFileNameSecondary);
                    }
                    else progressBar.PerformStep();

                    toolStripStatusLabel.Text = "Done...";
                    double thickness = double.Parse(partThickness);

                    if (thickness <= 0.001)
                    {
                        toolStripStatusLabel.Text = "Part Thickness Could Not Be Calculated, Aborting Operation";
                        return false;
                    }
                    else
                    {
                        modifiedPart.thickness = thickness.ToString();
                        modifiedPart.materialType = materialName;
                        progressBar.PerformStep();
                    }

                    PartsToImport[index] = modifiedPart;

                    RadanInterface radanInterface = new RadanInterface();
                    char[] thumbnailCharArray = radanInterface.GetThumbnailDataFromSym(symFileNamePrimary);

                    if (thumbnailCharArray != null)
                    {
                        byte[] thumbnailByteArray = Convert.FromBase64CharArray(thumbnailCharArray, 0, thumbnailCharArray.Length);
                        picBoxSym.Image = ByteToImage(thumbnailByteArray);
                    }

                    if (openProjectName != "")
                    {
                        radInterface.LoadProject(openProjectName);
                    }

                    if(openNestName != "")
                    {
                        radInterface.openNest(openNestName,ref errorMessage);
                    }
                }
                return true;
            }

            catch (Exception)
            {
                errorMessage = "Error in converting File";
                return false;
            }
        
        }

        private void btnConvertAll_Click(object sender, EventArgs e)
        {
            string errMessage = "";

            for (int i = 0; i < PartsToImport.Count; i++)
            {
                errMessage = "";
                if (!ConvertFile(PartsToImport[i].name, ref errMessage))
                {
                    statusStrip.Text = "Error in converting File";
                }
                else
                {
                    statusStrip.Text = PartsToImport[i].name + " converted successfully.";
                }

                int tItemIndex = objectListView1.TopItemIndex;
                objectListView1.RefreshObjects(PartsToImport);
                objectListView1.Refresh();
                objectListView1.SetObjects(PartsToImport);
                objectListView1.TopItemIndex = tItemIndex;
                //objectListView1.SelectedItem.Focused = true;
            }

            statusStrip.Text = "Finished Converting List.";

        }

        private void btnProjectBrowse_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK) // Test result.
            {
                txtBoxProject.Text = openFileDialog1.FileName;
            }
        }

        private void txtBoxProject_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default["radanProject"] = txtBoxProject.Text;
            Properties.Settings.Default.Save(); // Saves settings in application configuration file
        }

        private void btnAddToProject_Click(object sender, EventArgs e)
        {
            radInterface.SaveProject();

            System.IO.Stream stream = null;
            PartToImport part = new PartToImport();
            RadanProject rPrj = new RadanProject();
            string path = Properties.Settings.Default["radanProject"].ToString();
            int numOfPartsImported = 0;

            for (int i = 0; i < PartsToImport.Count; i++)
            {
                part = PartsToImport[i];
                if (part.qty != 0)
                {
                    string partName;
                    if (!checkBoxSecondarySymFolder.Checked)
                        partName = symFolderPrimary + part.name + ".sym";
                    else
                        partName = symFolderSecondary + part.name + ".sym";

                    double partThickness = double.Parse(part.thickness);
                    int partQty = part.qty;
                    
                    try
                    {
                        if (System.IO.File.Exists(partName))
                        {
                            var timeOut = TimeSpan.FromSeconds(1);
                            stream = System.IO.File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                            stream.Flush();
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                XmlSerializer prjSerializer = new XmlSerializer(typeof(RadanProject));
                                rPrj = (RadanProject)prjSerializer.Deserialize(reader);

                                RadanPart prt = new RadanPart(partName,
                                                              rPrj.Parts.NextID,
                                                              PartsToImport[i].materialType,
                                                              partThickness,
                                                              "in",
                                                              partQty);
                                rPrj.Parts.NextID++;
                                rPrj.Parts.Add(prt);

                                stream.Close();
                                reader.Close();
                            }

                            stream = System.IO.File.Open(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                            //stream.Flush();
                            using (StreamWriter Writer = new StreamWriter(stream))
                            {
                                XmlSerializer prjSerializer = new XmlSerializer(typeof(RadanProject));

                                stream = System.IO.File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                                prjSerializer.Serialize(stream, rPrj);

                                stream.Close();
                                Writer.Close();

                            }
                        }
                        else
                        {
                            MessageBox.Show("No local sym file found, please try converting file again.");
                        }
                    }
                    catch (Exception)
                    {
                        //Console.WriteLine("Error opening XML file");
                        if (stream != null)
                            stream.Close();
                    }
                    numOfPartsImported++;
                }
            }
            
            radInterface.LoadProject(path);

            toolStripStatusLabel.Text = numOfPartsImported + " items added to project";
        }
        
        #region ListBox event handlers

        private void objectListView1_SelectionChanged(object sender, EventArgs e)
        {
            if (objectListView1.SelectedItem != null)
            {
                string selectedItemName = ((PartToImport)objectListView1.SelectedItem.RowObject).name;

                selectedItemName = selectedItemName + ".ipt";

                Autodesk.Connectivity.WebServices.File webServicesFile = selectedFiles.FirstOrDefault(sel => sel.Name == selectedItemName);
                VDF.Vault.Currency.Entities.FileIteration fileIter = new Vault.Currency.Entities.FileIteration(connection, webServicesFile);
                PropertyDefinition thumbnailPropDef = connection.PropertyManager.GetPropertyDefinitionBySystemName("Thumbnail");
                ThumbnailInfo thumbnailInfo = connection.PropertyManager.GetPropertyValue(fileIter, thumbnailPropDef, null) as ThumbnailInfo;
                byte[] thumbnailBytes = thumbnailInfo.Image;
                picBoxIpt.Image = ByteToImage(thumbnailBytes);

                string selectedSymName = symFolderPrimary + System.IO.Path.GetFileNameWithoutExtension(selectedItemName) + ".sym";

                if (System.IO.File.Exists(selectedSymName))
                {
                    RadanInterface radanInterface = new RadanInterface();
                    char[] thumbnailCharArray = radanInterface.GetThumbnailDataFromSym(selectedSymName);

                    if (thumbnailCharArray != null)
                    {
                        byte[] thumbnailByteArray = Convert.FromBase64CharArray(thumbnailCharArray, 0, thumbnailCharArray.Length);
                        picBoxSym.Image = ByteToImage(thumbnailByteArray);
                    }
                }
                else
                {
                    this.picBoxSym.Image = null;
                }

                toolStripStatusLabel.Text = "";
                progressBar.Value = 0;

                objectListView1.Refresh();
            }
        }
        
        private void objectListView1_DoubleClick(object sender, EventArgs e)
        {
            string selectedItemName = ((PartToImport)objectListView1.SelectedItem.RowObject).name;
            PartToImport partToModify = PartsToImport.FirstOrDefault(sel => sel.name == selectedItemName);


            string errorMessage = "";
            if (!ConvertFile(partToModify.name, ref errorMessage))
            {
                statusStrip.Text = "Error in converting File";
            }
            
            int tItemIndex = objectListView1.TopItemIndex;
            objectListView1.RefreshObjects(PartsToImport);
            objectListView1.Refresh();
            objectListView1.SetObjects(PartsToImport);
            objectListView1.TopItemIndex = tItemIndex;
            //objectListView1.SelectedItem.Focused = true;
        }
        
        
        #endregion

        #region Vault Access
        private void DownloadFile(ADSK.File file, string filePath)
        {
            // remove the read-only attribute
            if (System.IO.File.Exists(filePath))
                System.IO.File.SetAttributes(filePath, System.IO.FileAttributes.Normal);

            VDF.Vault.Settings.AcquireFilesSettings settings = new VDF.Vault.Settings.AcquireFilesSettings(connection);

            Vault.Currency.Entities.FileIteration fIter = new Vault.Currency.Entities.FileIteration(connection, file);
            settings.AddFileToAcquire(fIter, VDF.Vault.Settings.AcquireFilesSettings.AcquisitionOption.Download, new VDF.Currency.FilePathAbsolute(filePath));

            connection.FileManager.AcquireFiles(settings);
        }

        string GetERPDescriptionProperty(VDF.Vault.Currency.Entities.FileIteration fileInteration)
        {
            object partDesc = connection.PropertyManager.GetPropertyValue(
                        fileInteration, propDefs["Description"], null);

            string partDescString = partDesc == null ? "" : partDesc.ToString();

            return partDescString;
        }

        string GetERPPartNumberProperty(VDF.Vault.Currency.Entities.FileIteration fileInteration)
        {
            object partNumber = connection.PropertyManager.GetPropertyValue(
                        fileInteration, propDefs["Keywords"], null);

            string partNumberString = partNumber == null ? "" : partNumber.ToString();

            return partNumberString;
        }
        #endregion

        #region Thumbnail Routines
        public static Bitmap ByteToImage(byte[] blob)
        {
            MemoryStream mStream = new MemoryStream();
            byte[] pData = blob;
            mStream.Write(pData, 0, Convert.ToInt32(pData.Length));
            Bitmap bm = new Bitmap(mStream, false);
            mStream.Dispose();
            return bm;
        }


        #endregion

        private void btnSymFolderBrowse_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK) // Test result.
            {
                symFolderPrimary = folderBrowserDialog1.SelectedPath;
                if (!symFolderPrimary.EndsWith("\\")) symFolderPrimary += "\\";
                txtBoxSymFolder.Text = symFolderPrimary;

                Properties.Settings.Default["symFolder"] = symFolderPrimary;
                Properties.Settings.Default.Save(); // Saves settings in application configuration file

                PopulateListBox();

                int tItemIndex = objectListView1.TopItemIndex;
                objectListView1.RefreshObjects(PartsToImport);
                objectListView1.Refresh();
                objectListView1.SetObjects(PartsToImport);
                objectListView1.TopItemIndex = tItemIndex;
            }
        }

        private void btnBrowseForSym2_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK) // Test result.
            {
                symFolderSecondary = folderBrowserDialog1.SelectedPath;
                if (!symFolderSecondary.EndsWith("\\")) symFolderSecondary += "\\";
                txtBoxSymFolder2.Text = symFolderSecondary;

                Properties.Settings.Default["symFolder2"] = symFolderSecondary;
                Properties.Settings.Default.Save(); // Saves settings in application configuration file

                PopulateListBox();

                int tItemIndex = objectListView1.TopItemIndex;
                objectListView1.RefreshObjects(PartsToImport);
                objectListView1.Refresh();
                objectListView1.SetObjects(PartsToImport);
                objectListView1.TopItemIndex = tItemIndex;
            }
        }

        private void checkBoxSecondarySymFolder_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxSecondarySymFolder.Checked)
            {
                txtBoxSymFolder2.Enabled = true;
                btnBrowseForSym2.Enabled = true;
                Properties.Settings.Default["useSecondarySymLocation"] = "yes";
            }
            else
            {
                txtBoxSymFolder2.Enabled = false;
                btnBrowseForSym2.Enabled = false;
                Properties.Settings.Default["useSecondarySymLocation"] = "no";
            }
        }
    }

    public class PartToImport
    {
        public string name { get; set; }
        public string desc { get; set; }
        public string thickness { get; set; }
        public string materialType { get; set; }
        public int qty { get; set; }

    }
}
