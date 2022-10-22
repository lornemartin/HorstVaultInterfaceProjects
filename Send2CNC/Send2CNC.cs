using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;


namespace WindowsFormsApplication1
{
    public partial class Send2CNC : Form
    {
        List<string> ncfile1 = new List<string>();    // a list of strings representing the first file
        List<string> ncfile2 = new List<string>();    // a list of strings representing the second file
        List<string> ncfile3 = new List<string>();    // a list of strings representing the code to outpu to the machine

        string ncFileName1 = @"C:\NCFiles\Side1.nc";                         // the file containing nc code for the first half of a 2 sided lathe program
        string ncFileName2 = @"C:\NCFiles\Side2.nc";                         // the file containing nc code for the second half of a 2 sided lathe program
        string ncFileName3 = @"C:\NCFiles\Side1BarFeed.nc";
        string ncSubSpindle1FileName = @"C:\NCFiles\Side1SubSpindle.nc";
        string ncSubSpindle2FileName = @"C:\NCFiles\Side2SubSpindle.nc";

        string TC20FolderName = @"Z:\Takisawa TC-20 Lathe\Send to CNC\";
        string EX110FolderName = @"Z:\Takisawa EX-10 Lathe\Send to CNC\";
        string HaasSL30FolderName = @"Z:\Haas SL30\Send to CNC\";
        string HyundaiFolderName = @"Z:\Hyundai Kia\Send to CNC\";

        //string HaasVF1FolderName = @"C:\NCFiles\HaasVF1";

        string progName = null;

        Boolean HaasFlag = false;
        Boolean HyundaiFlag = false;
        Boolean TC20Flag = false;
        Boolean EX110Flag = false;

        public Send2CNC()
        {
            InitializeComponent();
        }

        private void fileExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private List<string> readFile(string fileName)
        {
            HaasFlag = false;
            List<string> l = new List<string>();
            string s;

            using (StreamReader valueReader = new StreamReader(fileName))
            {
                while (valueReader.EndOfStream != true)
                {
                    s = valueReader.ReadLine();
                    if (s.Contains("HAAS SL-30")) HaasFlag = true;
                    if (s.Contains("HYUNDAI-KIA SKT250SY")) HyundaiFlag = true;
                    if (s.Contains("TC-20")) TC20Flag = true;
                    if (s.Contains("EX-110")) EX110Flag = true;
                    if ((s.Contains("M4")) && 
                        (!s.Contains("M40")) &&
                        (!s.Contains("M41")) &&
                        (!s.Contains("M42")) &&
                        (!s.Contains("M43"))) MessageBox.Show("M4 Found!");
                    if (s.Contains("M04 ")) MessageBox.Show("M04 Found!");

                    if (s.StartsWith("O") && s.Contains("(") && s.Contains(")"))
                    {
                        progName = s;
                        s = null;
                        int start = progName.IndexOf("(");
                        int end = progName.IndexOf(")");
                        progName = progName.Substring(start, end + 1 - start);
                    }
                    else if (s.Contains("% ")) s = null;
                    else if (s.Contains("M99")) s = null;
                    else if (s.Contains("M30")) s = null;

                    if (s != null)
                    {
                        l.Add(s);
                    }


                }
            }

            return l;
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            if ((File.Exists(ncFileName1)) && (File.Exists(ncFileName2) == true))
            {
                ncfile1 = readFile(ncFileName1);
                ncfile2 = readFile(ncFileName2);

                generate2SidedProgram();
            }
            else if (File.Exists(ncFileName3))
            {
                ncfile1 = readFile(ncFileName3);
                generateBarFeedProgram();
            }
            else if ((File.Exists(ncSubSpindle1FileName)) && (File.Exists(ncSubSpindle2FileName) == true))
            {
                ncfile1 = readFile(ncSubSpindle1FileName);
                ncfile2 = readFile(ncSubSpindle2FileName);

                generateSubSpindleProgram();
            }
            else
            {
                ncfile1 = readFile(ncFileName1);

                generate1SidedProgram();
            }

            if (File.Exists(ncFileName1)) File.Delete(ncFileName1);
            if (File.Exists(ncFileName2)) File.Delete(ncFileName2);
            if (File.Exists(ncFileName3)) File.Delete(ncFileName3);
            if (File.Exists(ncSubSpindle1FileName)) File.Delete(ncSubSpindle1FileName);
            if (File.Exists(ncSubSpindle2FileName)) File.Delete(ncSubSpindle2FileName);
        }

