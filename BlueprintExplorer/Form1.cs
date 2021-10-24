using ModKit;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static BlueprintExplorer.BlueprintDB;

namespace BlueprintExplorer {
    public partial class Form1 : Form {
        private BlueprintDB db => BlueprintDB.Instance;
        public static bool Dark;
        bool Good => initialize?.IsCompleted ?? false;

        public Form1() {
            var env = Environment.GetEnvironmentVariable("BubbleprintsTheme");
            Dark = env?.Equals("dark") ?? false;

            InitializeComponent();
            omniSearch.TextChanged += OmniSearch_TextChanged;
            bpView.NodeMouseClick += BpView_NodeMouseClick;
            resultsGrid.CellClick += ResultsGrid_CellClick;

            Color bgColor = omniSearch.BackColor;
            resultsGrid.RowHeadersVisible = false;

            if (Dark) {
                bgColor = Color.FromArgb(50, 50, 50);

                this.BackColor = bgColor;
                filter.BackColor = bgColor;
                filter.ForeColor = Color.White;
                omniSearch.BackColor = bgColor;
                omniSearch.ForeColor = Color.White;
                bpView.BackColor = bgColor;
                bpView.ForeColor = Color.White;
                resultsGrid.BackgroundColor = bgColor;
                resultsGrid.ForeColor = Color.White;
                resultsGrid.ColumnHeadersDefaultCellStyle.BackColor = bgColor;
                resultsGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                resultsGrid.EnableHeadersVisualStyles = false;
                resultsGrid.DefaultCellStyle.ForeColor = Color.White;
                resultsGrid.DefaultCellStyle.BackColor = bgColor;
            }

            omniSearch.Enabled = false;
            filter.Enabled = false;
            resultsGrid.Enabled = false;
            bpView.Enabled = false;

            omniSearch.Text = "LOADING";


            initialize = BlueprintDB.Instance.TryConnect();
            initialize.ContinueWith(b => {
                omniSearch.Enabled = true;
                filter.Enabled = false;
                resultsGrid.Enabled = true;
                bpView.Enabled = true;
                omniSearch.Text = "";
                //omniSearch.Select();
                GC.Collect();
            }, TaskScheduler.FromCurrentSynchronizationContext());

            new Thread(() => {
                const string plane = "LOADING-🛬";
                const int frames = 90;

                while (true) {
                    for (int frame = 0; frame < frames; frame++) {
                        if (Good)
                            return;

                        if (omniSearch.Visible) {
                            omniSearch.Invoke(new Action(() => {
                                if (!Good) {
                                    omniSearch.Text = plane.PadLeft(plane.Length + frame);
                                }
                            }));
                        }
                        Thread.Sleep(33);
                    }
                }
            }).Start();

        }


        private void ResultsGrid_CellClick(object sender, DataGridViewCellEventArgs e) {
            ShowSelected();
        }

        private Stack<BlueprintHandle> history = new();

        private void BpView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e) {
            if (e.Node.Tag == null)
                return;
            Guid guid = Guid.Parse(e.Node.Tag as string);
            if (guid == Guid.Empty)
                return;

            ShowBlueprint(db.Blueprints[guid], true);
        }
        static System.Windows.Forms.Timer InvalidateTimer;

        private DateTime lastChange = DateTime.MinValue;
        private TimeSpan debounceTime = TimeSpan.FromSeconds(1.5);

        private void timer1_Tick(object sender, EventArgs e) {
            Console.WriteLine("tick");
            lastChange = DateTime.Now;
            resultsCache = db.SearchBlueprints(Search?.ToLower());
            count.Text = $"{resultsCache.Count}";
            InvalidateTimer.Stop();
            InvalidateResults();
        }
        private void UpdateSearchResults(double delay) {
            InvalidateTimer?.Stop();
            InvalidateTimer = new System.Windows.Forms.Timer();
            InvalidateTimer.Interval = (int)(delay * 1000);
            InvalidateTimer.Tick += new EventHandler(timer1_Tick);
            InvalidateTimer.Start();

        }
        private void InvalidateResults() {
            resultsCache = db.SearchBlueprints(Search?.ToLower());
            count.Text = $"{resultsCache.Count()}";
            var oldRowCount = resultsGrid.Rows.Count;
            var newRowCount = resultsCache.Count();
            if (newRowCount > oldRowCount)
                resultsGrid.Rows.Add(newRowCount - oldRowCount);
            else {
                resultsGrid.Rows.Clear();
                if (newRowCount > 0)
                    resultsGrid.Rows.Add(newRowCount);
            }
            resultsGrid.Invalidate();
        }
        private void OmniSearch_TextChanged(object sender, EventArgs e) {
            if (!Good)
                return;
            InvalidateResults();
            //UpdateSearchResults(1f);
        }

