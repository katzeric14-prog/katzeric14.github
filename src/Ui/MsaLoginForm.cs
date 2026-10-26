using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vexor.Core;

namespace Vexor.Ui
{
    public sealed class MsaLoginForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        private const int WmNcLButtonDown = 0xA1;
        private const int HtCaption = 0x2;

        public MsaSession Account;
        public string ErrorMessage = "";

        private readonly CodeBox _codeBox;
        private readonly Label _status;
        private readonly RoundedButton _primary;
        private readonly RoundedButton _copy;
        private readonly RoundedButton _cancel;
        private CancellationTokenSource _cts;
        private bool _closed;
        private bool _failed;
        private string _verifyUrl = "https://www.microsoft.com/link";

        public MsaLoginForm()
        {
            Text = "Вход через Microsoft";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MaximizeBox = false;
            MinimizeBox = false;
            KeyPreview = true;
            DoubleBuffered = true;
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            ClientSize = new Size(Theme.S(460), Theme.S(382));
            Padding = new Padding(0);

            var title = new Label();
            title.AutoSize = false;
            title.BackColor = Color.Transparent;
            title.ForeColor = Theme.Text;
            title.Font = Theme.Display(19f, FontStyle.Bold);
            title.Text = "Вход через Microsoft";
            title.UseMnemonic = false;
            title.Bounds = new Rectangle(Theme.S(26), Theme.S(18), Theme.S(370), Theme.S(32));
            title.MouseDown += DragWindow;
            Controls.Add(title);

            var subtitle = new Label();
            subtitle.AutoSize = false;
            subtitle.BackColor = Color.Transparent;
            subtitle.ForeColor = Theme.AccentLight;
            subtitle.Font = Theme.F(9.5f, FontStyle.Bold);
            subtitle.Text = "ЛИЦЕНЗИОННЫЙ АККАУНТ MINECRAFT";
            subtitle.UseMnemonic = false;
            subtitle.Bounds = new Rectangle(Theme.S(27), Theme.S(50), Theme.S(370), Theme.S(18));
            subtitle.MouseDown += DragWindow;
            Controls.Add(subtitle);

            var close = new WindowButton();
            close.CloseButton = true;
            close.Glyph = "\uE8BB";
            close.Bounds = new Rectangle(Theme.S(414), Theme.S(14), Theme.S(32), Theme.S(28));
            close.Click += delegate { Close(); };
            Controls.Add(close);

            var divider = new Panel();
            divider.BackColor = Theme.BorderSoft;
            divider.Bounds = new Rectangle(Theme.S(26), Theme.S(82), Theme.S(408), Math.Max(1, Theme.S(1)));
            Controls.Add(divider);

            var info = new Label();
            info.AutoSize = false;
            info.BackColor = Color.Transparent;
            info.ForeColor = Theme.Muted;
            info.Font = Theme.F(10.5f);
            info.Text = "Откройте страницу Microsoft, введите код и подтвердите вход. " +
                        "Пароль вводится только на сайте Microsoft.";
            info.UseMnemonic = false;
            info.Bounds = new Rectangle(Theme.S(26), Theme.S(96), Theme.S(408), Theme.S(42));
            info.MouseDown += DragWindow;
            Controls.Add(info);

            var codeLabel = new Label();
            codeLabel.AutoSize = false;
            codeLabel.BackColor = Color.Transparent;
            codeLabel.ForeColor = Theme.Muted2;
            codeLabel.Font = Theme.F(9f, FontStyle.Bold);
            codeLabel.Text = "КОД ВХОДА";
            codeLabel.UseMnemonic = false;
            codeLabel.Bounds = new Rectangle(Theme.S(26), Theme.S(144), Theme.S(200), Theme.S(18));
            Controls.Add(codeLabel);

            _codeBox = new CodeBox();
            _codeBox.Bounds = new Rectangle(Theme.S(26), Theme.S(164), Theme.S(408), Theme.S(58));
            Controls.Add(_codeBox);

            _primary = new RoundedButton();
            _primary.Text = "ОТКРЫТЬ MICROSOFT.COM/LINK";
            _primary.Glyph = "\uE8A7";
            _primary.Style = RoundedButton.ButtonStyle.Accent;
            _primary.Font = Theme.F(10.5f, FontStyle.Bold);
            _primary.Bounds = new Rectangle(Theme.S(26), Theme.S(234), Theme.S(408), Theme.S(42));
            _primary.Enabled = false;
            _primary.Click += delegate { PrimaryClicked(); };
            Controls.Add(_primary);

            _copy = new RoundedButton();
            _copy.Text = "КОПИРОВАТЬ КОД";
            _copy.Style = RoundedButton.ButtonStyle.Surface;
            _copy.Font = Theme.F(10f, FontStyle.Bold);
            _copy.Bounds = new Rectangle(Theme.S(26), Theme.S(286), Theme.S(198), Theme.S(34));
            _copy.Enabled = false;
            _copy.Click += delegate { CopyCode(); };
            Controls.Add(_copy);

            _cancel = new RoundedButton();
            _cancel.Text = "ОТМЕНА";
            _cancel.Style = RoundedButton.ButtonStyle.Ghost;
            _cancel.Font = Theme.F(10f, FontStyle.Bold);
            _cancel.Bounds = new Rectangle(Theme.S(236), Theme.S(286), Theme.S(198), Theme.S(34));
            _cancel.Click += delegate { Close(); };
            Controls.Add(_cancel);

            _status = new Label();
            _status.AutoSize = false;
            _status.BackColor = Color.Transparent;
            _status.ForeColor = Theme.Muted;
            _status.Font = Theme.F(9.5f);
            _status.Text = "Получаем код у Microsoft…";
            _status.UseMnemonic = false;
            _status.Bounds = new Rectangle(Theme.S(26), Theme.S(328), Theme.S(408), Theme.S(44));
            Controls.Add(_status);

            MouseDown += DragWindow;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            StartAsync();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _closed = true;
            CancelPending();
            base.OnFormClosing(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                Close();
                return;
            }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            Theme.Quality(g);
            using (var path = RoundedClient())
            using (var brush = new LinearGradientBrush(new Rectangle(0, 0, Width, Height),
                       Theme.Bg, Theme.SurfaceDeep, 90f))
            {
                g.FillPath(brush, path);
            }
            using (GraphicsPath path = RoundedClient())
            using (var pen = new Pen(Theme.Border, Math.Max(1f, Theme.Sf(1f))))
            {
                g.DrawPath(pen, path);
            }
        }

