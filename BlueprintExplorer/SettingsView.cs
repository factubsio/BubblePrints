using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlueprintExplorer
{
    public partial class SettingsView : Form
    {
        public SettingsView()
        {
            InitializeComponent();
            propertyGrid1.SelectedObject = new SettingsProxy();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            (propertyGrid1.SelectedObject as SettingsProxy).Sync();
            Properties.Settings.Default.Save();
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
