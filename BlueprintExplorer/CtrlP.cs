using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlueprintExplorer
{
    public partial class CtrlP : Form
    {
        private bool _Pinned = false;
        public bool Pinned
        {
            get => _Pinned;
            set
            {
                if (_Pinned == value) return;
                _Pinned = value;
                UpdatePinned();
            }
        }
        private int _PinnedHeight;
        public int PinnedHeight
        {
            get => _PinnedHeight;
            set
            {
                _PinnedHeight = value;

                if (!_Pinned || Height == value) return;

                UpdatePinned();
            }
        }
        private bool AutoColChange = false;
        private DataGridView root;
        private Label noResults;
        private Font defaultRootFont;
        private Font smallRootFont;
        public CtrlP()
        {
            root = new();
            noResults = new();
            noResults.Text = "Start typing to see results";
            noResults.TextAlign = ContentAlignment.MiddleCenter;
            noResults.Font = new Font(noResults.Font.FontFamily, 26f);

            InitializeComponent();
            Form1.InstallReadline(input);
            input.KeyDown += Input_KeyDown;
            root.KeyDown += Input_KeyDown;
            input.TextChanged += Input_TextChanged;

            root.ReadOnly = true;
            root.Cursor = Cursors.Arrow;

            root.AutoGenerateColumns = false;
            root.Columns.Add("Name", "Name");
            root.Columns.Add("Type", "Type");
            root.Columns.Add("Guid", "Guid");

            root.Columns[0].Width = BubblePrints.Settings.GetColumnSize(0);
            root.Columns[0].DataPropertyName = "Name";
            root.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            root.Columns[0].FillWeight = 1;

            root.Columns[1].Width = BubblePrints.Settings.GetColumnSize(1);
            root.Columns[1].DataPropertyName = "TypeForResults";
            root.Columns[1].FillWeight = 0.2f;

            root.Columns[2].Width = BubblePrints.Settings.GetColumnSize(2);
            root.Columns[2].DataPropertyName = "GuidText";
            root.Columns[2].FillWeight = 0.2f;

            defaultRootFont = root.Font;
            smallRootFont = new(defaultRootFont.FontFamily, defaultRootFont.Size * 0.75f);
            root.ColumnWidthChanged += (sender, e) =>
            {
                if (AutoColChange)
                {
                    return;
                }
                BubblePrints.Settings.SetColumnSize(0, root.Columns[0].Width);
                BubblePrints.Settings.SetColumnSize(1, root.Columns[1].Width);
                BubblePrints.Settings.SetColumnSize(2, root.Columns[2].Width);
                BubblePrints.SaveSettings();
            };

            root.RowHeadersVisible = false;
            root.ColumnHeadersVisible = false;
            root.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            root.MultiSelect = false;
            root.AllowUserToResizeRows = false;
            root.AllowUserToAddRows = false;
            root.AllowUserToDeleteRows = false;

            if (Form1.Dark)
            {
                BubbleTheme.DarkenControls(this, rootPanel, input, root, closeHintLabel);
                BubbleTheme.DarkenStyles(root.DefaultCellStyle, root.ColumnHeadersDefaultCellStyle);
            }

            root.MouseClick += Root_MouseClick;

            root.GetType().GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(root, true);
            root.ShowCellToolTips = false;

            rootPanel.BorderStyle = BorderStyle.FixedSingle;
            if (Form1.Dark)
            {
                rootPanel.BackColor = Color.Navy;
            }
            else
            {
                rootPanel.BackColor = Color.MistyRose;
            }
            root.TabStop = false;

            rootHost.HostedControls.Add("results", root);
            rootHost.HostedControls.Add("none", noResults);
            rootHost.ShowControl("none");

            this.ShowInTaskbar = false;

            DoubleBuffered = true;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var defaultParams = base.CreateParams;
                defaultParams.ExStyle |= 0x80;
                return defaultParams;
            }
        }

        private void Root_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle || e.Button == MouseButtons.Left)
            {
                var row = root.HitTest(e.X, e.Y).RowIndex;
                if (row >= 0 && row < root.Rows.Count)
                {
                    root.Rows[row].Selected = true;
                    Daddy.ShowBlueprint(row, ModifierKeys.HasFlag(Keys.Control) || e.Button == MouseButtons.Middle);
                    Daddy.HideCtrlP();
                }
            }
        }

        public Form1 Daddy;

        private void Input_TextChanged(object sender, EventArgs e)
        {
            Daddy.InvalidateResults(input.Text);
        }

        private void UpdatePinned()
        {
            closeHintLabel.OverrideText = _Pinned ? "<pinned>" : null;
            UpdateSize();
        }

        private void UpdateSize()
        {
            if (Pinned)
            {
                Height = PinnedHeight;
                return;
            }
            int neededHeight = root.Rows.GetRowsHeight(DataGridViewElementStates.None) - 33;
            if (neededHeight < 640)
            {
                Height = neededHeight + 240;
            }
            else
            {
                Height = 640 + 82;
            }
        }

        public void SetResults(List<BlueprintHandle> results)
        {
            bool nowShort = Width < 1300;
            if (BlueprintHandle.ShortType != nowShort)
            {
                BlueprintHandle.ShortType = nowShort;
                AutoColChange = true;
                if (nowShort)
                {
                    root.Columns[1].Width = 220;
                    root.Columns[2].Width = 270;
                    root.Font = smallRootFont;
                }
                else
                {
                    root.Columns[1].Width = BubblePrints.Settings.GetColumnSize(1);
                    root.Columns[2].Width = BubblePrints.Settings.GetColumnSize(2);
                    root.Font = defaultRootFont;
                }
                AutoColChange = true;
            }
            root.DataSource = results;
            if (results.Count > 0)
                rootHost.ShowControl("results");
            else
                rootHost.ShowControl("none");
            UpdateSize();
        }

        private void TryScroll(int delta)
        {
            if (root.Rows.Count == 0) return;

            int current = root.SelectedRow();
            int wanted = current + delta;
            if (wanted < 0) wanted = 0;
            if (wanted >= root.Rows.Count) wanted = root.Rows.Count - 1;

            if (current != wanted)
            {
                root.Rows[wanted].Selected = true;
                root.CurrentCell = root.Rows[wanted].Cells[0];
            }
        }

        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Daddy.HideCtrlP();
            }
            if (e.KeyCode == Keys.F5)
            {
                StringBuilder sb = new();
                sb.Append($"Name,Type,Id\r\n");
                foreach (DataGridViewRow row in root.Rows)
                {
                    List<string> list = new();
                    foreach (DataGridViewTextBoxCell cell in row.Cells)
                    {
                        list.Add(cell.FormattedValue.ToString());
                    }
                    sb.Append(string.Join(",", list));
                    sb.Append("\r\n");
                }
                var userLocalFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BubblePrints", "saved-searches");
                if (!Directory.Exists(userLocalFolder))
                    Directory.CreateDirectory(userLocalFolder);
                var datestr = DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss");
                var path = Path.Combine(userLocalFolder, $"{datestr}.csv");
                File.WriteAllText(path, sb.ToString());
            }
            if (e.KeyCode == Keys.Up || (e.KeyCode == Keys.P && ModifierKeys.HasFlag(Keys.Control)))
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                TryScroll(-1);

            }
            if (e.KeyCode == Keys.Down || (e.KeyCode == Keys.N && ModifierKeys.HasFlag(Keys.Control)))
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                TryScroll(1);
            }

            if (e.KeyCode == Keys.PageUp)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                TryScroll(-16);
            }
            if (e.KeyCode == Keys.PageDown)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                TryScroll(16);
            }

            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return)
            {

                e.Handled = true;
                e.SuppressKeyPress = true;

                Daddy.ShowBlueprint(root.SelectedRow(), ModifierKeys.HasFlag(Keys.Control));

                Daddy.HideCtrlP();
            }
        }

        protected override void OnDeactivate(EventArgs e)
        {
            savedVerticalScroll = root.FirstDisplayedScrollingRowIndex;
        }

        private int savedVerticalScroll = -1;

        protected override void OnActivated(EventArgs e)
        {
            Owner = Daddy;
            UpdateSize();
            if (savedVerticalScroll != -1)
            {
                root.FirstDisplayedScrollingRowIndex = savedVerticalScroll;
            }
            input.Focus();
            base.OnActivated(e);
        }
    }
}