        private void generateSubSpindleProgram()
        {
            ncFileBox.Clear();
            ncfile3.Clear();

            //ncfile3.Add("% ");
            ncfile3.Add("O0001 " + progName);               // output file is always number 1
            foreach (string s in ncfile1)       // append file1 to this new file
            {
                if (!s.Contains("%"))
                {
                    ncfile3.Add(s);
                }
            }

            //ncfile3.Add("M00");

            foreach (String s in ncfile2)       // append file2
            {
                string temp = s;
                //if (temp.Contains("G54")) temp = temp.Replace("G54", "G55");
                if (!temp.Contains("%"))
                {
                    ncfile3.Add(temp);
                }

            }
            ncfile3.Add(" M30");

            foreach (string s in ncfile3)
            {
                ncFileBox.AppendText(s + '\r');
                ncFileBox.AppendText("\n");
            }

            btnTC20.Enabled = false;
            btnTC20Save.Enabled = false;
            btnEX110.Enabled = false;
            btnEX110Save.Enabled = false;
            btnSaveUSB.Enabled = false;
            btnSL30Save.Enabled = false;
            btnHyundai.Enabled = true;
            btnHyundaiSave.Enabled = true;
            btnTC20Save2.Enabled = false;
            btnEX110Save2.Enabled = false;
            btnSL30Save2.Enabled = false;
            btnHyundaiSave2.Enabled = true;
        }

        private void generateBarFeedProgram()
        {
            string pullDist = null;
            string stubLength = null;

            foreach (string s in ncfile1)       // scan file for bar puller data
            {
                if (s.Contains("(APPROACHX"))
                {
                    List<string> l = s.Split(',').ToList();
                    pullDist = l[4].ToString();
                    pullDist = pullDist.Replace("PULLDISTANCE", string.Empty);

                    stubLength = l[5].ToString();
                    stubLength = stubLength.Replace("STUBLENGTH", string.Empty);
                }



            }

            ncFileBox.Clear();
            ncfile3.Clear();

            ncfile3.Add("% ");
            ncfile3.Add("O0001 " + progName);               // output file is always number 1
            ncfile3.Add("IF[#504 LT 0]THEN #531 = #504");
            ncfile3.Add("IF[#504 GE 0]THEN #531 = 0.-#504");
            ncfile3.Add("G10 P0 Z#531   (SET WORKSHIFT)");

            if (chkBoxSafeCheck.Checked == true)
            {
                ncfile3.Add("/T" + txtBoxToolNo.Text);
                ncfile3.Add("/G99 G96 S0 M5");
                ncfile3.Add("/G0 X8.0 Z5.0");
                ncfile3.Add("/M00");
            }

            ncfile3.Add("(USE VARIABLE 500 TO SET BAR LENGTH IN INCHES)");
            ncfile3.Add("(VARIABLE 501 SHOWS CALCULATED NUMBER OF PIECES PER BAR)");
            ncfile3.Add("(VARIABLE 502 SHOWS AMOUNT OF PIECES DONE IN CURRENT BAR)");
            ncfile3.Add("(VARIABLE 508 COUNTS TOTAL NUMBER OF PIECES)");

            ncfile3.Add("/#100=0");
            //ncfile3.Add("#501=[[#500-" + txtBoxStubLength.Text + "]/" + txtBoxPullDist.Text + "]");
            ncfile3.Add("/#501=[FIX[[#500-" + stubLength + "]/" + pullDist + "]]");
            ncfile3.Add("/#502=0");
            ncfile3.Add("/WHILE[#100LT#501]DO1");

            foreach (string s in ncfile1)       // append file1 to this new file
            {
                ncfile3.Add(s);
                if (s.Contains("CONDITIONAL PROGRAM RESTART"))
                {
                    ncfile3.Add(" /GOTO100");
                    ncfile3.Add(" #508=#508+1");
                    ncfile3.Add(" M30");
                    ncfile3.Add(" N100");
                }
            }

            ncfile3.Add("/#100=#100+1");
            ncfile3.Add("/#502=#502+1");
            ncfile3.Add("/#508=#508+1");
            ncfile3.Add("END1");
            ncfile3.Add("M30");

            foreach (string s in ncfile3)
            {
                ncFileBox.AppendText(s + '\r');
                ncFileBox.AppendText("\n");
            }

            btnTC20.Enabled = true;
            btnEX110.Enabled = true;
            btnSaveUSB.Enabled = false;
            btnSL30Save.Enabled = false;
            btnEX110Save.Enabled = true;
            btnTC20Save.Enabled = true;
            btnTC20Save2.Enabled = true;
            btnEX110Save2.Enabled = true;
            btnSL30Save2.Enabled = false;
            btnHyundaiSave2.Enabled = false;

        }

