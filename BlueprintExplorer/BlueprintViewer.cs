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

        private DataGridView references;

        public void Navigate(NavigateTo to)
        {
            int target = to switch
            {
                NavigateTo.RelativeBackOne => ActiveHistoryIndex - 1,
                NavigateTo.RelativeForwardOne => ActiveHistoryIndex + 1,
                NavigateTo.AbsoluteFirst => 0,
                NavigateTo.AbsoluteLast => history.Count - 1,
                _ => throw new NotImplementedException(),
            };

            if (target >= 0 && target < history.Count)
            {
                ActiveHistoryIndex = target;
                ShowBlueprint(history[target], 0);
                InvalidateHistory();
            }
        }

        private BlueprintControl view;
        bool refShownFirstTime = true;

        private static event Action SharedFilterChanged;

        private static string _SharedFilter;
        private static string SharedFilter
        {
            get => _SharedFilter;
            set
            {
                if (_SharedFilter != value)
                {
                    _SharedFilter = value;
                    if (BubblePrints.Settings.ShareBlueprintFilter)
                        SharedFilterChanged?.Invoke();
                }
            }
        }

        public BlueprintViewer()
        {
            references = new();
            references.Columns.Add("ref", "References");
            references.Columns.Add("typ", "Type");
            references.Columns.Add("guid", "Guid");

            references.Columns[0].Width = 800;
            references.Columns[0].DataPropertyName = "References";
            references.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            references.Columns[1].Width = 800;
            references.Columns[1].DataPropertyName = "RefType";
            references.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            references.Columns[2].Width = 0;
            references.Columns[2].DataPropertyName = "Guid";
            references.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;


            references.ReadOnly = true;
            references.Cursor = Cursors.Arrow;

            references.AutoGenerateColumns = false;

            references.RowHeadersVisible = false;
            references.ColumnHeadersVisible = true;
            references.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            references.MultiSelect = false;
            references.AllowUserToResizeRows = false;
            references.AllowUserToAddRows = false;
            references.AllowUserToDeleteRows = false;

            InitializeComponent();
            searchTerm.Enabled = false;
            view = new();
            kryptonSplitContainer1.Panel1.Controls.Add(view);
            kryptonSplitContainer1.Panel2.Controls.Add(references);
            references.Dock = DockStyle.Fill;
            view.Dock = DockStyle.Fill;

            toggleReferencesVisible.ToolTipValues.EnableToolTips = true;
            toggleReferencesVisible.ToolTipValues.Description = "Toggle references panel visiblity";
            toggleReferencesVisible.ToolTipValues.Heading = "";
            toggleReferencesVisible.ToolTipValues.ToolTipStyle = Krypton.Toolkit.LabelStyle.SuperTip;

            toggleReferencesVisible.CheckedChanged += (sender, e) =>
            {
                toggleReferencesVisible.Text = toggleReferencesVisible.Checked ? ">>" : "<<";
                kryptonSplitContainer1.Panel2Collapsed = !toggleReferencesVisible.Checked;
                if (refShownFirstTime)
                {
                    kryptonSplitContainer1.SplitterDistance = this.Width - 500;
                    refShownFirstTime = false;

                }
            };


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

            BubblePrints.OnTemplatesChanged += BubblePrints_OnTemplatesChanged;
            BubblePrints_OnTemplatesChanged(0, BubblePrints.Settings.GeneratorTemplate?.Length ?? 0);
            if (templatesList.Items.Count > 0)
                templatesList.SelectedIndex = 0;

            copyTemplate.Click += (sender, e) =>
            {
                var availableTemplates = BubblePrints.Settings.GeneratorTemplate;
                if (templatesList.SelectedIndex == -1) return;
                if (templatesList.SelectedIndex >= availableTemplates.Length) return;

                var template = availableTemplates[templatesList.SelectedIndex];
                var result = TemplateRunner.Execute(template.Value, view.Blueprint as BlueprintHandle);
                Clipboard.SetDataObject(result, copy: true, retryTimes: 10, retryDelay: 100);
            };

            if (BubblePrints.Settings.ShareBlueprintFilter)
            {
                filter.Text = SharedFilter;
                view.Filter = SharedFilter;
            }

            view.OnPathHovered += path => currentPath.Text = path ?? "-";

            view.OnFilterChanged += filterValue =>
            {
                filter.Text = filterValue;
                SharedFilter = filterValue;
            };

            filter.TextChanged += (sender, e) =>
            {
                view.Filter = filter.Text;
                SharedFilter = filter.Text;
            };

            SharedFilterChanged += () =>
            {
                if (SharedFilter != filter.Text)
                    filter.Text = SharedFilter;
            };


            if (Form1.Dark)
            {
                BubbleTheme.DarkenControls(view, filter, references, openExternal, copyTemplate, templatesList, currentPath);
                BubbleTheme.DarkenStyles(references.DefaultCellStyle, references.ColumnHeadersDefaultCellStyle);
            }

            references.CellMouseClick += (sender, e) => ShowReferenceSelected(e.RowIndex, e.Button);
            references.Cursor = Cursors.Arrow;

            openExternal.Click += (sender, e) => OnOpenExternally?.Invoke(View.Blueprint as BlueprintHandle);

            this.AddMouseClickRecursively(HandleXbuttons);

            Load += (sender, e) =>
            {
                this.AddKeyDownRecursively((FindForm() as Form1).HandleGlobalKeys);
                this.AddKeyPressRecursively((FindForm() as Form1).HandleGlobalKeyPress);
            };
        }

        private void BubblePrints_OnTemplatesChanged(int oldCount, int newCount)
        {
            templatesList.Items.Clear();
            if (BubblePrints.Settings.GeneratorTemplate != null)
            {
                foreach (var template in BubblePrints.Settings.GeneratorTemplate)
                {
                    templatesList.Items.Add(template.Name);
                }
            }
            copyTemplate.Enabled = templatesList.Items.Count > 0;
        }

        private void HandleXbuttons(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.XButton1)
                Navigate(NavigateTo.RelativeBackOne);
            else if (e.Button == MouseButtons.XButton2)
                Navigate(NavigateTo.RelativeForwardOne);

            (ParentForm as Form1)?.HideCtrlP();
        }

        public void ShowBlueprint(BlueprintHandle handle, ShowFlags flags)
        {
            if (handle == view.Blueprint)
                return;

            view.Blueprint = handle;

            references.Rows.Clear();
            var me = Guid.Parse(handle.GuidText);
            foreach (var reference in handle.BackReferences)
            {
                var bp = BlueprintDB.Instance.Blueprints[reference];
                references.Rows.Add(bp.Name, bp.TypeName, bp.GuidText);
            }
            if (references.SortedColumn is not null) {
                references.Sort(references.SortedColumn, ListSortDirection.Ascending);
            }

            if (flags.ClearHistory())
            {
                historyBread.Controls.Clear();
                history.Clear();
                ActiveHistoryIndex = 0;
            }

            if (flags.UpdateHistory())
                PushHistory(handle);

            OnBlueprintShown?.Invoke(handle);
        }

        public BlueprintControl View => view;

        private readonly List<BlueprintHandle> history = new();
        private int ActiveHistoryIndex = 0;

        private void PushHistory(BlueprintHandle bp) {

            for (int i = ActiveHistoryIndex + 1; i < history.Count; i++)
            {
                history.RemoveAt(ActiveHistoryIndex + 1);
                historyBread.Controls.RemoveAt(ActiveHistoryIndex + 1);
            }

            var button = new Button();
            int historyIndex = history.Count;
            if (Form1.Dark)
                BubbleTheme.DarkenControls(button);
            button.MinimumSize = new Size(10, 44);
            button.Text = bp.Name;
            button.AutoSize = true;
            int here = historyBread.Controls.Count;
            button.Click += (sender, e) => {
                ActiveHistoryIndex = historyIndex;
                ShowBlueprint(bp, 0);
                InvalidateHistory();
            };
            historyBread.Controls.Add(button);
            history.Add(bp);

            ActiveHistoryIndex = historyIndex;
            InvalidateHistory();
        }

        private void InvalidateHistory()
        {
            for (int i = 0; i < history.Count; i++) {
                var button = historyBread.Controls[i];
                if (i == ActiveHistoryIndex)
                {
                    button.Font = new Font(button.Font, FontStyle.Bold);
                }
                else
                {
                    button.Font = new Font(button.Font, FontStyle.Italic);
                }
            }
        }

        private void ShowReferenceSelected(int row, MouseButtons button = MouseButtons.Left)
        {
            if (View.Blueprint is not BlueprintHandle handle) return;
            if (handle.BackReferences.Count != references.RowCount) return;
            Console.WriteLine(row);

            if (row >= 0 && row < handle.BackReferences.Count)
            {
                var obj = references.Rows[row];
                var guid = obj.Cells[2].Value as string;
                bool tabbed = ModifierKeys.HasFlag(Keys.Control) || button == MouseButtons.Middle;
                if (guid != handle.GuidText)
                {
                    BlueprintHandle bp = BlueprintDB.Instance.Blueprints[Guid.Parse(guid)];
                    if (tabbed)
                    {
                        OnLinkOpenNewTab?.Invoke(bp);
                    }
                    else
                    {
                        ShowBlueprint(bp, ShowFlags.F_UpdateHistory);
                    }
                }

            }
        }

        [Flags]
        public enum ShowFlags
        {
            F_UpdateHistory = 1,
            F_ClearHistory = 2,
        }

        private void close_Click(object sender, EventArgs e) => OnClose?.Invoke();

        public bool Searching => view.SearchDirection != 0;
        internal void BeginSearchBackward()
        {
            searchTerm.Enabled = true;

            view.PrepareForSearch(-1);
            SetSearchTerm("");
        }

        internal void BeginSearchForward()
        {
            searchTerm.Enabled = true;

            view.PrepareForSearch(1);
            SetSearchTerm("");
        }
        internal void DeleteLastSearchChar()
        {
            if (rawSearchTerm.Length > 0)
                SetSearchTerm(rawSearchTerm[..^1]);
        }

        string rawSearchTerm;

        private void SetSearchTerm(string term, bool propogate = true)
        {
            rawSearchTerm = term;
            if (term.Length == 0 && !searchTerm.Enabled)
                searchTerm.Text = "";
            else
                searchTerm.Text = (view.SearchDirection > 0 ? '/' : '?') + term;
            if (propogate)
                view.SearchTerm = term;

        }

        internal void AppendSearchChar(char ch)
        {
            SetSearchTerm(rawSearchTerm + ch);
        }

        internal void StopSearching(bool commit)
        {
            view.EndSearch(commit);
            if (!commit)
                SetSearchTerm("", false);
            searchTerm.Enabled = false;
        }
    }
}
