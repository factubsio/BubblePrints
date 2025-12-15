using BlueprintExplorer.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BlueprintExplorer;

public static class SeasonalOverlay
{
    public static bool NearChristmas
    {
        get
        {
            DateTime now = DateTime.Now;
            return ((now.Month == 1 && now.Day < 3) || (now.Month == 12 && now.Day > 10));
        }
    }

    public static bool InSeason => !BubblePrints.Settings.NoSeasonalTheme && (NearChristmas);

    private static readonly HebrewCalendar hebrewCal = new();
    public static int ChannukahDay
    {
        get
        {
            DateTime now = DateTime.Now;

            int hy = hebrewCal.GetYear(now);

            // Chanukah starts 25 Kislev, (the night before)
            DateTime start = hebrewCal.ToDateTime(hy, 3, 25, 0, 0, 0, 0).AddDays(-1);
            DateTime end = start.AddDays(7);

            if (now >= start && now <= end)
            {
                return (now - start).Days + 1;
                
            }

            return -1;
        }
    }

    public static bool IsChannukahToday => ChannukahDay != -1;

    internal static void Activate()
    {
        activeOverlay.ShowNoActivate();
    }

    private static SeasonalForm activeOverlay;

    internal static void Install(Form1 target)
    {
        if (BubblePrints.Settings.NoSeasonalTheme) return;
        if (activeOverlay != null) return;

        if (NearChristmas)
        {
            var overlay = new SeasonalForm();
            activeOverlay = overlay;

            overlay.Daddy = target;

            target.Resize += (_, _) => overlay.UpdatePosAndSize();
            target.ResizeEnd += (_, _) => overlay.UpdatePosAndSize();
            target.Move += (_, _) => overlay.UpdatePosAndSize();

            overlay.ShowNoActivate();
        }

        // I hate every fibre of my being
        System.Threading.Tasks.Task.Delay(20).ContinueWith(_ => activeOverlay.BeginInvoke(() => target.Activate()));
    }

    internal static void Disable() => activeOverlay?.Close();
}

public static class Noise1D
{
    public static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);

    public static float Lerp(float a, float b, float t) => a + t * (b - a);

    public static float Get(float x, int seed)
    {
        int xi = (int)MathF.Floor(x);
        float xf = x - xi;

        var rng = new Random(xi ^ seed);
        float g0 = (float)(rng.NextDouble() * 2 - 1); // gradient at xi
        rng = new Random((xi + 1) ^ seed);
        float g1 = (float)(rng.NextDouble() * 2 - 1); // gradient at xi+1

        float d0 = g0 * xf;
        float d1 = g1 * (xf - 1);

        float u = Fade(xf);
        return Lerp(d0, d1, u);
    }

    public static Octave[] MakeOctaves(int count, float decay)
    {
        return [.. Enumerable.Range(0, count).Select(n => new Octave(1.0f / MathF.Pow(2, n), MathF.Pow(decay, n)))];
    }


}

public class Candle
{
    public float Length;
    public bool IsLit;
    public float X;
    public float Y;
    public float Width;
}
public class Channukiah
{
    public readonly Bitmap Sprite = Resources.channukiah;
    public static readonly Point Shamesh = new(31, 14);
    public static readonly int CandleY = 21;
    public static readonly int[] CandleX = [
        10,
        15,
        20,
        25,

        38,
        43,
        48,
        53,
    ];

    public Candle[] Candles = [.. Enumerable.Repeat(new Candle(), 9)];

    public Channukiah(int channukahDay)
    {
        Candles[0].Width = 3;
        Candles[0].X = Shamesh.X;
        Candles[0].Y = Shamesh.Y;
        Candles[0].Length = 16;
        Candles[0].IsLit = true;

        for (int i = 1; i <= channukahDay; i++)
        {
            Candles[i] = new()
            {
                Width = 2,
                X = CandleX[i - 1],
                Y = CandleY,
                Length = 16,
                IsLit = true,
            };
        }

    }

