using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vexor.Core;

namespace Vexor.Ui
{
    public sealed class UpdateForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        private const int WmNcLButtonDown = 0xA1;
        private const int HtCaption = 0x2;

        public bool RestartRequested;

        private readonly UpdateManifest _manifest;
        private readonly ProgressBarEx _progress;
        private readonly Label _status;
        private readonly RoundedButton _action;
        private readonly RoundedButton _cancel;
        private CancellationTokenSource _cts;
        private bool _closed;

        public UpdateForm(UpdateManifest manifest)
        {
            _manifest = manifest;

            Text = "Обновление лаунчера";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MaximizeBox = false;
            MinimizeBox = false;
            KeyPreview = true;
            DoubleBuffered = true;
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            ClientSize = new Size(Theme.S(460), Theme.S(390));

            var title = new Label();
            title.AutoSize = false;
            title.BackColor = Color.Transparent;
            title.ForeColor = Theme.Text;
            title.Font = Theme.Display(19f, FontStyle.Bold);
            title.Text = "Обновление лаунчера";
            title.UseMnemonic = false;
            title.Bounds = new Rectangle(Theme.S(26), Theme.S(18), Theme.S(370), Theme.S(32));
            title.MouseDown += DragWindow;
            Controls.Add(title);

            var subtitle = new Label();
            subtitle.AutoSize = false;
            subtitle.BackColor = Color.Transparent;
            subtitle.ForeColor = Theme.AccentLight;
            subtitle.Font = Theme.F(9.5f, FontStyle.Bold);
            subtitle.Text = UpdateService.CurrentVersion + "  →  " + manifest.Version;
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

            var notesLabel = new Label();
            notesLabel.AutoSize = false;
            notesLabel.BackColor = Color.Transparent;
            notesLabel.ForeColor = Theme.Muted2;
            notesLabel.Font = Theme.F(9f, FontStyle.Bold);
            notesLabel.Text = "ЧТО НОВОГО";
            notesLabel.UseMnemonic = false;
            notesLabel.Bounds = new Rectangle(Theme.S(26), Theme.S(96), Theme.S(200), Theme.S(18));
            Controls.Add(notesLabel);

            var notes = new TextBox();
            notes.Multiline = true;
            notes.ReadOnly = true;
            notes.ScrollBars = ScrollBars.Vertical;
            notes.BorderStyle = BorderStyle.None;
            notes.BackColor = Theme.SurfaceAlt;
            notes.ForeColor = Theme.Text;
            notes.Font = Theme.F(10.5f);
            notes.Text = string.IsNullOrEmpty(manifest.Notes) ? "Без описания изменений." : manifest.Notes;
            notes.Bounds = new Rectangle(Theme.S(26), Theme.S(118), Theme.S(408), Theme.S(128));
            Controls.Add(notes);

            _progress = new ProgressBarEx();
            _progress.Bounds = new Rectangle(Theme.S(26), Theme.S(262), Theme.S(408), Theme.S(9));
            Controls.Add(_progress);

            _status = new Label();
            _status.AutoSize = false;
            _status.BackColor = Color.Transparent;
            _status.ForeColor = Theme.Muted;
            _status.Font = Theme.F(9.5f);
            _status.Text = "Подготовка к загрузке…";
            _status.UseMnemonic = false;
            _status.Bounds = new Rectangle(Theme.S(26), Theme.S(278), Theme.S(408), Theme.S(38));
            Controls.Add(_status);

            _action = new RoundedButton();
            _action.Text = "ЗАГРУЗКА…";
            _action.Style = RoundedButton.ButtonStyle.Accent;
            _action.Font = Theme.F(10f, FontStyle.Bold);
            _action.Bounds = new Rectangle(Theme.S(26), Theme.S(322), Theme.S(198), Theme.S(38));
            _action.Enabled = false;
            _action.Click += delegate { StartAsync(); };
            Controls.Add(_action);

            _cancel = new RoundedButton();
            _cancel.Text = "ОТМЕНА";
            _cancel.Style = RoundedButton.ButtonStyle.Ghost;
            _cancel.Font = Theme.F(10f, FontStyle.Bold);
            _cancel.Bounds = new Rectangle(Theme.S(236), Theme.S(322), Theme.S(198), Theme.S(38));
            _cancel.Click += delegate { Close(); };
            Controls.Add(_cancel);

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

            _progress.Value = 0;
            _action.Enabled = false;
            _action.Text = "ЗАГРУЗКА…";
            _action.Glyph = "";
            _cancel.Text = "ОТМЕНА";
            _cancel.Enabled = true;
            SetStatus("Скачивание " + _manifest.Version + "…", Theme.Muted);

            try
            {
                var progress = new Progress<BatchProgress>(delegate(BatchProgress value)
                {
                    ShowProgress(value);
                });
                string staging = await UpdateService.DownloadAsync(_manifest, progress, null, ct);
                if (_closed || ct.IsCancellationRequested) return;

                SetStatus("Обновление готово. Перезапуск…", Theme.Green);
                _progress.Value = 1;
                UpdateService.ApplyAndRestart(_manifest, staging);
                RestartRequested = true;
                _closed = true;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (_closed) return;
                _action.Enabled = true;
                _action.Text = "ПОВТОРИТЬ";
                _action.Glyph = "\uE72C";
                _action.Style = RoundedButton.ButtonStyle.Accent;
                _cancel.Text = "ЗАКРЫТЬ";
                SetStatus(ex.Message, Theme.Red);
            }
        }

        private void ShowProgress(BatchProgress progress)
        {
            if (_closed || progress == null) return;
            _progress.Value = progress.Fraction;
            string text = progress.Status ?? "";
            if (progress.BytesTotal > 0)
            {
                int percent = (int)Math.Round(progress.Fraction * 100);
                text += " · " + percent + "% · " + BatchProgress.FormatMb(progress.BytesDone) +
                        " из " + BatchProgress.FormatMb(progress.BytesTotal);
            }
            else if (progress.FilesTotal > 0)
            {
                text += " · " + progress.FilesDone + " из " + progress.FilesTotal;
            }
            SetStatus(text, Theme.Muted);
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
    }
}
