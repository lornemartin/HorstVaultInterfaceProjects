using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VaultAccess;

namespace VaultDrawingRetrieveTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string fileName = "CHC - 04";

            VaultAccess.VaultAccess hlaVault = new VaultAccess.VaultAccess();

            hlaVault.Login("lorne", "lorne", "hwvsvt01", "Vault");

            Dictionary<long, string> idwDict = new Dictionary<long, string>();
            hlaVault.GetIDWsAssociatedWithModelByVaultName(fileName);

        }
    }
}
