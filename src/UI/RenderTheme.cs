using System.Drawing;

namespace TitanCalc.UI
{
    public class ThemeColors
    {
        // Window Background & Surfaces
        public Color WindowBackground { get; set; }
        public Color TitleBarBackground { get; set; }
        public Color DisplayBackground { get; set; }
        public Color DisplayBorder { get; set; }
        public Color SurfaceBackground { get; set; }

        // Text & Labels
        public Color TextPrimary { get; set; }
        public Color TextSecondary { get; set; }
        public Color TextMuted { get; set; }
        public Color TextDisplayExpression { get; set; }
        public Color TextDisplayResult { get; set; }

        // Keycaps
        public Color KeyNumberBackground { get; set; }
        public Color KeyNumberHover { get; set; }
        public Color KeyNumberPressed { get; set; }
        public Color KeyNumberBorder { get; set; }
        public Color KeyNumberText { get; set; }

        public Color KeyFunctionBackground { get; set; }
        public Color KeyFunctionHover { get; set; }
        public Color KeyFunctionPressed { get; set; }
        public Color KeyFunctionBorder { get; set; }
        public Color KeyFunctionText { get; set; }

        public Color KeyAccentBackground { get; set; }
        public Color KeyAccentHover { get; set; }
        public Color KeyAccentPressed { get; set; }
        public Color KeyAccentText { get; set; }

        public Color KeyDangerBackground { get; set; }
        public Color KeyDangerHover { get; set; }
        public Color KeyDangerText { get; set; }

        // Casio Secondary Function Legends
        public Color ShiftColor { get; set; }  // Gold/Yellow
        public Color AlphaColor { get; set; }  // Red/Orange

        // Indicators & Badges
        public Color BadgeActive { get; set; }
        public Color BadgeInactive { get; set; }

        public bool IsRetroCasio { get; set; }
    }

    /// <summary>
    /// Color palletes and style metrics for Minimalist Modern vs Maximalist Retro Casio.
    /// </summary>
    public static class RenderTheme
    {
        public static ThemeColors GetModernTheme()
        {
            ThemeColors c = new ThemeColors();
            c.IsRetroCasio = false;

            c.WindowBackground = Color.FromArgb(20, 24, 33);          // Deep Slate
            c.TitleBarBackground = Color.FromArgb(16, 19, 27);
            c.DisplayBackground = Color.FromArgb(27, 33, 46);         // Sleek container
            c.DisplayBorder = Color.FromArgb(43, 52, 72);
            c.SurfaceBackground = Color.FromArgb(24, 29, 41);

            c.TextPrimary = Color.FromArgb(245, 247, 250);
            c.TextSecondary = Color.FromArgb(160, 174, 192);
            c.TextMuted = Color.FromArgb(100, 116, 139);
            c.TextDisplayExpression = Color.FromArgb(148, 163, 184);
            c.TextDisplayResult = Color.FromArgb(255, 255, 255);

            c.KeyNumberBackground = Color.FromArgb(32, 38, 54);
            c.KeyNumberHover = Color.FromArgb(44, 52, 73);
            c.KeyNumberPressed = Color.FromArgb(24, 28, 40);
            c.KeyNumberBorder = Color.FromArgb(46, 55, 78);
            c.KeyNumberText = Color.FromArgb(240, 244, 248);

            c.KeyFunctionBackground = Color.FromArgb(39, 47, 66);
            c.KeyFunctionHover = Color.FromArgb(52, 63, 88);
            c.KeyFunctionPressed = Color.FromArgb(28, 34, 48);
            c.KeyFunctionBorder = Color.FromArgb(55, 66, 92);
            c.KeyFunctionText = Color.FromArgb(186, 215, 233);

            c.KeyAccentBackground = Color.FromArgb(0, 180, 216);      // Vibrant Cyan/Teal
            c.KeyAccentHover = Color.FromArgb(0, 150, 199);
            c.KeyAccentPressed = Color.FromArgb(0, 119, 182);
            c.KeyAccentText = Color.FromArgb(255, 255, 255);

            c.KeyDangerBackground = Color.FromArgb(224, 49, 49);      // Coral Red
            c.KeyDangerHover = Color.FromArgb(201, 42, 42);
            c.KeyDangerText = Color.FromArgb(255, 255, 255);

            c.ShiftColor = Color.FromArgb(245, 166, 35);
            c.AlphaColor = Color.FromArgb(230, 57, 70);

            c.BadgeActive = Color.FromArgb(46, 196, 182);
            c.BadgeInactive = Color.FromArgb(55, 65, 81);

            return c;
        }

        public static ThemeColors GetRetroCasioTheme()
        {
            ThemeColors c = new ThemeColors();
            c.IsRetroCasio = true;

            c.WindowBackground = Color.FromArgb(34, 38, 43);          // Casio fx-991EX textured casing
            c.TitleBarBackground = Color.FromArgb(25, 28, 32);
            c.DisplayBackground = Color.FromArgb(194, 207, 188);      // Authentic greenish-silver LCD
            c.DisplayBorder = Color.FromArgb(40, 45, 50);
            c.SurfaceBackground = Color.FromArgb(30, 33, 38);

            c.TextPrimary = Color.FromArgb(235, 235, 235);
            c.TextSecondary = Color.FromArgb(170, 175, 180);
            c.TextMuted = Color.FromArgb(110, 115, 120);
            c.TextDisplayExpression = Color.FromArgb(35, 45, 33);     // Dark LCD pixel ink
            c.TextDisplayResult = Color.FromArgb(12, 18, 12);

            c.KeyNumberBackground = Color.FromArgb(45, 48, 54);
            c.KeyNumberHover = Color.FromArgb(58, 62, 70);
            c.KeyNumberPressed = Color.FromArgb(30, 32, 36);
            c.KeyNumberBorder = Color.FromArgb(65, 70, 78);
            c.KeyNumberText = Color.FromArgb(250, 250, 250);

            c.KeyFunctionBackground = Color.FromArgb(70, 75, 84);    // Upper scientific sculpted keycaps
            c.KeyFunctionHover = Color.FromArgb(85, 91, 102);
            c.KeyFunctionPressed = Color.FromArgb(50, 54, 60);
            c.KeyFunctionBorder = Color.FromArgb(95, 102, 114);
            c.KeyFunctionText = Color.FromArgb(245, 245, 245);

            c.KeyAccentBackground = Color.FromArgb(40, 110, 180);     // Classic = key
            c.KeyAccentHover = Color.FromArgb(50, 130, 210);
            c.KeyAccentPressed = Color.FromArgb(30, 85, 140);
            c.KeyAccentText = Color.FromArgb(255, 255, 255);

            c.KeyDangerBackground = Color.FromArgb(195, 60, 45);      // DEL / AC keys
            c.KeyDangerHover = Color.FromArgb(220, 70, 55);
            c.KeyDangerText = Color.FromArgb(255, 255, 255);

            c.ShiftColor = Color.FromArgb(245, 180, 0);               // Casio gold Shift
            c.AlphaColor = Color.FromArgb(235, 65, 55);               // Casio red/orange Alpha

            c.BadgeActive = Color.FromArgb(15, 20, 15);
            c.BadgeInactive = Color.FromArgb(170, 180, 165);

            return c;
        }
    }
}
