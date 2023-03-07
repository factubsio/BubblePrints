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
                Frame = (int)((value / 100.0) * _Count);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Rectangle rect = new(Point.Empty, ClientSize);
            if (_Image != null)
            {
                g.DrawImage(_Image, rect, new(Point.Empty, new(_Image.Width, _Image.Height)), GraphicsUnit.Pixel);
            }
            else
            {
                g.FillRectangle(Brushes.Orange, rect);
            }
        }

        internal void IncrementSafe()
        {
            Invoke(new Action(IncrementUnsafe));
        }

        private void IncrementUnsafe()
        {
            Frame++;
        }

        public AnimatedImageBox()
        {
            DoubleBuffered = true;
        }
    }
}
