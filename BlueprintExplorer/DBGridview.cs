using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlueprintExplorer
{
    public class DBGridview : DataGridView
    {
        DBGridview() : base()
        {
            DoubleBuffered = true;
        }
    }
}
