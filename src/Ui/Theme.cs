using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Vexor.Ui
{
    public static class Theme
    {
        public static float Scale = 1f;

        public static readonly Color Bg = Hex("#0b0e14");
        public static readonly Color BgDeep = Hex("#080b11");
        public static readonly Color SidebarTop = Hex("#0e131e");
        public static readonly Color SidebarBottom = Hex("#090c12");
        public static readonly Color Surface = Hex("#111623");
        public static readonly Color SurfaceDeep = Hex("#0e1320");
        public static readonly Color SurfaceAlt = Hex("#151b2a");
        public static readonly Color SurfaceHover = Hex("#1b2334");
        public static readonly Color Border = Hex("#222b3f");
        public static readonly Color BorderSoft = Hex("#1a2130");
        public static readonly Color Text = Hex("#eef1f8");
        public static readonly Color Muted = Hex("#8f98ad");
        public static readonly Color Muted2 = Hex("#667089");
        public static readonly Color Accent = Hex("#7c5cff");
        public static readonly Color AccentLight = Hex("#a68bff");
        public static readonly Color AccentDark = Hex("#5a3fe0");
        public static readonly Color Green = Hex("#34d399");
        public static readonly Color Red = Hex("#f87171");
        public static readonly Color Yellow = Hex("#fbbf24");

        public static readonly string IconFamily = FindIconFamily();

        private static readonly Dictionary<string, Font> Fonts = new Dictionary<string, Font>();

        public static void Initialize()
        {
            float dpi;
            try
            {
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) dpi = g.DpiX;
            }
            catch
            {
                dpi = 96f;
            }

            float scale = dpi / 96f;
            try
            {
                Rectangle area = Screen.PrimaryScreen.WorkingArea;
                float fitW = (area.Width - 24f) / 1120f;
                float fitH = (area.Height - 24f) / 720f;
                if (fitW < scale) scale = fitW;
                if (fitH < scale) scale = fitH;
            }
            catch
            {
            }
            if (scale < 0.75f) scale = 0.75f;
            Scale = scale;
        }

        public static int S(float value)
        {
            return (int)Math.Round(value * Scale);
        }

        public static float Sf(float value)
        {
            return value * Scale;
        }

        public static Color Hex(string hex)
        {
            try
            {
                return ColorTranslator.FromHtml(hex);
            }
            catch
            {
                return Color.Magenta;
            }
        }

        public static Font F(float pixels, FontStyle style)
        {
            string key = "ui|" + pixels + "|" + (int)style;
            Font font;
            if (Fonts.TryGetValue(key, out font)) return font;
            font = new Font("Segoe UI", pixels * Scale, style, GraphicsUnit.Pixel);
            Fonts[key] = font;
            return font;
        }

        public static Font F(float pixels)
        {
            return F(pixels, FontStyle.Regular);
        }

        public static Font Display(float pixels, FontStyle style)
        {
            string key = "display|" + pixels + "|" + (int)style;
            Font font;
            if (Fonts.TryGetValue(key, out font)) return font;
            font = new Font("Bahnschrift", pixels * Scale, style, GraphicsUnit.Pixel);
            Fonts[key] = font;
            return font;
        }

        public static Font Display(float pixels)
        {
            return Display(pixels, FontStyle.Regular);
        }

        public static Font Mono(float pixels)
        {
            string key = "mono|" + pixels;
            Font font;
            if (Fonts.TryGetValue(key, out font)) return font;
            font = new Font("Consolas", pixels * Scale, FontStyle.Regular, GraphicsUnit.Pixel);
            Fonts[key] = font;
            return font;
        }

        public static Font Icon(float pixels)
        {
            string key = "icon|" + pixels;
            Font font;
            if (Fonts.TryGetValue(key, out font)) return font;
            font = new Font(IconFamily, pixels * Scale, FontStyle.Regular, GraphicsUnit.Pixel);
            Fonts[key] = font;
            return font;
        }

        public static void Quality(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        }

        public static GraphicsPath RoundRect(RectangleF rect, float radius)
        {
            var path = new GraphicsPath();
            float diameter = radius * 2f;
            if (diameter <= 0.5f)
            {
                path.AddRectangle(rect);
                return path;
            }
            if (diameter > rect.Width) diameter = rect.Width;
            if (diameter > rect.Height) diameter = rect.Height;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180f, 90f);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270f, 90f);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0f, 90f);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90f, 90f);
            path.CloseFigure();
            return path;
        }

        public static void FillRound(Graphics g, RectangleF rect, float radius, Color color)
        {
            using (GraphicsPath path = RoundRect(rect, radius))
            using (var brush = new SolidBrush(color))
                g.FillPath(brush, path);
        }

        public static void DrawRound(Graphics g, RectangleF rect, float radius, Color color, float width)
        {
            using (GraphicsPath path = RoundRect(rect, radius))
            using (var pen = new Pen(color, width))
                g.DrawPath(pen, path);
        }

        public static Color Blend(Color a, Color b, float t)
        {
            if (t < 0) t = 0;
            if (t > 1) t = 1;
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t),
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        public static Color Alpha(Color color, int alpha)
        {
            if (alpha < 0) alpha = 0;
            if (alpha > 255) alpha = 255;
            return Color.FromArgb(alpha, color.R, color.G, color.B);
        }

        public static string FormatRam(int mb)
        {
            double gb = mb / 1024.0;
            return gb.ToString(gb >= 10 ? "0" : "0.#") + " ГБ";
        }

        private static string FindIconFamily()
        {
            try
            {
                using (var collection = new InstalledFontCollection())
                {
                    foreach (FontFamily family in collection.Families)
                    {
                        if (string.Equals(family.Name, "Segoe Fluent Icons", StringComparison.OrdinalIgnoreCase))
                            return family.Name;
                    }
                }
            }
            catch
            {
            }
            return "Segoe MDL2 Assets";
        }
    }
}