    internal void Update(Random rng, float dt)
    {
        const float meltSpeed = 0.1f;

        foreach (var candle in Candles)
        {
            if (candle.Length > 2)
            {
                candle.Length -= rng.NextSingle() * meltSpeed * dt;
            }
        }
    }

    internal static Point GetPoint(int width, int height) => new(width - 100, height - 120);
}

public class SnowFlake
{
    private static readonly Random rng = new();

    private static readonly int seed = rng.Next();

    public static float Wind = 0;
    private static readonly Octave[] windOctaves = Noise1D.MakeOctaves(4, 0.5f);
    private static long lastTime = 0;

    public PointF Position;
    public double Drift;
    public double Rate;
    internal bool Dead;
    public float Size;

    public static void UpdateShared(long t)
    {
        long dt = t - lastTime;
        lastTime = t;
        Wind = windOctaves.Sum(x => Noise1D.Get((t * 0.00004f) / x.Period, seed) * x.Amplitude) * 32.0f;
    }

    public void Update()
    {
        Drift += rng.NextDouble() - 0.5;
        Position = new PointF(Position.X + Wind + (float)Drift, Position.Y + (float)Rate);
    }
}

public record struct Octave(float Period, float Amplitude);



public class SeasonalForm : Form
{
    public SeasonalForm()
    {
        this.ShowInTaskbar = false;
        DoubleBuffered = true;
        Enabled = false;
        AllowTransparency = true;


        FormBorderStyle = FormBorderStyle.None;
        BackColor = Color.Lime;
        TransparencyKey = Color.Lime;

        if (SeasonalOverlay.NearChristmas)
        {
            flakes = [];
            groundLevel = [.. Enumerable.Range(0, 4096).Select(n => (1 + Noise1D.Get(n * 0.01f, 1)) * 6.0f)];
            meltSpeed = [.. Enumerable.Repeat(0, 4096)];
        }

        int channukahDay = SeasonalOverlay.ChannukahDay;
        if (channukahDay != -1)
        {
            channukiah = new(channukahDay);

        }

        UpdateTimer = new()
        {
            Interval = 33
        };
        UpdateTimer.Start();
        UpdateTimer.Tick += DoUpdate;
    }

    private static Timer UpdateTimer = null;
    private static readonly Stopwatch GlobalTime = Stopwatch.StartNew();

    private Channukiah channukiah;
    private Pen heapPen = null;
    private static readonly Random rng = new();
    private readonly List<SnowFlake> flakes = null;
    private readonly List<float> groundLevel = null;
    private readonly List<float> meltSpeed = null;
    private BearState bearState = BearState.Hiding;
    private float bearStateFinished = 0f;
    private int bearX = 0;
    private float bearY = 0;
    private float bearAlpha = 0;

    private float lastT = 0;

