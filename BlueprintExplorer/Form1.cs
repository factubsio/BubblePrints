using ModKit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using static BlueprintExplorer.BlueprintHandle;
using static BlueprintExplorer.BlueprintPropertyConverter;

namespace BlueprintExplorer {
    public partial class Form1 : Form {
        
        private static readonly Color RegularDarkColor = Color.FromArgb(50, 50, 50);
        private static readonly Color ChristmasColorBG = Color.FromArgb(150, 10, 10);
        private static readonly Color ChristmasColorFG = Color.White;


        public static Color SeasonalFGColor
        {
            get
            {

                if (SeasonalOverlay.NearChristmas)
                    return ChristmasColorFG;

                return Color.Green;
            }
        }

        public static Color SeasonalBGColor
        {
            get
            {
                if (SeasonalOverlay.NearChristmas)
                    return ChristmasColorBG;

                return Color.Black;
            }
        }


        public static void SeasonStyles(params DataGridViewCellStyle []styles)
        {
            foreach (var style in styles)
            {
                style.ForeColor = SeasonalFGColor;
                style.BackColor = SeasonalBGColor;
            }
        }
        public static void SeasonControls(params Control []controls)
        {
            foreach (var c in controls)
            {
                c.ForeColor = SeasonalFGColor;
                c.BackColor = SeasonalBGColor;
            }
        }

        public static void DarkenStyles(params DataGridViewCellStyle []styles)
        {
            foreach (var style in styles)
            {
                style.ForeColor = Color.White;
                style.BackColor = RegularDarkColor;
            }
        }
        public static void DarkenControls(params Control []controls)
        {
            foreach (var c in controls)
            {
                c.ForeColor = Color.White;
                c.BackColor = RegularDarkColor;
            }
        }
        private BlueprintDB db => BlueprintDB.Instance;
        private static bool dark;
        bool Good => initialize?.IsCompleted ?? false;

