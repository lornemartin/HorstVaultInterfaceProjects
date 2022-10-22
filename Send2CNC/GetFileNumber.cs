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
    public partial class GetFileNumber : Form
    {
        public string fileNumber { get; set; }

        public GetFileNumber(string text)
        {
            InitializeComponent();
            textBox1.Text = text;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            fileNumber = textBox1.Text;
            this.Close();
        }
    }
}
