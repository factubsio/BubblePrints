using BlueprintExplorer.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlueprintExplorer
{
    internal class BubbleLabel : Control
    {

        private SolidBrush _ForeBrush;

        private bool _Marquee;

        private float _MarqueePos;

        private static Timer _MarqueeTimer;

        public string OverrideText
        {
            get => _OverrideText;
            set
            {
                _OverrideText = value;
                Invalidate();
            }

        }
        public string Text2
        {
            get => _Text2;
            set
            {
                _Text2 = value;
                InvalidateFragments(value, secondaryFragments);
                Invalidate();
            }

        }

        public BubbleLabel()
        {
            if (_MarqueeTimer == null)
            {
                _MarqueeTimer = new();
                _MarqueeTimer.Interval = 16;
                _MarqueeTimer.Start();
            }

            _MarqueeTimer.Tick += OnMarqueeTick;

            DoubleBuffered = true;
        }

        int _MarqueeSpeed = 4;
        int _LastRenderedWidth = 0;

        private void OnMarqueeTick(object sender, EventArgs e)
        {
            if (!_Marquee) return;

            _MarqueePos += _MarqueeSpeed;

            if (_MarqueeSpeed > 0 && (_MarqueePos + _LastRenderedWidth) >= Width)
                _MarqueeSpeed = -_MarqueeSpeed;
            else if (_MarqueeSpeed < 0 && _MarqueePos <= 0)
                _MarqueeSpeed = -_MarqueeSpeed;

            Invalidate();
        }

        public bool Marquee
        {
            get { return _Marquee; }
            set
            {
                _Marquee = value;
                _MarqueePos = 18;
                Invalidate();
            }
        }

        interface IFragment
        {
            SizeF Render(Graphics g, BubbleLabel label);
        }

        class TextFragment : IFragment
        {
            public string Text = "";

            public SizeF Render(Graphics g, BubbleLabel label)
            {
                if (Text?.Length == 0) return SizeF.Empty;

                TextRenderer.DrawText(g, Text, label.Font, new Point((int)g.Transform.OffsetX, (int)g.Transform.OffsetY), label.ForeColor);
                return TextRenderer.MeasureText(Text, label.Font);
            }
        }

        class KeyFragment : IFragment
        {
            public string Key = "";

            private bool Meta => Key is "ctrl" or "shift" or "return" or "alt" or "enter";

            public SizeF Render(Graphics g, BubbleLabel label)
            {
                double ratio = Meta ? 2 : 1;
                if (Key == "esc") ratio = 1.5;
                int height = Math.Min(label.Height - 2, label.Font.Height - 2);
                int width = (int)(ratio * height);

                var pen = new Pen(label._ForeBrush, 3);

                float stringWidth = g.MeasureString(Key, label.Font).Width;

                float c = width / 2 - stringWidth / 2;

                g.DrawRectangle(pen, 0, 1, width, height);
                g.DrawString(Key, label.Font, label._ForeBrush, new PointF(c, 1));

                return new SizeF(width, height);

            }
        }

        class ImageFragment : IFragment
        {
            public Image img;

            public SizeF Render(Graphics g, BubbleLabel label)
            {
                if (img == null) return SizeF.Empty;

                float ratio = img.Width / (float)img.Height;
                int height = label.Height - 2;
                int width = (int)(ratio * height);


                g.DrawImage(img, new Rectangle(0, 0, width, height));
                return new SizeF(width, height);
            }
        }

        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e);
            _ForeBrush = new(ForeColor);
        }

        private List<IFragment> primaryFragments = new();
        private List<IFragment> secondaryFragments = new();
        private string _OverrideText;
        private string _Text2;

        private void InvalidateFragments(string template, List<IFragment> fragments)
        {
            fragments.Clear();
            if (template != null)
            {
                var rawFragments = TemplateRunner.Iterate(template);

                foreach (var raw in rawFragments)
                {
                    if (raw.IsError)
                    {
                        fragments.Add(new TextFragment() { Text = "ERROR:<" + raw.Raw + ">" });
                        continue;
                    }

                    if (!raw.IsVariable)
                    {
                        fragments.Add(new TextFragment() { Text = raw.Raw });
                    }
                    else
                    {
                        if (raw.Object == "key")
                        {
                            fragments.Add(new KeyFragment() { Key = raw.Property });
                            continue;
                        }

                        //object obj = null;
                        //if (obj is Bitmap img)
                        //{
                        //    fragments.Add(new ImageFragment() { img = img });

                        //}
                    }
                }
            }

            Invalidate();

        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            InvalidateFragments(Text, primaryFragments);
        }

        private StringFormat _StringFormat;

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            pevent.Graphics.Clear(BackColor);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            _ForeBrush ??= new(ForeColor);
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            //_StringFormat = new StringFormat
            //{
            //    FormatFlags = StringFormatFlags.NoWrap
            //};

            if (_Marquee)
            {
                e.Graphics.TranslateTransform(_MarqueePos, 0);
            }

            var needed = Font.Height;
            var have = Height;
            if (have > needed) {
                int padding = (have - needed) / 2;
                e.Graphics.TranslateTransform(0, padding);
            }


            if (_OverrideText != null)
            {
                e.Graphics.DrawString(_OverrideText, Font, _ForeBrush, PointF.Empty);
                return;
            }


            foreach (var frag in primaryFragments)
            {
                var advance = frag.Render(e.Graphics, this);
                e.Graphics.TranslateTransform(advance.Width, 0);
            }
            foreach (var frag in secondaryFragments)
            {
                var advance = frag.Render(e.Graphics, this);
                e.Graphics.TranslateTransform(advance.Width, 0);
            }

            _LastRenderedWidth = (int)e.Graphics.Transform.OffsetX;
        }
    }
}
