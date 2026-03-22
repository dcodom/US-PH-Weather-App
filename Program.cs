using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WeatherApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LauncherForm());
        }
    }

    // ─── LAUNCHER SPLASH ────────────────────────────────────────────────────────
    public class LauncherForm : Form
    {
        private System.Windows.Forms.Timer _fadeTimer = null!;
        private float _fadeAlpha = 0f;

        public LauncherForm()
        {
            Text = "Weather Center — Launcher";
            Size = new Size(540, 420);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(8, 14, 30);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);

            BuildUI();

            _fadeTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _fadeTimer.Tick += (s, e) => { _fadeAlpha = Math.Min(1f, _fadeAlpha + 0.03f); Invalidate(); if (_fadeAlpha >= 1f) _fadeTimer.Stop(); };
            _fadeTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // Radial gradient bg
            using var bgBrush = new PathGradientBrush(new PointF[] {
                new PointF(0, 0), new PointF(Width, 0), new PointF(Width, Height), new PointF(0, Height)
            })
            {
                CenterPoint = new PointF(Width / 2f, Height / 2f),
                CenterColor = Color.FromArgb(22, 30, 70),
                SurroundColors = new[] { Color.FromArgb(8, 14, 30) }
            };
            e.Graphics.FillRectangle(bgBrush, 0, 0, Width, Height);
        }

        private void BuildUI()
        {
            var titleLabel = new Label
            {
                Text = "🌦️  Weather Center",
                Font = new Font("Segoe UI Light", 26, FontStyle.Regular),
                ForeColor = Color.FromArgb(220, 240, 255),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            titleLabel.Location = new Point((540 - 340) / 2, 50);
            Controls.Add(titleLabel);

            var subLabel = new Label
            {
                Text = "Real-time weather · Animated maps · 7-day forecasts",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(100, 140, 190),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            subLabel.Location = new Point((540 - 330) / 2, 98);
            Controls.Add(subLabel);

            // Divider
            var divider = new Panel
            {
                BackColor = Color.FromArgb(30, 80, 140),
                Size = new Size(400, 1),
                Location = new Point(70, 125)
            };
            Controls.Add(divider);

            // US Card
            var usCard = CreateLaunchCard(
                "🇺🇸", "United States Weather",
                "Search any US city · °F · mph · Windy maps",
                Color.FromArgb(20, 35, 80), Color.FromArgb(41, 182, 246),
                new Point(60, 148),
                onClick: () => new USWeatherForm().Show()
            );
            usCard.MouseEnter += (s, e) => { usCard.BorderColor = Color.FromArgb(41, 182, 246); usCard.Invalidate(); };
            usCard.MouseLeave += (s, e) => { usCard.BorderColor = Color.FromArgb(50, 41, 182, 246); usCard.Invalidate(); };
            Controls.Add(usCard);

            // PH Card
            var phCard = CreateLaunchCard(
                "🇵🇭", "Philippines Weather",
                "Search any PH city · °C · km/h · PAGASA aware",
                Color.FromArgb(20, 40, 35), Color.FromArgb(255, 111, 60),
                new Point(60, 260),
                onClick: () => new PHWeatherForm().Show()
            );
            phCard.MouseEnter += (s, e) => { phCard.BorderColor = Color.FromArgb(255, 111, 60); phCard.Invalidate(); };
            phCard.MouseLeave += (s, e) => { phCard.BorderColor = Color.FromArgb(50, 255, 111, 60); phCard.Invalidate(); };
            Controls.Add(phCard);

            var footer = new Label
            {
                Text = "Powered by Open-Meteo · No API key required · Free & open data",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(60, 90, 130),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            footer.Location = new Point((540 - 310) / 2, 374);
            Controls.Add(footer);
        }

        private RoundedPanel CreateLaunchCard(string flag, string title, string subtitle,
            Color bg, Color accent, Point location, Action onClick)
        {
            var card = new RoundedPanel
            {
                BackColor = bg,
                BorderColor = Color.FromArgb(50, accent),
                CornerRadius = 14,
                Location = location,
                Size = new Size(418, 90),
                Cursor = Cursors.Hand
            };

            var flagLbl = new Label
            {
                Text = flag,
                Font = new Font("Segoe UI Emoji", 28),
                ForeColor = accent,
                AutoSize = true,
                Location = new Point(16, 18),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            card.Controls.Add(flagLbl);

            var titleLbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(230, 240, 255),
                AutoSize = true,
                Location = new Point(74, 18),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            card.Controls.Add(titleLbl);

            var subLbl = new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(130, 160, 190),
                AutoSize = true,
                Location = new Point(74, 46),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            card.Controls.Add(subLbl);

            var arrow = new Label
            {
                Text = "→",
                Font = new Font("Segoe UI", 16),
                ForeColor = accent,
                AutoSize = true,
                Location = new Point(374, 28),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            card.Controls.Add(arrow);

            // Wire the shared Action to the card and every child label directly
            card.Click     += (s, e) => onClick();
            flagLbl.Click  += (s, e) => onClick();
            titleLbl.Click += (s, e) => onClick();
            subLbl.Click   += (s, e) => onClick();
            arrow.Click    += (s, e) => onClick();

            return card;
        }
    }
}