using BlueprintExplorer.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BlueprintExplorer
{
    public class BlueprintControl : ScrollableControl
    {
        public delegate void LinkClickedDelegate(string link, bool newTab);
        public delegate void PathDelegate(string path);
        public delegate void FilterChangedDelegate(string filter);
        public delegate void NavigateDelegate(NavigateTo to);

        public event NavigateDelegate OnNavigate;
        public event LinkClickedDelegate OnLinkClicked;
        public event PathDelegate OnPathHovered;
        public event FilterChangedDelegate OnFilterChanged;

        private Dictionary<string, (int position, string filter)> HistoryCache = new();

        private IDisplayableElementCollection DisplayedObject;

        private Timer ToastTimer;
        private Stopwatch Timer = new();
        private long LastTick = 0;

        public IDisplayableElementCollection Blueprint
        {
            get => DisplayedObject;
            set
            {
                if (DisplayedObject == value) return;
                if (DisplayedObject != null)
                {
                    HistoryCache[DisplayedObject.GuidText] = (VerticalScroll.Value, _Filter);
                }
                DisplayedObject = value;
                DisplayedObject.EnsureParsed();
                ValidateBlueprint(true);
            }
        }

        private int wantedHeight = 1;
        private List<RowElement> Elements = new();

        [Flags]
        public enum StyleFlags
        {
            Bold = 1,
            Italic = 2,
        }

        public class StyledString
        {

            public StyledString(IEnumerable<StyleSpan> spans)
            {
                Spans = spans.ToArray();
            }

            public struct StyleSpan
            {
                public string Value;
                public StyleFlags Flags;

                public StyleSpan(string value, StyleFlags flags = 0) {
                    Value = value;
                    Flags = flags;
                }

                public bool Bold => (Flags & StyleFlags.Bold) != 0;
                public bool Italic => (Flags & StyleFlags.Italic) != 0;
            }

            public StyleSpan[] Spans;

            public String Raw => string.Concat(Spans.Select(s => s.Value));
        }

        public class RowElement
        {
            public bool IsObj;
            public string key, value, link;
            public StyledString ValueStyled;
            public int level;
            public bool Last;
            public bool Visible = true;
            public bool Collapsed = false;
            public RowElement Parent;
            public string TypeFull;
            public string String;
            public List<string> Lines;
            public List<RowElement> Children = new();
            public int RowCount;
            public int PrimaryRow;
            internal bool Hover = false;
            internal bool HoverButton;
            internal bool PreviewHover;
            private string _Path;
            public string Type;
            internal string Default;

            internal void AllChildren(Action<RowElement> p)
            {
                foreach (var child  in Children)
                {
                    p(child);
                    child.AllChildren(p);
                }

            }

            internal bool AnyParent(Predicate<RowElement> p, bool self = false)
            {
                if (self && p(this)) return true;

                if (Parent != null)
                {
                    return p(Parent) || Parent.AnyParent(p);
                }
                return false;
            }

            internal void AllParents(Action<RowElement> p)
            {
                if (Parent != null)
                {
                    p(Parent);
                    Parent.AllParents(p);
                }
            }

            public string Path => _Path ??= CalculatePath();

            private string PathKey
            {
                get
                {
                    var ret = key;
                    if (Type != null)
                    {
                        if (IsObj)
                            ret += "{" + Type + "}";
                        else
                            ret += "[" + Type + "]";
                    }
                    return ret;

                }
            }

            public bool HasChildren => Children.Count > 0;
            public bool HasLink => link != null;

            public string SearchableValue => value ?? ValueStyled?.Raw ?? "";

            private string CalculatePath()
            {
                List<string> components = new();
                components.Add(PathKey);
                AllParents(p => components.Add(p.PathKey));
                return string.Join("/", Enumerable.Reverse(components));
            }
        }

        public int RowHeight
        {
            get { return (Regular?.Height ?? 32) + 4; }
            set { }
        }

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

            Regular = new Font(Font.FontFamily, (float)BubblePrints.Settings.BlueprintFontSize, FontStyle.Regular);
            Bold = new Font(Font.FontFamily, (float)BubblePrints.Settings.BlueprintFontSize, FontStyle.Bold);

            BubblePrints.OnSettingsChanged += BubblePrints_OnSettingsChanged;
        }

        private void BubblePrints_OnSettingsChanged()
        {
            Regular = new Font(Font.FontFamily, (float)BubblePrints.Settings.BlueprintFontSize, FontStyle.Regular);
            Bold = new Font(Font.FontFamily, (float)BubblePrints.Settings.BlueprintFontSize, FontStyle.Bold);
            ValidateBlueprint(false);
        }

        protected override void OnBackColorChanged(EventArgs e) => UpdateRowHoverColor();

        private void UpdateRowHoverColor()
        {
            RowHoverColor = new(ControlPaint.Dark(BackColor, -0.4f));
            RowLineGuide = new(ControlPaint.Light(BackColor, 0.1f), 2);
            RowPreviewHoverColor = new(ControlPaint.Dark(BackColor, -0.41f));
        }

        private int Count => Remap.Count;

        private void ValidateFilter(int? scrollTo)
        {
            Remap.Clear();
            if (_Filter.Length == 0)
            {
                for (int i = 0; i < Elements.Count; i++)
                    Elements[i].Visible = true;
            }
            else
            {
                var filter = _Filter;
                bool inParents = false;
                if (filter[^1] == '/')
                {
                    filter = filter[..^1];
                    inParents = true;
                }

                bool matches(RowElement r) => r.key.ContainsIgnoreCase(filter) || r.SearchableValue.ContainsIgnoreCase(filter);
                for (int i = 0; i < Elements.Count; i++)
                {
                    bool good = false;
                    if (inParents)
                        good = Elements[i].AnyParent(matches, true);
                    else
                        good = matches(Elements[i]);
                    if (good)
                    {
                        Elements[i].AllParents(p => p.Visible = true);
                        Elements[i].Visible = true;
                    }
                    else
                        Elements[i].Visible = false;
                }
            }

            for (int i = 0; i < Elements.Count; i++)
            {
                if (Elements[i].Collapsed)
                    Elements[i].AllChildren(ch => ch.Visible = false);

                if (Elements[i].Visible)
                {
                    Elements[i].PrimaryRow = Remap.Count;
                    Remap.Add(i);
                    if (!Elements[i].Collapsed)
                        for (int r = 1; r < Elements[i].RowCount; r++)
                            Remap.Add(i);
                }
            }
            wantedHeight = Count * RowHeight;
            AutoScrollMinSize = new Size(1, wantedHeight);
            Invalidate();
            if (scrollTo != null)
            {
                AutoScrollPosition = new Point(0, scrollTo.Value);
            }
        }

        int StringWidthAllowed => Width - NameColumnWidth - 32;

        private void ValidateBlueprint(bool scroll)
        {
            Elements.Clear();
            var oneRow = Font.Height;
            using var g = CreateGraphics();
            int strWidthAllowed = StringWidthAllowed;
            currentHover = -1;
            int totalRows = 0;
            if (DisplayedObject == null) return;

            if (DisplayedObject != null)
            {
                Elements.Add(new ()
                {
                    key = "Blueprint ID",
                    value = DisplayedObject.GuidText,
                    level = 0,
                    link = null,
                    Parent = null,
                    String = null,
                    RowCount = 1,
                    Collapsed = true,
                });
                Elements.Add(new ()
                {
                    key = "Blueprint Name",
                    value = DisplayedObject.Name,
                    level = 1,
                    link = null,
                    Parent = Elements[0],
                    String = null,
                    RowCount = 1,
                    Collapsed = false,
                });
                Elements.Add(new ()
                {
                    key = "Blueprint Type",
                    value = DisplayedObject.TypeName,
                    level = 1,
                    link = null,
                    Parent = Elements[0],
                    String = null,
                    RowCount = 1,
                    Collapsed = false,
                });
                Elements[0].Children.Add(Elements[1]);
                Elements[0].Children.Add(Elements[2]);
                totalRows = 3;

                int level = 0;
                Stack<RowElement> stack = new();
                stack.Push(null);
                foreach (var e in DisplayedObject.DisplayableElements)
                {
                    int currentLevel = level;

                    if (e.levelDelta < 0)
                    {
                        var prev = stack.Pop();
                        if (!prev.IsObj)
                            prev.value += "[" + prev.Children.Count + "]";

                        Elements.Last().Last = true;
                        level--;
                    }
                    else if (e.levelDelta > 0)
                    {
                        level++;
                    }

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
                            IsObj = e.isObj,
                            Collapsed = totalRows != 0 && !BubblePrints.Settings.EagerExpand && currentLevel > 0,
                        };

                        if (e.isObj && e.HasType)
                        {
                            List<StyledString.StyleSpan> spans = new();
                            spans.Add(new(e.MaybeType.Name + "  ", StyleFlags.Bold));
                            spans.Add(new("typeId: " + e.MaybeType.Guid));
                            row.ValueStyled = new(spans);
                            row.Type = e.MaybeType.Name;
                            row.TypeFull = e.MaybeType.FullName;
                        }

                        if (row.key == "$type" && row.Parent != null)
                            continue;

                        if (e.levelDelta == 0 && row.Parent != null)
                        {
                            row.Default = BlueprintDB.Instance.DefaultForField(row.Parent?.TypeFull, e.key);
                        }

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

                            if (lines.Count == 0)
                            {
                                lines.Add("");
                            }
                            row.Lines = lines;
                            row.RowCount = lines.Count;
                        }

                        stack.Peek()?.Children.Add(row);
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
            if (scroll)
            {
                if (HistoryCache.TryGetValue(DisplayedObject.GuidText, out var history))
                {
                    _Filter = history.filter;
                    ValidateFilter(history.position);
                    OnFilterChanged?.Invoke(_Filter);
                }
                else
                {
                    VerticalScroll.Value = 0;
                    _Filter = "";
                    ValidateFilter(null);
                    OnFilterChanged?.Invoke(_Filter);
                }
            }
            else
            {
                ValidateFilter(null);
            }
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
            ValidateBlueprint(false);
        }

        private string _Filter;
        public string Filter
        {
            get => _Filter;
            set
            {
                var newFilter = value?.Trim() ?? "";
                if (_Filter != newFilter)
                {
                    _Filter = newFilter;
                    ValidateFilter(null);
                }
            }
        }

        private ImageAttributes HoverAttribs;
        private ImageAttributes ExpandAttribs;

        private void DrawElement(int row, DrawParams render)
        {
            if (HoverAttribs == null)
            {
                HoverAttribs = new();
                {
                    float[][] colorMatrixElements = {
                       new float[] {1,  0,  0,  0, 0},        // red scaling factor
                       new float[] {0,  1,  0,  0, 0},        // green scaling factor
                       new float[] {0,  0,  1,  0, 0},        // blue scaling factor
                       new float[] {0,  0,  0,  1, 0},        // alpha scaling factor
                       new float[] {0.1f, 0.4f, 0.1f, 0, 1} // translations
                    };
                    HoverAttribs.SetColorMatrix(new(colorMatrixElements));
                }

                ExpandAttribs = new();
                {
                    float[][] colorMatrixElements = {
                       new float[] {1,  0,  0,  0, 0},        // red scaling factor
                       new float[] {0,  1,  0,  0, 0},        // green scaling factor
                       new float[] {0,  0,  1,  0, 0},        // blue scaling factor
                       new float[] {0,  0,  0,  1, 0},        // alpha scaling factor
                       new float[] {0.25f, 0.0f, 0.0f, 0, 1} // translations
                    };
                    ExpandAttribs.SetColorMatrix(new(colorMatrixElements));
                }
            }

            var elem = GetElement(row);

            var valueColor = ForeColor;
            var valueFont = render.Regular;

            float xOffset = 48 + elem.level * LevelIndent;

            var extra = "";
            if (elem.HasLink) 
            {
                if (BackColor.GetBrightness() < 0.5f)
                    valueColor = Color.LightGreen;
                else
                    valueColor = Color.DarkGreen;

                if (BlueprintDB.Instance.Blueprints.TryGetValue(Guid.Parse(elem.link), out var target))
                {
                    extra = "  -> " + target.Name + " :" + target.TypeName;
                }
                else
                {
                    valueColor = Color.Gray;
                    extra = "  -> STALE";
                }
                valueFont = LinkFont;
            }
            else if (elem.value is "null" or "NULL" or "[0]")
                valueColor = Color.Gray;


            if (elem.Hover)
                render.Graphics.FillRectangle(RowHoverColor, 0, 0, Width, RowHeight);
            if (elem.PreviewHover)
                render.Graphics.FillRectangle(RowPreviewHoverColor, 0, 0, Width, RowHeight);
            if (elem.level> 0)
            {
                var lineColor = Color.Gray;

                var lineBrush = new SolidBrush(lineColor);
                float x = xOffset - LevelIndent * 0.5f;
                float h = RowHeight;
                if (elem.Last)
                    h = RowHeight / 2;
                render.Graphics.FillRectangle(lineBrush, x, 0, 3f, h);
                render.Graphics.FillRectangle(lineBrush, x, RowHeight/2f, 8f, 2f);
            }
            if (elem.PrimaryRow == row)
            {
                float keyWidth = render.Graphics.MeasureString(elem.key, render.Bold).Width;
                render.Graphics.DrawString(elem.key, render.Bold, new SolidBrush(ForeColor), new PointF(xOffset, 0));
                float lineY = RowHeight / 2.0f;
                render.Graphics.DrawLine(RowLineGuide, xOffset + keyWidth + 3, lineY, NameColumnWidth - 3, lineY);
                if (elem.String == null)
                {
                    bool empty = false;
                    float right = NameColumnWidth;
                    var brush = new SolidBrush(valueColor);
                    if (elem.ValueStyled == null)
                    {
                        var str = elem.value + extra;
                        if (str.Length > 0)
                        {
                            right += render.Graphics.MeasureString(str, valueFont).Width;
                            render.Graphics.DrawString(str, valueFont, brush, new PointF(NameColumnWidth, 0));
                        }
                        else
                        {
                            empty = true;
                        }
                    }
                    else
                    {
                        PointF p = new(NameColumnWidth, 0);
                        foreach (var span in elem.ValueStyled.Spans)
                        {
                            var font = render.Regular;
                            if (span.Bold)
                                font = render.Bold;
                            var width = render.Graphics.MeasureString(span.Value, font).Width;
                            render.Graphics.DrawString(span.Value, font, brush, p);
                            p.X += width;
                        }

                        right = p.X;
                    }


                    if (elem.Default != null)
                    {
                        if (!empty)
                        {
                            right += 64;
                            if (right < NameColumnWidth + 400)
                                right = NameColumnWidth + 400;
                        }

                        render.Graphics.DrawString("[default: " + elem.Default + "]", Font, Brushes.Gray, new PointF(right, 0));
                    }
                }
            }

            if (elem.String != null)
            {
                int stringIndex = row - elem.PrimaryRow;
                string line = elem.Lines[stringIndex];
                render.Graphics.DrawString(line, valueFont, new SolidBrush(valueColor), new Rectangle(NameColumnWidth, 0, StringWidthAllowed, 500));
            }

            if (elem.HasChildren && row == elem.PrimaryRow)
            {
                var img = elem.Collapsed ? Resources.expand : Resources.collapse;
                int topPad = (RowHeight - 32) / 2;
                var imgRect = new Rectangle((int)xOffset - 34 - LevelIndent / 2, topPad, 32, 32);
                ImageAttributes attribs = null;
                if (elem.Hover && elem.HoverButton)
                    attribs = HoverAttribs;
                else if (elem.Collapsed)
                    attribs = ExpandAttribs;
                render.Graphics.DrawImage(img, imgRect, 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, attribs);

            }

        }

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

        private void Toggle(RowElement elem)
        {
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                bool forceOff = elem.Children.Any(ch => ch.HasChildren && !ch.Collapsed);
                foreach (var child in elem.Children.Where(ch => ch.HasChildren))
                    child.Collapsed = forceOff || !child.Collapsed;
            }
            else
                elem.Collapsed = !elem.Collapsed;

            ValidateFilter(null);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            bool valid = GetCurrent(out var elem);
            if (valid && e.Clicks == 2 && elem.HasChildren && !elem.HasLink && !elem.HoverButton)
            {
                Toggle(elem);
            }
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.XButton1)
            {
                OnNavigate?.Invoke(NavigateTo.RelativeBackOne);
            }
            else if (e.Button == MouseButtons.XButton2)
            {
                OnNavigate?.Invoke(NavigateTo.RelativeForwardOne);
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            bool valid = GetCurrent(out var elem);
            if (e.Button == MouseButtons.Left && valid)
            {
                if (elem.HoverButton)
                {
                    Toggle(elem);
                }
                else if (elem.HasLink)
                {
                    OnLinkClicked?.Invoke(elem.link, ModifierKeys.HasFlag(Keys.Control));
                }
            }
            else if (e.Button == MouseButtons.Middle && valid && elem.HasLink)
            {
                OnLinkClicked?.Invoke(elem.link, true);
            }
            else if (e.Button == MouseButtons.Right && valid)
            {
                string value = elem.value;
                value ??= elem.ValueStyled?.Raw;
                bool jbpCompatible = ModifierKeys.HasFlag(Keys.Shift);


                if (elem.HasLink)
                {
                    value = elem.link;
                    if (jbpCompatible)
                        value = "!bp_" + value;
                }

                if (jbpCompatible && elem.key == "Blueprint ID") {
                    value = "!bp_" + value;
                }
                if (string.IsNullOrWhiteSpace(value))
                    return;
                Clipboard.SetText(value);
                int displayAt = elem.PrimaryRow - 1;
                if (elem.PrimaryRow < 3)
                    displayAt = elem.PrimaryRow + 1;
                var toast = new Toast
                {
                    Text = "Value Copied",
                    Lifetime = 600,
                    Bounds = new(e.X - 80, displayAt * RowHeight, 160, 40)
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

        private void Invalidate(RowElement elem)
        {
            Invalidate(new Rectangle(0, elem.PrimaryRow * RowHeight - VerticalScroll.Value, Width, elem.RowCount * RowHeight));
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int y = (e.Y + VerticalScroll.Value) / RowHeight;
            bool invalidateChildren = false;
            RowElement elem = null;
            if (y >= 0 && y < Count)
            {
                elem  = GetElement(y);
                bool onButton = elem.HasChildren && e.X < (LevelIndent * elem.level + 48 - LevelIndent / 2);
                invalidateChildren = onButton != elem.HoverButton;
                elem.HoverButton = onButton;
                if (!elem.HoverButton && elem.HasLink)
                    Cursor = Cursors.Hand;
                else
                    Cursor = Cursors.Default;
            }

            if (y != currentHover)
            {
                if (GetCurrent(out var oldCurrent))
                {
                    oldCurrent.Hover = false;
                    Invalidate(oldCurrent);
                    if (oldCurrent.HoverButton)
                        oldCurrent.AllChildren(ch =>
                        {
                            ch.PreviewHover = false;
                            Invalidate(ch);
                        });
                    oldCurrent.HoverButton = false;
                }

                currentHover = y;

                OnPathHovered?.Invoke(elem?.Path);

            }
            if (elem != null)
            {
                elem.Hover = true;
                Invalidate(elem);
                if (invalidateChildren)
                    elem.AllChildren(ch =>
                    {
                        ch.PreviewHover = elem.HoverButton;
                        Invalidate(ch);
                    });
                    
            }
        }
        private Font Regular; 
        private Font Bold; 

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;


            DrawParams drawing = new()
            {
                Regular = Regular,
                Bold = Bold,
                Graphics = g,
            };

            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

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

        public Font LinkFont { get => Regular; set => linkFont = value; }
        private SolidBrush RowHoverColor;
        private Pen RowLineGuide;
        private SolidBrush RowPreviewHoverColor;

        public class DrawParams
        {
            public Font Bold;
            public Font Regular;
            public Graphics Graphics;
        }
    }
    public enum NavigateTo
    {
        RelativeBackOne,
        RelativeForwardOne,
        AbsoluteFirst,
        AbsoluteLast,
    }
}
