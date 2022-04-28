using Krypton.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlueprintExplorer
{
    public class NonFocusButton : Button
    {
        public NonFocusButton()
        {
            SetStyle(ControlStyles.Selectable, false);
        }
    }

    public class NonFocusKryptonCheckButton : KryptonCheckButton
    {
        public NonFocusKryptonCheckButton()
        {
            SetStyle(ControlStyles.Selectable, false);
        }
    }
}
