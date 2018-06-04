using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ItemExport
{
    public partial class ExportToHorstMFG : Form
    {
        public string OrderNumber { get; set; }
        public string CustomerName { get; set; }

        public ExportToHorstMFG()
        {
            InitializeComponent();
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            OrderNumber = txtboxOrderNumber.Text;
            CustomerName = txtkBoxCustomerName.Text;
            DialogResult = DialogResult.OK;
        }
    }
}
