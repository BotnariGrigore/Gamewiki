using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GameWikiApp.Helpers
{
    public static class ThemeHelper
    {
        public const int SpacingSmall = 12;
        public const int SpacingMedium = 16;
        public const int BorderRadiusLarge = 16;

        public enum ThemeMode { Dark, Light }
        public static ThemeMode CurrentTheme { get; set; } = ThemeMode.Dark;

        // These are kept for auth forms only
        public static Color Dark_BgPrimary => Color.FromArgb(24, 24, 24);
        public static Color Dark_BgSecondary => Color.FromArgb(30, 30, 30);

        // Minimal colors kept for auth forms
        public static Color BgPrimary => SystemColors.Control;
        public static Color BgSecondary => SystemColors.ControlLight;
        public static Color BgTertiary => SystemColors.ControlLightLight;
        public static Color BgCard => SystemColors.Control;
        public static Color BgInput => SystemColors.Window;
        public static Color BgHover => SystemColors.ControlLight;
        public static Color Accent => SystemColors.Highlight;
        public static Color AccentHover => SystemColors.Highlight;
        public static Color AccentDim => SystemColors.GrayText;
        public static Color TextPrimary => SystemColors.ControlText;
        public static Color TextSecondary => SystemColors.ControlText;
        public static Color TextMuted => SystemColors.GrayText;
        public static Color Border => SystemColors.ActiveBorder;
        public static Color BorderLight => SystemColors.InactiveBorder;
        public static Color Sidebar => SystemColors.Control;
        public static Color Header => SystemColors.Control;
        public static Color Surface => SystemColors.Control;
        public static Color Error => Color.Red;
        public static Color Success => Color.Green;
        public static Color Warning => Color.Orange;

        public static bool SimplifiedUI { get; set; } = true;

        public static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            path.AddRectangle(rect);
            return path;
        }

        public static void ApplyTheme(Form form)
        {
        }

        public static void ApplyThemeToControl(Control control)
        {
        }

        public static Button CreateThemedButton(string text, int x, int y, int width = 120, int height = 36)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                UseVisualStyleBackColor = true
            };
        }

        public static Panel CreateCardPanel(int width, int height)
        {
            return new Panel
            {
                Size = new Size(width, height),
                Margin = new Padding(8),
                Padding = new Padding(12)
            };
        }

        public static Panel WrapInput(TextBoxBase tb, int width = 350, int height = 42)
        {
            tb.Size = new Size(width - 24, height - 16);
            tb.Location = new Point(12, 10);

            var wrapper = new Panel
            {
                Location = tb.Location,
                Size = new Size(width, height)
            };

            wrapper.Controls.Add(tb);
            return wrapper;
        }

        public static Label CreateLabel(string text, float fontSize, FontStyle style, Color? color = null, int x = 0, int y = 0)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Location = new Point(x, y)
            };
        }

        public static Panel CreateSeparator(int width, int x = 0, int y = 0)
        {
            var sep = new Panel
            {
                Size = new Size(width, 1),
                Location = new Point(x, y)
            };
            return sep;
        }
    }
}
