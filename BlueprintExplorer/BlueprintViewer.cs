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

        public BlueprintViewer()
        {
            references = new();
            references.Columns.Add("ref", "References");

            references.Columns[0].Width = 800;
            references.Columns[0].DataPropertyName = "References";
            references.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            references.ReadOnly = true;
            references.Cursor = Cursors.Arrow;

            references.AutoGenerateColumns = false;

            references.RowHeadersVisible = false;
            references.ColumnHeadersVisible = false;
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
                    kryptonSplitContainer1.SplitterDistance = this.Width - 300;
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
                Clipboard.SetText(result);
            };

            view.OnPathHovered += path =>
            {
                currentPath.Text = path ?? "-";
            };

            view.OnFilterChanged += filterValue =>
            {
                filter.Text = filterValue;
            };

            view.OnNavigate += Navigate;

            filter.TextChanged += (sender, e) => view.Filter = filter.Text;
            if (Form1.Dark)
            {
                BubbleTheme.DarkenControls(view, filter, references, openExternal, copyTemplate, templatesList, currentPath);
                BubbleTheme.DarkenStyles(references.DefaultCellStyle, references.ColumnHeadersDefaultCellStyle);
            }

            references.CellClick += (sender, e) => ShowReferenceSelected();
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
                    references.Rows.Add(BlueprintDB.Instance.Blueprints[reference].Name);
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

        private void ShowReferenceSelected()
        {
            var handle = View.Blueprint as BlueprintHandle;
            if (handle == null) return;
            if (handle.BackReferences.Count != references.RowCount) return;
            int row = references.SelectedRow();

            if (row >= 0 && row < handle.BackReferences.Count)
            {
                if (handle.BackReferences[row] != Guid.Parse(handle.GuidText))
                    ShowBlueprint(BlueprintDB.Instance.Blueprints[handle.BackReferences[row]], ShowFlags.F_UpdateHistory);

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
            searchTerm.Text = "?";

            view.PrepareForSearch(-1);
        }

        internal void BeginSearchForward()
        {
            searchTerm.Enabled = true;
            searchTerm.Text = "/";

            view.PrepareForSearch(1);
        }
        internal void DeleteLastSearchChar()
        {
            if (searchTerm.Text.Length > 1)
                searchTerm.Text = searchTerm.Text[..^1];
        }

        internal void AppendSearchChar(char ch)
        {
            searchTerm.Text += ch;
            view.SearchTerm = searchTerm.Text[1..];
        }

        internal void StopSearching(bool commit)
        {
            view.EndSearch(commit);
            searchTerm.Text = "";
            searchTerm.Enabled = false;
        }
    }
}
