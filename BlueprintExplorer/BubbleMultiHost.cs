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
    public partial class BubbleMultiHost : UserControl
    {
        public BubbleMultiHost()
        {
            InitializeComponent();

            Dock = DockStyle.Fill;
        }

        public Dictionary<string, Control> HostedControls = new();

        private Control _Current = null;
        private string _CurrentId = null;

        public void ShowControl(string id)
        {
            if (id == _CurrentId) return;

            try
            {
                SuspendLayout();
                if (_Current != null)
                    Controls.Remove(_Current);

                _CurrentId = id;
                if (HostedControls.TryGetValue(id, out _Current))
                {
                    Controls.Add(_Current);
                    _Current.Dock = DockStyle.Fill;
                }
            }
            finally
            {
                ResumeLayout();
            }

        }
    }


}
