using Krypton.Navigator;
using Krypton.Toolkit;
using Krypton.Workspace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static BlueprintExplorer.BlueprintViewer;

namespace BlueprintExplorer
{
    public partial class Form1 : BubbleprintsForm
    {

        private static bool dark;
        bool Good => initialize?.IsCompleted ?? false;

        public BlueprintViewer NewBlueprintViewer(KryptonWorkspaceCell cell = null)
        {
            var viewer = new BlueprintViewer();
            if (Dark)
            {
                BubbleTheme.DarkenControls(viewer);
            }

            viewer.View.Font = BlueprintFont;
            viewer.View.LinkFont = LinkFont;
            var page = new KryptonPage
            {
                Text = "<empty>",
                TextTitle = "<empty>",
                TextDescription = "<empty>",
            };
            page.ClearFlags(KryptonPageFlags.DockingAllowClose | KryptonPageFlags.DockingAllowFloating | KryptonPageFlags.DockingAllowAutoHidden);

            viewer.OnBlueprintShown += bp =>
            {
                page.Text = "  " + bp.Name + "   ";
                page.UniqueName = bp.GuidText;
            };

            viewer.OnOpenExternally += bp =>
            {
                DoOpenInEditor(bp);
            };

            viewer.OnLinkOpenNewTab += bp =>
            {
                //var existing = blueprintDock.PageForUniqueName(bp.GuidText);
                //if (existing != null)
                //{
                //    blueprintDock.ActivePage = existing;
                //}
                //else
                {
                    var cell = blueprintDock.CellForPage(page);
                    var viewer = NewBlueprintViewer(cell);
                    viewer.ShowBlueprint(bp, ShowFlags.F_ClearHistory | ShowFlags.F_UpdateHistory);

                    var parent = (viewer.Parent as KryptonPage);

                    cell.SelectedPage = parent;
                }
            };

            var nav = cell ?? blueprintDock.ActiveCell;
            ButtonSpecAny bsa = new()
            {
                Style = PaletteButtonStyle.Standalone,
                Type = PaletteButtonSpecStyle.Close,
                Tag = page,
            };
            bsa.Click += (sender, e) =>
            {
                (page.KryptonParentContainer as KryptonNavigator)?.Pages.Remove(page);
            };
            page.ButtonSpecs.Add(bsa);

            page.Controls.Add(viewer);
            viewer.Dock = DockStyle.Fill;
            if (cell != null)
            {
                cell.Pages.Add(page);
            }
            else
            {
                kDockManager.AddToWorkspace("Workspace", new KryptonPage[]
                {
                    page
                });
            }


            //for (int i =0; i < blueprintViews.TabCount; i++)
            //{
            //    (blueprintViews.TabPages[i].Controls[0] as BlueprintViewer).CanClose = blueprintViews.TabCount > 1;
            //}


            return viewer;
        }

        public class BubbleNotification
        {
            public string Message;
            public Action Action;
            public string ActionText;

            public bool Complete;
        }

        private NotificationsView notificationsView = new();
        private List<BubbleNotification> pendingNotifications = new();
        private ToolTip notificationTooltip = new();

        public void AddNotification(BubbleNotification notification)
        {
            pendingNotifications.Add(notification);
            ValidateNotifications();
        }

        public void ValidateNotifications()
        {
            pendingNotifications = pendingNotifications.Where(n => !n.Complete).ToList();

            if (true || pendingNotifications.Count == 0)
            {
                controlBar.ColumnStyles[^1].Width = 0;
            }
            else
            {
                controlBar.ColumnStyles[^1].Width = 64;
                notifications.Text = pendingNotifications.Count.ToString();
                notificationTooltip.SetToolTip(notifications, pendingNotifications.Count.ToString() + " pending notifications");
            }

        }

        private static long ParseVersion(string v)
        {
            var c = v.Split('.');
            return int.Parse(c[0]) * 65536 + int.Parse(c[1]) * 256 + int.Parse(c[2]);
        }

