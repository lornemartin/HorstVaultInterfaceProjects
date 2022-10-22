using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RadanMaster
{
    [RunInstaller(true)]
    public partial class ModifyConfig : System.Configuration.Install.Installer
    {
        public ModifyConfig()
        {
            InitializeComponent();
        }

        public override void Commit(IDictionary savedState)
        {
            base.Commit(savedState);

            try
            {
                //UpdateConnectionString();
                MessageBox.Show("Hi");
                
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to update application configuration file: " + e.Message);
                base.Rollback(savedState);
            }
        }
    }
}
