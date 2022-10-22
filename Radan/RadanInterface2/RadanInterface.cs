using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Linq.Expressions;

namespace RadanInterface2
{
    public class RadanInterface
    {
        //private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType); 

        XNamespace symNameSpace = "http://www.radan.com/ns/rcd";
        const string MATERIAL_ATT_NUMBER = "119";
        const string THICKNESS_ATT_NUMBER = "120";
        const string UNIT_ATT_NUMBER = "121";
        const string DESCRIPTION_ATT_NUMBER = "200";
        const string ORDER_NUMBER_ATT_NUMBER = "201";
        const string SCHEDULE_NAME_ATT_NUMBER = "202";
        const string BATCH_NAME_ATT_NUMBER = "203";
        const string ATT_ELEMENT = "Attr";
        const string NUM_ATTRIBUTE = "num";
        const string VALUE_ATTRIBUTE = "num";

        private static Radraft.Interop.Application rApp;

        public Boolean Initialize()
        {
            try
            {
                Object obj = null;
                obj = Marshal.GetActiveObject("Radraft.Application");
                rApp = (Radraft.Interop.Application)obj;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public Boolean IsActive()
        {
            if (rApp != null)
                return true;
            else
                return false;
        }

        public bool Open3DFileInRadan(string FilePath, string MaterialName, ref string ErrorMessage)
        {
            double ModelScale = 1;
            double PartThickness = 1;

            try
            {
                rApp.Mac.Execute("set_gui_state(GS_NEW3D)");
                rApp.NewDrawing(true);

                if (rApp.Mac.mfl_read_model("inventor", false, false, false, false, false, ModelScale, PartThickness, MaterialName, FilePath) != true)
                {
                    ErrorMessage = "Unable to read the Inventor file: " + FilePath;
                    return false;
                }

                ErrorMessage = "";
                return true;
            }
            catch (Exception x)
            {
                ErrorMessage = "An error occured trying to read the Inventor model " + FilePath + ".\r\n\r\nError Reports:" + x.Message;
                return false;
            }
        }

        public bool UnfoldActive3DFile(ref string partName, ref string MaterialName, ref string PartThickness, ref string TopPattern, ref string ErrorMessage)
        {
            string sIdnet = "";
            bool ContinueProcess = false;
            int type = 0;
            int nIDCount = 0;

            try
            {
                rApp.Mac.mfl_info_scan_start();
                ContinueProcess = rApp.Mac.mfl_info_scan_start();

                if (ContinueProcess == true)
                {
                    nIDCount = nIDCount + 1;
                    sIdnet = rApp.Mac.mfl_info_scan_next();

                    if (sIdnet != "")
                    {
                        TopPattern = "/NEW RAD 3D/Developments/unfold" + nIDCount.ToString() + "TOP";
                        rApp.Mac.mfl_ray_start(0, 0, 0);
                        type = rApp.Mac.MFL_U_TYPE_DEFAULT;

                        rApp.Mac.MFL_U_TOP_SURFACE_SPEC = true;
                        rApp.Mac.MFL_U_MATERIAL_SPEC = MaterialName;
                        rApp.Mac.mfl_object_info(sIdnet, false);
                        rApp.Mac.MFL_U_DEV_NAME_SPEC = "/unfold" + nIDCount.ToString() + "TOP";

                        if (rApp.Mac.mfl_auto_unfold(sIdnet))
                        {
                            try
                            {
                                if (String.IsNullOrEmpty(PartThickness))
                                {
                                    PartThickness = Math.Round(Convert.ToDecimal(rApp.Mac.MFL_U_THICKNESS_RES), 3).ToString();
                                }
                                else
                                {
                                    PartThickness = Math.Round(Convert.ToDecimal(PartThickness), 3).ToString();
                                }
                            }
                            catch (Exception)
                            {
                                if (String.IsNullOrEmpty(PartThickness))
                                {
                                    PartThickness = rApp.Mac.MFL_U_THICKNESS_RES.ToString();
                                }
                            }

                            try
                            {
                                if (String.IsNullOrEmpty(MaterialName))
                                {
                                    // if material type can not be read, we will default to Steel, Mild
                                    MaterialName = "Steel, Mild";
                                }
                                else
                                {
                                    MaterialName = "Undefined Material";
                                }
                            }
                            catch (Exception)
                            {
                                if (String.IsNullOrEmpty(MaterialName))
                                {
                                    MaterialName = "Undefined Material";
                                }
                            }

                            try
                            {
                                if (String.IsNullOrEmpty(partName))
                                {
                                    partName = rApp.Mac.ND_NAME1;
                                }
                                else
                                {
                                    MaterialName = "Undefined Name";
                                }
                            }
                            catch (Exception)
                            {
                                if (String.IsNullOrEmpty(partName))
                                {
                                    MaterialName = "Undefined Name";
                                }
                            }


                            return true;
                        }
                        else
                        {
                            ErrorMessage = "There was an unknown error unfolding the Inventor part within Radan.";
                            return false;
                        }
                    }
                    else
                    {
                        ErrorMessage = "There were no 3D entites found within the Inventor model for Radan to process.";
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = "Radan was unable to initialize 3D entities within Radan";
                    return false;
                }
            }
            catch (Exception)
            {
                ErrorMessage = "There was a process error within Radan when the 3D model was unfolded";
                return false;
            }
        }

        public bool HasBends(string filePath)
        {
            try
            {
                rApp.OpenSymbol(filePath, true, "");
                bool hasBend = false;
                string lineType = "";
                Radraft.Interop.Mac mac = rApp.Mac;


                rApp.Mac.scan(mac.PART_PATTERN, "l", 0);
                while (rApp.Mac.next() != 0)
                {
                    rApp.Mac.find();

                    lineType = RadanLineString(rApp.Mac.LT0);
                    var v3 = rApp.Mac.F_BEND_ID0;

                    if (v3!="" || lineType.Contains("Dash") || lineType.Contains("Chain")) // if v3 has something in it, or if linetype is dashed or chained, we found a bend line
                    {
                        hasBend = true;
                        break;
                    }
                }

                rApp.Mac.end_scan();

                if (hasBend)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string RadanLineString(uint LT0)
        {
            if (LT0 == 1)
            {
                return "Full";
            }
            else if (LT0 == 2)
            {
                return "Dash";
            }
            else if (LT0 == 3)
            {
                return "Chain";
            }
            else if (LT0 == 4)
            {
                return "Random";
            }
            else if (LT0 % 64 == 5)
            {
                return "Dash 2";
            }
            else if (LT0 % 64 == 6)
            {
                return "Chain 2";
            }
            else if (LT0 % 64 == 7)
            {
                return "Random 2";
            }
            else if (LT0 % 64 == 8)
            {
                return "Double Dash Chain";
            }
            else if (LT0 % 64 == 9)
            {
                return "Hedge";
            }
            else if (LT0 % 64 == 10)
            {
                return "Cross Fence";
            }
            else if (LT0 % 64 == 11)
            {
                return "Square Fence";
            }
            else if (LT0 % 64 == 12)
            {
                return "Zig Zag";
            }
            else
            {
                return "Full";
            }
        }

        public bool SavePart(string TopPattern, string SymFilePath, ref string ErrorMessage)
        {
            string command = "save_symbol(S0, S1, G0, G1, G2)";

            try
            {
                if (SymFilePath.Contains('_'))
                {
                    ErrorMessage = "Sym File contains an illegal character, cannot save.";
                    return false;
                }
                rApp.Mac.SetString("S0", TopPattern);
                rApp.Mac.SetString("S1", SymFilePath);
                rApp.Mac.SetNumber("G0", 0);
                rApp.Mac.SetNumber("G1", 5);
                rApp.Mac.SetNumber("G2", 0);

                if (rApp.Mac.Execute(command) == true)
                {
                    return true;
                }
                else
                {
                    ErrorMessage = "Unable to save the part " + SymFilePath;
                    return false;
                }
            }
            catch (Exception x)
            {
                ErrorMessage = "Error saving the part " + SymFilePath + "\r\n\r\nError Reports:\r\n" + x.Message;
                return false;
            }
        }

        public bool SavePart(string symFile, ref string ErrorMessage)
        {
            rApp.OpenSymbol(symFile, false, "");
            rApp.ActiveDocument.Save();
            return true;
        }

        public bool InsertAttributes(string SymFilePath, string MaterialName, string MaterialThickness, string unitType, string partDesc, ref string ErrorMessage)
        {
            #region oldcode
            //if (!System.IO.File.Exists(SymFilePath))
            //{
            //    ErrorMessage = "The SYM file does not exist and material and thickness cannot be inserted";
            //    return false;
            //}
            //try
            //{
            //    XDocument symDoc = XDocument.Load(SymFilePath);

            //    // set material name attribute
            //    XElement temp = symDoc.Descendants(symNameSpace + ATT_ELEMENT)
            //                                    .Where(t => t.Attribute(NUM_ATTRIBUTE).Value == MATERIAL_ATT_NUMBER).FirstOrDefault();
            //    if (temp == null)
            //    {
            //        ErrorMessage = "The material element (" + MATERIAL_ATT_NUMBER + ") is missing in the SYM file, material cannot be inserted";
            //        return false;
            //    }
            //    if (temp.Attribute(VALUE_ATTRIBUTE) == null)
            //    {
            //        XAttribute value = new XAttribute(VALUE_ATTRIBUTE, MaterialName);
            //        temp.Add(value);
            //    }
            //    else
            //    {
            //        temp.SetAttributeValue("value", MaterialName);
            //    }

            //    // set material thickness attribute
            //    temp = symDoc.Descendants(symNameSpace + ATT_ELEMENT)
            //                .Where(t => t.Attribute(NUM_ATTRIBUTE).Value == THICKNESS_ATT_NUMBER).FirstOrDefault();
            //    if (temp == null)
            //    {
            //        ErrorMessage = "The material thickness element (" + THICKNESS_ATT_NUMBER + ") is missing in the SYM file, thickness cannot be inserted";
            //        return false;
            //    }
            //    if (temp.Attribute(VALUE_ATTRIBUTE) == null)
            //    {
            //        XAttribute value = new XAttribute(VALUE_ATTRIBUTE, MaterialThickness);
            //        temp.Add(value);
            //    }
            //    else
            //    {
            //        temp.SetAttributeValue("value", MaterialThickness);
            //    }

            //    // set part description attribute
            //    temp = symDoc.Descendants(symNameSpace + ATT_ELEMENT)
            //                .Where(t => t.Attribute(NUM_ATTRIBUTE).Value == DESCRIPTION_ATT_NUMBER).FirstOrDefault();
            //    if (temp == null)
            //    {
            //        ErrorMessage = "The part description element (" + DESCRIPTION_ATT_NUMBER + ") is missing in the SYM file, description cannot be inserted";
            //        return false;
            //    }
            //    if (temp.Attribute(VALUE_ATTRIBUTE) == null)
            //    {
            //        XAttribute value = new XAttribute(VALUE_ATTRIBUTE, partDesc);
            //        temp.Add(value);
            //    }
            //    else
            //    {
            //        temp.SetAttributeValue("value", partDesc);
            //    }


            //    // set part unit attribute
            //    temp = symDoc.Descendants(symNameSpace + ATT_ELEMENT)
            //                .Where(t => t.Attribute(NUM_ATTRIBUTE).Value == UNIT_ATT_NUMBER).FirstOrDefault();
            //    if (temp == null)
            //    {
            //        ErrorMessage = "The part unit element (" + UNIT_ATT_NUMBER + ") is missing in the SYM file, description cannot be inserted";
            //        return false;
            //    }
            //    if (temp.Attribute(VALUE_ATTRIBUTE) == null)
            //    {
            //        XAttribute value = new XAttribute(VALUE_ATTRIBUTE, partDesc);
            //        temp.Add(value);
            //    }
            //    else
            //    {
            //        temp.SetAttributeValue("value", unitType);
            //    }

            //    // save sym file
            //    try
            //    {
            //        symDoc.Save(SymFilePath);
            //        return true;
            //    }
            //    catch (Exception x)
            //    {
            //        ErrorMessage = "An IO Error occured and material and thickness cannot be inserted\r\n\r\nError Reports\r\n" + x.Message;
            //        return false;
            //    }
            //}
            //catch (Exception x)
            //{
            //    ErrorMessage = "An IO Error occured and material and thickness cannot be inserted\r\n\r\nError Reports\r\n" + x.Message;
            //    return false;
            //}
            #endregion

            if (!System.IO.File.Exists(SymFilePath))
            {
                ErrorMessage = "The SYM file does not exist and attributes cannot be inserted";
                return false;
            }
            try
            {
                string folderName = System.IO.Path.GetDirectoryName(SymFilePath);
                string debugName = Path.Combine(folderName, "debug.log");
                using (StreamWriter writer = new StreamWriter(debugName))
                {
                    // get a handle to a brand new set of attributes
                    int newHandle = rApp.Mac.att_new(SymFilePath);
                    writer.WriteLine("newHandle = " + newHandle);

                    // get a handle to the existing attributes
                    int oldHandle = rApp.Mac.att_load(SymFilePath, false);
                    writer.WriteLine("oldHandle = " + oldHandle);

                    bool success1 = rApp.Mac.att_set_value(newHandle, 119, MaterialName);
                    writer.WriteLine("Material success = " + MaterialName + " " + success1);

                    bool success2 = rApp.Mac.att_set_value(newHandle, 120, MaterialThickness);
                    writer.WriteLine("Material Thickness success = " + MaterialThickness + " " + success2);

                    bool success3 = rApp.Mac.att_set_value(newHandle, 121, unitType);
                    writer.WriteLine("UnitType success = " + unitType + " " + success3);

                    bool success4 = rApp.Mac.att_set_value(newHandle, 200, partDesc);
                    writer.WriteLine("partDesc success = " + partDesc + " " + success4);

                    // merge the new attributes with the old.
                    bool success5 = rApp.Mac.att_update_file(SymFilePath, newHandle, true);
                    writer.WriteLine("Attrib update success = " + success5);

                    return success1 && success2 && success3 && success3 && success4 & success5;
                }
            }
            catch (Exception x)
            {
                ErrorMessage = "An IO Error occured and attributes cannot be inserted\r\n\r\nError Reports\r\n" + x.Message;
                return false;
            }

        }

        public bool InsertAdditionalAttributes(string SymFilePath, string MaterialName, string MaterialThickness, string unitType, string partDesc, string OrderNumber, string ScheduleName, string BatchName, bool HasBends, ref string ErrorMessage)
        {
            if (!System.IO.File.Exists(SymFilePath))
            {
                ErrorMessage = "The SYM file does not exist and attributes cannot be inserted";
                return false;
            }
            try
            {
                // get a handle to the existing attributes
                int oldhandle = rApp.Mac.att_load(SymFilePath, false);

                // get a handle to a brand new set of attributes
                int newHandle = rApp.Mac.att_new(SymFilePath);

                bool success1 = rApp.Mac.att_set_value(newHandle, 119, MaterialName);
                bool success2 = rApp.Mac.att_set_value(newHandle, 120, MaterialThickness);
                bool success3 = rApp.Mac.att_set_value(newHandle, 121, unitType);
                bool success4 = rApp.Mac.att_set_value(newHandle, 200, partDesc);
                bool success5 = rApp.Mac.att_set_value(newHandle, 201, OrderNumber);
                bool success6 = rApp.Mac.att_set_value(newHandle, 202, ScheduleName);
                bool success7 = rApp.Mac.att_set_value(newHandle, 203, BatchName);
                string hasBendsFlag = (HasBends == true) ? "1" : "0";
                bool success8 = rApp.Mac.att_set_value(newHandle, 301, hasBendsFlag);

                // merge the new attributes with the old.
                bool success9 = rApp.Mac.att_update_file(SymFilePath, newHandle, true);

                return success1 && success2 && success3 && success3 && success4 && success5 && success6 && success7 && success8 && success9;
            }
            catch (Exception x)
            {
                ErrorMessage = "An IO Error occured and attributes cannot be inserted\r\n\r\nError Reports\r\n" + x.Message;
                return false;
            }
        }

        public bool isProjectOpen(ref string errMsg)
        {
            try
            {
                if (rApp.Mac.prj_is_open())
                    return true;
                else
                    return false;
            }
            catch (Exception)
            {
                errMsg = "Error in checking project state.";
                return false;
            }
        }

        public string getOpenProjectName(ref string errMsg)
        {
            try
            {
                if (rApp.Mac.prj_is_open())
                    return rApp.Mac.prj_get_file_path();
                else
                    return "";
            }
            catch (Exception)
            {
                errMsg = "Error in checking project state.";
                return "";
            }
        }

        public string getOpenNestName(ref string errMsg)
        {
            try
            {
                // this does not work
                if (rApp.ActiveDocument.Type == Radraft.Interop.RadDocumentType.radDrawingDocument &&
                        rApp.GUIState == Radraft.Interop.RadGUIState.radNest)
                {
                    // this does not work
                    //return rApp.Mac.NST_NAME;

                    return rApp.Mac.C_DWGDIR + "\\" + rApp.Mac.C_DWG + ".drg";
                }
                else return "";
            }
            catch (Exception)
            {
                errMsg = "Error in getting open nest name";
                return "";
            }
        }

        public bool openNest(string nestName, ref string errMsg)
        {
            try
            {
                rApp.OpenDrawing(nestName, true, "");
                return true;
            }
            catch (Exception)
            {
                errMsg = "Could not open nest";
                return false;
            }
        }

        public bool SaveNest(ref string errMsg)
        {
            try
            {
                if (rApp.ActiveDocument.Type == Radraft.Interop.RadDocumentType.radDrawingDocument &&
                    rApp.GUIState == Radraft.Interop.RadGUIState.radNest)
                {
                    rApp.ActiveDocument.Save();
                }
                if (rApp.Mac.prj_is_edited() == true)
                {
                    rApp.Mac.prj_save();
                }
                return true;
            }
            catch (Exception)
            {
                errMsg = "Error in saving nest.";
                return false;
                //return true;    // just for now till I get time to fix it.
            }
        }

        public bool LoadProject(string projectFileName)
        {
            return rApp.Mac.prj_open(projectFileName);
        }

        public bool SaveProject()
        {
            return rApp.Mac.prj_save();
        }

        public bool IsProjectReady()
        {
            // see if we can query interface, return true if we can, false if we can't
            try
            {
                string version = rApp.Application.SoftwareVersion;
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public string GetThicknessFromSym(string fileName)
        {
            XmlDocument doc = new XmlDocument();

            if (System.IO.File.Exists(fileName))
            {
                doc.Load(fileName);
            }
            else
            {
                return "No Thickness Calculated";
            }

            // cycle through each child node
            foreach (XmlNode node in doc.DocumentElement.FirstChild)
            {
                if (node.InnerXml.Contains("Thickness"))
                {
                    foreach (XmlNode subNode in node)
                    {
                        if (subNode.OuterXml.Contains("Thickness"))
                        {
                            string attr = subNode.Attributes["value"].Value;
                            return attr;
                        }
                    }
                }

            }
            return "Error, No Thickness Found.";
        }

        public string GetThicknessUnitsFromSym(string fileName)
        {
            XmlDocument doc = new XmlDocument();

            if (System.IO.File.Exists(fileName))
            {
                doc.Load(fileName);
            }
            else
            {
                return "No Thickness Unit Found";
            }

            // cycle through each child node
            foreach (XmlNode node in doc.DocumentElement.FirstChild)
            {
                if (node.InnerXml.Contains("Thickness units"))
                {
                    foreach (XmlNode subNode in node)
                    {
                        if (subNode.OuterXml.Contains("Thickness units"))
                        {
                            string attr = subNode.Attributes["value"].Value;
                            return attr;
                        }
                    }
                }

            }
            return "Error, No Thickness Found.";
        }

        public string GetMaterialTypeFromSym(string fileName)
        {
            XmlDocument doc = new XmlDocument();

            if (System.IO.File.Exists(fileName))
            {
                doc.Load(fileName);
            }
            else
            {
                return "No Material Specified";
            }

            // cycle through each child node
            foreach (XmlNode node in doc.DocumentElement.FirstChild)
            {
                if (node.InnerXml.Contains("Material"))
                {
                    foreach (XmlNode subNode in node)
                    {
                        if (subNode.OuterXml.Contains("Material"))
                        {
                            string attr = subNode.Attributes["value"].Value;
                            return attr;
                        }
                    }
                }

            }
            return "Error, No Material Specified.";
        }

        public string GetDescriptionFromSym(string fileName)
        {
            XmlDocument doc = new XmlDocument();

            if (System.IO.File.Exists(fileName))
            {
                doc.Load(fileName);
            }
            else
            {
                return "";
            }

            // cycle through each child node
            foreach (XmlNode node in doc.DocumentElement.FirstChild)
            {
                try
                {
                    if (node.InnerXml.Contains("Part Description"))
                    {
                        foreach (XmlNode subNode in node)
                        {
                            if (subNode.OuterXml.Contains("Part Description"))
                            {
                                string attr = subNode.Attributes["value"].Value;
                                return attr;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // shouldn't really do this, but I couldn't figure out how to test for a null value in this attribute, so relying on the exception handler instead
                    return null;
                }

            }
            return "";
        }

        public char[] GetThumbnailDataFromSym(string fileName)
        {
            XmlDocument doc = new XmlDocument();

            if (System.IO.File.Exists(fileName))
            {
                doc.Load(fileName);
            }
            else
            {
                return null;
            }

            string thumbNailString = "";
            string s2 = "";

            // cycle through each child node
            foreach (XmlNode node in doc.DocumentElement)
            {
                if (node.OuterXml.Contains("Thumbnail"))
                {
                    thumbNailString = node.InnerText;

                }
            }

            return thumbNailString.ToCharArray();
        }

        public char[] GetThumbnailDataFromNest(string fileName)
        {
            XmlDocument doc = new XmlDocument();

            if (System.IO.File.Exists(fileName))
            {
                doc.Load(fileName);
            }
            else
            {
                return null;
            }

            string thumbNailString = "";

            // cycle through each child node
            foreach (XmlNode node in doc.DocumentElement)
            {
                if (node.OuterXml.Contains("Thumbnail"))
                {
                    thumbNailString = node.InnerText;

                }
            }

            return thumbNailString.ToCharArray();
        }

        public bool AddPartToProject(int binNumber, string fileName, string material, int qtyRequired, double thickness)
        {
            // not tested yet.
            try
            {
                //rApp.Application.Mac.prj_add_part(binNumber, 0, 0, 0, 0, false, 0, fileName,"", material, 0, false, qtyRequired, 0, false, 5, "", thickness, "in");
                rApp.Application.Mac.PRJ_PART_BIN = binNumber;
                rApp.Application.Mac.PRJ_PART_COMMON_CUT = 0;
                rApp.Application.Mac.PRJ_PART_CUSTOM_COLOUR_BLUE = -1;
                rApp.Application.Mac.PRJ_PART_CUSTOM_COLOUR_GREEN = -1;
                rApp.Application.Mac.PRJ_PART_CUSTOM_COLOUR_RED = -1;
                rApp.Application.Mac.PRJ_PART_EXCLUDE = false;
                rApp.Application.Mac.PRJ_PART_EXTRA_ALLOWED = 0;
                rApp.Application.Mac.PRJ_PART_FILENAME = fileName;
                rApp.Application.Mac.PRJ_PART_KIT_FILENAME = "";
                rApp.Application.Mac.PRJ_PART_MATERIAL = material;
                rApp.Application.Mac.PRJ_PART_MAX_COMMON_CUT = 0;
                rApp.Application.Mac.PRJ_PART_MIRROR = false;
                rApp.Application.Mac.PRJ_PART_NUMBER_REQUIRED = qtyRequired;
                rApp.Application.Mac.PRJ_PART_ORIENT = 0;
                rApp.Application.Mac.PRJ_PART_PICKING_CLUSTER = false;
                rApp.Application.Mac.PRJ_PART_PRIORITY = 5;
                rApp.Application.Mac.PRJ_PART_STRATEGY = "";
                rApp.Application.Mac.PRJ_PART_THICKNESS = thickness;
                rApp.Application.Mac.PRJ_PART_THICK_UNITS = "in";

                rApp.Application.Mac.prj_add_part();
                return true;
            }
            catch(Exception)
            {
                return false;
            }
        }
    }
}