        public Form1() {
            var env = Environment.GetEnvironmentVariable("BubbleprintsTheme");
            Dark = env?.Equals("dark") ?? false;
            Dark |= Properties.Settings.Default.DarkMode;

            Properties.Settings.Default.PropertyChanged += Default_PropertyChanged;

            InitializeComponent();
#if DEBUG
            BubblePrints.SetupLogging();
#endif
            omniSearch.TextChanged += OmniSearch_TextChanged;
            resultsGrid.CellClick += ResultsGrid_CellClick;
            references.CellClick += references_CellClick;

            InstallReadline(omniSearch);
            InstallReadline(filter);

            Color bgColor = omniSearch.BackColor;
            resultsGrid.RowHeadersVisible = false;
            bpProps.DisabledItemForeColor = Color.Black;

            availableVersions.Enabled = false;


            settingsButton.Click += (sender, evt) =>
            {
                new SettingsView().ShowDialog();
            };

            //bpProps.Categor

            bpProps.PropertySort = PropertySort.NoSort;

            ToolStrip strip = bpProps.Controls.OfType<ToolStrip>().First();
            strip.Items.Add("expand all").Click += (sender, e) =>
            {
                bpProps.ExpandAllGridItems();
            };
            followLink = strip.Items.Add("follow link");
            followLink.Click += (sender, e) =>
            {
                if (CurrentView != null)
                    ShowBlueprint(BlueprintDB.Instance.Blueprints[Guid.Parse(CurrentLink)], true);
            };
            followLink.Enabled = false;

            openInEditor = strip.Items.Add("open in editor");
            openInEditor.Click += DoOpenInEditor;

            BubblePrints.SetWrathPath();

            if (BubblePrints.TryGetWrathPath(out var wrathPath))
            {
                BubblePrints.Wrath = Assembly.LoadFrom(Path.Combine(wrathPath, "Wrath_Data", "Managed", "Assembly-CSharp.dll"));
            }

            resultsGrid.AllowUserToResizeRows = false;

            if (Dark)
            {
                DarkenPropertyGrid(bpProps);
                resultsGrid.EnableHeadersVisualStyles = false;
                DarkenControls(filter, omniSearch, resultsGrid, count, splitContainer1, references, panel1, bottomPanel, settingsButton);
                DarkenStyles(resultsGrid.ColumnHeadersDefaultCellStyle, resultsGrid.DefaultCellStyle, references.DefaultCellStyle, references.ColumnHeadersDefaultCellStyle);

                Invalidate();
            }


            omniSearch.Enabled = false;
            filter.Enabled = false;
            resultsGrid.Enabled = false;

            if (SeasonalOverlay.InSeason)
            {
                SeasonControls(omniSearch, filter, panel1, settingsButton, count, resultsGrid);
                SeasonStyles(resultsGrid.ColumnHeadersDefaultCellStyle, resultsGrid.DefaultCellStyle);
                SeasonalOverlay.Install(resultsGrid);
            }

            var loadType = BlueprintDB.Instance.GetLoadType();

            var loadString = loadType switch
            {
                BlueprintDB.GoingToLoad.FromLocalFile => "LOADING (debug)",
                BlueprintDB.GoingToLoad.FromCache => "LOADING (local)",
                BlueprintDB.GoingToLoad.FromWeb => "DOWNLOADING",
                BlueprintDB.GoingToLoad.FromNewImport => "IMPORTING",
                _ => throw new Exception(),
            };

            var progress = new BlueprintDB.ConnectionProgress();

            initialize = Task.Run(() => BlueprintDB.Instance.TryConnect(progress));
            initialize.ContinueWith(b => {
                omniSearch.Enabled = true;
                resultsGrid.Enabled = true;
                omniSearch.Text = "";
                omniSearch.Select();
                ShowBlueprint(BlueprintDB.Instance.Blueprints.Values.First(), false);
                bpProps.SetLabelColumnWidth(450);

                foreach (var v in BlueprintDB.Instance.Available)
                    availableVersions.Items.Add(v);
                availableVersions.SelectedIndex = availableVersions.Items.Count - 1;
                availableVersions.Enabled = true;
            }, TaskScheduler.FromCurrentSynchronizationContext());

            bpProps.SelectedGridItemChanged += BpProps_SelectedGridItemChanged;


            new Thread(() => {
                string plane = $"{loadString}-🛬";
                const int frames = 90;

                while (true) {
                    for (int frame = 0; frame < frames; frame++) {
                        if (Good)
                            return;

                        if (omniSearch.Visible) {
                            omniSearch.Invoke(new Action(() => {
                                if (!Good) {
                                    omniSearch.Text = plane.PadLeft(plane.Length + frame) + $"{progress.Status}";
                                }
                            }));
                        }
                        Thread.Sleep(33);
                    }
                }
            }).Start();

        }

        private void ResultsGrid_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.FillRectangle(Brushes.Yellow, 0, 0, 100, 100);
        }

