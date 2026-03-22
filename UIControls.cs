using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WeatherApp
{
    // ─── THEME ──────────────────────────────────────────────────────────────────
    public static class Theme
    {
        // US Form – deep navy / arctic blue
        public static readonly Color US_BgDark      = Color.FromArgb(10, 15, 35);
        public static readonly Color US_BgMid       = Color.FromArgb(18, 28, 60);
        public static readonly Color US_BgCard      = Color.FromArgb(22, 38, 80);
        public static readonly Color US_Accent      = Color.FromArgb(41, 182, 246);
        public static readonly Color US_AccentGlow  = Color.FromArgb(0, 229, 255);
        public static readonly Color US_Text        = Color.FromArgb(220, 240, 255);
        public static readonly Color US_TextDim     = Color.FromArgb(120, 160, 200);
        public static readonly Color US_Border      = Color.FromArgb(41, 182, 246, 60);

        // PH Form – tropical sunset: deep teal / coral
        public static readonly Color PH_BgDark      = Color.FromArgb(8, 28, 35);
        public static readonly Color PH_BgMid       = Color.FromArgb(12, 45, 55);
        public static readonly Color PH_BgCard      = Color.FromArgb(15, 58, 70);
        public static readonly Color PH_Accent      = Color.FromArgb(255, 111, 60);
        public static readonly Color PH_AccentGlow  = Color.FromArgb(255, 200, 80);
        public static readonly Color PH_Text        = Color.FromArgb(255, 245, 230);
        public static readonly Color PH_TextDim     = Color.FromArgb(160, 200, 190);
        public static readonly Color PH_Border      = Color.FromArgb(255, 111, 60);

        public static Font FontTitle(float size) => new Font("Segoe UI", size, FontStyle.Bold);
        public static Font FontBody(float size) => new Font("Segoe UI", size, FontStyle.Regular);
        public static Font FontLight(float size) => new Font("Segoe UI Light", size, FontStyle.Regular);
        public static Font FontMono(float size) => new Font("Consolas", size, FontStyle.Regular);
    }

    // ─── ROUNDED PANEL ──────────────────────────────────────────────────────────
    public class RoundedPanel : Panel
    {
        public int CornerRadius { get; set; } = 14;
        public Color BorderColor { get; set; } = Color.Transparent;
        public int BorderWidth { get; set; } = 1;

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(1, 1, Width - 2, Height - 2);
            using var path = RoundedRect(rect, CornerRadius);
            using var brush = new SolidBrush(BackColor);
            e.Graphics.FillPath(brush, path);
            if (BorderColor != Color.Transparent)
            {
                using var pen = new Pen(BorderColor, BorderWidth);
                e.Graphics.DrawPath(pen, path);
            }
        }

        public static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.Left, r.Top, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // ─── GLOWING BUTTON ─────────────────────────────────────────────────────────
    public class GlowButton : Button
    {
        public Color GlowColor { get; set; } = Color.FromArgb(41, 182, 246);
        private bool _hovered = false;
        private bool _pressed = false;

        public GlowButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }

        protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _pressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = RoundedPanel.RoundedRect(rect, 10);

            Color bg = _pressed
                ? ControlPaint.Dark(GlowColor, 0.2f)
                : _hovered
                    ? ControlPaint.Light(GlowColor, 0.1f)
                    : GlowColor;

            using var brush = new SolidBrush(bg);
            e.Graphics.FillPath(brush, path);

            if (_hovered && !_pressed)
            {
                var glowRect = new Rectangle(-4, -4, Width + 8, Height + 8);
                using var glowPath = RoundedPanel.RoundedRect(glowRect, 14);
                using var glowBrush = new PathGradientBrush(glowPath)
                {
                    CenterColor = Color.FromArgb(60, GlowColor),
                    SurroundColors = new[] { Color.Transparent }
                };
                e.Graphics.FillPath(glowBrush, glowPath);
            }

            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var textBrush = new SolidBrush(ForeColor);
            e.Graphics.DrawString(Text, Font, textBrush, new RectangleF(0, 0, Width, Height), sf);
        }
    }

    // ─── STYLED TEXT BOX ────────────────────────────────────────────────────────
    public class StyledTextBox : Panel
    {
        public TextBox InnerBox { get; }
        public Color AccentColor { get; set; } = Color.FromArgb(41, 182, 246);
        private bool _focused = false;

        public StyledTextBox()
        {
            // Create InnerBox BEFORE setting Height, because setting Height
            // triggers OnLayout which references InnerBox.
            InnerBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(22, 38, 80),
                ForeColor = Color.FromArgb(220, 240, 255),
                Font = new Font("Segoe UI", 11),
                Dock = DockStyle.None
            };
            Controls.Add(InnerBox);
            InnerBox.GotFocus += (s, e) => { _focused = true; Invalidate(); };
            InnerBox.LostFocus += (s, e) => { _focused = false; Invalidate(); };
            Padding = new Padding(12, 0, 12, 0);
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            Height = 42; // Set AFTER InnerBox is ready
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (InnerBox == null) return; // Guard against layout during base ctor
            InnerBox.Width = Math.Max(1, Width - 28);
            InnerBox.Top = (Height - InnerBox.Height) / 2;
            InnerBox.Left = 14;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = RoundedPanel.RoundedRect(rect, 10);
            using var bg = new SolidBrush(Color.FromArgb(22, 38, 80));
            e.Graphics.FillPath(bg, path);

            Color borderColor = _focused ? AccentColor : Color.FromArgb(50, 80, 130);
            int borderWidth = _focused ? 2 : 1;
            using var pen = new Pen(borderColor, borderWidth);
            e.Graphics.DrawPath(pen, path);

            if (_focused)
            {
                using var glowPen = new Pen(Color.FromArgb(40, AccentColor), 4);
                e.Graphics.DrawPath(glowPen, path);
            }
        }
    }

    // ─── WEATHER STAT CARD ──────────────────────────────────────────────────────
    public class StatCard : RoundedPanel
    {
        private readonly Label _iconLabel;
        private readonly Label _valueLabel;
        private readonly Label _titleLabel;

        public StatCard(string icon, string title, string value, Color cardBg, Color accent, Color text, Color textDim)
        {
            Size = new Size(175, 90);
            BackColor = cardBg;
            BorderColor = Color.FromArgb(40, accent);
            CornerRadius = 12;

            _iconLabel  = new Label { Text = icon, Font = new Font("Segoe UI Emoji", 18), ForeColor = accent, AutoSize = true, Location = new Point(10, 12), BackColor = Color.Transparent };
            _valueLabel = new Label { Text = value, Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = text, AutoSize = false, Width = 120, Height = 26, Location = new Point(50, 12), BackColor = Color.Transparent };
            _titleLabel = new Label { Text = title, Font = new Font("Segoe UI", 8), ForeColor = textDim, AutoSize = false, Width = 150, Height = 18, Location = new Point(10, 58), BackColor = Color.Transparent };

            Controls.Add(_iconLabel);
            Controls.Add(_valueLabel);
            Controls.Add(_titleLabel);
        }

        public void Update(string value) => _valueLabel.Text = value;
    }

    // ─── FORECAST CARD ──────────────────────────────────────────────────────────
    public class ForecastCard : RoundedPanel
    {
        private readonly Label _dayLabel;
        private readonly Label _iconLabel;
        private readonly Label _highLabel;
        private readonly Label _lowLabel;
        private readonly Label _descLabel;

        public ForecastCard(Color cardBg, Color accent, Color text, Color textDim)
        {
            Size = new Size(100, 130);
            BackColor = cardBg;
            BorderColor = Color.FromArgb(30, accent);
            CornerRadius = 10;

            _dayLabel  = new Label { Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = accent, AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.None, Width = 90, Height = 20, Left = 5, Top = 6, BackColor = Color.Transparent };
            _iconLabel = new Label { Font = new Font("Segoe UI Emoji", 20), ForeColor = text, AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Width = 90, Height = 34, Left = 5, Top = 26, BackColor = Color.Transparent };
            _highLabel = new Label { Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = text, AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Width = 90, Height = 20, Left = 5, Top = 60, BackColor = Color.Transparent };
            _lowLabel  = new Label { Font = new Font("Segoe UI", 9), ForeColor = textDim, AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Width = 90, Height = 18, Left = 5, Top = 78, BackColor = Color.Transparent };
            _descLabel = new Label { Font = new Font("Segoe UI", 7), ForeColor = textDim, AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Width = 90, Height = 28, Left = 5, Top = 98, BackColor = Color.Transparent };

            Controls.AddRange(new Control[] { _dayLabel, _iconLabel, _highLabel, _lowLabel, _descLabel });
        }

        public void SetData(ForecastDay day, bool fahrenheit)
        {
            string unit = fahrenheit ? "°F" : "°C";
            _dayLabel.Text = day.Date.Date == DateTime.Today ? "Today" : day.Date.ToString("ddd");
            _iconLabel.Text = day.Icon;
            _highLabel.Text = $"{day.TempMax:0}{unit}";
            _lowLabel.Text = $"{day.TempMin:0}{unit}";
            _descLabel.Text = day.Description;
        }
    }
}