        private void generate2SidedProgram()
        {
            if (TC20Flag == true)
            {
                ncFileBox.Clear();
                ncfile3.Clear();

                ncfile3.Add("% ");
                ncfile3.Add("O0001 " + progName);               // output file is always number 1
                ncfile3.Add("IF[#504 LT 0]THEN #531 = #504");
                ncfile3.Add("IF[#504 GE 0]THEN #531 = 0.-#504");
                ncfile3.Add("G10 P0 Z#531   (SET WORKSHIFT)");

                foreach (string s in ncfile1)       // append file1 to this new file
                {
                    ncfile3.Add(s);
                }

                ncfile3.Add("M00");
                ncfile3.Add("IF[#505 LT 0]THEN #531 = #505");
                ncfile3.Add("IF[#505 GE 0]THEN #531 = 0.-#505");
                ncfile3.Add("G10 P0 Z#531   (SET WORKSHIFT)");


                foreach (String s in ncfile2)       // append file2
                {
                    ncfile3.Add(s);
                }
                ncfile3.Add(" M30");

                foreach (string s in ncfile3)
                {
                    ncFileBox.AppendText(s+'\r');
                    ncFileBox.AppendText("\n");
                }

                btnTC20.Enabled = true;
                btnTC20Save.Enabled = true;
                btnEX110.Enabled = false;
                btnEX110Save.Enabled = false;
                btnSaveUSB.Enabled = false;
                btnSL30Save.Enabled = false;
                btnHyundai.Enabled = false;
                btnHyundaiSave.Enabled = false;
                btnTC20Save2.Enabled = true;
                btnEX110Save2.Enabled = false;
                btnSL30Save2.Enabled = false;
                btnHyundaiSave2.Enabled = false;
            }
            else if (EX110Flag == true)
            {
                ncFileBox.Clear();
                ncfile3.Clear();

                ncfile3.Add("% ");
                ncfile3.Add("O0001 " + progName);               // output file is always number 1
                ncfile3.Add("IF[#504 LT 0]THEN #531 = #504");
                ncfile3.Add("IF[#504 GE 0]THEN #531 = 0.-#504");
                ncfile3.Add("G10 P0 Z#531   (SET WORKSHIFT)");

                foreach (string s in ncfile1)       // append file1 to this new file
                {
                    ncfile3.Add(s);
                }

                ncfile3.Add("M00");
                ncfile3.Add("IF[#505 LT 0]THEN #531 = #505");
                ncfile3.Add("IF[#505 GE 0]THEN #531 = 0.-#505");
                ncfile3.Add("G10 P0 Z#531   (SET WORKSHIFT)");


                foreach (String s in ncfile2)       // append file2
                {
                    ncfile3.Add(s);
                }
                ncfile3.Add(" M30");

                foreach (string s in ncfile3)
                {
                    ncFileBox.AppendText(s + '\r');
                    ncFileBox.AppendText("\n");
                }

                btnTC20.Enabled = false;
                btnTC20Save.Enabled = false;
                btnEX110.Enabled = true;
                btnEX110Save.Enabled = true;
                btnSaveUSB.Enabled = false;
                btnSL30Save.Enabled = false;
                btnHyundai.Enabled = false;
                btnHyundaiSave.Enabled = false;
                btnTC20Save2.Enabled = false;
                btnEX110Save2.Enabled = true;
                btnSL30Save2.Enabled = false;
                btnHyundaiSave2.Enabled = false;
            }

            else if (HaasFlag == true)
            {
                ncFileBox.Clear();
                ncfile3.Clear();

                //ncfile3.Add("% ");
                ncfile3.Add("O0001 " + progName);               // output file is always number 1
                foreach (string s in ncfile1)       // append file1 to this new file
                {
                    if (!s.Contains("%"))
                    {
                        ncfile3.Add(s);
                    }
                }

                ncfile3.Add("M00");

                foreach (String s in ncfile2)       // append file2
                {
                    string temp = s;
                    if (temp.Contains("G54")) temp = temp.Replace("G54", "G55");
                    if (!temp.Contains("%"))
                    {
                        ncfile3.Add(temp);
                    }

                }
                ncfile3.Add(" M30");

                foreach (string s in ncfile3)
                {
                    ncFileBox.AppendText(s + '\r');
                    ncFileBox.AppendText("\n");
                }

                btnTC20.Enabled = false;
                btnTC20Save.Enabled = false;
                btnEX110.Enabled = false;
                btnEX110Save.Enabled = false;
                btnSaveUSB.Enabled = true;
                btnSL30Save.Enabled = true;
                btnHyundai.Enabled = false;
                btnHyundaiSave.Enabled = false;
                btnTC20Save2.Enabled = false;
                btnEX110Save2.Enabled = false;
                btnSL30Save2.Enabled = true;
                btnHyundaiSave2.Enabled = false;
            }

            else if (HyundaiFlag == true)
            {
                ncFileBox.Clear();
                ncfile3.Clear();

                //ncfile3.Add("% ");
                ncfile3.Add("O0001 " + progName);               // output file is always number 1
                foreach (string s in ncfile1)       // append file1 to this new file
                {
                    if (!s.Contains("%"))
                    {
                        ncfile3.Add(s);
                    }
                }

                ncfile3.Add("M00");

                foreach (String s in ncfile2)       // append file2
                {
                    string temp = s;
                    if (temp.Contains("G54")) temp = temp.Replace("G54", "G55");
                    if (!temp.Contains("%"))
                    {
                        ncfile3.Add(temp);
                    }

                }
                ncfile3.Add(" M30");

                foreach (string s in ncfile3)
                {
                    ncFileBox.AppendText(s + '\r');
                    ncFileBox.AppendText("\n");
                }

                btnTC20.Enabled = false;
                btnTC20Save.Enabled = false;
                btnEX110.Enabled = false;
                btnEX110Save.Enabled = false;
                btnSaveUSB.Enabled = false;
                btnSL30Save.Enabled = false;
                btnHyundai.Enabled = true;
                btnHyundaiSave.Enabled = true;
                btnTC20Save2.Enabled = false;
                btnEX110Save2.Enabled = false;
                btnSL30Save2.Enabled = false;
                btnHyundaiSave2.Enabled = true;
            }
        }

