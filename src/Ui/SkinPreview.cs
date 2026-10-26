using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Vexor.Ui
{
    public class SkinPreview : Control
    {
        private Image _skin;
        private bool _slim;
        private Bitmap _composed;

        public string EmptyText = "Скин не выбран";

        public SkinPreview()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        public void SetSkin(Image skin, bool slim)
        {
            if (ReferenceEquals(_skin, skin) && _slim == slim) return;
            if (_composed != null)
            {
                _composed.Dispose();
                _composed = null;
            }
            if (_skin != null && !ReferenceEquals(_skin, skin)) _skin.Dispose();
            _skin = skin;
            _slim = slim;
            _composed = Compose();
            Invalidate();
        }

        public void Clear()
        {
            SetSkin(null, false);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            int armWidth = _slim ? 3 : 4;
            int width = 8 + armWidth * 2;
            int reserved = Height >= 220 ? Theme.S(32) : 0;
            int scale = Math.Min(Width / width, (Height - reserved) / 32);
            if (scale < 1) scale = 1;
            int drawWidth = width * scale;
            int drawHeight = 32 * scale;
            int x = (Width - drawWidth) / 2;
            int y = (Height - drawHeight - reserved) / 2;
            if (y < 0) y = 0;

            using (var shadow = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
            {
                g.FillEllipse(shadow, x - scale * 2, y + drawHeight - scale * 2,
                    drawWidth + scale * 4, scale * 3);
            }

            if (_composed == null)
            {
                DrawPlaceholder(g, x, y, scale, width);
                if (Height >= 220 && !string.IsNullOrEmpty(EmptyText))
                {
                    using (var brush = new SolidBrush(Theme.Muted2))
                    using (var format = new StringFormat())
                    {
                        format.Alignment = StringAlignment.Center;
                        format.LineAlignment = StringAlignment.Center;
                        g.DrawString(EmptyText, Theme.F(10.5f), brush,
                            new RectangleF(0, y + drawHeight + Theme.Sf(8f), Width, Theme.Sf(24f)), format);
                    }
                }
                return;
            }

            InterpolationMode oldInterpolation = g.InterpolationMode;
            PixelOffsetMode oldOffset = g.PixelOffsetMode;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(_composed, new Rectangle(x, y, drawWidth, drawHeight));
            g.InterpolationMode = oldInterpolation;
            g.PixelOffsetMode = oldOffset;
        }

        private void DrawPlaceholder(Graphics g, int x, int y, int scale, int width)
        {
            int armWidth = _slim ? 3 : 4;
            int bodyX = _slim ? 3 : 4;
            using (var fill = new SolidBrush(Theme.Hex("#252b3a")))
            using (var edge = new Pen(Theme.Hex("#39415a"), Math.Max(1f, Theme.Sf(1f))))
            {
                DrawBox(g, fill, edge, x + bodyX * scale, y, 8 * scale, 8 * scale);
                DrawBox(g, fill, edge, x + bodyX * scale, y + 8 * scale, 8 * scale, 12 * scale);
                DrawBox(g, fill, edge, x + (bodyX - armWidth) * scale, y + 8 * scale,
                    armWidth * scale, 12 * scale);
                DrawBox(g, fill, edge, x + (bodyX + 8) * scale, y + 8 * scale,
                    armWidth * scale, 12 * scale);
                DrawBox(g, fill, edge, x + bodyX * scale, y + 20 * scale, 4 * scale, 12 * scale);
                DrawBox(g, fill, edge, x + (bodyX + 4) * scale, y + 20 * scale, 4 * scale, 12 * scale);
            }
        }

        private static void DrawBox(Graphics g, Brush fill, Pen edge, int x, int y, int w, int h)
        {
            var rect = new Rectangle(x, y, w, h);
            g.FillRectangle(fill, rect);
            g.DrawRectangle(edge, rect);
        }

        private Bitmap Compose()
        {
            if (_skin == null) return null;

            int armWidth = _slim ? 3 : 4;
            int bodyX = _slim ? 3 : 4;
            int width = 8 + armWidth * 2;
            bool modern = _skin.Width >= 64 && _skin.Height >= 64;

            var bitmap = new Bitmap(width, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;

                DrawPart(g, 8, 8, 8, 8, bodyX, 0);
                DrawPart(g, 20, 20, 8, 12, bodyX, 8);
                DrawPart(g, 44, 20, armWidth, 12, bodyX - armWidth, 8);
                DrawPart(g, 4, 20, 4, 12, bodyX, 20);

                if (modern)
                {
                    DrawPart(g, 36, 52, armWidth, 12, bodyX + 8, 8);
                    DrawPart(g, 20, 52, 4, 12, bodyX + 4, 20);

                    DrawPart(g, 40, 8, 8, 8, bodyX, 0);
                    DrawPart(g, 20, 36, 8, 12, bodyX, 8);
                    DrawPart(g, 44, 36, armWidth, 12, bodyX - armWidth, 8);
                    DrawPart(g, 4, 36, 4, 12, bodyX, 20);
                    DrawPart(g, 52, 52, armWidth, 12, bodyX + 8, 8);
                    DrawPart(g, 4, 52, 4, 12, bodyX + 4, 20);
                }
                else
                {
                    DrawMirrored(g, 44, 20, armWidth, 12, bodyX + 8, 8);
                    DrawMirrored(g, 4, 20, 4, 12, bodyX + 4, 20);
                    DrawPart(g, 40, 8, 8, 8, bodyX, 0);
                }
            }
            return bitmap;
        }

        private void DrawPart(Graphics g, int sx, int sy, int sw, int sh, int dx, int dy)
        {
            var source = new Rectangle(sx, sy, sw, sh);
            var target = new Rectangle(dx, dy, sw, sh);
            g.DrawImage(_skin, target, source, GraphicsUnit.Pixel);
        }

        private void DrawMirrored(Graphics g, int sx, int sy, int sw, int sh, int dx, int dy)
        {
            using (var part = new Bitmap(sw, sh, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (Graphics pg = Graphics.FromImage(part))
                {
                    pg.PixelOffsetMode = PixelOffsetMode.Half;
                    pg.DrawImage(_skin, new Rectangle(0, 0, sw, sh),
                        new Rectangle(sx, sy, sw, sh), GraphicsUnit.Pixel);
                }
                part.RotateFlip(RotateFlipType.RotateNoneFlipX);
                g.DrawImage(part, new Rectangle(dx, dy, sw, sh));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_composed != null)
                {
                    _composed.Dispose();
                    _composed = null;
                }
                if (_skin != null)
                {
                    _skin.Dispose();
                    _skin = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