        private string Search => omniSearch.Text;

        private char[] wordSeparators =
        {
            ' ',
            '.',
            '/',
            ':',
        };
        private void KillForwardLine() {
            var here = omniSearch.SelectionStart;
            if (omniSearch.SelectionLength == 0) {
                if (here > 0)
                    omniSearch.Text = Search.Substring(0, here);
                else
                    omniSearch.Text = "";
                omniSearch.Select(Search.Length, 0);

            }

        }

        private void KillBackLine() {
            var here = omniSearch.SelectionStart;
            if (omniSearch.SelectionLength == 0) {
                if (here < Search.Length)
                    omniSearch.Text = Search.Substring(here);
                else
                    omniSearch.Text = "";

            }

        }

        private void KillBackWord() {
            var here = omniSearch.SelectionStart;
            if (omniSearch.SelectionLength == 0) {
                while (here > 0 && Search[here - 1] == ' ')
                    here--;

                var killTo = Search.LastIndexOfAny(wordSeparators, here - 1);
                if (killTo == -1)
                    killTo = 0;

                string newSearch;

                if (killTo > 0)
                    newSearch = Search.Substring(0, killTo + 1);
                else
                    newSearch = "";

                if (here < Search.Length) {
                    newSearch += Search.Substring(here);
                }

                omniSearch.Text = newSearch;
                omniSearch.SelectionStart = killTo + 1;
            }
        }

        private void omniSearch_KeyDown(object sender, KeyEventArgs e) {
            if (e.Control) {
                switch (e.KeyCode) {
                    case Keys.W:
                        KillBackWord();
                        break;
                    case Keys.K:
                        KillForwardLine();
                        break;
                    case Keys.U:
                        KillBackLine();
                        break;
                    case Keys.E:
                        omniSearch.Select(Search.Length, 0);
                        break;
                    case Keys.A:
                        omniSearch.Select(0, 0);
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        break;
                }
            }
            else if (e.KeyCode == Keys.Return || e.KeyCode == Keys.Enter) {
                if (resultsCache.Count > 0) {
                    ShowSelected();
                }
            }
            else if (e.KeyCode == Keys.Up) {
                if (resultsCache.Count > 1) {
                    int row = SelectedRow - 1;
                    if (row >= 0 && row < resultsCache.Count) {
                        resultsGrid.Rows[row].Selected = true;
                        resultsGrid.CurrentCell = resultsGrid[0, row];
                        resultsGrid.CurrentCell.ToolTipText = "";
                    }
                }
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Down) {
                if (resultsCache.Count > 1) {
                    int row = SelectedRow + 1;
                    if (row < resultsCache.Count) {
                        resultsGrid.Rows[row].Selected = true;
                        resultsGrid.CurrentCell = resultsGrid[0, row];
                        resultsGrid.CurrentCell.ToolTipText = "";
                    }
                }
                e.Handled = true;
                e.SuppressKeyPress = true;
            }

        }

        private void ShowSelected() {
            historyBread.Controls.Clear();
            history.Clear();

            if (TryGetSelected(out var row))
                ShowBlueprint(resultsCache[row], true);
        }

        private int SelectedRow => resultsGrid.SelectedRows.Count > 0 ? resultsGrid.SelectedRows[0].Index : 0;

        private bool TryGetSelected(out int row) {
            row = SelectedRow;
            return row >= 0 && row < resultsCache.Count;
        }

        private void PopHistory(int to) {
            int toRemove = history.Count;
            for (int i = to + 1; i < toRemove; i++) {
                historyBread.Controls.RemoveAt(to + 1);
                history.Pop();
            }
        }