        private void generate1SidedProgram()
        {
            if (TC20Flag == true)
            {
                ncFileBox.Clear();
                ncfile3.Clear();

                ncfile3.Add("% ");
                ncfile3.Add("O0001 " + progName);               // output file is always number 1
                ncfile3.Add("IF[#504 LT 0]THEN #531 = #504");
                ncfile3.Add("IF[#504 GE 0]THEN #531 = 0.-#504");
                ncfile3.Add("G10 P0 Z#531   (SET WORKSHIFT)");

                foreach (string s in ncfile1)       // append file1 to this new file
                {
                    ncfile3.Add(s);
                }

                ncfile3.Add(" M30");

                foreach (string s in ncfile3)
                {
                    ncFileBox.AppendText(s + '\r');
                    ncFileBox.AppendText("\n");
                }

                btnTC20.Enabled = true;
                btnTC20Save.Enabled = true;
                btnEX110.Enabled = false;
                btnEX110Save.Enabled = false;
                btnSaveUSB.Enabled = false;
                btnSL30Save.Enabled = false;
                btnHyundai.Enabled = false;
                btnHyundaiSave.Enabled = false;
                btnTC20Save2.Enabled = true;
                btnEX110Save2.Enabled = false;
                btnSL30Save2.Enabled = false;
                btnHyundaiSave2.Enabled = false;
            }
            else if (EX110Flag == true)
            {
                ncFileBox.Clear();
                ncfile3.Clear();

                ncfile3.Add("% ");
                ncfile3.Add("O0001 " + progName);               // output file is always number 1
                ncfile3.Add("IF[#504 LT 0]THEN #531 = #504");
                ncfile3.Add("IF[#504 GE 0]THEN #531 = 0.-#504");
                ncfile3.Add("G10 P0 Z#531   (SET WORKSHIFT)");

                foreach (string s in ncfile1)       // append file1 to this new file
                {
                    ncfile3.Add(s);
                }

                ncfile3.Add(" M30");

                foreach (string s in ncfile3)
                {
                    ncFileBox.AppendText(s + '\r');
                    ncFileBox.AppendText("\n");
                }

                btnTC20.Enabled = false;
                btnTC20Save.Enabled = false;
                btnEX110.Enabled = true;
                btnEX110Save.Enabled = true;
                btnSaveUSB.Enabled = false;
                btnSL30Save.Enabled = false;
                btnHyundai.Enabled = false;
                btnHyundaiSave.Enabled = false;
                btnTC20Save2.Enabled = false;
                btnEX110Save2.Enabled = true;
                btnSL30Save2.Enabled = false;
                btnHyundaiSave2.Enabled = false;
            }
            else if (HaasFlag == true)
            {
                ncFileBox.Clear();
                ncfile3.Clear();

                //ncfile3.Add("% ");
                ncfile3.Add("O0001 " + progName);               // output file is always number 1

                foreach (string s in ncfile1)       // append file1 to this new file
                {
                    if (!s.Contains("%"))
                    {
                        ncfile3.Add(s+'\r');
                    }
                }

                ncfile3.Add(" M30");

                foreach (string s in ncfile3)
                {
                    ncFileBox.AppendText(s + '\r');
                    ncFileBox.AppendText("\n");
                }

                btnTC20.Enabled = false;
                btnTC20Save.Enabled = false;
                btnEX110.Enabled = false;
                btnEX110Save.Enabled = false;
                btnSaveUSB.Enabled = true;
                btnSL30Save.Enabled = true;
                btnHyundai.Enabled = false;
                btnHyundaiSave.Enabled = false;
                btnTC20Save2.Enabled = false;
                btnEX110Save2.Enabled = false;
                btnSL30Save2.Enabled = true;
                btnHyundaiSave2.Enabled = false;
            }
            else if (HyundaiFlag == true)
            {
                ncFileBox.Clear();
                ncfile3.Clear();

                //ncfile3.Add("% ");
                ncfile3.Add("O0001 " + progName);               // output file is always number 1

                foreach (string s in ncfile1)       // append file1 to this new file
                {
                    if (!s.Contains("%"))
                    {
                        ncfile3.Add(s + '\r');
                    }
                }

                ncfile3.Add(" M30");

                foreach (string s in ncfile3)
                {
                    ncFileBox.AppendText(s);
                    ncFileBox.AppendText("\n");
                }

                btnTC20.Enabled = false;
                btnTC20Save.Enabled = false;
                btnEX110.Enabled = false;
                btnEX110Save.Enabled = false;
                btnSaveUSB.Enabled = false;
                btnSL30Save.Enabled = false;
                btnHyundai.Enabled = true;
                btnHyundaiSave.Enabled = true;
                btnTC20Save2.Enabled = false;
                btnEX110Save2.Enabled = false;
                btnSL30Save2.Enabled = false;
                btnHyundaiSave2.Enabled = true;
            }
        }

