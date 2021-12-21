using System.Reflection;
using System.Windows.Forms;
using static BlueprintExplorer.BlueprintViewer;

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


        public static int SelectedRow(this DataGridView view) => view.SelectedRows.Count > 0 ? view.SelectedRows[0].Index : 0;

        public static bool UpdateHistory(this ShowFlags flags) => (flags & ShowFlags.F_UpdateHistory) != 0;
        public static bool ClearHistory(this ShowFlags flags) => (flags & ShowFlags.F_ClearHistory) != 0;
    }
}