        private GraphicsPath RoundedClient()
        {
            var rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            var path = new GraphicsPath();
            float radius = Theme.Sf(14f);
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }

        private async void StartAsync()
        {
            if (_closed) return;
            CancelPending();
            _cts = new CancellationTokenSource();
            CancellationToken ct = _cts.Token;

            _failed = false;
            ErrorMessage = "";
            _codeBox.Code = "";
            _copy.Enabled = false;
            _primary.Text = "ОТКРЫТЬ MICROSOFT.COM/LINK";
            _primary.Glyph = "\uE8A7";
            _primary.Enabled = false;
            SetStatus("Получаем код у Microsoft…", Theme.Muted);

            try
            {
                MsaDeviceCode code = await MsaAuth.StartDeviceCodeAsync(ct);
                if (_closed || ct.IsCancellationRequested) return;

                _verifyUrl = code.VerificationUri;
                _codeBox.Code = code.UserCode;
                _copy.Enabled = true;
                _primary.Enabled = true;
                CopyCode(false);
                SetStatus("Введите код на сайте Microsoft. Ожидаем подтверждение…", Theme.Muted);
                OpenLink();

                MsaSession account = await MsaAuth.LoginAsync(code, delegate(string message)
                {
                    SetStatus(message, Theme.Muted);
                }, ct);
                if (_closed || ct.IsCancellationRequested) return;

                Account = account;
                SetStatus("Вход выполнен: " + account.Name, Theme.Green);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (_closed) return;
                ErrorMessage = ex.Message;
                _failed = true;
                _codeBox.Code = "";
                _copy.Enabled = false;
                _primary.Text = "ПОВТОРИТЬ";
                _primary.Glyph = "\uE72C";
                _primary.Enabled = true;
                SetStatus(ex.Message, Theme.Red);
            }
        }

        private void PrimaryClicked()
        {
            if (_failed)
            {
                StartAsync();
                return;
            }
            OpenLink();
        }

        private void OpenLink()
        {
            try
            {
                var info = new ProcessStartInfo(_verifyUrl);
                info.UseShellExecute = true;
                Process.Start(info);
            }
            catch
            {
                try { Process.Start(_verifyUrl); }
                catch
                {
                }
            }
        }

        private void CopyCode()
        {
            CopyCode(true);
        }

        private void CopyCode(bool announce)
        {
            string code = _codeBox.Code;
            if (string.IsNullOrEmpty(code)) return;
            try
            {
                Clipboard.SetText(code);
                if (announce) SetStatus("Код скопирован в буфер обмена.", Theme.Green);
            }
            catch
            {
            }
        }

        private void SetStatus(string text, Color color)
        {
            if (_closed || IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke((Action)delegate { SetStatus(text, color); }); }
                catch
                {
                }
                return;
            }
            _status.Text = text;
            _status.ForeColor = color;
        }

        private void CancelPending()
        {
            if (_cts == null) return;
            try { _cts.Cancel(); }
            catch
            {
            }
            try { _cts.Dispose(); }
            catch
            {
            }
            _cts = null;
        }

        private void DragWindow(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            ReleaseCapture();
            SendMessage(Handle, WmNcLButtonDown, HtCaption, 0);
        }

        private sealed class CodeBox : Control
        {
            private string _code = "";

            public string Code
            {
                get { return _code; }
                set
                {
                    _code = value ?? "";
                    Invalidate();
                }
            }

            public CodeBox()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
                BackColor = Theme.SurfaceAlt;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                Theme.Quality(g);
                var rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
                using (GraphicsPath path = Theme.RoundRect(rect, Theme.Sf(11f)))
                using (var brush = new SolidBrush(Theme.SurfaceAlt))
                {
                    g.FillPath(brush, path);
                }
                using (GraphicsPath path = Theme.RoundRect(rect, Theme.Sf(11f)))
                using (var pen = new Pen(Theme.Border, Math.Max(1f, Theme.Sf(1f))))
                {
                    g.DrawPath(pen, path);
                }

                string code = _code ?? "";
                if (code.Length == 0)
                {
                    TextRenderer.DrawText(g, "…", Theme.Mono(20f),
                        new Rectangle(0, 0, Width, Height), Theme.Muted2,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                    return;
                }

                var shown = new StringBuilder();
                for (int i = 0; i < code.Length; i++)
                {
                    if (i > 0) shown.Append(' ');
                    shown.Append(code[i]);
                }
                TextRenderer.DrawText(g, shown.ToString(), Theme.Mono(21f),
                    new Rectangle(0, 0, Width, Height), Theme.Text,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            }
        }
    }
}