        private void Default_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
        }

        public static void DarkenPropertyGrid(PropertyGrid grid)
        {
            grid.LineColor = Color.DimGray;
            grid.DisabledItemForeColor = Color.White;
            grid.ViewForeColor = Color.White;
            grid.HelpForeColor = Color.LightGray;
            grid.HelpBackColor = RegularDarkColor;
            grid.BackColor = RegularDarkColor;
            grid.ViewBackColor = RegularDarkColor;
            grid.ViewBorderColor = Color.DimGray;
            grid.ViewBackColor = RegularDarkColor;        }

        class ElementWriteState
        {
            public bool IsObj;
            public int Children;
        }

        private void DoOpenInEditor(object sender, EventArgs e)
        {
            if (CurrentView != null)
            {
                var userLocalFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BubblePrints", "cache");
                if (!Directory.Exists(userLocalFolder))
                    Directory.CreateDirectory(userLocalFolder);

                string fileToOpen = Path.Combine(userLocalFolder, CurrentView.Name + "_" +  CurrentView.GuidText + ".json");

                bool json = Properties.Settings.Default.StrictJsonForEditor;
                if (!File.Exists(fileToOpen))
                {
                    using var stream = new StreamWriter(File.OpenWrite(fileToOpen));
                    int level = 0;
                    Stack<ElementWriteState> stack = new();
                    stack.Push(new() { IsObj = true });

                    void Quote(string str)
                    {
                        if (json)
                            stream.Write('"');
                        stream.Write(str);
                        if (json)
                            stream.Write('"');
                    }

                    void WriteMultiLine(string key, string value)
                    {
                        if (json)
                        {
                            WriteLine(key, value.Replace("\r\n", "\\n").Replace("\n", "\\n"), null);
                            return;
                        }
                        stream.WriteLine();
                        stream.Write("".PadLeft(level * 4));
                        if (stack.Peek().IsObj)
                        {
                            stream.Write(key);
                            stream.Write(": ");
                        }

                        int indent = level * 4 + key.Length + 2;

                        var lines = value.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (i > 0)
                                stream.Write("".PadLeft(indent));
                            stream.Write(lines[i]);
                            if (i != lines.Length - 1)
                                stream.WriteLine();
                        }
                    }

                    void WriteLine(string key, string value, string link)
                    {
                        if (json)
                        {
                            if (stack.Peek().Children > 1)
                                stream.Write(',');
                        }
                        stream.WriteLine();
                        stream.Write("".PadLeft(level * 4));
                        if (stack.Peek().IsObj)
                        {
                            Quote(key);
                            stream.Write(": ");
                        }

                        if (link != null)
                        {
                            var target = BlueprintDB.Instance.Blueprints[Guid.Parse(link)];

                            value = "link: " + link + " (" + target.Name + " :" + target.TypeName + ")";
                        }

                        Quote(value);
                    }

                    void Open(string key, bool obj)
                    {
                        if (json)
                        {
                            if (stack.Peek().Children > 1)
                                stream.Write(',');
                            stream.WriteLine();
                        }
                        stream.Write("".PadLeft(level * 4));
                        if (stack.Peek().IsObj)
                        {
                            Quote(key);
                            stream.Write(": ");
                        }
                        if (json)
                            stream.Write(obj ? '{' : '[');
                    }
                    void Close(ElementWriteState state)
                    {
                        if (state.Children > 0)
                        {
                            if (json)
                            {
                                stream.WriteLine();
                                stream.Write("".PadLeft(level * 4));
                            }
                        }
                        else
                        {
                            if (!json)
                                stream.WriteLine(" [empty]");
                        }
                        if (json)
                            stream.Write(state.IsObj ? '}' : ']');
                    }

                    if (json)
                        stream.WriteLine("{");

                    foreach (var elem in CurrentView.Elements)
                    {
                        if (elem.levelDelta < 0)
                        {
                            var closeObj = stack.Pop();
                            level--;
                            Close(closeObj);
                        }
                        else if (elem.levelDelta == 0)
                        {
                            stack.Peek().Children++;
                            WriteLine(elem.key, elem.value, elem.link);
                        }
                        else if (elem.levelDelta > 0)
                        {
                            stack.Peek().Children++;
                            Open(elem.key, elem.isObj);
                            stack.Push(new() { IsObj = elem.isObj });
                            level++;

                            var renderedString = NestedProxy.ParseAsString(elem.Node);
                            if (renderedString != null)
                            {
                                stack.Peek().Children++;
                                WriteMultiLine("LocalisedValue", renderedString);
                            }

                        }

                    }
                    if (json)
                        stream.WriteLine("}");
                }
                var editor = Properties.Settings.Default.Editor;
                if (editor == null || !File.Exists(editor))
                    editor = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "notepad.exe");
                Process.Start(editor, fileToOpen);
            }
        }

        private void BpProps_SelectedGridItemChanged(object sender, SelectedGridItemChangedEventArgs e)
        {
            if (e.NewSelection.Value is BlueprintPropertyConverter.BlueprintLink link)
            {
                CurrentLink = link.Link;
                followLink.Enabled = true;
                if (Properties.Settings.Default.EagerFollowLink)
                    followLink.PerformClick();
            }
            else
            {
                followLink.Enabled = false;
            }
        }

        private void ResultsGrid_CellClick(object sender, DataGridViewCellEventArgs e) {
            ShowSelected();
        }

        private void references_CellClick(object sender, DataGridViewCellEventArgs e) {
            ShowReferenceSelected();
        }

        private Stack<BlueprintHandle> history = new();

        private DateTime lastChange = DateTime.MinValue;
        private TimeSpan debounceTime = TimeSpan.FromSeconds(1.5);

        int lastFinished = 0;
        private CancellationTokenSource finishingFirst;
        private CancellationTokenSource finishingLast;
        private Task<List<BlueprintHandle>> overlappedSearch;

        private void SetResults(List<BlueprintHandle> results, CancellationTokenSource cancellation, int matchBuffer)
        {
            if (cancellation == finishingFirst)
                finishingFirst = null;
            if (cancellation == finishingLast)
                finishingLast = null;

            lastFinished = matchBuffer;
            BlueprintDB.UnlockBuffer(matchBuffer);
            resultsCache = results;
            count.Text = $"{resultsCache.Count}";
            var oldRowCount = resultsGrid.Rows.Count;
            var newRowCount = resultsCache.Count;
            if (newRowCount > oldRowCount)
                resultsGrid.Rows.Add(newRowCount - oldRowCount);
            else {
                resultsGrid.Rows.Clear();
                if (newRowCount > 0)
                    resultsGrid.Rows.Add(newRowCount);
            }
            resultsGrid.Invalidate();
        }

        private void InvalidateResults() {
            CancellationTokenSource cancellation = new();

            int matchBuffer = 0;

            if (finishingLast != null && finishingLast != finishingFirst)
            {
                finishingLast.Cancel();
                finishingLast.Token.WaitHandle.WaitOne();
                BlueprintDB.UnlockBuffer(1);
            }

            finishingLast = cancellation;

            if (finishingFirst == null)
            {
                matchBuffer = 0;
                finishingFirst = cancellation;
            }
            else
            {
                matchBuffer = 1;
            }


            Task<List<BlueprintHandle>> search;

            if (matchBuffer == 1)
            {
                overlappedSearch = db.SearchBlueprintsAsync(Search, cancellation.Token, matchBuffer);
                search = overlappedSearch;
            }
            else
            {
                search = db.SearchBlueprintsAsync(Search, cancellation.Token, matchBuffer);
            }

            search.ContinueWith(task =>
            {
                if (!task.IsCanceled && !cancellation.IsCancellationRequested)
                    this.Invoke((Action<List<BlueprintHandle>, CancellationTokenSource, int>)SetResults, task.Result, cancellation, matchBuffer);
            });

         }
        private void OmniSearch_TextChanged(object sender, EventArgs e) {
            if (!Good)
                return;
            InvalidateResults();
        }

        private string Search => omniSearch.Text;

        public static bool Dark { get => dark; set => dark = value; }

        private static readonly char[] wordSeparators =
        {
            ' ',
            '.',
            '/',
            ':',
        };
        private static void KillForwardLine(TextBox box) {
            var here = box.SelectionStart;
            string Search = box.Text;
            if (box.SelectionLength == 0) {
                if (here > 0)
                    box.Text = Search.Substring(0, here);
                else
                    box.Text = "";
                box.Select(Search.Length, 0);

            }

        }

        private static void KillBackLine(TextBox box) {
            var here = box.SelectionStart;
            string Search = box.Text;
            if (box.SelectionLength == 0) {
                if (here < Search.Length)
                    box.Text = Search.Substring(here);
                else
                    box.Text = "";

            }

        }

        private static void KillBackWord(TextBox box) {
            var here = box.SelectionStart;
            string Search = box.Text;
            if (box.SelectionLength == 0) {
                if (here == 0)
                    return;

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

                box.Text = newSearch;
                box.SelectionStart = killTo + 1;
            }
        }

        public static void InstallReadline(TextBox box)
        {
            List<string> history = new();
            int historyIndex = -1;

            box.KeyDown += (sender, e) =>
            {
                if (e.Control)
                {
                    switch (e.KeyCode)
                    {
                        case Keys.W:
                            KillBackWord(box);
                            break;
                        case Keys.K:
                            KillForwardLine(box);
                            break;
                        case Keys.U:
                            KillBackLine(box);
                            break;
                        case Keys.E:
                            box.Select(box.Text.Length, 0);
                            break;
                        case Keys.A:
                            box.Select(0, 0);
                            e.Handled = true;
                            e.SuppressKeyPress = true;
                            break;
                        case Keys.N:
                            int next = historyIndex + 1;
                            if (next < history.Count)
                            {
                                historyIndex = next;
                                box.Text = history[historyIndex];
                            }
                            e.Handled = true;
                            e.SuppressKeyPress = true;
                            break;
                        case Keys.P:
                            if (history.Count > 0 && historyIndex > 0)
                            {
                                historyIndex--;
                                box.Text = history[historyIndex];
                            }
                            e.Handled = true;
                            e.SuppressKeyPress = true;
                            break;
                    }
                }
                else if (e.KeyCode == Keys.Return || e.KeyCode == Keys.Enter)
                {
                    history.Add(box.Text);
                    historyIndex = history.Count - 1;
                }
            };
        }

        private void omniSearch_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Return || e.KeyCode == Keys.Enter) {
                if (resultsCache.Count > 0) {
                    ShowSelected();
                }
            }
            else if (e.KeyCode == Keys.Up) {
                if (resultsCache.Count > 1) {
                    int row = resultsGrid.SelectedRow() - 1;
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
                    int row = resultsGrid.SelectedRow() + 1;
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
            {
                Console.WriteLine($"results[{row}]");
                ShowBlueprint(resultsCache[row], true);
            }
        }

        private void ShowReferenceSelected() {
            if (CurrentView == null) return;
            if (CurrentView.BackReferences.Count != references.RowCount) return;

            if (TryReferencedSelected(out var row))
            {
                Console.WriteLine($"ROW: {row}");
                ShowBlueprint(BlueprintDB.Instance.Blueprints[CurrentView.BackReferences[row]], true);
            }
        }

        private bool TryReferencedSelected(out int row) {
            row = -1;
            if (CurrentView == null) return false;
            row = references.SelectedRow();
            return row >= 0 && row < CurrentView.BackReferences.Count;
        }

        private bool TryGetSelected(out int row) {
            row = resultsGrid.SelectedRow();
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

        private BlueprintHandle CurrentView = null;

        private void ShowBlueprint(BlueprintHandle bp, bool updateHistory) {
            filter.Enabled = true;
            bp.EnsureParsed();
            CurrentView = bp;

            references.Rows.Clear();
            var me = Guid.Parse(bp.GuidText);
            foreach (var reference in bp.BackReferences)
            {
                if (reference != me)
                    references.Rows.Add(BlueprintDB.Instance.Blueprints[reference].Name);
            }

            bpProps.SelectedObject = bp;
            var propertiesItem = bpProps.SelectedGridItem?.Parent?.GridItems["Properties"];
            if (propertiesItem != null && propertiesItem.Expandable)
                propertiesItem.Expanded = true;
            if (Properties.Settings.Default.EagerExpand)
                bpProps.ExpandAllGridItems();



            if (updateHistory)
                PushHistory(bp);

            if (updateHistory && Properties.Settings.Default.AlwaysOpenInEditor)
                openInEditor.PerformClick();
        }

        private List<BlueprintHandle> resultsCache = new();
        private Task<bool> initialize;
        private ToolStripItem followLink;
        private string CurrentLink;
        private ToolStripItem openInEditor;

        private void dataGridView1_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e) {
            var row = e.RowIndex;
            if ((resultsCache?.Count ?? 0) == 0) {
                e.Value = "...";
                return;
            }

            if (row >= resultsCache.Count)
                return;

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
                    e.Value = resultsCache[row].Score(lastFinished).ToString();
                    break;
                case 4:
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

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void bpProps_Click(object sender, EventArgs e)
        {

        }
    }
}
