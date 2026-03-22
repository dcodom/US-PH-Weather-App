using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WeatherApp
{
    public class USWeatherForm : Form
    {
        // ── Search row
        private StyledTextBox _cityBox = null!;
        private StyledTextBox _stateBox = null!;
        private GlowButton    _searchBtn = null!;
        private Label         _statusLabel = null!;

        // ── Current weather
        private Label _locationLabel = null!;
        private Label _tempLabel = null!;
        private Label _iconLabel = null!;
        private Label _descLabel = null!;
        private Label _feelsLabel = null!;
        private Label _updatedLabel = null!;

        // ── Stat cards
        private StatCard _humidCard = null!;
        private StatCard _windCard = null!;
        private StatCard _uvCard = null!;
        private StatCard _pressCard = null!;
        private StatCard _visCard = null!;
        private StatCard _precipCard = null!;

        // ── Forecast
        private FlowLayoutPanel _forecastPanel = null!;

        // ── Map
        private Microsoft.Web.WebView2.WinForms.WebView2 _mapBrowser = null!;
        private ComboBox   _mapLayerBox = null!;

        // ── State
        private WeatherData? _lastData;
        private readonly bool _useFahrenheit = true;

        // Gradient particles for background
        private System.Windows.Forms.Timer _animTimer = null!;
        private float _animPhase = 0;

        public USWeatherForm()
        {
            InitUI();
            this.Load += async (s, e) => await LoadDefaultCity();
        }

        private async Task LoadDefaultCity()
        {
            _cityBox.InnerBox.Text = "New York";
            _stateBox.InnerBox.Text = "NY";
            await DoSearch();
        }

        private void InitUI()
        {
            // Form setup
            Text = "⚡ US Weather Center";
            Size = new Size(1080, 900);
            MinimumSize = new Size(900, 780);
            BackColor = Theme.US_BgDark;
            ForeColor = Theme.US_Text;
            StartPosition = FormStartPosition.CenterScreen;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);

            // Animation timer for aurora background
            _animTimer = new System.Windows.Forms.Timer { Interval = 30 };
            _animTimer.Tick += (s, e) => { _animPhase += 0.01f; if (_animPhase > Math.PI * 2) _animPhase -= (float)(Math.PI * 2); Invalidate(new Rectangle(0, 0, Width, 80)); };
            _animTimer.Start();

            BuildUI();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            DrawAuroraHeader(e.Graphics);
        }

        private void DrawAuroraHeader(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            // Subtle animated aurora glow at top
            for (int i = 0; i < 3; i++)
            {
                float y = 30 + (float)Math.Sin(_animPhase + i * 1.2f) * 12;
                var rect = new RectangleF(-50 + i * 180, y, 400, 60);
                using var brush = new PathGradientBrush(new PointF[]
                {
                    new PointF(rect.Left, rect.Top + rect.Height / 2),
                    new PointF(rect.Left + rect.Width / 2, rect.Top),
                    new PointF(rect.Right, rect.Top + rect.Height / 2),
                    new PointF(rect.Left + rect.Width / 2, rect.Bottom),
                })
                {
                    CenterColor = Color.FromArgb(25, Theme.US_Accent),
                    SurroundColors = new[] { Color.Transparent, Color.Transparent, Color.Transparent, Color.Transparent }
                };
                g.FillEllipse(brush, rect);
            }
        }

        private void BuildUI()
        {
            int pad = 16;

            // ─── HEADER ──────────────────────────────────────────────────────────
            var header = new Label
            {
                Text = "🇺🇸  UNITED STATES WEATHER",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Theme.US_AccentGlow,
                AutoSize = true,
                Location = new Point(pad + 4, 18),
                BackColor = Color.Transparent
            };
            Controls.Add(header);

            var phBtn = new GlowButton
            {
                Text = "🌏  Open PH Weather",
                Size = new Size(175, 38),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                GlowColor = Color.FromArgb(0, 150, 136),
                Location = new Point(Width - 195, 18)
            };
            phBtn.Click += (s, e) => { new PHWeatherForm().Show(); this.Close(); };
            Controls.Add(phBtn);
            phBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            // ─── SEARCH ROW ──────────────────────────────────────────────────────
            var searchPanel = new RoundedPanel
            {
                BackColor = Theme.US_BgMid,
                BorderColor = Color.FromArgb(50, Theme.US_Accent),
                CornerRadius = 14,
                Location = new Point(pad, 62),
                Size = new Size(Width - pad * 2 - 1, 66)
            };
            searchPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(searchPanel);

            var lbl1 = new Label { Text = "City", ForeColor = Theme.US_TextDim, Font = new Font("Segoe UI", 8), AutoSize = true, Location = new Point(14, 8), BackColor = Color.Transparent };
            var lbl2 = new Label { Text = "State (abbr.)", ForeColor = Theme.US_TextDim, Font = new Font("Segoe UI", 8), AutoSize = true, Location = new Point(280, 8), BackColor = Color.Transparent };
            searchPanel.Controls.Add(lbl1);
            searchPanel.Controls.Add(lbl2);

            _cityBox = new StyledTextBox { AccentColor = Theme.US_Accent, Location = new Point(12, 22), Width = 240 };
            _cityBox.InnerBox.BackColor = Theme.US_BgCard;
            _cityBox.BackColor = Theme.US_BgCard;
            _cityBox.InnerBox.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _ = DoSearch(); } };
            searchPanel.Controls.Add(_cityBox);

            _stateBox = new StyledTextBox { AccentColor = Theme.US_Accent, Location = new Point(278, 22), Width = 100 };
            _stateBox.InnerBox.BackColor = Theme.US_BgCard;
            _stateBox.BackColor = Theme.US_BgCard;
            _stateBox.InnerBox.MaxLength = 2;
            _stateBox.InnerBox.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _ = DoSearch(); } };
            searchPanel.Controls.Add(_stateBox);

            _searchBtn = new GlowButton
            {
                Text = "🔍  Search",
                Size = new Size(130, 38),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                GlowColor = Theme.US_Accent,
                Location = new Point(394, 14)
            };
            _searchBtn.Click += (s, e) => _ = DoSearch();
            searchPanel.Controls.Add(_searchBtn);

            _statusLabel = new Label
            {
                Text = "",
                ForeColor = Theme.US_TextDim,
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Location = new Point(540, 24),
                BackColor = Color.Transparent
            };
            searchPanel.Controls.Add(_statusLabel);

            // ─── CURRENT WEATHER CARD ─────────────────────────────────────────
            var curCard = new RoundedPanel
            {
                BackColor = Theme.US_BgCard,
                BorderColor = Color.FromArgb(60, Theme.US_Accent),
                CornerRadius = 16,
                Location = new Point(pad, 140),
                Size = new Size(400, 215)
            };
            Controls.Add(curCard);

            _iconLabel = new Label
            {
                Text = "☀️",
                Font = new Font("Segoe UI Emoji", 28),
                ForeColor = Theme.US_AccentGlow,
                AutoSize = true,
                Location = new Point(14, 8),
                BackColor = Color.Transparent
            };
            curCard.Controls.Add(_iconLabel);

            _tempLabel = new Label
            {
                Text = "--°F",
                Font = new Font("Segoe UI Light", 52, FontStyle.Regular),
                ForeColor = Theme.US_Text,
                AutoSize = false,
                Width = 360,
                Height = 80,
                Location = new Point(14, 38),
                BackColor = Color.Transparent
            };
            curCard.Controls.Add(_tempLabel);

            _locationLabel = new Label
            {
                Text = "Enter a city above",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Theme.US_AccentGlow,
                AutoSize = false,
                Width = 370,
                Location = new Point(14, 122),
                BackColor = Color.Transparent
            };
            curCard.Controls.Add(_locationLabel);

            _descLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 10),
                ForeColor = Theme.US_TextDim,
                AutoSize = true,
                Location = new Point(14, 146),
                BackColor = Color.Transparent
            };
            curCard.Controls.Add(_descLabel);

            _feelsLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9),
                ForeColor = Theme.US_TextDim,
                AutoSize = true,
                Location = new Point(14, 166),
                BackColor = Color.Transparent
            };
            curCard.Controls.Add(_feelsLabel);

            _updatedLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(80, 120, 160),
                AutoSize = true,
                Location = new Point(14, 184),
                BackColor = Color.Transparent
            };
            curCard.Controls.Add(_updatedLabel);

            // ─── STAT CARDS ───────────────────────────────────────────────────
            _humidCard  = new StatCard("💧", "Humidity",    "--",  Theme.US_BgCard, Theme.US_Accent, Theme.US_Text, Theme.US_TextDim) { Location = new Point(pad + 408, 140) };
            _windCard   = new StatCard("💨", "Wind",        "--",  Theme.US_BgCard, Theme.US_Accent, Theme.US_Text, Theme.US_TextDim) { Location = new Point(pad + 408 + 185, 140) };
            _uvCard     = new StatCard("🌞", "UV Index",    "--",  Theme.US_BgCard, Theme.US_Accent, Theme.US_Text, Theme.US_TextDim) { Location = new Point(pad + 408 + 370, 140) };
            _pressCard  = new StatCard("🔵", "Pressure",   "--",  Theme.US_BgCard, Theme.US_Accent, Theme.US_Text, Theme.US_TextDim) { Location = new Point(pad + 408, 243) };
            _visCard    = new StatCard("👁️", "Visibility",  "--",  Theme.US_BgCard, Theme.US_Accent, Theme.US_Text, Theme.US_TextDim) { Location = new Point(pad + 408 + 185, 243) };
            _precipCard = new StatCard("🌧️", "Precip.",     "--",  Theme.US_BgCard, Theme.US_Accent, Theme.US_Text, Theme.US_TextDim) { Location = new Point(pad + 408 + 370, 243) };

            foreach (var sc in new[] { _humidCard, _windCard, _uvCard, _pressCard, _visCard, _precipCard })
                Controls.Add(sc);

            // ─── 7-DAY FORECAST ───────────────────────────────────────────────
            var fcTitle = new Label
            {
                Text = "7-DAY FORECAST",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Theme.US_TextDim,
                AutoSize = true,
                Location = new Point(pad + 4, 370),
                BackColor = Color.Transparent
            };
            Controls.Add(fcTitle);

            _forecastPanel = new FlowLayoutPanel
            {
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.LeftToRight,
                Location = new Point(pad, 390),
                Size = new Size(Width - pad * 2 - 2, 140),
                WrapContents = false,
                AutoScroll = false
            };
            _forecastPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(_forecastPanel);

            for (int i = 0; i < 7; i++)
            {
                var fc = new ForecastCard(Theme.US_BgCard, Theme.US_Accent, Theme.US_Text, Theme.US_TextDim);
                fc.Margin = new Padding(0, 0, 8, 0);
                _forecastPanel.Controls.Add(fc);
            }

            // ─── MAP SECTION ──────────────────────────────────────────────────
            var mapTitle = new Label
            {
                Text = "ANIMATED WEATHER MAP",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Theme.US_TextDim,
                AutoSize = true,
                Location = new Point(pad + 4, 544),
                BackColor = Color.Transparent
            };
            Controls.Add(mapTitle);

            _mapLayerBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9),
                BackColor = Theme.US_BgCard,
                ForeColor = Theme.US_Text,
                Location = new Point(Width - 220, 538),
                Width = 200,
                FlatStyle = FlatStyle.Flat
            };
            _mapLayerBox.Items.AddRange(new object[] { "Precipitation", "Wind Speed", "Temperature", "Clouds", "Radar" });
            _mapLayerBox.SelectedIndex = 0;
            _mapLayerBox.SelectedIndexChanged += (s, e) => UpdateMap();
            _mapLayerBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Controls.Add(_mapLayerBox);

            var mapCard = new RoundedPanel
            {
                BackColor = Theme.US_BgMid,
                BorderColor = Color.FromArgb(50, Theme.US_Accent),
                CornerRadius = 14,
                Location = new Point(pad, 566),
                Size = new Size(Width - pad * 2 - 2, 300)
            };
            mapCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            Controls.Add(mapCard);

            _mapBrowser = new Microsoft.Web.WebView2.WinForms.WebView2
            {
                Location = new Point(4, 4),
                Size = new Size(mapCard.Width - 8, mapCard.Height - 8),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right
            };
            mapCard.Controls.Add(_mapBrowser);
            mapCard.Resize += (s, e) => _mapBrowser.Size = new Size(mapCard.Width - 8, mapCard.Height - 8);
        }

        private async Task DoSearch()
        {
            string city = _cityBox.InnerBox.Text.Trim();
            string state = _stateBox.InnerBox.Text.Trim();
            if (string.IsNullOrEmpty(city))
            {
                _statusLabel.Text = "⚠ Enter a city name";
                return;
            }

            _searchBtn.Enabled = false;
            _statusLabel.ForeColor = Theme.US_TextDim;
            _statusLabel.Text = "⏳ Searching...";

            try
            {
                string query = city; // Pass city name ONLY — state used as a filter, not in query
                var geo = await WeatherService.GeocodeAsync(query, "US", state);
                if (geo == null)
                {
                    _statusLabel.ForeColor = Color.FromArgb(255, 80, 80);
                    _statusLabel.Text = "❌ Location not found";
                    return;
                }

                string locName = string.IsNullOrEmpty(geo.State)
                    ? $"{geo.Name}, {geo.Country}"
                    : $"{geo.Name}, {geo.State}";

                var data = await WeatherService.GetWeatherAsync(geo.Latitude, geo.Longitude, locName, _useFahrenheit);
                if (data == null)
                {
                    _statusLabel.ForeColor = Color.FromArgb(255, 80, 80);
                    _statusLabel.Text = "❌ Weather data unavailable";
                    return;
                }

                _lastData = data;
                UpdateDisplay(data);
                _statusLabel.ForeColor = Color.FromArgb(60, 200, 100);
                _statusLabel.Text = "✓ Data refreshed";
            }
            catch (Exception ex)
            {
                _statusLabel.ForeColor = Color.FromArgb(255, 80, 80);
                _statusLabel.Text = $"❌ {ex.Message}";
            }
            finally
            {
                _searchBtn.Enabled = true;
            }
        }

        private void UpdateDisplay(WeatherData d)
        {
            string unit = "°F";
            string wunit = "mph";

            _iconLabel.Text = d.Icon;
            _tempLabel.Text = $"{d.Temperature:0}{unit}";
            _locationLabel.Text = d.LocationName;
            _descLabel.Text = d.Description;
            _feelsLabel.Text = $"Feels like {d.FeelsLike:0}{unit}";
            _updatedLabel.Text = $"Updated {d.ObservationTime:hh:mm tt}";

            _humidCard.Update($"{d.Humidity}%");
            _windCard.Update($"{d.WindSpeed:0} {wunit}\n{d.WindDirection}");
            _uvCard.Update($"{d.UVIndex:0.0}");
            _pressCard.Update($"{d.Pressure:0} hPa");
            _visCard.Update($"{d.Visibility / 1609.34:0.0} mi");
            _precipCard.Update($"{d.Precipitation / 25.4:0.00} in");

            // Forecast
            for (int i = 0; i < _forecastPanel.Controls.Count && i < d.Forecast.Length; i++)
            {
                if (_forecastPanel.Controls[i] is ForecastCard fc)
                    fc.SetData(d.Forecast[i], _useFahrenheit);
            }

            UpdateMap();
        }

        private void UpdateMap()
        {
            if (_lastData == null) return;

            string[] layers = { "rain", "wind", "temp", "clouds", "radar" };
            int idx = _mapLayerBox.SelectedIndex < 0 ? 0 : _mapLayerBox.SelectedIndex;
            string layer = layers[Math.Min(idx, layers.Length - 1)];
            string url = WeatherService.GetMapUrl(_lastData.Latitude, _lastData.Longitude, layer);

            _mapBrowser.Source = new Uri(url);
        }
    }
}