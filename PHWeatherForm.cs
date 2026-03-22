using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WeatherApp
{
    public class PHWeatherForm : Form
    {
        // ── Search row
        private StyledTextBox _cityBox = null!;
        private StyledTextBox _regionBox = null!;
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

        // Animation
        private System.Windows.Forms.Timer _animTimer = null!;
        private float _animPhase = 0;

        // PH preset cities
        private static readonly (string City, string Region)[] PHCities =
        {
            ("Manila", "NCR"), ("Cebu City", "Cebu"), ("Davao City", "Davao del Sur"),
            ("Quezon City", "NCR"), ("Zamboanga City", "Zamboanga del Sur"),
            ("Baguio", "Benguet"), ("Iloilo City", "Iloilo"), ("Cagayan de Oro", "Misamis Oriental"),
            ("Tacloban", "Leyte"), ("Bacolod", "Negros Occidental"), ("General Santos", "South Cotabato"),
            ("Palawan", "Palawan"), ("Boracay", "Aklan"), ("Batangas", "Batangas"),
        };

        public PHWeatherForm()
        {
            InitUI();
            this.Load += async (s, e) => await LoadDefaultCity();
        }

        private async Task LoadDefaultCity()
        {
            _cityBox.InnerBox.Text = "Manila";
            _regionBox.InnerBox.Text = "NCR";
            await DoSearch();
        }

        private void InitUI()
        {
            Text = "🌺 Philippines Weather Center";
            Size = new Size(1080, 950);
            MinimumSize = new Size(900, 830);
            BackColor = Theme.PH_BgDark;
            ForeColor = Theme.PH_Text;
            StartPosition = FormStartPosition.CenterScreen;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);

            _animTimer = new System.Windows.Forms.Timer { Interval = 30 };
            _animTimer.Tick += (s, e) => { _animPhase += 0.008f; if (_animPhase > Math.PI * 2) _animPhase -= (float)(Math.PI * 2); Invalidate(new Rectangle(0, 0, Width, 80)); };
            _animTimer.Start();

            BuildUI();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            DrawTropicalHeader(e.Graphics);
        }

        private void DrawTropicalHeader(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            // Warm sunset glows
            Color[] glowColors = { Color.FromArgb(20, 255, 111, 60), Color.FromArgb(15, 255, 200, 80), Color.FromArgb(18, 255, 60, 120) };
            float[] offsets = { 0, 1.4f, 2.8f };
            for (int i = 0; i < 3; i++)
            {
                float y = 20 + (float)Math.Sin(_animPhase + offsets[i]) * 10;
                var rect = new RectangleF(-30 + i * 200, y, 380, 70);
                using var brush = new PathGradientBrush(new PointF[]
                {
                    new PointF(rect.Left, rect.Top + rect.Height / 2),
                    new PointF(rect.Left + rect.Width / 2, rect.Top),
                    new PointF(rect.Right, rect.Top + rect.Height / 2),
                    new PointF(rect.Left + rect.Width / 2, rect.Bottom),
                })
                {
                    CenterColor = glowColors[i],
                    SurroundColors = new[] { Color.Transparent, Color.Transparent, Color.Transparent, Color.Transparent }
                };
                g.FillEllipse(brush, rect);
            }
        }

        private void BuildUI()
        {
            int pad = 16;

            // ─── ROW 1: HEADER + BUTTON ───────────────────────────────────────────
            var header = new Label
            {
                Text = "🇵🇭  PHILIPPINES WEATHER",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Theme.PH_AccentGlow,
                AutoSize = true,
                Location = new Point(pad + 4, 14),
                BackColor = Color.Transparent
            };
            Controls.Add(header);

            var usBtn = new GlowButton
            {
                Text = "🇺🇸  Open US Weather",
                Size = new Size(175, 36),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                GlowColor = Color.FromArgb(41, 130, 200),
                Location = new Point(Width - 195, 14)
            };
            usBtn.Click += (s, e) => { new USWeatherForm().Show(); this.Close(); };
            usBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Controls.Add(usBtn);

            // ─── ROW 2: QUICK CITY DROPDOWN ──────────────────────────────────────
            var quickLabel = new Label
            {
                Text = "Quick City:",
                Font = new Font("Segoe UI", 9),
                ForeColor = Theme.PH_TextDim,
                AutoSize = true,
                Location = new Point(pad + 4, 64),
                BackColor = Color.Transparent
            };
            Controls.Add(quickLabel);

            var quickDropdown = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9),
                BackColor = Theme.PH_BgCard,
                ForeColor = Theme.PH_Text,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(pad + 84, 60),
                Width = 220,
                Cursor = Cursors.Hand
            };
            quickDropdown.Items.Add("— Select a city —");
            foreach (var (city, _) in PHCities)
                quickDropdown.Items.Add(city);
            quickDropdown.SelectedIndex = 0;
            quickDropdown.MaxDropDownItems = 10; // scrollbar appears automatically for remaining cities
            quickDropdown.SelectedIndexChanged += (s, e) =>
            {
                int idx = quickDropdown.SelectedIndex - 1; // offset for placeholder
                if (idx < 0 || idx >= PHCities.Length) return;
                var (city, region) = PHCities[idx];
                _cityBox.InnerBox.Text = city;
                _regionBox.InnerBox.Text = region;
                _ = DoSearch();
            };
            quickDropdown.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            Controls.Add(quickDropdown);

            // ─── ROW 3: SEARCH PANEL ─────────────────────────────────────────────
            var searchPanel = new RoundedPanel
            {
                BackColor = Theme.PH_BgMid,
                BorderColor = Color.FromArgb(50, Theme.PH_Accent),
                CornerRadius = 14,
                Location = new Point(pad, 96),
                Size = new Size(Width - pad * 2 - 1, 66)
            };
            searchPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(searchPanel);

            var lbl1 = new Label { Text = "City / Municipality", ForeColor = Theme.PH_TextDim, Font = new Font("Segoe UI", 8), AutoSize = true, Location = new Point(14, 8), BackColor = Color.Transparent };
            var lbl2 = new Label { Text = "Province / Region", ForeColor = Theme.PH_TextDim, Font = new Font("Segoe UI", 8), AutoSize = true, Location = new Point(280, 8), BackColor = Color.Transparent };
            searchPanel.Controls.Add(lbl1);
            searchPanel.Controls.Add(lbl2);

            _cityBox = new StyledTextBox { AccentColor = Theme.PH_Accent, Location = new Point(12, 22), Width = 240 };
            _cityBox.InnerBox.BackColor = Theme.PH_BgCard;
            _cityBox.BackColor = Theme.PH_BgCard;
            _cityBox.InnerBox.ForeColor = Theme.PH_Text;
            _cityBox.InnerBox.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _ = DoSearch(); } };
            searchPanel.Controls.Add(_cityBox);

            _regionBox = new StyledTextBox { AccentColor = Theme.PH_Accent, Location = new Point(278, 22), Width = 160 };
            _regionBox.InnerBox.BackColor = Theme.PH_BgCard;
            _regionBox.BackColor = Theme.PH_BgCard;
            _regionBox.InnerBox.ForeColor = Theme.PH_Text;
            _regionBox.InnerBox.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _ = DoSearch(); } };
            searchPanel.Controls.Add(_regionBox);

            _searchBtn = new GlowButton
            {
                Text = "🔍  Search",
                Size = new Size(130, 38),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                GlowColor = Theme.PH_Accent,
                Location = new Point(454, 14)
            };
            _searchBtn.Click += (s, e) => _ = DoSearch();
            searchPanel.Controls.Add(_searchBtn);

            _statusLabel = new Label
            {
                Text = "",
                ForeColor = Theme.PH_TextDim,
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Location = new Point(600, 24),
                BackColor = Color.Transparent
            };
            searchPanel.Controls.Add(_statusLabel);

            // ─── CURRENT WEATHER CARD ─────────────────────────────────────────
            var curCard = new RoundedPanel
            {
                BackColor = Theme.PH_BgCard,
                BorderColor = Color.FromArgb(60, Theme.PH_Accent),
                CornerRadius = 16,
                Location = new Point(pad, 174),
                Size = new Size(400, 215)
            };
            Controls.Add(curCard);

            _iconLabel = new Label
            {
                Text = "🌺",
                Font = new Font("Segoe UI Emoji", 28),
                ForeColor = Theme.PH_AccentGlow,
                AutoSize = true,
                Location = new Point(14, 8),
                BackColor = Color.Transparent
            };
            curCard.Controls.Add(_iconLabel);

            _tempLabel = new Label
            {
                Text = "--°C",
                Font = new Font("Segoe UI Light", 52, FontStyle.Regular),
                ForeColor = Theme.PH_Text,
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
                ForeColor = Theme.PH_AccentGlow,
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
                ForeColor = Theme.PH_TextDim,
                AutoSize = true,
                Location = new Point(14, 146),
                BackColor = Color.Transparent
            };
            curCard.Controls.Add(_descLabel);

            _feelsLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9),
                ForeColor = Theme.PH_TextDim,
                AutoSize = true,
                Location = new Point(14, 166),
                BackColor = Color.Transparent
            };
            curCard.Controls.Add(_feelsLabel);

            _updatedLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(80, 130, 110),
                AutoSize = true,
                Location = new Point(14, 184),
                BackColor = Color.Transparent
            };
            curCard.Controls.Add(_updatedLabel);

            // ─── STAT CARDS ───────────────────────────────────────────────────
            _humidCard  = new StatCard("💧", "Humidity",   "--", Theme.PH_BgCard, Theme.PH_Accent, Theme.PH_Text, Theme.PH_TextDim) { Location = new Point(pad + 408, 174) };
            _windCard   = new StatCard("💨", "Wind",       "--", Theme.PH_BgCard, Theme.PH_Accent, Theme.PH_Text, Theme.PH_TextDim) { Location = new Point(pad + 408 + 185, 174) };
            _uvCard     = new StatCard("🌞", "UV Index",   "--", Theme.PH_BgCard, Theme.PH_Accent, Theme.PH_Text, Theme.PH_TextDim) { Location = new Point(pad + 408 + 370, 174) };
            _pressCard  = new StatCard("🔵", "Pressure",  "--", Theme.PH_BgCard, Theme.PH_Accent, Theme.PH_Text, Theme.PH_TextDim) { Location = new Point(pad + 408, 277) };
            _visCard    = new StatCard("👁️", "Visibility", "--", Theme.PH_BgCard, Theme.PH_Accent, Theme.PH_Text, Theme.PH_TextDim) { Location = new Point(pad + 408 + 185, 277) };
            _precipCard = new StatCard("🌧️", "Precip.",    "--", Theme.PH_BgCard, Theme.PH_Accent, Theme.PH_Text, Theme.PH_TextDim) { Location = new Point(pad + 408 + 370, 277) };

            foreach (var sc in new[] { _humidCard, _windCard, _uvCard, _pressCard, _visCard, _precipCard })
                Controls.Add(sc);

            // PAGASA / Typhoon Alert bar
            var alertBar = new RoundedPanel
            {
                BackColor = Color.FromArgb(30, 255, 111, 60),
                BorderColor = Color.FromArgb(80, Theme.PH_Accent),
                CornerRadius = 10,
                Location = new Point(pad + 408, 371),
                Size = new Size(Width - pad - 408 - 16, 44)
            };
            alertBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(alertBar);

            var alertLabel = new Label
            {
                Text = "🌀  PAGASA: Check pagasa.dost.gov.ph for typhoon bulletins & PSWS warnings",
                Font = new Font("Segoe UI", 9),
                ForeColor = Theme.PH_AccentGlow,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(10, 0, 0, 0)
            };
            alertBar.Controls.Add(alertLabel);

            // ─── 7-DAY FORECAST ───────────────────────────────────────────────
            var fcTitle = new Label
            {
                Text = "7-DAY FORECAST",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Theme.PH_TextDim,
                AutoSize = true,
                Location = new Point(pad + 4, 427),
                BackColor = Color.Transparent
            };
            Controls.Add(fcTitle);

            _forecastPanel = new FlowLayoutPanel
            {
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.LeftToRight,
                Location = new Point(pad, 447),
                Size = new Size(Width - pad * 2 - 2, 140),
                WrapContents = false,
                AutoScroll = false
            };
            _forecastPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(_forecastPanel);

            for (int i = 0; i < 7; i++)
            {
                var fc = new ForecastCard(Theme.PH_BgCard, Theme.PH_Accent, Theme.PH_Text, Theme.PH_TextDim);
                fc.Margin = new Padding(0, 0, 8, 0);
                _forecastPanel.Controls.Add(fc);
            }

            // ─── MAP SECTION ──────────────────────────────────────────────────
            var mapTitle = new Label
            {
                Text = "ANIMATED WEATHER MAP",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Theme.PH_TextDim,
                AutoSize = true,
                Location = new Point(pad + 4, 600),
                BackColor = Color.Transparent
            };
            Controls.Add(mapTitle);

            _mapLayerBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9),
                BackColor = Theme.PH_BgCard,
                ForeColor = Theme.PH_Text,
                Location = new Point(Width - 220, 595),
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
                BackColor = Theme.PH_BgMid,
                BorderColor = Color.FromArgb(50, Theme.PH_Accent),
                CornerRadius = 14,
                Location = new Point(pad, 620),
                Size = new Size(Width - pad * 2 - 2, 270)
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
            if (string.IsNullOrEmpty(city))
            {
                _statusLabel.Text = "⚠ Enter a city name";
                return;
            }

            _searchBtn.Enabled = false;
            _statusLabel.ForeColor = Theme.PH_TextDim;
            _statusLabel.Text = "⏳ Searching...";

            try
            {
                string region = _regionBox.InnerBox.Text.Trim();
                // Pass city name ONLY to geocoder — region used as a filter, not in the query string
                var geo = await WeatherService.GeocodeAsync(city, "PH", region);
                if (geo == null)
                {
                    _statusLabel.ForeColor = Color.FromArgb(255, 80, 80);
                    _statusLabel.Text = "❌ Location not found";
                    return;
                }

                string locName = string.IsNullOrEmpty(geo.State)
                    ? $"{geo.Name}, Philippines"
                    : $"{geo.Name}, {geo.State}";

                var data = await WeatherService.GetWeatherAsync(geo.Latitude, geo.Longitude, locName, false);
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
            string unit = "°C";
            string wunit = "km/h";

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
            _visCard.Update($"{d.Visibility / 1000.0:0.0} km");
            _precipCard.Update($"{d.Precipitation:0.0} mm");

            for (int i = 0; i < _forecastPanel.Controls.Count && i < d.Forecast.Length; i++)
            {
                if (_forecastPanel.Controls[i] is ForecastCard fc)
                    fc.SetData(d.Forecast[i], false);
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