    private void DoUpdate(object sender, EventArgs e)
    {

        lastT = GlobalTime.ElapsedMilliseconds / 1e3f;
        float dt = UpdateTimer.Interval / 1e3f;

        if (flakes != null)
        {
            const int spawnTries = 12;
            float spawnProb = 0.7f * BubblePrints.Settings.SeasonalDensity / 10.0f;

            for (int spawnAttempt = 0; spawnAttempt < spawnTries; spawnAttempt++)
            {
                if (rng.NextSingle() < spawnProb)
                {
                    float x = rng.Next(-200, Width + 200);
                    flakes.Add(new()
                    {
                        Position = new(x, 0),
                        Rate = rng.Next(20, 40) / 2.0f,
                        Drift = (rng.NextSingle() - 0.5f) * 10,
                        Size = 1 + rng.NextSingle() * 3,
                    });
                }
            }

            const float depositMass = 6.0f;

            SnowFlake.UpdateShared(GlobalTime.ElapsedMilliseconds);

            List<CandleWrapper> flamePoints = [];

            if (channukiah != null)
            {
                var pos = Channukiah.GetPoint(Width, Height);

                foreach (var candle in channukiah.Candles)
                {
                    if (candle.IsLit)
                    {
                        flamePoints.Add(new(candle, new(pos.X + candle.X, pos.Y + candle.Y - candle.Length)));
                    }
                }
            }

            foreach (var flake in flakes)
            {
                flake.Update();

                int x = (int)MathF.Round(flake.Position.X);

                float ground = Height;
                if (x >= 0 && x < groundLevel.Count)
                {
                    ground -= groundLevel[x];
                }

                if (flake.Position.Y >= ground)
                {
                    flake.Dead = true;
                    foreach (var (dx, weight) in DistributeHeap)
                    {
                        int gx = x + dx;
                        if (gx >= 0 && gx < groundLevel.Count)
                        {
                            groundLevel[gx] += weight * depositMass;
                        }
                    }
                }

                if (channukiah != null && !flake.Dead)
                {
                    var vec2 = flake.Position.ToVector2();
                    const float meltDistance = 16 * 16;
                    CandleWrapper candle = flamePoints.FirstOrDefault(x => (x.Flame - vec2).LengthSquared() < meltDistance);
                    if (candle != null)
                    {
                        if (rng.NextSingle() > 0.9999f)
                            candle.Candle.IsLit = false;
                        flake.Dead = true;
                    }

                }

            }

            flakes.RemoveAll(x => x.Dead);
            var truck = SnowWorld.Truck;

            if (truck.Parked)
            {
                truck.Parked = false;
                truck.Self.Local.Reset();
                truck.Self.Local.Translate(-100, 0);
                truck.Acc = 100.0f;
            }


            if (!truck.Parked)
            {
                truck.TopSpeed = Math.Max(Width * 0.025f, 50.0f);
                truck.Update(dt);
                float backWheelX = truck.BackWheel.World.OffsetX;

                if (backWheelX > Width + 80)
                {
                    truck.Parked = true;
                }
            }

            if (!truck.Parked)
            {
                // Step 2: get wheel positions
                float frontWheelX = truck.FrontWheel.World.OffsetX;
                float backWheelX = truck.BackWheel.World.OffsetX;

                // Y comes from terrain sampling
                float frontWheelY = FindWheelY(frontWheelX);
                float backWheelY = FindWheelY(backWheelX);

                //// Step 3: compute heading from back→front vector
                float dx = frontWheelX - backWheelX;
                float dy = frontWheelY - backWheelY;
                float angle = -(float)Math.Atan2(dy, dx); // radians

                // Step 4: rebuild local transform
                truck.Self.Local.Reset();

                // rotate to heading
                truck.Self.Local.Rotate(angle * 180f / MathF.PI);
                truck.Self.Local.Translate(backWheelX, backWheelY, MatrixOrder.Append);

                for (int sprayX = (int)backWheelX - 40; sprayX < backWheelX - 30; sprayX++)
                {
                    if (sprayX >= 0 && sprayX < meltSpeed.Count)
                    {
                        meltSpeed[sprayX] += 0.01f;
                    }
                }
            }

            for (int x = 0; x < meltSpeed.Count; x++)
            {
                groundLevel[x] -= (meltSpeed[x] * groundLevel[x]);
                if (groundLevel[x] < 0) groundLevel[x] = 0;
                meltSpeed[x] *= 0.95f;
            }

            if (bearState == BearState.Hiding && rng.NextSingle() < 0.05f)
            {
                int x = rng.Next(0, Width - 1);

                bool safeToEmerge = truck.Parked || Math.Abs(truck.BackWheel.World.OffsetX - x) > 400;

                if (safeToEmerge)
                {
                    bearState = BearState.Emerging;
                    bearAlpha = 0;
                    bearX = x;
                    bearY = MathF.Ceiling(groundLevel[x]) + 20;
                }
            }

            switch (bearState)
            {
                case BearState.Emerging:
                    bearAlpha += 5 * dt;
                    if (bearAlpha >= 1)
                    {
                        bearAlpha = 1;
                        bearStateFinished = lastT + 0.3f;
                        bearState = BearState.Looking;
                    }
                    break;
                case BearState.Looking:
                    if (lastT > bearStateFinished)
                    {
                        bearState = BearState.LookingLeft;
                        bearStateFinished = lastT + 0.3f;
                    }
                    break;
                case BearState.LookingLeft:
                    if (lastT > bearStateFinished)
                    {
                        bearState = BearState.Looking2;
                        bearStateFinished = lastT + 0.3f;
                    }
                    break;
                case BearState.Looking2:
                    if (lastT > bearStateFinished)
                    {
                        bearState = BearState.LookingRight;
                        bearStateFinished = lastT + 0.3f;
                    }
                    break;
                case BearState.LookingRight:
                    if (lastT > bearStateFinished)
                    {
                        bearState = BearState.Looking3;
                        bearStateFinished = lastT + 0.3f;
                    }
                    break;
                case BearState.Looking3:
                    if (lastT > bearStateFinished)
                    {
                        bearState = BearState.Submerging;
                    }
                    break;
                case BearState.Submerging:
                    bearAlpha -= 5 * dt;
                    if (bearAlpha <= 0)
                    {
                        bearState = BearState.Hiding;
                    }
                    break;
                default:
                    break;
            }

            this.Invalidate();

            float FindWheelY(float wheel)
            {
                float wheelHeight = 0;
                for (int wx = (int)Math.Floor(wheel - 1); wx <= (int)Math.Ceiling(wheel + 1); wx++)
                {
                    if (wx >= 0 && wx < groundLevel.Count)
                    {
                        if (groundLevel[wx] > wheelHeight)
                            wheelHeight = groundLevel[wx];
                    }
                }
                return wheelHeight;
            }
        }

        channukiah?.Update(rng, dt);
        
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;

        if (flakes != null)
        {

            heapPen ??= new Pen(Brushes.White, 4);
            foreach (var flake in flakes)
            {
                g.FillRectangle(Brushes.White, flake.Position.X, flake.Position.Y, flake.Size, flake.Size);
            }

            for (int x = 0; x < Width && x < groundLevel.Count; x++)
            {
                if (groundLevel[x] > 0)
                {
                    g.DrawLine(heapPen, x, Height, x, Height - (groundLevel[x]));
                }
            }

            {
                var truck = SnowWorld.Truck;
                var m = truck.Self.World;

                float worldX = m.OffsetX;
                float worldY = m.OffsetY;

                float angle = (float)Math.Atan2(m.Elements[1], m.Elements[0]);


                // convert to screen coordinates if y-up
                float screenX = worldX;
                float screenY = Height - worldY;

                g.TranslateTransform(screenX, screenY);
                g.RotateTransform(angle * 180f / (float)Math.PI);

                Rectangle spriteBounds = new(-Truck.Sprite.Width / 2 + 10, -Truck.Sprite.Height, Truck.Sprite.Width, Truck.Sprite.Height);

                // render
                g.DrawImage(Truck.Sprite, spriteBounds);

                for (int sprayX = -30; sprayX < -20; sprayX++)
                {
                    float y = -30 + rng.NextSingle() * 40;
                    g.FillRectangle(Brushes.LightYellow, sprayX, y, 2, 2);
                }

                g.ResetTransform();
            }

            if (bearState != BearState.Hiding)
            {

                ImageAttributes imageAttributes = new();
                imageAttributes.SetColorMatrix(new()
                {
                    Matrix33 = bearAlpha,
                }, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                var sprite = bearState switch
                {
                    BearState.LookingRight or BearState.LookingLeft => Resources.pbear_left,
                    _ => Resources.pbear_front,
                };

                bool flip = bearState == BearState.LookingRight;

                g.ResetTransform();
                g.TranslateTransform(bearX - 16, Height - bearY);

                if (flip)
                {
                    g.ScaleTransform(-1, 1);
                    g.TranslateTransform(-32, 0);
                }

                g.DrawImage(sprite,
                    new Rectangle(0, 0, 32, 32),
                    0, 0, sprite.Width, sprite.Height,
                    GraphicsUnit.Pixel, imageAttributes);
            }
        }

        if (channukiah != null)
        {
            g.ResetTransform();

            if (Width >= 300 && Height >= 300)
            {
                Point basePoint = Channukiah.GetPoint(Width, Height);

                g.DrawImage(channukiah.Sprite, basePoint.X, basePoint.Y, 64, 64);

                foreach (var candle in channukiah.Candles)
                {
                    g.FillRectangle(Brushes.WhiteSmoke, basePoint.X + candle.X, basePoint.Y + candle.Y - candle.Length, candle.Width, candle.Length);
                    if (candle.IsLit)
                    {
                        FlameRenderer.DrawTinyFlame(g, basePoint.X + candle.X + 1, basePoint.Y + candle.Y - candle.Length - 2, lastT + candle.X);
                    }
                }
            }


        }
    }

    private Pen flagPolePen = new(Brushes.Black, 3);

    protected override CreateParams CreateParams
    {
        get
        {
            var defaultParams = base.CreateParams;
            defaultParams.ExStyle |= 0x20;   // WS_EX_TRANSPARENT: clicks pass through
            defaultParams.ExStyle |= 0x8000000; // WS_EX_NOACTIVATE: form never takes focus
            defaultParams.ExStyle |= 0x80;
            return defaultParams;
        }
    }
    private static (int Dx, float Weight)[] MakeKernel(int radius, float sigma)
    {
        var raw = Enumerable.Range(-radius, 2 * radius + 1)
            .Select(dx => (Dx: dx, Weight: (float)Math.Exp(-(dx * dx) / (2 * sigma * sigma))))
            .ToArray();

        float sum = raw.Sum(x => x.Weight);

        return [.. raw.Select(x => (x.Dx, x.Weight / sum))];
    }

    private static readonly (int Dx, float Weight)[] DistributeHeap = MakeKernel(8, 6.0f);

    public Form1 Daddy;

    protected override void WndProc(ref Message m)
    {
        const int WM_MOUSEACTIVATE = 0x0021;
        const int MA_NOACTIVATE = 3;
        if (m.Msg == WM_MOUSEACTIVATE) { m.Result = new IntPtr(MA_NOACTIVATE); return; }
        base.WndProc(ref m);
    }

    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
    int X, int Y, int cx, int cy, uint uFlags);

