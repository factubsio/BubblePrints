namespace FontTested
{
    public partial class Form1 : Form
    {
        Color bgcol = Color.FromArgb(30, 30, 30);
        private SaberRenderer saber;
        private Bitmap buffer;

        public Form1()
        {
            saber = new((Bitmap)Bitmap.FromFile(@"D:\font_atlas.png"), File.ReadAllLines(@"D:\font_atlas_.txt"));
            InitializeComponent();
            buffer = new(Size.Width, Size.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(buffer);
            g.FillRectangle(new SolidBrush(bgcol), new(Point.Empty, buffer.Size));

            DoubleBuffered = true;
        }

        protected override void OnResize(EventArgs e)
        {
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            {
                if (buffer.Size != Size)
                {
                    buffer = new(Size.Width, Size.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                }
            }

            var g = e.Graphics;
            g.FillRectangle(new SolidBrush(Color.LightYellow), new(Point.Empty, Size));

            saber.Render("PR", buffer, new()
            {
                Thickness = this.Thickness,
                Softness = this.Softness,
                OutlineThickness = this.OutlineThickness,
                OutlineSoftness = this.OutlineSoftness,
                TextScale = this.TextScale,

                Color = new(font_r, font_g, font_b),
                Border = 0.02f,

                LetterSpacing = -9,
            });

            g.DrawImage(buffer, Point.Empty);
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            Thickness = trackBar1.Value / 1000f;
            thicknessLabel.Text = Thickness.ToString();
            Invalidate();

        }

        public float Thickness = 0.5f;
        public float Softness = 0.0f;
        public float OutlineThickness = 0.5f;
        public float OutlineSoftness = 0.1f;
        public float TextScale = 1.0f;

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            TextScale = trackBar3.Value / 100.0f;
            scaleLabel.Text = TextScale.ToString();
            Invalidate();
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            Softness = trackBar2.Value / 100.0f;
            featherLabel.Text = Softness.ToString();
            Invalidate();
        }

        private void outlineThicknessBar_Scroll(object sender, EventArgs e)
        {
            OutlineThickness = outlineThicknessBar.Value / 1000f;
            outlineThicknessLabel.Text = OutlineThickness.ToString();
            Invalidate();

        }

        private void outlineFeatherBar_Scroll(object sender, EventArgs e)
        {
            OutlineSoftness = outlineFeatherBar.Value / 100.0f;
            outlineFeatherLabel.Text = OutlineSoftness.ToString();
            Invalidate();
        }

        private void kryptonColorButton1_SelectedColorChanged(object sender, Krypton.Toolkit.ColorEventArgs e)
        {
            font_r = e.Color.R / 255.0f;
            font_g = e.Color.G / 255.0f;
            font_b = e.Color.B / 255.0f;
            Invalidate();
        }

        public float font_r = 1;
        public float font_g = 1;
        public float font_b = 1;
    }
}