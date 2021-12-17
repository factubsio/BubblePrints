using BlueprintExplorer.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static BlueprintExplorer.BlueprintHandle;

namespace BlueprintExplorer
{
    public class BlueprintControl : ScrollableControl
    {
        public delegate void LinkClickedDelegate(string link, bool newTab);

        public event LinkClickedDelegate OnLinkClicked;

        private BlueprintHandle blueprint;

        private Timer ToastTimer;
        private Stopwatch Timer = new();
        private long LastTick = 0;

        public BlueprintHandle Blueprint
        {
            get => blueprint;
            set
            {
                if (blueprint == value) return;
                blueprint = value;
                blueprint.EnsureParsed();
                ValidateBlueprint();
            }
        }

        private int wantedHeight = 1;
        private List<RowElement> Elements = new();

        public class RowElement
        {
            public string key, value, link;
            public int level;
            public bool Last;
            public bool Visible = true;
            public RowElement Parent;
            public string String;
            public List<string> Lines;
            public int RowCount;
            public int PrimaryRow;
            internal bool Hover = false;

            internal void AllParents(Action<RowElement> p)
            {
                if (Parent != null)
                {
                    p(Parent);
                    Parent.AllParents(p);
                }
            }
        }

        public int RowHeight { get; set; } = 36;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.Style |= 0x00200000;
                return cp;
            }
        }

        public BlueprintControl()
        {
            DoubleBuffered = true;
            VerticalScroll.SmallChange = RowHeight;
            VerticalScroll.LargeChange = RowHeight;
            UpdateRowHoverColor();
            this.VScroll = true;
            Timer.Start();
            ToastTimer = new();
            ToastTimer.Tick += (sender, e) =>
            {
                bool valid = LastTick > 0;
                long now = Timer.ElapsedMilliseconds;
                long elapsed = now - LastTick;
                LastTick = now;
                if (!valid) return;

                Toasts = Toasts.Where(t => t.Life > 0).ToList();
                foreach (var toast in Toasts)
                    toast.Life -= elapsed;

                InvalidateToasts();
            };
            ToastTimer.Interval = 33;
            ToastTimer.Start();
        }

        protected override void OnBackColorChanged(EventArgs e) => UpdateRowHoverColor();

        private void UpdateRowHoverColor()
        {
            RowHoverColor = ControlPaint.Dark(BackColor, -0.4f);
        }

        private int Count => Remap.Count;

        private void ValidateFilter()
        {
            wantedHeight = Count * RowHeight;
            AutoScrollMinSize = new Size(1, wantedHeight);
            Invalidate();
        }

        private void ValidateBlueprint()
        {
            Elements.Clear();
            var oneRow = Font.Height;
            using var g = CreateGraphics();
            int strWidthAllowed = Width - NameColumnWidth - 32;
            currentHover = -1;
            int totalRows = 0;
            if (blueprint != null)
            {
                int level = 0;
                Stack<RowElement> stack = new();
                stack.Push(null);
                foreach (var e in blueprint.Elements)
                {
                    int currentLevel = level;

                    if (e.levelDelta < 0)
                    {
                        stack.Pop();
                        Elements.Last().Last = true;
                        level--;
                    }
                    else if (e.levelDelta > 0)
                        level++;

                    if (e.levelDelta >= 0)
                    {
                        var row = new RowElement()
                        {
                            key = e.key,
                            value = e.value,
                            level = currentLevel,
                            link = e.link,
                            Parent = stack.Peek(),
                            String = JsonExtensions.ParseAsString(e.Node),
                            RowCount = 1,
                        };

                        if (row.String != null)
                        {
                            List<string> lines = new();

                            string[] inputRows = row.String.Split("\n", StringSplitOptions.RemoveEmptyEntries);
                            StringBuilder l = new();
                            foreach (var r in inputRows)
                            {
                                List<string> words = r.Split(' ', StringSplitOptions.RemoveEmptyEntries).Reverse().ToList();

                                l.Clear();
                                while (words.Count > 0)
                                {
                                    if ((int)(g.MeasureString(l + " " + words.Last(), Font, strWidthAllowed).Height / oneRow) == 1) {
                                        if (l.Length > 0)
                                            l.Append(' ');
                                        l.Append(words.Last());
                                        words.RemoveAt(words.Count - 1);
                                    }
                                    else
                                    {
                                        if (l.Length == 0) {
                                            l.Append(words.Last());
                                            words.RemoveAt(words.Count - 1);
                                        }
                                        lines.Add(l.ToString());
                                        l.Clear();
                                    }
                                }
                                if (l.Length > 0)
                                    lines.Add(l.ToString());
                            }
                            row.Lines = lines;
                            row.RowCount = lines.Count;
                        }
                        row.PrimaryRow = totalRows;
                        Elements.Add(row);
                        totalRows += row.RowCount;

                        if (e.levelDelta > 0)
                        {
                            stack.Push(row);
                        }
                    }
                }
            }
            AutoScroll = true;
            //VerticalScroll.Value = 0;
            Filter = "";
        }

        public override Size GetPreferredSize(Size proposedSize) => new(proposedSize.Width, wantedHeight);

        private List<int> Remap = new();

        private RowElement GetElement(int row)
        {
            row = Remap[row];
            return Elements[row];
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ValidateBlueprint();
        }

        private string _Filter;
        public string Filter
        {
            get => _Filter;
            set
            {
                _Filter = value?.Trim() ?? "";
                Remap.Clear();
                if (_Filter.Length == 0)
                {
                    Console.WriteLine("all");
                    for (int i = 0; i < Elements.Count; i++)
                    {
                        Elements[i].Visible = true;
                        Elements[i].PrimaryRow = Remap.Count;
                        for (int r = 0; r < Elements[i].RowCount; r++)
                            Remap.Add(i);
                    }
                }
                else
                {
                    for (int i = 0; i < Elements.Count; i++)
                    {
                        if (Elements[i].key.Contains(_Filter, StringComparison.OrdinalIgnoreCase) || (Elements[i].value?.Contains(_Filter, StringComparison.OrdinalIgnoreCase) ?? false))
                        {
                            Elements[i].AllParents(p => p.Visible = true);
                            Elements[i].Visible = true;
                        }
                        else
                            Elements[i].Visible = false;
                    }
                    for (int i = 0; i < Elements.Count; i++)
                    {
                        if (Elements[i].Visible)
                        {
                            Elements[i].PrimaryRow = Remap.Count;
                            for (int r = 0; r < Elements[i].RowCount; r++)
                                Remap.Add(i);
                        }
                    }
                }
                ValidateFilter();
            }
        }


        private void DrawElement(int row, DrawParams render)
        {
            var elem = GetElement(row);

            var valueColor = ForeColor;
            var valueFont = render.Regular;

            var extra = "";

            string link = null;

            if (elem.link != null)
            {
                if (BackColor.GetBrightness() < 0.5f)
                    valueColor = Color.LightGreen;
                else
                    valueColor = Color.DarkGreen;

                if (BlueprintDB.Instance.Blueprints.TryGetValue(Guid.Parse(elem.link), out var target))
                {
                    extra = "  -> " + target.Name + " :" + target.TypeName;
                    link = elem.link;
                }
                else
                    extra = "  -> STALE";
                valueFont = LinkFont;
            }
            else if (elem.value is "null" or "NULL")
                valueColor = Color.Gray;


            if (elem.Hover)
                render.Graphics.FillRectangle(new SolidBrush(RowHoverColor), 0, 0, Width, RowHeight);
            if (elem.level> 0)
            {
                var lineColor = Color.Gray;
                var outerColor = ControlPaint.Dark(lineColor, 0.2f);

                var lineBrush = new SolidBrush(lineColor);
                //var outerBrush = new SolidBrush(outerColor);
                //for (int i = 0; i < elem.levelDelta; i++)
                //{
                //    float mx = i * LevelIndent - LevelIndent * 0.5f;
                //    render.Graphics.FillRectangle(outerBrush, mx, 0, 3f, RowHeight);
                //}
                float x = elem.level * LevelIndent - LevelIndent * 0.5f;
                float h = RowHeight;
                if (elem.Last)
                    h = RowHeight / 2;
                render.Graphics.FillRectangle(lineBrush, x, 0, 3f, h);
                render.Graphics.FillRectangle(lineBrush, x, RowHeight/2f, 8f, 2f);
            }
            if (elem.PrimaryRow == row)
            {
                render.Graphics.DrawString(elem.key, render.Bold, new SolidBrush(ForeColor), new PointF(elem.level * LevelIndent, 0));
                if (elem.String == null)
                    render.Graphics.DrawString(elem.value + extra, valueFont, new SolidBrush(valueColor), new PointF(NameColumnWidth, 0));
            }

            if (elem.String != null)
            {
                string line = elem.Lines[row - elem.PrimaryRow];
                render.Graphics.DrawString(line, valueFont, new SolidBrush(valueColor), new Rectangle(NameColumnWidth, 0, Width - NameColumnWidth - 32, 500));
            }

        }


        private Rectangle CopyIconRect => new(NameColumnWidth - 42, 2, RowHeight - 4, RowHeight - 4);


        public int LevelIndent { get; set; } = 20;
        public int NameColumnWidth { get; set; } = 600;

        int currentHover = -1;
        private Font linkFont;

        private bool GetCurrent(out RowElement current)
        {
            if (currentHover >= 0 && currentHover < Count)
            {
                current = GetElement(currentHover);
                return true;
            }
            current = null;
            return false;
        }

        public bool IsOverCopy(int x) => x >= CopyIconRect.Left && x <= (CopyIconRect.Right + 8);

        protected override void OnMouseClick(MouseEventArgs e)
        {
            bool valid = GetCurrent(out var elem);
            if (e.Button == MouseButtons.Left && valid)
            {
                if (elem.link != null)
                {
                    OnLinkClicked?.Invoke(elem.link, ModifierKeys.HasFlag(Keys.Control));
                }
            }
            else if (e.Button == MouseButtons.Right && valid)
            {
                string value = elem.value;
                if (elem.link != null)
                    value = elem.link;
                if (value == null)
                    return;
                Clipboard.SetText(value);
                var toast = new Toast
                {
                    Text = "Value Copied",
                    Lifetime = 900,
                    Bounds = new(e.X - 80, (elem.PrimaryRow - 1) * RowHeight, 160, 40)
                };
                Toasts.Add(toast);
                InvalidateToasts();
            }
        }

        private void InvalidateToasts()
        {
            foreach (var toast in Toasts)
                Invalidate(toast.OnScreen(VerticalScroll.Value));
        }

        public class Toast
        {
            public string Text;
            public long Life;
            private long _lifetime;
            public long Lifetime
            {
                set { Life = _lifetime = value; }
            }
            public Rectangle Bounds;
            public float Age => Life / (float)_lifetime;
            public Rectangle OnScreen(int vscroll)
            {
                var rect = Bounds;
                rect.Offset(0, -vscroll);
                return rect;
            }
        }
        private List<Toast> Toasts = new();

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int y = (e.Y + VerticalScroll.Value) / RowHeight;
            RowElement elem = null;
            if (y >= 0 && y < Count)
            {
                elem = GetElement(y);
                if (elem.link != null)
                {
                    if (IsOverCopy(e.X))
                    {
                        Cursor = Cursors.Help;
                    }
                    else
                        Cursor = Cursors.Hand;
                }
                else
                    Cursor = Cursors.Default;
            }

            if (y != currentHover)
            {
                if (GetCurrent(out var oldCurrent))
                {
                    oldCurrent.Hover = false;
                    Invalidate(new Rectangle(0, oldCurrent.PrimaryRow * RowHeight - VerticalScroll.Value, Width, oldCurrent.RowCount * RowHeight));
                }

                currentHover = y;

                if (GetCurrent(out var newCurrent))
                {
                    newCurrent.Hover = true;
                    Invalidate(new Rectangle(0, newCurrent.PrimaryRow * RowHeight - VerticalScroll.Value, Width, newCurrent.RowCount * RowHeight));
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;

            DrawParams drawing = new()
            {
                Bold = new Font(Font, FontStyle.Bold),
                Regular = Font,
                Graphics = g,
            };


            int current = ((VerticalScroll.Value + e.ClipRectangle.Top) / RowHeight);
            if (current < 0)
                current = 0;
            int last = (VerticalScroll.Value + e.ClipRectangle.Bottom + RowHeight - 1) / RowHeight;
            if (last > Count)
                last = Count;

            for (int i = current; i < last; i++)
            {
                g.ResetTransform();
                g.TranslateTransform(0, (i * RowHeight) - VerticalScroll.Value);
                DrawElement(i, drawing);
            }

            g.ResetTransform();

            foreach (var toast in Toasts.Where(t => t.Life > 0))
            {
                var rect = toast.OnScreen(VerticalScroll.Value);
                int alpha = (int)(255 * toast.Age);
                var bg = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0));
                var fg = new SolidBrush(Color.FromArgb(alpha, 255, 255, 255));
                if (rect.IntersectsWith(e.ClipRectangle)) {
                    g.FillRectangle(bg, rect);
                    g.DrawString(toast.Text, Font, fg, rect);
                }
            }

        }

        public Font LinkFont { get => linkFont ?? Font; set => linkFont = value; }
        private Color RowHoverColor;

        public class DrawParams
        {
            public Font Bold;
            public Font Regular;
            public Graphics Graphics;
        }
    }
}