    const int SW_SHOWNOACTIVATE = 4;
    const uint SWP_NOACTIVATE = 0x0010;
    const uint SWP_NOMOVE = 0x0002;
    const uint SWP_NOSIZE = 0x0001;
    const uint SWP_SHOWWINDOW = 0x0040;


    public void ShowNoActivate()
    {
        if (this.Handle == 0) CreateHandle();
        UpdatePosAndSize();
        ShowWindow(this.Handle, SW_SHOWNOACTIVATE); // visible, no activation
        SetWindowPos(this.Handle, Daddy.Handle, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE);
    }


    protected override bool ShowWithoutActivation => true;

    public void UpdatePosAndSize()
    {
        Point clientTopLeft = Daddy.PointToScreen(Point.Empty);

        Location = clientTopLeft;
        Size = Daddy.ClientSize;
    }

    protected override void OnActivated(EventArgs e)
    {
        Owner = Daddy;
        UpdatePosAndSize();
    }
}

public static class FlameRenderer
{
    public static void DrawTinyFlame(Graphics g, float x, float y, float t)
    {
        // Flicker intensity
        float n = Noise1D.Get(t * 3.0f, (int)x);

        // Surround glow (outer pixels)
        var glow = Brushes.DarkGoldenrod;
        g.FillRectangle(glow, x - 2 - n, y - 6, 4, 4);     // top

        // Flame core (bright center pixel)
        var core = Brushes.Yellow;
        g.FillRectangle(core, x - 1 - n, y - 4, 2, 3);
    }

}

record class CandleWrapper(Candle Candle, Vector2 Flame);
