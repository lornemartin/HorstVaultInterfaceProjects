using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class OverWriteOrCancel : Form
    {
        public OverWriteOrCancel(string text)
        {
            InitializeComponent();
            label1.Text = "File # " + text + " already exists, do you want to overwrite?";
        }

        private void btnYes_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Yes;
            this.Close();
        }
    }
}
