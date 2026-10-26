using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Vexor.Core;

namespace Vexor.Ui
{
    public class VersionListView : Control
    {
        private List<VersionEntry> _items = new List<VersionEntry>();
        private int _scroll;
        private int _selectedIndex = -1;
        private int _hoverIndex = -1;

        public VersionListView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            TabStop = true;
        }

        public event EventHandler SelectionChanged;

        public VersionEntry SelectedEntry
        {
            get
            {
                if (_selectedIndex >= 0 && _selectedIndex < _items.Count) return _items[_selectedIndex];
                return null;
            }
        }

        public int SelectedIndex
        {
            get { return _selectedIndex; }
        }

        public int Count
        {
            get { return _items.Count; }
        }

        private int RowHeight
        {
            get { return Theme.S(56); }
        }

        public void SetItems(List<VersionEntry> items)
        {
            _items = items ?? new List<VersionEntry>();
            _selectedIndex = -1;
            _scroll = 0;
            _hoverIndex = -1;
            Invalidate();
        }

        public bool SelectId(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            for (int i = 0; i < _items.Count; i++)
            {
                if (string.Equals(_items[i].Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    SelectIndex(i);
                    return true;
                }
            }
            return false;
        }

        public void SelectIndex(int index)
        {
            if (index < 0 || index >= _items.Count) return;
            bool changed = _selectedIndex != index;
            _selectedIndex = index;
            ScrollToSelection();
            Invalidate();
            if (changed && SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
        }

        public void ScrollToSelection()
        {
            if (_selectedIndex < 0) return;
            int top = _selectedIndex * RowHeight;
            int bottom = top + RowHeight;
            int visible = Height - Theme.S(8);
            if (top < _scroll) _scroll = top;
            else if (bottom > _scroll + visible) _scroll = bottom - visible;
            if (_scroll < 0) _scroll = 0;
            // Keep the viewport row-aligned so a partial row never peeks at the top.
            _scroll = (_scroll + RowHeight / 2) / RowHeight * RowHeight;
            if (_scroll > MaxScroll) _scroll = MaxScroll;
            Invalidate();
        }

        private int MaxScroll
        {
            get
            {
                int content = _items.Count * RowHeight + Theme.S(8);
                int max = content - Height;
                return max > 0 ? max : 0;
            }
        }

        private int IndexAt(int y)
        {
            if (y < 0 || y > Height) return -1;
            int index = (y + _scroll - Theme.S(4)) / RowHeight;
            if (index < 0 || index >= _items.Count) return -1;
            return index;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            Focus();
            int index = IndexAt(e.Y);
            if (index >= 0) SelectIndex(index);
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int index = IndexAt(e.Y);
            if (index != _hoverIndex)
            {
                _hoverIndex = index;
                Cursor = index >= 0 ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (_hoverIndex != -1)
            {
                _hoverIndex = -1;
                Cursor = Cursors.Default;
                Invalidate();
            }
            base.OnMouseLeave(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            _scroll -= (e.Delta / 120) * RowHeight * 2;
            if (_scroll < 0) _scroll = 0;
            if (_scroll > MaxScroll) _scroll = MaxScroll;
            Invalidate();
            base.OnMouseWheel(e);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            if (key == Keys.Up || key == Keys.Down || key == Keys.PageUp || key == Keys.PageDown ||
                key == Keys.Home || key == Keys.End) return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_items.Count > 0)
            {
                int index = _selectedIndex < 0 ? 0 : _selectedIndex;
                if (e.KeyCode == Keys.Down) index++;
                else if (e.KeyCode == Keys.Up) index--;
                else if (e.KeyCode == Keys.PageDown) index += 8;
                else if (e.KeyCode == Keys.PageUp) index -= 8;
                else if (e.KeyCode == Keys.Home) index = 0;
                else if (e.KeyCode == Keys.End) index = _items.Count - 1;
                else
                {
                    base.OnKeyDown(e);
                    return;
                }
                if (index < 0) index = 0;
                if (index >= _items.Count) index = _items.Count - 1;
                SelectIndex(index);
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Quality(g);

            if (_scroll > MaxScroll) _scroll = MaxScroll;
            if (_scroll < 0) _scroll = 0;

            if (_items.Count == 0)
            {
                TextRenderer.DrawText(g, "Нет версий", Theme.F(12f), ClientRectangle,
                    Theme.Muted2, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                return;
            }

            int rowHeight = RowHeight;
            int startY = Theme.S(4) - _scroll;

            for (int i = 0; i < _items.Count; i++)
            {
                int y = startY + i * rowHeight;
                if (y + rowHeight < 0) continue;
                if (y > Height) break;

                VersionEntry entry = _items[i];
                bool selected = i == _selectedIndex;
                bool hover = i == _hoverIndex && !selected;
                var row = new Rectangle(Theme.S(6), y, Width - Theme.S(12), rowHeight - Theme.S(4));
                float radius = Theme.Sf(11f);

                if (selected)
                {
                    Theme.FillRound(g, row, radius, Theme.SurfaceAlt);
                    Theme.DrawRound(g, row, radius, Theme.Alpha(Theme.Accent, 120), Theme.Sf(1.2f));
                    Theme.FillRound(g,
                        new RectangleF(row.X + Theme.S(2), row.Y + Theme.S(12), Theme.S(3), row.Height - Theme.S(24)),
                        Theme.Sf(1.5f), Theme.Accent);
                }
                else if (hover)
                {
                    Theme.FillRound(g, row, radius, Color.FromArgb(12, 255, 255, 255));
                }

                int textLeft = row.X + Theme.S(18);
                int textWidth = row.Width - Theme.S(72);

                TextRenderer.DrawText(g, entry.DisplayNameOrId, Theme.F(13.5f, FontStyle.Bold),
                    new Rectangle(textLeft, row.Y + Theme.S(7), textWidth, Theme.S(20)),
                    selected ? Color.White : Theme.Text,
                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

                Color typeColor = TypeColor(entry.Type);
                var dot = new RectangleF(textLeft, row.Y + Theme.S(34), Theme.Sf(6f), Theme.Sf(6f));
                using (var dotBrush = new SolidBrush(typeColor))
                    g.FillEllipse(dotBrush, dot);

                string meta = entry.TypeLabel + "  ·  " + entry.ReleaseDateText;
                TextRenderer.DrawText(g, meta, Theme.F(10.5f),
                    new Rectangle(textLeft + Theme.S(13), row.Y + Theme.S(28), textWidth, Theme.S(18)),
                    Theme.Muted, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

                if (selected)
                {
                    float centerX = row.Right - Theme.Sf(25f);
                    float centerY = row.Y + row.Height / 2f;
                    float radiusCircle = Theme.Sf(9f);
                    var circle = new RectangleF(centerX - radiusCircle, centerY - radiusCircle, radiusCircle * 2, radiusCircle * 2);
                    Theme.FillRound(g, circle, radiusCircle, Theme.Alpha(Theme.Accent, 52));
                    Theme.DrawRound(g, circle, radiusCircle, Theme.Accent, Theme.Sf(1.2f));
                    using (var brush = new SolidBrush(Theme.AccentLight))
                    using (var format = new StringFormat())
                    {
                        format.Alignment = StringAlignment.Center;
                        format.LineAlignment = StringAlignment.Center;
                        g.DrawString("\uE73E", Theme.Icon(9f), brush,
                            new RectangleF(circle.X, circle.Y, circle.Width, circle.Height), format);
                    }
                }
                else if (hover)
                {
                    float centerX = row.Right - Theme.Sf(25f);
                    float centerY = row.Y + row.Height / 2f;
                    using (var brush = new SolidBrush(Theme.Muted))
                    using (var format = new StringFormat())
                    {
                        format.Alignment = StringAlignment.Center;
                        format.LineAlignment = StringAlignment.Center;
                        g.DrawString("\uE768", Theme.Icon(12f), brush,
                            new RectangleF(centerX - Theme.S(12), centerY - Theme.S(12), Theme.S(24), Theme.S(24)), format);
                    }
                }
            }

            if (MaxScroll > 0)
            {
                float content = _items.Count * RowHeight + Theme.S(8);
                float trackHeight = Height - Theme.S(8);
                float thumbHeight = Math.Max(Theme.Sf(28f), trackHeight * Height / content);
                float thumbY = Theme.S(4) + (trackHeight - thumbHeight) * (_scroll / (float)MaxScroll);
                var thumb = new RectangleF(Width - Theme.S(6), thumbY, Theme.S(3), thumbHeight);
                Theme.FillRound(g, thumb, Theme.Sf(1.5f), Theme.Alpha(Theme.Border, 230));
            }
        }

        private static Color TypeColor(string type)
        {
            if (type == "release") return Theme.Green;
            if (type == "fabric") return Theme.AccentLight;
            if (type == "forge") return Theme.Yellow;
            if (type == "snapshot") return Theme.AccentLight;
            if (type == "old_beta" || type == "old_alpha") return Theme.Yellow;
            return Theme.Muted;
        }
    }
}
