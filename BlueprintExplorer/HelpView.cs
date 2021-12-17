using BlueprintExplorer.Properties;
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
    public partial class HelpView : Form
    {
        public HelpView()
        {
            InitializeComponent();
            Text = "Bubbleprints - Help";
            contents.Rtf = Resources.help;
        }
    }
}
