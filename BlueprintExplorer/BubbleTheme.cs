using System.Drawing;
using System.Windows.Forms;

namespace BlueprintExplorer
{
    public static class BubbleTheme
    {
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
            grid.ViewBackColor = RegularDarkColor;
        }

    }
}