        private void btnTC20_Click(object sender, EventArgs e)
        {
            serialPortEX110.Open();
            toolStripStatusLabel.Text = "Sending to CNC...";
            foreach (String s in ncfile3)
            {
                if (!s.StartsWith("*"))      // skip over warnings, don't send to control
                {
                    serialPortEX110.Write(s);
                    serialPortEX110.Write("\n\r\r");
                }

            }
            serialPortEX110.Write("%");
            toolStripStatusLabel.Text = "Sending to CNC...Done";

            serialPortEX110.Close();
        }

        private void btnEX110_Click(object sender, EventArgs e)
        {
            // create a writer and open the file
            TextWriter tw = new StreamWriter("send2debug.txt");
            serialPortEX110.Open();
            toolStripStatusLabel.Text = "Sending to CNC...";
            foreach (String s in ncfile3)
            {
                if (!s.StartsWith("*"))      // skip over warnings, don't send to control
                {
                    // write a line of text to the file
                    tw.WriteLine(s);
                    serialPortEX110.Write(s);
                    serialPortEX110.Write("\n\r\r");
                }
            }
            serialPortEX110.Write("%");
            toolStripStatusLabel.Text = "Sending to CNC...Done";

            serialPortEX110.Close();
            // close the stream
            tw.Close();
        }

