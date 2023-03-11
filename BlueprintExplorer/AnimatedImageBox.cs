using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlueprintExplorer
{
    public class AnimatedImageBox : Control
    {
        private FrameDimension _FrameDim;
        private int _Count = 0;
        private Bitmap _Image;
        private double _Progress = 0;
        private readonly Font _CaptionFont;
        private string _Caption;
        public Bitmap Image
        {
            get => _Image;
            set
            {
                _Image = value;
                if (value == null)
                {
                    _FrameDim = null;
                    _Count = 0;
                }
                else
                {
                    _FrameDim = new(value.FrameDimensionsList[0]);
                    _Count = value.GetFrameCount(_FrameDim);
                    Console.WriteLine(_Count);
                }

                Invalidate();
            }
        }
        private int _Frame = 0;
        public int Frame
        {
            get => _Frame;
            set
            {
                if (_Image == null)
                {
                    return;
                }
                _Frame = value % _Count;
                _Image.SelectActiveFrame(_FrameDim, _Frame);
                Invalidate();
            }

        }

        public int Percent
        {
            set
            {
                _Progress = (value / 100.0);
                Frame++;
            }
        }

        public string Caption
        {
            get => _Caption;
            set
            {
                if (value != _Caption)
                {
                    _Caption = value;
                    Invalidate();
                }

            }
        }
        public bool ShowProgressBar;

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Rectangle rect = new(Point.Empty, ClientSize);
            if (_Image != null)
            {
                g.DrawImage(_Image, rect, new(Point.Empty, new(_Image.Width, _Image.Height)), GraphicsUnit.Pixel);

                const int barThickness = 30;
                const int barMargin = 10;
                if (ShowProgressBar)
                {
                    Rectangle progRect = new(barMargin, rect.Bottom - barMargin - barThickness, rect.Width - barMargin * 2, barThickness);
                    g.FillRectangle(Brushes.BlanchedAlmond, progRect);
                    g.DrawRectangle(new Pen(Brushes.DarkBlue, 2), progRect);
                    progRect.Inflate(-3, -3);
                    progRect.Width = (int)((progRect.Width - 6) * _Progress);
                    g.FillRectangle(Brushes.DarkBlue, progRect);

                }
                if (Caption != null)
                {
                    var height = _CaptionFont.Height;
                    g.DrawString(Caption, _CaptionFont, Brushes.White, barMargin, rect.Bottom - barMargin - barThickness - height - barMargin);
                }
            }
            else
            {
                g.FillRectangle(Brushes.Orange, rect);
            }
        }
        internal void SetPercentSafe(int value)
        {
            Invoke(new Action(() => Percent = value));
        }

        internal void SetSafe(int value)
        {
            Invoke(new Action(() => Frame = value));
        }

        internal void IncrementSafe()
        {
            Invoke(new Action(() => Frame++));
        }

        public AnimatedImageBox()
        {
            DoubleBuffered = true;
            _CaptionFont = new(this.Font.FontFamily, 24);
        }
    }
}
