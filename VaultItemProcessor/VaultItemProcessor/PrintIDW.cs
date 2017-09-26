using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VaultItemProcessor
{
    public partial class PrintIDW : Form
    {
        public Dictionary<long, string> IdwFileList { get; set; }
        public long SelectedID { get; set; }

        public PrintIDW(Dictionary<long, string> idwFileList)
        {
            InitializeComponent();

            IdwFileList = idwFileList;

            foreach (KeyValuePair<long, string> entry in IdwFileList)
            {
                treeListIdwList.Nodes.Add(new object[] { entry.Key, entry.Value });
            }

            //treeListIdwList.DataSource = IdwFileList;
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            SelectedID = long.Parse(treeListIdwList.FocusedNode[0].ToString());
            this.Close();
        }
    }
}