        private void Send2CNC_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (File.Exists(ncFileName1)) File.Delete(ncFileName1);
            if (File.Exists(ncFileName2)) File.Delete(ncFileName2);
        }

        private void btnTC20Save_Click(object sender, EventArgs e)
        {
            saveNCFile(TC20FolderName,1,false,4,"TC-20");
        }

        private void btnTC20Save2_Click(object sender, EventArgs e)
        {
            DialogResult = folderBrowserDialog1.ShowDialog();

            if(DialogResult==DialogResult.OK)
            {
                string folderName = folderBrowserDialog1.SelectedPath.ToString()+ "\\";
                saveNCFile(folderName, 1, false, 4, "TC-20");
            }
        }

        private void btnEX110Save_Click(object sender, EventArgs e)
        {
            saveNCFile(EX110FolderName,1,false,4, "EX-110");
        }

        private void btnEX110Save2_Click(object sender, EventArgs e)
        {
            DialogResult = folderBrowserDialog1.ShowDialog();

            if (DialogResult == DialogResult.OK)
            {
                string folderName = folderBrowserDialog1.SelectedPath.ToString() + "\\";
                saveNCFile(folderName, 1, false, 4, "EX-110");
            }
        }

        private void btnSL30Save_Click(object sender, EventArgs e)
        {
            saveNCFile(HaasSL30FolderName,0,true,5,"Haas SL30");
        }

        private void btnSL30Save2_Click(object sender, EventArgs e)
        {
            DialogResult = folderBrowserDialog1.ShowDialog();

            if (DialogResult == DialogResult.OK)
            {
                string folderName = folderBrowserDialog1.SelectedPath.ToString() + "\\";
                saveNCFile(folderName, 0, true, 5, "Haas SL30");
            }
        }
        private void btnSaveUSB_Click(object sender, EventArgs e)
        {
            string usbFileName = @"F:\1.nc";
            using (StreamWriter writer = new StreamWriter(usbFileName, false))
            {
                writer.WriteLine("%");
                foreach (String s in ncfile3)
                {
                    {
                        // write a line of text to the file
                        writer.WriteLine(s);
                    }
                }
                writer.WriteLine("%");
                toolStripStatusLabel.Text = "Saved to USB";
            }
        }

        private void btnHyundai_Click(object sender, EventArgs e)
        {
            // create a writer and open the file
            TextWriter tw = new StreamWriter("send2debug.txt");
            serialPortEX110.Open();
            toolStripStatusLabel.Text = "Sending to CNC...";
            foreach (String s in ncfile3)
            {
                if (!s.StartsWith("*"))      // skip over warnings, don't send to control
                {
                    // write a line of text to the file
                    tw.WriteLine(s);
                    serialPortEX110.Write(s);
                    serialPortEX110.Write("\n\r\r");
                }
            }
            serialPortEX110.Write("%");
            toolStripStatusLabel.Text = "Sending to CNC...Done";

            serialPortEX110.Close();
            // close the stream
            tw.Close();
        }

