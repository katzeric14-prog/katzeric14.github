using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Vexor.Ui
{
    public class SidebarPanel : Panel
    {
        public string AccountName = "Player";
        public string AccountSubtitle = "Офлайн-аккаунт";
        public bool AccountOnline;
        public string SystemText = "";

        public SidebarPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.SidebarTop;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Quality(g);

            using (var brush = new LinearGradientBrush(new Rectangle(0, 0, Width, Height),
                       Theme.SidebarTop, Theme.SidebarBottom, 90f))
            {
                g.FillRectangle(brush, ClientRectangle);
            }

            using (var path = new GraphicsPath())
            {
                var glow = new Rectangle(-Theme.S(90), -Theme.S(90), Theme.S(300), Theme.S(300));
                path.AddEllipse(glow);
                using (var brush = new PathGradientBrush(path))
                {
                    brush.CenterColor = Color.FromArgb(58, Theme.Accent);
                    brush.SurroundColors = new[] { Color.FromArgb(0, Theme.Accent) };
                    g.FillPath(brush, path);
                }
            }

            var tile = new RectangleF(Theme.Sf(20f), Theme.Sf(18f), Theme.Sf(40f), Theme.Sf(40f));
            using (GraphicsPath path = Theme.RoundRect(tile, Theme.Sf(12f)))
            using (var brush = new LinearGradientBrush(tile, Theme.Hex("#9b7dff"), Theme.Hex("#573ed6"), 45f))
            {
                g.FillPath(brush, path);
            }
            using (GraphicsPath path = Theme.RoundRect(tile, Theme.Sf(12f)))
            using (var pen = new Pen(Color.FromArgb(95, 255, 255, 255)))
            {
                g.DrawPath(pen, path);
            }
            using (var format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                g.DrawString("V", Theme.Display(23f, FontStyle.Bold), Brushes.White,
                    new RectangleF(tile.X, tile.Y - Theme.Sf(1f), tile.Width, tile.Height), format);
            }

            using (var brush = new SolidBrush(Theme.Text))
            {
                g.DrawString("VEXOR", Theme.Display(16f, FontStyle.Bold), brush,
                    new RectangleF(Theme.Sf(70f), Theme.Sf(19f), Theme.Sf(150f), Theme.Sf(21f)));
            }
            using (var brush = new SolidBrush(Theme.Muted2))
            {
                g.DrawString("L A U N C H E R", Theme.F(8.5f, FontStyle.Bold), brush,
                    new RectangleF(Theme.Sf(71f), Theme.Sf(41f), Theme.Sf(150f), Theme.Sf(14f)));
            }

            TextRenderer.DrawText(g, "МЕНЮ", Theme.F(9f, FontStyle.Bold),
                new Point(Theme.S(32), Theme.S(112)), Theme.Muted2,
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

            int cardTop = Height - Theme.S(116);
            var card = new RectangleF(Theme.Sf(16f), cardTop, Theme.Sf(204f), Theme.Sf(96f));
            Theme.FillRound(g, card, Theme.Sf(14f), Color.FromArgb(205, Theme.SurfaceDeep));
            Theme.DrawRound(g, card, Theme.Sf(14f), Theme.BorderSoft, Theme.Sf(1f));

            var dot = new RectangleF(card.X + Theme.Sf(16f), card.Y + Theme.Sf(21f), Theme.Sf(8f), Theme.Sf(8f));
            using (var brush = new SolidBrush(AccountOnline ? Theme.Green : Theme.Muted2))
            {
                g.FillEllipse(brush, dot);
            }

            string account = string.IsNullOrWhiteSpace(AccountName) ? "Player" : AccountName.Trim();
            TextRenderer.DrawText(g, account, Theme.F(12.5f, FontStyle.Bold),
                new Rectangle((int)card.X + Theme.S(32), (int)card.Y + Theme.S(12), Theme.S(158), Theme.S(20)),
                Theme.Text, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

            string subtitle = string.IsNullOrWhiteSpace(AccountSubtitle) ? "Офлайн-аккаунт" : AccountSubtitle;
            TextRenderer.DrawText(g, subtitle, Theme.F(10f),
                new Rectangle((int)card.X + Theme.S(16), (int)card.Y + Theme.S(40), Theme.S(172), Theme.S(16)),
                AccountOnline ? Theme.Green : Theme.Muted,
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

            TextRenderer.DrawText(g, SystemText, Theme.F(9.5f),
                new Rectangle((int)card.X + Theme.S(16), (int)card.Y + Theme.S(62), Theme.S(172), Theme.S(16)),
                Theme.Muted2, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

            using (var pen = new Pen(Theme.BorderSoft))
            {
                g.DrawLine(pen, Width - 1, 0, Width - 1, Height);
            }
        }
    }

    public class GlyphTile : Control
    {
        public string Glyph = "";
        public Color Tint = Theme.Accent;
        public float GlyphSize = 20f;
        public Color PictureBackdrop = Color.Transparent;
        public float PictureInset = 0f;
        private Image _picture;

        public GlyphTile()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        public void SetPicture(Image picture)
        {
            _picture = picture;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Quality(g);

            var rect = new RectangleF(Theme.Sf(0.5f), Theme.Sf(0.5f),
                Width - Theme.Sf(1f), Height - Theme.Sf(1f));
            float radius = Theme.Sf(13f);

            if (_picture != null)
            {
                using (GraphicsPath clip = Theme.RoundRect(rect, radius))
                {
                    GraphicsState state = g.Save();
                    g.SetClip(clip);
                    using (var brush = new SolidBrush(PictureBackdrop.A > 0 ? PictureBackdrop : Theme.Alpha(Tint, 28)))
                        g.FillRectangle(brush, rect);
                    float inset = Theme.Sf(PictureInset);
                    var target = new RectangleF(rect.X + inset, rect.Y + inset,
                        Math.Max(1f, rect.Width - inset * 2), Math.Max(1f, rect.Height - inset * 2));
                    g.DrawImage(_picture, target);
                    g.Restore(state);
                }
                Theme.DrawRound(g, rect, radius, Theme.Alpha(Tint, 95), Theme.Sf(1f));
                return;
            }

            Theme.FillRound(g, rect, radius, Theme.Alpha(Tint, 28));
            Theme.DrawRound(g, rect, radius, Theme.Alpha(Tint, 75), Theme.Sf(1f));

            using (var brush = new SolidBrush(Theme.Blend(Tint, Color.White, 0.28f)))
            using (var format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                g.DrawString(Glyph, Theme.Icon(GlyphSize), brush,
                    new RectangleF(0, 0, Width, Height), format);
            }
        }
    }

    public class ModIcon : Control
    {
        public Color Tint = Theme.Accent;
        public string Letter = "?";
        private Image _picture;

        public ModIcon()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        public void SetPicture(Image picture)
        {
            // Images live in ImageCache and are shared between rows, so the control never disposes them.
            _picture = picture;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Quality(g);
            var rect = new RectangleF(Theme.Sf(0.5f), Theme.Sf(0.5f),
                Width - Theme.Sf(1f), Height - Theme.Sf(1f));
            float radius = Theme.Sf(10f);

            if (_picture != null)
            {
                using (GraphicsPath clip = Theme.RoundRect(rect, radius))
                {
                    GraphicsState state = g.Save();
                    g.SetClip(clip);
                    g.DrawImage(_picture, Rectangle.Round(rect));
                    g.Restore(state);
                }
                Theme.DrawRound(g, rect, radius, Theme.Alpha(Theme.Border, 180), Theme.Sf(1f));
                return;
            }

            Theme.FillRound(g, rect, radius, Theme.Alpha(Tint, 28));
            Theme.DrawRound(g, rect, radius, Theme.Alpha(Tint, 75), Theme.Sf(1f));
            using (var brush = new SolidBrush(Theme.Blend(Tint, Color.White, 0.35f)))
            using (var format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                string text = string.IsNullOrEmpty(Letter) ? "?" : Letter.Substring(0, 1).ToUpperInvariant();
                g.DrawString(text, Theme.Display(Height * 0.42f, FontStyle.Bold), brush,
                    new RectangleF(0, 0, Width, Height), format);
            }
        }

        protected override void Dispose(bool disposing)
        {
            _picture = null;
            base.Dispose(disposing);
        }
    }
}
