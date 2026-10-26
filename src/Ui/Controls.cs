using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Vexor.Ui
{
    public class PagePanel : Panel
    {
        public PagePanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Theme.Bg);
            DrawGlow(g, new Rectangle(Width - Theme.S(430), -Theme.S(200), Theme.S(540), Theme.S(540)), 30);
            DrawGlow(g, new Rectangle(-Theme.S(170), Height - Theme.S(280), Theme.S(460), Theme.S(460)), 16);
        }

        private static void DrawGlow(Graphics g, Rectangle bounds, int alpha)
        {
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(bounds);
                using (var brush = new PathGradientBrush(path))
                {
                    brush.CenterColor = Color.FromArgb(alpha, Theme.Accent);
                    brush.SurroundColors = new[] { Color.FromArgb(0, Theme.Accent) };
                    g.FillPath(brush, path);
                }
            }
        }
    }

    public class CardPanel : Panel
    {
        public int Radius = 18;
        public Color FillTop = Theme.Surface;
        public Color FillBottom = Theme.SurfaceDeep;
        public Color Stroke = Theme.BorderSoft;
        public bool Shadow = true;

        public CardPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Quality(g);

            var rect = new RectangleF(Theme.Sf(0.5f), Theme.Sf(0.5f), Width - Theme.Sf(1f), Height - Theme.Sf(1f));
            if (rect.Width <= 0 || rect.Height <= 0) return;
            float radius = Theme.Sf(Radius);

            if (Shadow)
            {
                RectangleF shadowRect = rect;
                shadowRect.Offset(0, Theme.Sf(3f));
                Theme.FillRound(g, shadowRect, radius, Color.FromArgb(55, 0, 0, 0));
            }

            using (GraphicsPath path = Theme.RoundRect(rect, radius))
            using (var brush = new LinearGradientBrush(new RectangleF(0, 0, Width, Height),
                       FillTop, FillBottom, 90f))
            {
                g.FillPath(brush, path);
            }

            using (GraphicsPath path = Theme.RoundRect(rect, radius))
            using (var pen = new Pen(Stroke, Theme.Sf(1f)))
            {
                g.DrawPath(pen, path);
            }
        }
    }

    public class RoundedButton : Control
    {
        public enum ButtonStyle { Accent, Surface, Ghost, Danger }

        private ButtonStyle _style = ButtonStyle.Surface;
        private string _glyph = "";
        private float _hover;
        private bool _hovering;
        private bool _pressed;
        private readonly Timer _timer;

        public RoundedButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = Theme.F(14f, FontStyle.Bold);
            Cursor = Cursors.Hand;

            _timer = new Timer();
            _timer.Interval = 15;
            _timer.Tick += delegate
            {
                float target = _hovering && Enabled ? 1f : 0f;
                if (Math.Abs(_hover - target) < 0.02f)
                {
                    _hover = target;
                    _timer.Stop();
                }
                else
                {
                    _hover += (_hover < target ? 1f : -1f) * 0.16f;
                }
                Invalidate();
            };
        }

        public ButtonStyle Style
        {
            get { return _style; }
            set { _style = value; Invalidate(); }
        }

        public string Glyph
        {
            get { return _glyph; }
            set { _glyph = value ?? ""; Invalidate(); }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hovering = true;
            _timer.Start();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hovering = false;
            _pressed = false;
            _timer.Start();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) _pressed = true;
            base.OnMouseDown(e);
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _pressed = false;
            base.OnMouseUp(e);
            Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Quality(g);
            if (Width <= 2 || Height <= 2) return;

            float radius = Theme.Sf(13f);
            var rect = new RectangleF(Theme.Sf(1f), Theme.Sf(1f), Width - Theme.Sf(2f), Height - Theme.Sf(2f));
            float t = _hover;

            Color top, bottom, border, textColor;

            if (!Enabled)
            {
                top = Theme.Alpha(Theme.SurfaceAlt, 120);
                bottom = top;
                border = Theme.Alpha(Theme.BorderSoft, 160);
                textColor = Theme.Muted2;
            }
            else if (_style == ButtonStyle.Accent)
            {
                top = Theme.Blend(Theme.Hex("#8a6bff"), Theme.Hex("#aa90ff"), t);
                bottom = Theme.Blend(Theme.Hex("#6246ee"), Theme.Hex("#7c5cff"), t);
                border = Theme.Blend(Theme.AccentDark, Theme.AccentLight, t);
                textColor = Color.White;

                RectangleF glowRect = rect;
                glowRect.Inflate(Theme.Sf(2f), Theme.Sf(2f));
                Theme.FillRound(g, glowRect, radius + Theme.Sf(2f),
                    Color.FromArgb(16 + (int)(26 * t), Theme.Accent));
            }
            else if (_style == ButtonStyle.Danger)
            {
                top = Theme.Blend(Theme.Hex("#e0474c"), Theme.Hex("#f0565b"), t);
                bottom = Theme.Blend(Theme.Hex("#c23a3f"), Theme.Hex("#e0474c"), t);
                border = Theme.Hex("#f47074");
                textColor = Color.White;
            }
            else if (_style == ButtonStyle.Ghost)
            {
                top = Color.FromArgb((int)(16 * t), 255, 255, 255);
                bottom = top;
                border = Color.FromArgb(110 + (int)(50 * t), Theme.Border);
                textColor = Theme.Blend(Theme.Muted, Theme.Text, t);
            }
            else
            {
                top = Theme.Blend(Theme.SurfaceAlt, Theme.SurfaceHover, t);
                bottom = Theme.Blend(Theme.SurfaceDeep, Theme.SurfaceAlt, t);
                border = Theme.Blend(Theme.Border, Theme.Muted2, t * 0.55f);
                textColor = Theme.Text;
            }

            using (GraphicsPath path = Theme.RoundRect(rect, radius))
            using (var brush = new LinearGradientBrush(new RectangleF(0, 0, Width, Height), top, bottom, 90f))
            {
                g.FillPath(brush, path);
            }
            using (GraphicsPath path = Theme.RoundRect(rect, radius))
            using (var pen = new Pen(border, Theme.Sf(1f)))
            {
                g.DrawPath(pen, path);
            }

            if (_pressed) g.TranslateTransform(0, Theme.Sf(1f));

            using (var textBrush = new SolidBrush(textColor))
            using (var center = new StringFormat())
            {
                center.Alignment = StringAlignment.Center;
                center.LineAlignment = StringAlignment.Center;
                center.Trimming = StringTrimming.EllipsisCharacter;
                center.FormatFlags = StringFormatFlags.NoWrap;

                string text = Text ?? "";
                bool hasIcon = !string.IsNullOrEmpty(_glyph);
                float pad = Theme.Sf(8f);
                float available = Math.Max(1f, Width - pad * 2f);
                float textWidth = string.IsNullOrEmpty(text) ? 0f : g.MeasureString(text, Font, 10000, StringFormat.GenericTypographic).Width;
                float iconSize = hasIcon ? Theme.Sf(18f) : 0f;
                float gap = hasIcon && textWidth > 0 ? Theme.Sf(8f) : 0f;
                float allowed = Math.Max(0f, available - iconSize - gap);
                if (textWidth > allowed) textWidth = allowed;
                float total = textWidth + iconSize + gap;
                float startX = Math.Max(pad, (Width - total) / 2f);

                if (hasIcon)
                {
                    using (var iconBrush = new SolidBrush(textColor))
                    {
                        g.DrawString(_glyph, Theme.Icon(15f), iconBrush,
                            new RectangleF(startX, 0, iconSize, Height), center);
                    }
                }
                if (!string.IsNullOrEmpty(text))
                {
                    var textRect = new RectangleF(startX + iconSize + gap, 0, textWidth + Theme.Sf(8f), Height);
                    g.DrawString(text, Font, textBrush, textRect, center);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer.Dispose();
            base.Dispose(disposing);
        }
    }

    public class NavButton : Control
    {
        private bool _selected;
        private bool _hovering;
        private string _glyph = "";

        public NavButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = Theme.F(13f);
            Cursor = Cursors.Hand;
            Height = Theme.S(44);
        }

        public bool Selected
        {
            get { return _selected; }
            set { _selected = value; Invalidate(); }
        }

        public string Glyph
        {
            get { return _glyph; }
            set { _glyph = value ?? ""; Invalidate(); }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hovering = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hovering = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Quality(g);
            float radius = Theme.Sf(12f);
            var rect = new RectangleF(Theme.Sf(0.5f), Theme.Sf(0.5f), Width - Theme.Sf(1f), Height - Theme.Sf(1f));

            if (_selected)
            {
                using (GraphicsPath path = Theme.RoundRect(rect, radius))
                using (var brush = new SolidBrush(Theme.SurfaceAlt))
                    g.FillPath(brush, path);
                using (GraphicsPath path = Theme.RoundRect(rect, radius))
                using (var pen = new Pen(Theme.BorderSoft))
                    g.DrawPath(pen, path);

                Theme.FillRound(g, new RectangleF(0, (Height - Theme.Sf(20f)) / 2f, Theme.Sf(3f), Theme.Sf(20f)),
                    Theme.Sf(1.5f), Theme.Accent);
            }
            else if (_hovering)
            {
                Theme.FillRound(g, rect, radius, Color.FromArgb(12, 255, 255, 255));
            }

            Color iconColor = _selected ? Theme.AccentLight : Theme.Muted;
            Color textColor = _selected ? Theme.Text : Theme.Muted;

            using (var iconBrush = new SolidBrush(iconColor))
            using (var textBrush = new SolidBrush(textColor))
            using (var center = new StringFormat())
            {
                center.Alignment = StringAlignment.Center;
                center.LineAlignment = StringAlignment.Center;
                g.DrawString(_glyph, Theme.Icon(16f), iconBrush,
                    new RectangleF(Theme.Sf(14f), 0, Theme.Sf(20f), Height), center);
                using (var left = new StringFormat())
                {
                    left.Alignment = StringAlignment.Near;
                    left.LineAlignment = StringAlignment.Center;
                    left.Trimming = StringTrimming.EllipsisCharacter;
                    left.FormatFlags = StringFormatFlags.NoWrap;
                    g.DrawString(Text, _selected ? Theme.F(13f, FontStyle.Bold) : Font, textBrush,
                        new RectangleF(Theme.Sf(46f), 0, Width - Theme.Sf(56f), Height), left);
                }
            }
        }
    }

    public class PillButton : Control
    {
        private bool _selected;
        private bool _hovering;

        public PillButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = Theme.F(11.5f);
            Cursor = Cursors.Hand;
        }

        public bool Selected
        {
            get { return _selected; }
            set { _selected = value; Invalidate(); }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hovering = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hovering = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Quality(g);
            var rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            float radius = rect.Height / 2f;

            Color fill;
            Color textColor;
            Color border = Color.Transparent;

            if (_selected)
            {
                fill = Theme.Alpha(Theme.Accent, 38);
                border = Theme.Alpha(Theme.Accent, 150);
                textColor = Theme.AccentLight;
            }
            else
            {
                fill = _hovering ? Color.FromArgb(16, 255, 255, 255) : Color.Transparent;
                border = _hovering ? Theme.Alpha(Theme.Border, 200) : Theme.Alpha(Theme.BorderSoft, 170);
                textColor = _hovering ? Theme.Text : Theme.Muted;
            }

            using (GraphicsPath path = Theme.RoundRect(rect, radius))
            {
                if (fill.A > 0)
                    using (var brush = new SolidBrush(fill)) g.FillPath(brush, path);
                if (border.A > 0)
                    using (var pen = new Pen(border)) g.DrawPath(pen, path);
            }

            using (var brush = new SolidBrush(textColor))
            using (var format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                g.DrawString(Text, Font, brush, new RectangleF(0, 0, Width, Height), format);
            }
        }
    }

    public class WindowButton : Control
    {
        private bool _hovering;
        private bool _close;
        private string _glyph = "";

        public WindowButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        public bool CloseButton
        {
            get { return _close; }
            set { _close = value; Invalidate(); }
        }

        public string Glyph
        {
            get { return _glyph; }
            set { _glyph = value ?? ""; Invalidate(); }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hovering = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hovering = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Quality(g);
            var rect = new RectangleF(Theme.Sf(2f), Theme.Sf(2f), Width - Theme.Sf(4f), Height - Theme.Sf(4f));
            if (_hovering)
            {
                Color fill = _close ? Theme.Hex("#d83b40") : Color.FromArgb(22, 255, 255, 255);
                Theme.FillRound(g, rect, Theme.Sf(7f), fill);
            }

            using (var brush = new SolidBrush(_close && _hovering ? Color.White : Theme.Muted))
            using (var format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                g.DrawString(_glyph, Theme.Icon(11f), brush,
                    new RectangleF(0, Theme.Sf(1f), Width, Height), format);
            }
        }
    }

    public class TextField : Control
    {
        private readonly TextBox _box;
        private readonly Label _placeholder;
        private bool _focused;
        private bool _syncing;

        public TextField()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Height = Theme.S(38);

            _box = new TextBox();
            _box.BorderStyle = BorderStyle.None;
            _box.BackColor = Theme.SurfaceAlt;
            _box.ForeColor = Theme.Text;
            _box.Font = Theme.F(13f);
            _box.AutoSize = false;
            _box.TextChanged += delegate
            {
                UpdatePlaceholder();
                Invalidate();
                if (!_syncing && ValueChanged != null) ValueChanged(this, EventArgs.Empty);
            };
            _box.GotFocus += delegate { _focused = true; UpdatePlaceholder(); Invalidate(); };
            _box.LostFocus += delegate { _focused = false; UpdatePlaceholder(); Invalidate(); };
            _box.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    if (Submitted != null) Submitted(this, EventArgs.Empty);
                }
            };
            Controls.Add(_box);

            _placeholder = new Label();
            _placeholder.AutoSize = false;
            _placeholder.BackColor = Theme.SurfaceAlt;
            _placeholder.ForeColor = Theme.Muted2;
            _placeholder.Font = Theme.F(13f);
            _placeholder.TextAlign = ContentAlignment.MiddleLeft;
            _placeholder.Cursor = Cursors.IBeam;
            _placeholder.Click += delegate { _box.Focus(); };
            Controls.Add(_placeholder);
            _placeholder.BringToFront();

            LayoutChildren();
        }

        public event EventHandler ValueChanged;
        public event EventHandler Submitted;

        public string Value
        {
            get { return _box.Text; }
            set
            {
                string text = value ?? "";
                if (_box.Text == text) return;
                _syncing = true;
                _box.Text = text;
                _syncing = false;
                UpdatePlaceholder();
                Invalidate();
            }
        }

        public string Placeholder
        {
            get { return _placeholder.Text; }
            set
            {
                _placeholder.Text = value ?? "";
                UpdatePlaceholder();
            }
        }

        public int MaxLength
        {
            get { return _box.MaxLength; }
            set { _box.MaxLength = value; }
        }

        public bool ReadOnly
        {
            get { return _box.ReadOnly; }
            set { _box.ReadOnly = value; }
        }

        public void FocusBox()
        {
            _box.Focus();
        }

        private void LayoutChildren()
        {
            if (_box == null || _placeholder == null) return;
            int pad = Theme.S(12);
            int height = Math.Max(Theme.S(20), _box.PreferredHeight);
            int top = (Height - height) / 2;
            int width = Math.Max(10, Width - pad * 2);
            _box.Bounds = new Rectangle(pad, top, width, height);
            _placeholder.Bounds = _box.Bounds;
            _placeholder.Font = _box.Font;
        }

        private void UpdatePlaceholder()
        {
            _placeholder.Visible = !_focused && _box.TextLength == 0 && _placeholder.Text.Length > 0;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutChildren();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _box.Focus();
            base.OnMouseDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Quality(g);
            var rect = new RectangleF(Theme.Sf(0.5f), Theme.Sf(0.5f), Width - Theme.Sf(1f), Height - Theme.Sf(1f));
            using (GraphicsPath path = Theme.RoundRect(rect, Theme.Sf(10f)))
            using (var brush = new SolidBrush(Theme.SurfaceAlt))
                g.FillPath(brush, path);
            using (GraphicsPath path = Theme.RoundRect(rect, Theme.Sf(10f)))
            using (var pen = new Pen(_focused ? Theme.Accent : Theme.Border, Theme.Sf(_focused ? 1.4f : 1f)))
                g.DrawPath(pen, path);
        }
    }

    public class ToggleSwitch : Control
    {
        private bool _checked;
        private float _position;
        private readonly Timer _timer;

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Size = new Size(Theme.S(44), Theme.S(24));

            _timer = new Timer();
            _timer.Interval = 15;
            _timer.Tick += delegate
            {
                float target = _checked ? 1f : 0f;
                if (Math.Abs(_position - target) < 0.02f)
                {
                    _position = target;
                    _timer.Stop();
                }
                else
                {
                    _position += (_position < target ? 1f : -1f) * 0.16f;
                }
                Invalidate();
            };
        }

        public event EventHandler CheckedChanged;

        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked == value) return;
                _checked = value;
                _timer.Start();
                if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
            }
        }

        protected override void OnClick(EventArgs e)
        {
            Checked = !Checked;
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Quality(g);

            float height = Math.Min(Height, Theme.Sf(24f));
            float width = Math.Min(Width, Theme.Sf(44f));
            var track = new RectangleF((Width - width) / 2f, (Height - height) / 2f, width, height);
            Color trackColor = Theme.Blend(Theme.SurfaceHover, Theme.Accent, _position);
            Theme.FillRound(g, track, height / 2f, trackColor);

            float knob = height - Theme.Sf(6f);
            float left = track.X + Theme.Sf(3f) + _position * (width - knob - Theme.Sf(6f));
            var knobRect = new RectangleF(left, track.Y + Theme.Sf(3f), knob, knob);
            Theme.FillRound(g, knobRect, knob / 2f, Color.FromArgb(240, 248, 250, 255));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer.Dispose();
            base.Dispose(disposing);
        }
    }

    public class RamSlider : Control
    {
        private int _minimum = 1024;
        private int _maximum = 16384;
        private int _step = 512;
        private int _value = 4096;
        private bool _dragging;

        public RamSlider()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Height = Theme.S(28);
            Cursor = Cursors.Hand;
        }

        public event EventHandler ValueChanged;

        public int Minimum
        {
            get { return _minimum; }
            set { _minimum = value; Invalidate(); }
        }

        public int Maximum
        {
            get { return _maximum; }
            set { _maximum = value; Invalidate(); }
        }

        public int Step
        {
            get { return _step; }
            set { _step = Math.Max(1, value); }
        }

        public int Value
        {
            get { return _value; }
            set
            {
                int clamped = Math.Max(_minimum, Math.Min(_maximum, value));
                if (_step > 0)
                    clamped = _minimum + (int)Math.Round((double)(clamped - _minimum) / _step) * _step;
                if (clamped > _maximum) clamped = _maximum;
                if (clamped == _value) return;
                _value = clamped;
                Invalidate();
                if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
            }
        }

        private int PaddingPx
        {
            get { return Theme.S(9); }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _dragging = true;
                Capture = true;
                SetFromX(e.X);
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_dragging) SetFromX(e.X);
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _dragging = false;
            Capture = false;
            base.OnMouseUp(e);
        }

        private void SetFromX(int x)
        {
            int pad = PaddingPx;
            int usable = Math.Max(1, Width - pad * 2);
            double ratio = (double)(x - pad) / usable;
            if (ratio < 0) ratio = 0;
            if (ratio > 1) ratio = 1;
            Value = _minimum + (int)Math.Round(ratio * (_maximum - _minimum));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Quality(g);

            int pad = PaddingPx;
            float trackHeight = Theme.Sf(5f);
            float top = (Height - trackHeight) / 2f;
            var track = new RectangleF(pad, top, Width - pad * 2, trackHeight);
            Theme.FillRound(g, track, trackHeight / 2f, Theme.SurfaceHover);

            double ratio = _maximum > _minimum ? (double)(_value - _minimum) / (_maximum - _minimum) : 0;
            var fill = new RectangleF(track.X, track.Y, (float)(track.Width * ratio), track.Height);
            if (fill.Width > 0.5f)
            {
                using (GraphicsPath path = Theme.RoundRect(fill, fill.Height / 2f))
                using (var brush = new LinearGradientBrush(
                    new RectangleF(track.X, 0, Math.Max(2f, track.Width), 1),
                    Theme.Accent, Theme.AccentLight, 0f))
                {
                    g.FillPath(brush, path);
                }
            }

            float knobRadius = Theme.Sf(8f);
            float knobX = (float)(track.X + track.Width * ratio);
            var knob = new RectangleF(knobX - knobRadius, Height / 2f - knobRadius, knobRadius * 2, knobRadius * 2);
            Theme.FillRound(g, new RectangleF(knob.X - Theme.Sf(1f), knob.Y - Theme.Sf(1f), knob.Width + Theme.Sf(2f), knob.Height + Theme.Sf(2f)),
                knobRadius + Theme.Sf(1f), Color.FromArgb(40, Theme.Accent));
            Theme.FillRound(g, knob, knobRadius, Color.FromArgb(245, 250, 250, 255));
            Theme.DrawRound(g, knob, knobRadius, Theme.Accent, Theme.Sf(2f));
        }
    }

    public class ProgressBarEx : Control
    {
        private double _value;
        private double _display;
        private readonly Timer _timer;

        public ProgressBarEx()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Height = Theme.S(9);
            _timer = new Timer();
            _timer.Interval = 16;
            _timer.Tick += delegate
            {
                if (Math.Abs(_display - _value) < 0.004)
                {
                    _display = _value;
                    _timer.Stop();
                }
                else
                {
                    _display += (_value - _display) * 0.18;
                }
                Invalidate();
            };
        }

        public double Value
        {
            get { return _value; }
            set
            {
                _value = value < 0 ? 0 : (value > 1 ? 1 : value);
                _timer.Start();
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Quality(g);
            float radius = Height / 2f;
            var track = new RectangleF(0, 0, Width, Height);
            Theme.FillRound(g, track, radius, Theme.SurfaceHover);

            if (_display > 0.001)
            {
                var fill = new RectangleF(0, 0, (float)(Width * _display), Height);
                if (fill.Width < Height) fill.Width = Math.Min(Width, Height);
                using (GraphicsPath path = Theme.RoundRect(fill, radius))
                using (var brush = new LinearGradientBrush(
                    new RectangleF(0, 0, Math.Max(2f, Width), Math.Max(2f, Height)),
                    Theme.Accent, Theme.Green, 0f))
                {
                    g.FillPath(brush, path);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer.Dispose();
            base.Dispose(disposing);
        }
    }

    public class DarkScrollBar : Control
    {
        private int _minimum;
        private int _maximum;
        private int _value;
        private int _largeChange = 1;
        private int _smallChange = 30;
        private bool _dragging;
        private int _dragOffset;
        private bool _hover;

        public DarkScrollBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Width = Theme.S(6);
            Cursor = Cursors.Hand;
        }

        public event EventHandler ValueChanged;

        public int Minimum
        {
            get { return _minimum; }
            set { _minimum = value; Coerce(); Invalidate(); }
        }

        public int Maximum
        {
            get { return _maximum; }
            set { _maximum = value; Coerce(); Invalidate(); }
        }

        public int LargeChange
        {
            get { return _largeChange; }
            set { _largeChange = Math.Max(1, value); Invalidate(); }
        }

        public int SmallChange
        {
            get { return _smallChange; }
            set { _smallChange = Math.Max(1, value); }
        }

        public int Value
        {
            get { return _value; }
            set { SetValue(value); }
        }

        public void ScrollBy(int delta)
        {
            SetValue(_value + delta);
        }

        private void Coerce()
        {
            if (_maximum < _minimum) _maximum = _minimum;
            if (_value < _minimum) _value = _minimum;
            if (_value > _maximum) _value = _maximum;
        }

        private void SetValue(int value)
        {
            if (value < _minimum) value = _minimum;
            if (value > _maximum) value = _maximum;
            if (value == _value) return;
            _value = value;
            Invalidate();
            if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
        }

        private float ThumbHeight(float trackHeight)
        {
            double content = (double)(_maximum - _minimum) + _largeChange;
            if (content <= 0) return trackHeight;
            float thumb = (float)(trackHeight * _largeChange / content);
            float minThumb = Theme.Sf(24f);
            return Math.Max(Math.Min(minThumb, trackHeight), thumb);
        }

        private RectangleF ThumbRect()
        {
            float pad = Theme.Sf(2f);
            float trackHeight = Math.Max(1f, Height - pad * 2f);
            float thumb = ThumbHeight(trackHeight);
            float range = _maximum - _minimum;
            float ratio = range > 0 ? (float)(_value - _minimum) / range : 0f;
            float top = pad + (trackHeight - thumb) * ratio;
            return new RectangleF(0, top, Width, thumb);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                RectangleF thumb = ThumbRect();
                if (e.Y >= thumb.Y && e.Y <= thumb.Bottom)
                {
                    _dragging = true;
                    _dragOffset = (int)(e.Y - thumb.Y);
                    Capture = true;
                }
                else
                {
                    SetValue(e.Y < thumb.Y ? _value - _largeChange : _value + _largeChange);
                }
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_dragging)
            {
                float pad = Theme.Sf(2f);
                float trackHeight = Math.Max(1f, Height - pad * 2f);
                float thumb = ThumbHeight(trackHeight);
                float usable = trackHeight - thumb;
                if (usable > 0.5f)
                {
                    float ratio = (e.Y - pad - _dragOffset) / usable;
                    if (ratio < 0f) ratio = 0f;
                    if (ratio > 1f) ratio = 1f;
                    SetValue(_minimum + (int)Math.Round(ratio * (_maximum - _minimum)));
                }
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _dragging = false;
            Capture = false;
            base.OnMouseUp(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            ScrollBy(-Math.Sign(e.Delta) * _smallChange);
            base.OnMouseWheel(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Quality(g);

            float pad = Theme.Sf(2f);
            var track = new RectangleF(0, pad, Width, Math.Max(1f, Height - pad * 2f));
            Theme.FillRound(g, track, Width / 2f, Color.FromArgb(26, 255, 255, 255));

            RectangleF thumb = ThumbRect();
            Color color = _dragging || _hover
                ? Theme.Accent
                : Color.FromArgb(150, 255, 255, 255);
            Theme.FillRound(g, thumb, Width / 2f, color);
        }
    }

    public class LogView : Control
    {
        private sealed class LogLine
        {
            public string Time;
            public string Text;
            public Color Color;
        }

        private readonly List<LogLine> _lines = new List<LogLine>();
        private int _scroll;
        private bool _hovering;

        public LogView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = Theme.Mono(11.5f);
        }

        private int LineHeight
        {
            get { return Theme.S(17); }
        }

        private int Pad
        {
            get { return Theme.S(12); }
        }

        private int MaxScroll
        {
            get
            {
                int content = _lines.Count * LineHeight + Pad * 2;
                int max = content - Height;
                return max > 0 ? max : 0;
            }
        }

        public void Append(string text, Color color)
        {
            if (string.IsNullOrEmpty(text)) return;
            bool stick = _scroll >= MaxScroll - LineHeight;
            string[] parts = text.Replace("\r", "").Split('\n');
            string time = DateTime.Now.ToString("HH:mm:ss");
            foreach (string part in parts)
            {
                var line = new LogLine();
                line.Time = time;
                line.Text = part;
                line.Color = color;
                _lines.Add(line);
            }
            if (_lines.Count > 900) _lines.RemoveRange(0, _lines.Count - 900);
            if (stick) _scroll = MaxScroll;
            if (_scroll < 0) _scroll = 0;
            Invalidate();
        }

        public void Clear()
        {
            _lines.Clear();
            _scroll = 0;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hovering = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hovering = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            _scroll -= (e.Delta / 120) * LineHeight * 3;
            if (_scroll < 0) _scroll = 0;
            if (_scroll > MaxScroll) _scroll = MaxScroll;
            Invalidate();
            base.OnMouseWheel(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Quality(g);
            var rect = new RectangleF(Theme.Sf(0.5f), Theme.Sf(0.5f), Width - Theme.Sf(1f), Height - Theme.Sf(1f));
            using (GraphicsPath path = Theme.RoundRect(rect, Theme.Sf(12f)))
            using (var brush = new SolidBrush(Theme.BgDeep))
                g.FillPath(brush, path);
            using (GraphicsPath path = Theme.RoundRect(rect, Theme.Sf(12f)))
            using (var pen = new Pen(Theme.Alpha(Theme.BorderSoft, 180)))
                g.DrawPath(pen, path);

            if (_scroll > MaxScroll) _scroll = MaxScroll;

            int lineHeight = LineHeight;
            int pad = Pad;
            int timeWidth = Theme.S(56);
            int textLeft = pad + timeWidth;
            int textWidth = Math.Max(10, Width - textLeft - pad - Theme.S(8));

            var clip = new Rectangle(pad, pad, Math.Max(10, Width - pad * 2), Math.Max(10, Height - pad * 2));
            g.SetClip(clip);

            int first = Math.Max(0, _scroll / lineHeight);
            int y = pad - (_scroll % lineHeight);
            for (int i = first; i < _lines.Count; i++)
            {
                LogLine line = _lines[i];
                if (y > Height - pad) break;
                TextRenderer.DrawText(g, line.Time, Font, new Point(pad, y), Theme.Muted2,
                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(g, line.Text, Font, new Rectangle(textLeft, y, textWidth, lineHeight),
                    line.Color, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
                y += lineHeight;
            }
            g.ResetClip();

            if (MaxScroll > 0)
            {
                float trackHeight = Height - pad * 2;
                float thumbHeight = Math.Max(Theme.Sf(24f), trackHeight * Height / (float)(_lines.Count * lineHeight + pad * 2));
                float thumbY = pad + (trackHeight - thumbHeight) * (_scroll / (float)MaxScroll);
                var thumb = new RectangleF(Width - Theme.Sf(6f), thumbY, Theme.Sf(3f), thumbHeight);
                Theme.FillRound(g, thumb, Theme.Sf(1.5f),
                    _hovering ? Theme.Muted2 : Theme.Alpha(Theme.Border, 220));
            }
        }
    }
}