        private void btnHyundaiSave_Click(object sender, EventArgs e)
        {
            saveNCFile(HyundaiFolderName,0,true,4,"Hyundai");
        }

        private void btnHyundaiSave2_Click(object sender, EventArgs e)
        {
            DialogResult = folderBrowserDialog1.ShowDialog();

            if (DialogResult == DialogResult.OK)
            {
                string folderName = folderBrowserDialog1.SelectedPath.ToString() + "\\";
                saveNCFile(folderName, 0, true, 4, "Hyundai");
            }
        }

        private void chkBoxSafeCheck_CheckedChanged(object sender, EventArgs e)
        {
            txtBoxToolNo.Enabled = !txtBoxToolNo.Enabled;
            lblToolNo.Enabled = !lblToolNo.Enabled; ;
        }

        private void saveNCFile(string saveFolderName,          // the folder to save NC files to
                                int lineToExtractFileNumFrom,   // is file name on first or second line of nc file?
                                Boolean outputInitialPercent,   // does the nc file begin with a % sign?
                                int progNumLength,              // 4 or 5 character file names?
                                string machineName)             // the name of the machine
        {
            string fileNumber = "";
            Boolean fileExists = false;
            int maxFileNumber = 0;
            int testFileNumber = 0;

            DirectoryInfo d = new DirectoryInfo(saveFolderName);
            FileInfo[] Files = d.GetFiles("*.*"); //Getting Text files
            foreach (FileInfo file in Files)
            {
                testFileNumber = int.Parse(file.Name.Substring(0, progNumLength));
                if (testFileNumber > maxFileNumber)
                {
                    maxFileNumber = testFileNumber;
                }
            }

            maxFileNumber += 1;
            Boolean overWriteFile = true;
            do
            {
                fileExists = false;
                using (var form = new GetFileNumber(maxFileNumber.ToString().PadLeft(progNumLength, '0')))
                {
                    var result = form.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        fileNumber = form.fileNumber;
                        fileNumber = fileNumber.PadLeft(progNumLength, '0');

                        d = new DirectoryInfo(saveFolderName);
                        Files = d.GetFiles("*.*"); //Getting Text files
                        foreach (FileInfo file in Files)
                        {
                            if (file.Name.StartsWith(fileNumber)) fileExists = true;
                        }

                        if (fileExists)
                        {
                            using (var form2 = new OverWriteOrCancel(fileNumber))
                            {
                                var result2 = form2.ShowDialog();
                                if (result2 == DialogResult.Yes)
                                {
                                    overWriteFile = true;
                                    fileExists = false;
                                }
                                else overWriteFile = false;
                            }
                        }

                    }

                    else
                    {
                        toolStripStatusLabel.Text = "File Save Canceled";
                        overWriteFile = false;
                    }
                }
            } while (fileExists);

            if (overWriteFile == true)
            {
                // first extract the program name and number from nc3
                //string fileName = ncfile3[1].Split(new char[] { '(', ')' })[1];
                string fileName = ncfile3[lineToExtractFileNumFrom];
                fileName = fileName.Replace("O0001", fileNumber);

                fileName = fileName.Replace("(", "").Replace(")","");   // remove all parenthesis from filename

                // create a writer and open the file
                TextWriter tw = new StreamWriter(saveFolderName + fileName + ".nc");
                if (outputInitialPercent == true)
                {
                    tw.Write("%\r\n");
                }
                string newString = "";
                foreach (String s in ncfile3)
                {
                    if (s.Contains("O0001"))
                    {
                        // change the program number from 1 to whatever is embeddeded in file name
                        //tw.WriteLine("O" + fileNumber);
                        newString = s.Replace("0001", fileNumber);
                        tw.WriteLine(newString);
                    }
                    else
                        // write a line of text to the file
                        tw.WriteLine(s);
                }
                tw.Write("%\r\n");

                toolStripStatusLabel.Text = "Saved to the " + machineName + " File Server";

                // close the stream
                tw.Close();
            }
        }

        private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e)
        {

        }
    }
}
