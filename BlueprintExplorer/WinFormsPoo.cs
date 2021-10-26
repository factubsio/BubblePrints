using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlueprintExplorer
{
    public static class PropGridExtensions
    {
        public static void SetLabelColumnWidth(this PropertyGrid grid, int width)
        {
            FieldInfo fi = grid?.GetType().GetField("_gridView", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fi == null)
                fi = grid?.GetType().GetField("gridView", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fi == null)
                return;
            Control view = fi?.GetValue(grid) as Control;
            MethodInfo mi = view?.GetType().GetMethod("MoveSplitterTo", BindingFlags.Instance | BindingFlags.NonPublic);
            mi?.Invoke(view, new object[] { width });
        }
    }
}