        public Form1()
        {
            var env = Environment.GetEnvironmentVariable("BubbleprintsTheme");
            Dark = env?.Equals("dark") ?? false;
            Dark |= BubblePrints.Settings.DarkMode;

            long version = ParseVersion(Application.ProductVersion);

            if (BubblePrints.Settings.CheckForUpdates)
            {
                Task.Run(async () =>
                {
                    using WebClient client = new();
                    client.Headers.Add("User-Agent", "BubblePrints");
                    var raw = await client.DownloadStringTaskAsync("https://api.github.com/repos/factubsio/BubblePrints/releases/latest");
                    return JsonSerializer.Deserialize<JsonElement>(raw);
                }).ContinueWith(t =>
                {
                    try
                    {
                        var json = t.Result;
                        if (json.TryGetProperty("tag_name", out var tag))
                        {
                            long latest = ParseVersion(tag.GetString()[1..]);
                            if (latest > version)
                            {
                                AddNotification(new()
                                {
                                    Message = "An update is available (" + tag + ")",
                                    Action = () => Process.Start("explorer", json.GetProperty("assets")[0].GetProperty("browser_download_url").GetString()),
                                    ActionText = "Download now",
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }

            InitializeComponent();

            header.Font = new Font(FontFamily.GenericSansSerif, 14);

            Text = "BubblePrints - " + Application.ProductVersion;

            ctrlP = new();
            ctrlP.Daddy = this;
            ctrlP.VisibleChanged += CtrlP_VisibleChanged;

            UpdatePinResults(BubblePrints.Settings.PinSearchResults);


            BubblePrints.OnSettingsChanged += () =>
            {
                if (ctrlP.Pinned != BubblePrints.Settings.PinSearchResults)
                {
                    UpdatePinResults(BubblePrints.Settings.PinSearchResults);
                }
            };

            Load += (sender, e) =>
            {
                var w = kDockManager.ManageWorkspace(blueprintDock);
                kDockManager.ManageFloating(this);
                blueprintDock.WorkspaceCellAdding += (sender, e) =>
                {
                    KryptonWorkspaceCell cell = e.Cell;

                    var closeToRight = new KryptonContextMenuItem("Close All To The Right");
                    closeToRight.Click += (sender, click) =>
                    {
                        var firstToClose = cell.SelectedIndex + 1;
                        int toClose = cell.Pages.Count - firstToClose;
                        for (int i = 0; i < toClose; i++)
                        {
                            cell.Pages.RemoveAt(firstToClose);
                        }
                    };


                    cell.ShowContextMenu += (sender, ctxMenuEvent) =>
                    {
                        ctxMenuEvent.Cancel = false;
                        var menu = ctxMenuEvent.KryptonContextMenu;
                        var itemList = menu.Items[1] as KryptonContextMenuItems;
                        if (itemList.Items.Count == 12)
                        {
                            itemList.Items.Add(closeToRight);
                        }
                        closeToRight.Enabled = cell.SelectedIndex < cell.Pages.Count - 1;

                    };
                    ButtonSpecNavigator bsa = new()
                    {
                        Style = PaletteButtonStyle.Command,
                        Type = PaletteButtonSpecStyle.FormRestore,
                        Tag = cell,
                        UniqueName = cell.UniqueName + "newtab",
                        ExtraText = "New Tab",
                    };
                    bsa.Click += (sender, e) =>
                    {
                        NewBlueprintViewer(cell);
                    };
                    cell.Button.ButtonSpecs.Add(bsa);
                    cell.Button.CloseButtonDisplay = ButtonDisplay.Hide;
                    cell.Button.ButtonDisplayLogic = ButtonDisplayLogic.None;

                    cell.MouseClick += (sender, clickEvent) =>
                    {
                        if (clickEvent.Button == MouseButtons.Middle)
                        {
                            var page = cell.PageFromPoint(clickEvent.Location);
                            if (page != null)
                            {
                                cell.Pages.Remove(page);
                            }
                        }
                    };

                };
                NewBlueprintViewer();

                kGlobalManager.GlobalPaletteMode = Krypton.Toolkit.PaletteModeManager.SparkleOrange;

            };


            this.AddMouseClickRecursively(HandleXbuttons);

            this.AddKeyDownRecursively(HandleGlobalKeys);
            this.AddKeyPressRecursively(HandleGlobalKeyPress);

            header.MouseClick -= HandleXbuttons;

            header.MouseClick += (sender, e) =>
            {
                ShowCtrlP();
            };

            controlBar.ColumnStyles[^1].Width = 0;

            availableVersions.Enabled = false;

            notifications.Click += (sender, evt) =>
            {
                notificationsView.Show(this, pendingNotifications);
                ValidateNotifications();
            };


            settingsButton.Click += (sender, evt) =>
            {
                HideCtrlP();
                new SettingsView().ShowDialog();
            };

            BubblePrints.SetWrathPath();

            if (BubblePrints.TryGetWrathPath(out var wrathPath))
            {
                BubblePrints.Wrath = Assembly.LoadFrom(Path.Combine(wrathPath, "Wrath_Data", "Managed", "Assembly-CSharp.dll"));
            }

            //blueprintViews.DrawMode = TabDrawMode.OwnerDrawFixed;
            //blueprintViews.DrawItem += (sender, e) =>
            //{
            //    var g = e.Graphics;
            //    g.FillRectangle(new SolidBrush(resultsGrid.BackColor), e.Bounds);
            //    var textBounds = e.Bounds;
            //    textBounds.Inflate(-2, -2);
            //    var title = blueprintViews.TabPages[e.Index].Text;
            //    int halfSize = (int)(g.MeasureString(title, Font).Width / 2);
            //    int center = textBounds.Left + textBounds.Width / 2;
            //    textBounds.X = center - halfSize;
            //    g.DrawString(title, Font, new SolidBrush(resultsGrid.ForeColor), textBounds);
            //};


            if (Dark)
            {
                BubbleTheme.DarkenControls(topBarContainer, settingsButton, helpButton, blueprintDockContainer);
                Invalidate();
            }


            if (SeasonalOverlay.InSeason)
            {
                BubbleTheme.SeasonControls(topBarContainer, settingsButton, helpButton);
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
            header.Marquee = true;
            header.Dock = DockStyle.Fill;

            initialize = Task.Run(() => BlueprintDB.Instance.TryConnect(progress));
            initialize.ContinueWith(b =>
            {
                ShowBlueprint(BlueprintDB.Instance.Blueprints.Values.First(), ShowFlags.F_UpdateHistory);

                header.Marquee = false;
                header.Text = "Press @{key.ctrl}-@{key.P} to search (or click here)";

                foreach (var v in BlueprintDB.Instance.Available)
                    availableVersions.Items.Add(v);
                availableVersions.SelectedIndex = availableVersions.Items.Count - 1;
                availableVersions.Enabled = true;

                ShowCtrlP();
            }, TaskScheduler.FromCurrentSynchronizationContext());


            new Thread(() =>
            {
                string plane = $"{loadString}-🛬";
                const int frames = 90;

                while (true)
                {
                    for (int frame = 0; frame < frames; frame++)
                    {
                        if (Good)
                            return;

                        if (!header.IsDisposed && header.Visible)
                        {
                            header.Invoke(new Action(() =>
                            {
                                if (!Good)
                                {
                                    header.Text = plane + $"     {progress.Status}";
                                }
                            }));
                        }
                        Thread.Sleep(33);
                    }
                }
            }).Start();

        }

        private void UpdatePinResults(bool pinned)
        {
            var blueprintPadding = blueprintDockContainer.Margin;
            blueprintPadding.Top = pinned ? 400 : 3;
            blueprintDockContainer.Margin = blueprintPadding;

            ctrlP.Pinned = BubblePrints.Settings.PinSearchResults;
            ctrlP.PinnedHeight = CtrlPPinnedHeight;
        }

        private Size CtrlPSize => new(ClientSize.Width - (ctrlP.Pinned ? 212 : 310), ctrlP.Pinned ? CtrlPPinnedHeight : blueprintDockContainer.Margin.Top);
        private int CtrlPPinnedHeight => blueprintDockContainer.Margin.Top + topBarContainer.Height - 2;
        private Point CtrlPPos => PointToScreen(new(ctrlP.Pinned ? 2 : 100, 2));

        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);

            if (CtrlPVisible)
            {
                ctrlP.Location = CtrlPPos;
            }
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            base.OnResizeEnd(e);

            if (CtrlPVisible)
            {
                ctrlP.Show();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (CtrlPVisible)
            {
                ctrlP.Size = CtrlPSize;
            }
        }

        private bool CtrlPVisible => ctrlP?.Visible == true;

        public void ShowCtrlP()
        {
            if (!Good || CtrlPVisible) return;

            header.OverrideText = "";

            ctrlP.StartPosition = FormStartPosition.Manual;
            ctrlP.Location = CtrlPPos;
            ctrlP.Size = CtrlPSize;
            ctrlP.input.Focus();
            ctrlP.Show(this);

        }

        private void CtrlP_VisibleChanged(object sender, EventArgs e)
        {
            if (ctrlP.Visible) return;

            Activate();


            if (ctrlP.input.Text.Length > 0)
                header.Text2 = "   ---    current: " + ctrlP.input.Text;
            else
                header.Text2 = "";
            header.OverrideText = null;
        }

        private bool WithActiveViewer(Func<BlueprintViewer, bool> predicate, Action<BlueprintViewer> action)
        {
            if (blueprintDock.PageCount > 0 && blueprintDock.ActivePage.Controls[0] is BlueprintViewer viewer && predicate(viewer))
            {
                action(viewer);
                return true;
            }

            return false;
        }

        private BlueprintViewer ActiveViewer => blueprintDock.PageCount > 0 ? blueprintDock.ActivePage.Controls[0] as BlueprintViewer : null;

        private void WithActiveViewer(Action<BlueprintViewer> action)
        {
            if (ActiveViewer != null)
            {
                action(ActiveViewer);
            }

        }

        public void HandleGlobalKeyPress(object sender, KeyPressEventArgs e)
        {
            if (CtrlPVisible) return;
            if (sender is TextBoxBase) return;

            if (ActiveViewer == null) return;


            if (ActiveViewer.Searching)
            {
                if (char.IsLetterOrDigit(e.KeyChar) || char.IsPunctuation(e.KeyChar))
                    ActiveViewer.AppendSearchChar(e.KeyChar);
                if (e.KeyChar == '\b')
                    ActiveViewer.DeleteLastSearchChar();

                return;
            }


            if (e.KeyChar == '!')
            {
                ActiveViewer.filter.Focus();
            }
            else if (e.KeyChar == 'j')
            {
                ActiveViewer.View.SoftRowSelection++;
            }
            else if (e.KeyChar == 'k')
            {
                ActiveViewer.View.SoftRowSelection--;
            }
            else if (e.KeyChar == '/')
            {
                ActiveViewer.BeginSearchForward();
            }
            else if (e.KeyChar == 'n')
            {
                ActiveViewer.View.NextMatch(1);
            }
            else if (e.KeyChar == 'N')
            {
                ActiveViewer.View.NextMatch(-1);
            }
            else if (e.KeyChar == ' ')
            {
                ActiveViewer.View.ToggleAtSoftSelection();
            }
        }

        public void HandleGlobalKeys(object sender, KeyEventArgs e)
        {

            if (e.KeyCode == Keys.P && ModifierKeys.HasFlag(Keys.Control))
            {
                e.Handled = true;
                ShowCtrlP();
                return;
            }
            if (e.KeyCode == Keys.F && ModifierKeys.HasFlag(Keys.Control))
            {
                e.Handled = true;
                ShowCtrlP();
                return;
            }


            if (!CtrlPVisible)
            {
                if (sender is TextBoxBase input)
                {
                    if (e.KeyCode == Keys.Escape)
                    {
                        input.FindForm().ActiveControl = null;
                    }
                    return;
                }

                if (ActiveViewer?.Searching == true)
                {
                    if (e.KeyCode == Keys.Escape)
                        ActiveViewer.StopSearching(false);

                    if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return)
                        ActiveViewer.StopSearching(true);

                    return;
                }

                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return)
                {
                    WithActiveViewer(viewer => viewer.View.FollowLinkAtSoftSelection());
                }
                else if (e.KeyCode == Keys.O && ModifierKeys.HasFlag(Keys.Control))
                {
                    WithActiveViewer(viewer => viewer.Navigate(NavigateTo.RelativeBackOne));
                }
                else if (e.KeyCode == Keys.I && ModifierKeys.HasFlag(Keys.Control))
                {
                    WithActiveViewer(viewer => viewer.Navigate(NavigateTo.RelativeForwardOne));
                }
                else if ((e.KeyCode == Keys.U && ModifierKeys.HasFlag(Keys.Control)) || e.KeyCode == Keys.PageUp)
                {
                    WithActiveViewer(viewer => viewer.View.SoftRowSelection -= viewer.View.VisibleRowCount / 2);
                }
                else if ((e.KeyCode == Keys.D && ModifierKeys.HasFlag(Keys.Control)) || e.KeyCode == Keys.PageDown)
                {
                    WithActiveViewer(viewer => viewer.View.SoftRowSelection += viewer.View.VisibleRowCount / 2);
                }
                else if (e.KeyCode == Keys.NumPad8)
                {
                    WithActiveViewer(v => v.View.SoftRowSelection--);
                }
                else if (e.KeyCode == Keys.NumPad2)
                {
                    WithActiveViewer(v => v.View.SoftRowSelection++);
                }
            }
        }

        public void HideCtrlP()
        {
            if (CtrlPVisible && !ctrlP.Pinned)
            {
                ctrlP.Hide();
            }
        }

        private void HandleXbuttons(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.XButton1)
                (blueprintDock.ActivePage.Controls[0] as BlueprintViewer).Navigate(NavigateTo.RelativeBackOne);
            else if (e.Button == MouseButtons.XButton2)
                (blueprintDock.ActivePage.Controls[0] as BlueprintViewer).Navigate(NavigateTo.RelativeForwardOne);

            HideCtrlP();
        }

        private void ResultsGrid_MouseDown(object sender, MouseEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void BlueprintView_OnLinkClicked(string link)
        {
            throw new NotImplementedException();
        }

        private void ResultsGrid_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.FillRectangle(Brushes.Yellow, 0, 0, 100, 100);
        }


        private void DoOpenInEditor(BlueprintHandle blueprint)
        {
            if (blueprint == null)
                return;
            var userLocalFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BubblePrints", "cache");
            if (!Directory.Exists(userLocalFolder))
                Directory.CreateDirectory(userLocalFolder);

            string fileToOpen = Path.Combine(userLocalFolder, blueprint.Name + "_" + blueprint.GuidText + ".json");

            //if (!File.Exists(fileToOpen))
            {
                using var stream = File.CreateText(fileToOpen);
                TextExporter.Export(stream, blueprint);
            }
            var editor = BubblePrints.Settings.Editor;
            if (editor == null || !File.Exists(editor))
                editor = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "notepad.exe");
            string[] args = BubblePrints.Settings.ExternalEditorTemplate.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "{blueprint}")
                    args[i] = fileToOpen;
            }
            Process.Start(editor, args);
        }

        public void omniSearch_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private DateTime lastChange = DateTime.MinValue;
        private TimeSpan debounceTime = TimeSpan.FromSeconds(1.5);

        int lastFinished = 0;
        private CancellationTokenSource finishingFirst;
        private CancellationTokenSource finishingLast;
        private Task<List<BlueprintHandle>> overlappedSearch;

        private void SetResults(List<BlueprintHandle> results, CancellationTokenSource cancellation, int matchBuffer, ulong sequenceNumber)
        {
            if (cancellation == finishingFirst)
                finishingFirst = null;
            if (cancellation == finishingLast)
                finishingLast = null;

            lastFinished = matchBuffer;
            BlueprintDB.UnlockBuffer(matchBuffer);

            if (sequenceNumber < lastCompleted)
                return;
            lastCompleted = sequenceNumber;

            resultsCache = results;
            ctrlP?.SetResults(results);
            //var oldRowCount = resultsGrid.Rows.Count;
            //var newRowCount = resultsCache.Count;
            //if (newRowCount > oldRowCount)
            //    resultsGrid.Rows.Add(newRowCount - oldRowCount);
            //else
            //{
            //    resultsGrid.Rows.Clear();
            //    if (newRowCount > 0)
            //        resultsGrid.Rows.Add(newRowCount);
            //}
            //resultsGrid.Invalidate();
        }

        public void InvalidateResults(string searchTerm)
        {
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

            ulong sequenceNumber = nextSequence++;


            Task<List<BlueprintHandle>> search;

            if (matchBuffer == 1)
            {
                overlappedSearch = BlueprintDB.Instance.SearchBlueprintsAsync(searchTerm, cancellation.Token, matchBuffer);
                search = overlappedSearch;
            }
            else
            {
                search = BlueprintDB.Instance.SearchBlueprintsAsync(searchTerm, cancellation.Token, matchBuffer);
            }

            search.ContinueWith(task =>
            {
                if (!task.IsCanceled && !cancellation.IsCancellationRequested)
                    this.Invoke((Action<List<BlueprintHandle>, CancellationTokenSource, int, ulong>)SetResults, task.Result, cancellation, matchBuffer, sequenceNumber);
            });

        }

        CtrlP ctrlP;

        public static bool Dark { get => dark; set => dark = value; }


        private static readonly char[] wordSeparators =
        {
            ' ',
            '.',
            //'/',
            ':',
        };
        private static void KillForwardLine(TextBox box)
        {
            var here = box.SelectionStart;
            string Search = box.Text;
            if (box.SelectionLength == 0)
            {
                if (here > 0)
                    box.Text = Search.Substring(0, here);
                else
                    box.Text = "";
                box.Select(Search.Length, 0);

            }

        }

        private static void KillBackLine(TextBox box)
        {
            var here = box.SelectionStart;
            string Search = box.Text;
            if (box.SelectionLength == 0)
            {
                if (here < Search.Length)
                    box.Text = Search[here..];
                else
                    box.Text = "";

            }

        }

        private static void KillBackWord(TextBox box)
        {
            var here = box.SelectionStart;
            string Search = box.Text;
            if (box.SelectionLength == 0)
            {
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

                if (here < Search.Length)
                {
                    newSearch += Search[here..];
                }

                box.Text = newSearch;
                box.SelectionStart = killTo + 1;
            }
        }

        public static void InstallReadline(TextBox box)
        {
            if (!BubblePrints.Settings.UseReadlineShortcuts) return;

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

        //private void omniSearch_KeyDown(object sender, KeyEventArgs e)
        //{
        //    if (e.KeyCode == Keys.Return || e.KeyCode == Keys.Enter)
        //    {
        //        if (resultsCache.Count > 0)
        //        {
        //            ShowSelected();
        //        }
        //    }
        //    else if (e.KeyCode == Keys.Up)
        //    {
        //        if (resultsCache.Count > 1)
        //        {
        //            int row = resultsGrid.SelectedRow() - 1;
        //            if (row >= 0 && row < resultsCache.Count)
        //            {
        //                resultsGrid.Rows[row].Selected = true;
        //                resultsGrid.CurrentCell = resultsGrid[0, row];
        //                resultsGrid.CurrentCell.ToolTipText = "";
        //            }
        //        }
        //        e.Handled = true;
        //        e.SuppressKeyPress = true;
        //    }
        //    else if (e.KeyCode == Keys.Down)
        //    {
        //        if (resultsCache.Count > 1)
        //        {
        //            int row = resultsGrid.SelectedRow() + 1;
        //            if (row < resultsCache.Count)
        //            {
        //                resultsGrid.Rows[row].Selected = true;
        //                resultsGrid.CurrentCell = resultsGrid[0, row];
        //                resultsGrid.CurrentCell.ToolTipText = "";
        //            }
        //        }
        //        e.Handled = true;
        //        e.SuppressKeyPress = true;
        //    }

        //}

        public void ShowBlueprint(int row, bool newTab)
        {
            if (row >= 0 && row < resultsCache.Count)
            {
                if (!newTab)
                {
                    ShowBlueprint(resultsCache[row], ShowFlags.F_ClearHistory | ShowFlags.F_UpdateHistory);
                }
                else
                {
                    var cell = blueprintDock.ActiveCell;
                    var viewer = NewBlueprintViewer(cell);
                    viewer.ShowBlueprint(resultsCache[row], ShowFlags.F_ClearHistory | ShowFlags.F_UpdateHistory);

                    var parent = (viewer.Parent as KryptonPage);

                    cell.SelectedPage = parent;
                }

                Show();
            }
        }


        private void ShowBlueprint(BlueprintHandle bp, ShowFlags flags)
        {
            if (flags.UpdateHistory() && BubblePrints.Settings.AlwaysOpenInEditor)
                DoOpenInEditor(bp);

            if (blueprintDock.PageCount == 0)
            {
                NewBlueprintViewer().ShowBlueprint(bp, flags);
            }
            else if (blueprintDock.ActivePage.Controls[0] is BlueprintViewer bpView)
            {
                bpView.ShowBlueprint(bp, flags);
            }
        }

        private List<BlueprintHandle> resultsCache = new();
        private Task<bool> initialize;

        private void dataGridView1_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            var row = e.RowIndex;
            if ((resultsCache?.Count ?? 0) == 0)
            {
                e.Value = "...";
                return;
            }

            if (row >= resultsCache.Count)
                return;

            e.Value = e.ColumnIndex switch
            {
                0 => resultsCache[row].Name,
                1 => resultsCache[row].TypeName,
                2 => resultsCache[row].Namespace,
                3 => resultsCache[row].Score(lastFinished).ToString(),
                4 => resultsCache[row].GuidText,
                _ => "<error>",
            };
        }


        private void omniSearch_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void resultsGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private HelpView helpView;
        private ulong nextSequence = 1;
        private ulong lastCompleted = 0;

        private void helpButton_Click(object sender, EventArgs e)
        {
            helpView ??= new();
            helpView.Disposed += (sender, e) => helpView = null;
            if (helpView.Visible)
                helpView.BringToFront();
            else
                helpView.Show();
        }
    }
}
