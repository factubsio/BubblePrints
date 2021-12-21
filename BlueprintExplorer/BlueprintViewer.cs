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
    public partial class BlueprintViewer : UserControl
    {
        public delegate void BlueprintHandleDelegate(BlueprintHandle bp);
        public event BlueprintHandleDelegate OnLinkOpenNewTab;
        public event BlueprintHandleDelegate OnBlueprintShown;
        public event BlueprintHandleDelegate OnOpenExternally;
        public event Action OnClose;

        public BlueprintViewer()
        {
            InitializeComponent();
            Form1.InstallReadline(filter);
            view.OnLinkClicked += (link, newTab) =>
            {
                if (BlueprintDB.Instance.Blueprints.TryGetValue(Guid.Parse(link), out var bp))
                {
                    if (newTab)
                        OnLinkOpenNewTab?.Invoke(bp);
                    else
                        ShowBlueprint(bp, ShowFlags.F_UpdateHistory);
                }
            };

            view.OnPathHovered += path =>
            {
                currentPath.Text = path ?? "-";
            };

            filter.TextChanged += (sender, e) => view.Filter = filter.Text;
            if (Form1.Dark)
            {
                BubbleTheme.DarkenControls(view, filter, references, openExternal, currentPath);
                BubbleTheme.DarkenStyles(references.DefaultCellStyle, references.ColumnHeadersDefaultCellStyle);
            }

            references.CellClick += (sender, e) => ShowReferenceSelected();

            openExternal.Click += (sender, e) => OnOpenExternally?.Invoke(View.Blueprint);
        }

        public void ShowBlueprint(BlueprintHandle handle, ShowFlags flags)
        {
            view.Blueprint = handle;

            references.Rows.Clear();
            var me = Guid.Parse(handle.GuidText);
            foreach (var reference in handle.BackReferences)
            {
                if (reference != me)
                    references.Rows.Add(BlueprintDB.Instance.Blueprints[reference].Name);
            }

            if (flags.ClearHistory())
            {
                historyBread.Controls.Clear();
                history.Clear();
            }

            if (flags.UpdateHistory())
                PushHistory(handle);

            OnBlueprintShown?.Invoke(handle);
        }

        public BlueprintControl View => view;

        private readonly Stack<BlueprintHandle> history = new();

        private void PopHistory(int to) {
            int toRemove = history.Count;
            for (int i = to + 1; i < toRemove; i++) {
                historyBread.Controls.RemoveAt(to + 1);
                history.Pop();
            }
        }

        private void PushHistory(BlueprintHandle bp) {
            var button = new Button();
            if (Form1.Dark)
                BubbleTheme.DarkenControls(button);
            button.MinimumSize = new Size(10, 44);
            button.Text = bp.Name;
            button.AutoSize = true;
            int here = historyBread.Controls.Count;
            button.Click += (sender, e) => {
                PopHistory(here);
                ShowBlueprint(bp, 0);
            };
            historyBread.Controls.Add(button);
            history.Push(bp);
        }

        private void ShowReferenceSelected()
        {
            var handle = View.Blueprint;
            if (handle == null) return;
            if (handle.BackReferences.Count != references.RowCount) return;
            int row = references.SelectedRow();

            if (row >= 0 && row < handle.BackReferences.Count)
                ShowBlueprint(BlueprintDB.Instance.Blueprints[handle.BackReferences[row]], ShowFlags.F_UpdateHistory);
        }

        [Flags]
        public enum ShowFlags
        {
            F_UpdateHistory = 1,
            F_ClearHistory = 2,
        }

        private void close_Click(object sender, EventArgs e) => OnClose?.Invoke();
    }
}