        private void PushHistory(BlueprintHandle bp) {
            var button = new Button();
            if (Dark)
                button.ForeColor = Color.White;
            button.MinimumSize = new Size(10, 44);
            button.Text = bp.Name;
            button.AutoSize = true;
            int here = historyBread.Controls.Count;
            button.Click += (sender, e) => {
                PopHistory(here);
                ShowBlueprint(bp, false);
            };
            historyBread.Controls.Add(button);
            history.Push(bp);
        }

        private void ShowBlueprint(BlueprintHandle bp, bool updateHistory) {
            if (!bp.Parsed) {
                bp.obj = JsonSerializer.Deserialize<dynamic>(bp.Raw);
                bp.Parsed = true;
            }

            bpView.BeginUpdate();

            bpView.Nodes.Clear();
            TreeNode node = null;

            var bpRoot = (JsonElement)bp.obj;

            Predicate<BlueprintHandle.VisitedElement> filterPred = _ => true;

            var query = filter.Text.Trim();

            if (query.Length > 0) {
                filterPred = node => {
                    if (node.levelDelta < 0)
                        return false;
                    if (node.key.Contains(filter.Text, StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (node.value?.Contains(filter.Text, StringComparison.OrdinalIgnoreCase) ?? false)
                        return true;
                    return false;
                };
            }

            var nodeList = BlueprintHandle.Visit(bpRoot, bp.Name).ToList();

            Stack<BlueprintHandle.VisitedElement> stack = new();

            foreach (var e in nodeList) {
                if (e.levelDelta > 0) {
                    e.Empty = !filterPred(e);
                    stack.Push(e);
                }
                else if (e.levelDelta < 0) {
                    var entry = stack.Pop();
                    e.Empty = entry.Empty;
                    if (stack.TryPeek(out var parent))
                        parent.Empty &= entry.Empty;
                }
                else {
                    if (!stack.Peek().Empty || filterPred(e)) {
                        e.Empty = false;
                        stack.Peek().Empty = false;
                    }
                    else {
                        e.Empty = true;
                    }
                }
            }

            nodeList.First().Empty = false;

            stack.Clear();

            foreach (var e in nodeList) {
                if (e.levelDelta > 0) {
                    stack.Push(e);
                    if (e.Empty)
                        continue;
                    var next = new TreeNode(e.key);
                    if (node == null)
                        bpView.Nodes.Add(next);
                    else
                        node.Nodes.Add(next);
                    node = next;
                }
                else if (e.levelDelta < 0) {
                    stack.Pop();
                    if (e.Empty)
                        continue;
                    node = node.Parent;
                }
                else {
                    if (stack.Peek().Empty || e.Empty)
                        continue;

                    var leaf = node.Nodes.Add(e.key, $"{e.key}: {e.value.Truncate(80)}");
                    leaf.ToolTipText = e.value;
                    leaf.Tag = e.link;
                    if (e.link != null) {
                        leaf.ForeColor = Color.Yellow;
                        leaf.BackColor = Color.Black;
                    }
                }
            }

            bpView.Nodes[0].ExpandAll();
            bpView.EndUpdate();
            bpView.SelectedNode = bpView.Nodes[0];

            if (updateHistory)
                PushHistory(bp);
        }

        private List<BlueprintHandle> resultsCache = new();
        private Task<bool> initialize;

        private void dataGridView1_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e) {
            var row = e.RowIndex;
            if ((resultsCache?.Count ?? 0) == 0) {
                e.Value = "...";
                return;
            }



            switch (e.ColumnIndex) {
                case 0:
                    e.Value = resultsCache[row].Name;
                    break;
                case 1:
                    e.Value = resultsCache[row].TypeName;
                    break;
                case 2:
                    e.Value = resultsCache[row].Namespace;
                    break;
                case 3:
                    e.Value = resultsCache[row].GuidText;
                    break;
                default:
                    e.Value = "<error>";
                    break;
            }
        }

        private void filter_TextChanged(object sender, EventArgs e) {
            if (history.Count == 0)
                return;
            ShowBlueprint(history.Peek(), false);

        }

        private void omniSearch_TextChanged_1(object sender, EventArgs e) {

        }

        private void label1_Click(object sender, EventArgs e) {

        }

        private void resultsGrid_CellContentClick(object sender, DataGridViewCellEventArgs e) {

        }
    }
}
