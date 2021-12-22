using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlueprintExplorer
{
    public class SeasonalOverlay
    {

        private List<SnowFlake> Flakes = new();

        private string flake = "*";
        private readonly Control control;

        public SeasonalOverlay(Control control)
        {
            var timer = new Timer();
            this.control = control;
            timer.Tick += OnTimerTick;
            timer.Interval = 33;
            timer.Start();

            var f = control.GetType().GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance);
            f.SetValue(control, true);

            control.Paint += Control_Paint;
        }

        private void Control_Paint(object sender, PaintEventArgs e)
        {
            foreach (var snowflake in Flakes)
                e.Graphics.DrawString(flake, control.Font, Brushes.White, snowflake.Position);
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            if (this.control == null)
                return;
            int toSpawn = Math.Min(1, 150 - Flakes.Count);
            for (int i =0; i < toSpawn; i++)
            {
                Flakes.Add(new SnowFlake
                {
                    Position = new PointF(SnowFlake.rng.Next(0, control.Width), 0),
                    Drift = 0,
                    Rate = 2 - (SnowFlake.rng.NextDouble() * 0.2),
                });
            }

            List<SnowFlake> nextGen = new();
            foreach (var snowflake in Flakes)
            {
                snowflake.Update();
                if (snowflake.Position.Y < (control.Height - 0.5))
                {
                    nextGen.Add(snowflake);
                }
            }

            Flakes = nextGen;
            control.Invalidate();
        }

        public static bool NearChristmas
        {
            get
            {
                DateTime now = DateTime.Now;
                return ((now.Month == 1 && now.Day < 3) || (now.Month == 12 && now.Day > 10));
            }

        }

        public static bool InSeason => !BubblePrints.Settings.NoSeasonalTheme && (NearChristmas);

        internal static void Install(Control target)
        {
            if (NearChristmas)
                _ = new SeasonalOverlay(target);
        }
    }

    public class SnowFlake
    {
        public static Random rng = new();

        public PointF Position;
        public double Drift;
        public double Rate;

        public void Update()
        {
            Drift += rng.NextDouble() - 0.5;
            Position = new PointF(Position.X + (float)Drift, Position.Y + (float)Rate);
        }
    }

}
