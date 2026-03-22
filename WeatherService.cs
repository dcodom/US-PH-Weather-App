using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace WeatherApp
{
    public class WeatherData
    {
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public int Humidity { get; set; }
        public double WindSpeed { get; set; }
        public string WindDirection { get; set; } = "";
        public double Precipitation { get; set; }
        public int WeatherCode { get; set; }
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string LocationName { get; set; } = "";
        public double UVIndex { get; set; }
        public int Visibility { get; set; }
        public double Pressure { get; set; }
        public bool IsDay { get; set; }
        public DateTime ObservationTime { get; set; }
        public ForecastDay[] Forecast { get; set; } = Array.Empty<ForecastDay>();
    }

    public class ForecastDay
    {
        public DateTime Date { get; set; }
        public double TempMax { get; set; }
        public double TempMin { get; set; }
        public int WeatherCode { get; set; }
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
        public double PrecipitationSum { get; set; }
    }

    public class GeoLocation
    {
        public string Name { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Country { get; set; } = "";
        public string State { get; set; } = "";
    }

    public static class WeatherService
    {
        private static readonly HttpClient _http = new HttpClient();

        static WeatherService()
        {
            _http.DefaultRequestHeaders.Add("User-Agent", "WeatherApp/1.0");
            _http.Timeout = TimeSpan.FromSeconds(15);
        }

        public static async Task<GeoLocation?> GeocodeAsync(string cityName, string countryCode = "US", string stateHint = "")
        {
            // Open-Meteo geocoding only accepts a plain city name — no commas or state abbreviations
            string url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(cityName)}&count=10&language=en&format=json";
            var resp = await _http.GetStringAsync(url);
            var json = JObject.Parse(resp);
            var results = json["results"] as JArray;
            if (results == null || results.Count == 0) return null;

            // Build candidate list matching the target country
            var candidates = new System.Collections.Generic.List<JToken>();
            foreach (var r in results)
            {
                string cc = r["country_code"]?.ToString() ?? "";
                if (cc.Equals(countryCode, StringComparison.OrdinalIgnoreCase))
                    candidates.Add(r);
            }

            // If no country match at all, fall back to full list
            if (candidates.Count == 0)
                foreach (var r in results) candidates.Add(r);

            // If a state hint was provided, try to match admin1 (state/province name or abbreviation)
            if (!string.IsNullOrWhiteSpace(stateHint) && candidates.Count > 1)
            {
                string hint = stateHint.Trim().ToLower();
                foreach (var r in candidates)
                {
                    string admin1 = r["admin1"]?.ToString() ?? "";
                    string admin1Code = r["admin1_id"]?.ToString() ?? "";
                    // Match full state name or common abbreviation
                    if (admin1.ToLower().Contains(hint) || admin1.ToLower().StartsWith(hint) ||
                        StateAbbrevToName(hint).ToLower() == admin1.ToLower())
                    {
                        return new GeoLocation
                        {
                            Name = r["name"]?.ToString() ?? cityName,
                            Latitude = r["latitude"]?.Value<double>() ?? 0,
                            Longitude = r["longitude"]?.Value<double>() ?? 0,
                            Country = r["country"]?.ToString() ?? "",
                            State = admin1
                        };
                    }
                }
            }

            // Return best country match (first candidate)
            var best = candidates[0];
            return new GeoLocation
            {
                Name = best["name"]?.ToString() ?? cityName,
                Latitude = best["latitude"]?.Value<double>() ?? 0,
                Longitude = best["longitude"]?.Value<double>() ?? 0,
                Country = best["country"]?.ToString() ?? "",
                State = best["admin1"]?.ToString() ?? ""
            };
        }

        // Maps common 2-letter US state abbreviations to full names for matching
        private static string StateAbbrevToName(string abbrev) => abbrev.ToUpper() switch
        {
            "AL" => "Alabama", "AK" => "Alaska", "AZ" => "Arizona", "AR" => "Arkansas",
            "CA" => "California", "CO" => "Colorado", "CT" => "Connecticut", "DE" => "Delaware",
            "FL" => "Florida", "GA" => "Georgia", "HI" => "Hawaii", "ID" => "Idaho",
            "IL" => "Illinois", "IN" => "Indiana", "IA" => "Iowa", "KS" => "Kansas",
            "KY" => "Kentucky", "LA" => "Louisiana", "ME" => "Maine", "MD" => "Maryland",
            "MA" => "Massachusetts", "MI" => "Michigan", "MN" => "Minnesota", "MS" => "Mississippi",
            "MO" => "Missouri", "MT" => "Montana", "NE" => "Nebraska", "NV" => "Nevada",
            "NH" => "New Hampshire", "NJ" => "New Jersey", "NM" => "New Mexico", "NY" => "New York",
            "NC" => "North Carolina", "ND" => "North Dakota", "OH" => "Ohio", "OK" => "Oklahoma",
            "OR" => "Oregon", "PA" => "Pennsylvania", "RI" => "Rhode Island", "SC" => "South Carolina",
            "SD" => "South Dakota", "TN" => "Tennessee", "TX" => "Texas", "UT" => "Utah",
            "VT" => "Vermont", "VA" => "Virginia", "WA" => "Washington", "WV" => "West Virginia",
            "WI" => "Wisconsin", "WY" => "Wyoming", "DC" => "District of Columbia",
            _ => abbrev
        };

        public static async Task<WeatherData?> GetWeatherAsync(double lat, double lon, string locationName, bool useFahrenheit = true)
        {
            string unit = useFahrenheit ? "fahrenheit" : "celsius";
            string windUnit = useFahrenheit ? "mph" : "kmh";

            string url = $"https://api.open-meteo.com/v1/forecast?" +
                $"latitude={lat}&longitude={lon}" +
                $"&current=temperature_2m,apparent_temperature,relative_humidity_2m,wind_speed_10m,wind_direction_10m," +
                $"precipitation,weather_code,is_day,pressure_msl,uv_index,visibility" +
                $"&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum" +
                $"&temperature_unit={unit}&wind_speed_unit={windUnit}" +
                $"&timezone=auto&forecast_days=7";

            var resp = await _http.GetStringAsync(url);
            var json = JObject.Parse(resp);

            var cur = json["current"];
            if (cur == null) return null;

            int wcode = cur["weather_code"]?.Value<int>() ?? 0;
            bool isDay = (cur["is_day"]?.Value<int>() ?? 1) == 1;

            var data = new WeatherData
            {
                Latitude = lat,
                Longitude = lon,
                LocationName = locationName,
                Temperature = cur["temperature_2m"]?.Value<double>() ?? 0,
                FeelsLike = cur["apparent_temperature"]?.Value<double>() ?? 0,
                Humidity = cur["relative_humidity_2m"]?.Value<int>() ?? 0,
                WindSpeed = cur["wind_speed_10m"]?.Value<double>() ?? 0,
                WindDirection = DegreesToCompass(cur["wind_direction_10m"]?.Value<double>() ?? 0),
                Precipitation = cur["precipitation"]?.Value<double>() ?? 0,
                WeatherCode = wcode,
                Description = WMODescription(wcode),
                Icon = WMOIcon(wcode, isDay),
                IsDay = isDay,
                UVIndex = cur["uv_index"]?.Value<double>() ?? 0,
                Visibility = (int)(cur["visibility"]?.Value<double>() ?? 0),
                Pressure = cur["pressure_msl"]?.Value<double>() ?? 0,
                ObservationTime = DateTime.Now
            };

            // Parse forecast
            var daily = json["daily"];
            if (daily != null)
            {
                var dates = daily["time"] as JArray;
                var maxTemps = daily["temperature_2m_max"] as JArray;
                var minTemps = daily["temperature_2m_min"] as JArray;
                var codes = daily["weather_code"] as JArray;
                var precip = daily["precipitation_sum"] as JArray;

                if (dates != null)
                {
                    int count = Math.Min(7, dates.Count);
                    data.Forecast = new ForecastDay[count];
                    for (int i = 0; i < count; i++)
                    {
                        int fc = codes?[i]?.Value<int>() ?? 0;
                        data.Forecast[i] = new ForecastDay
                        {
                            Date = DateTime.Parse(dates[i].ToString()),
                            TempMax = maxTemps?[i]?.Value<double>() ?? 0,
                            TempMin = minTemps?[i]?.Value<double>() ?? 0,
                            WeatherCode = fc,
                            Description = WMODescription(fc),
                            Icon = WMOIcon(fc, true),
                            PrecipitationSum = precip?[i]?.Value<double>() ?? 0
                        };
                    }
                }
            }

            return data;
        }

        private static string DegreesToCompass(double degrees)
        {
            string[] dirs = { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW" };
            int idx = (int)((degrees / 22.5) + 0.5) % 16;
            return dirs[idx];
        }

        public static string WMODescription(int code) => code switch
        {
            0 => "Clear Sky",
            1 => "Mainly Clear",
            2 => "Partly Cloudy",
            3 => "Overcast",
            45 => "Foggy",
            48 => "Icy Fog",
            51 => "Light Drizzle",
            53 => "Moderate Drizzle",
            55 => "Dense Drizzle",
            61 => "Slight Rain",
            63 => "Moderate Rain",
            65 => "Heavy Rain",
            71 => "Slight Snow",
            73 => "Moderate Snow",
            75 => "Heavy Snow",
            77 => "Snow Grains",
            80 => "Slight Showers",
            81 => "Moderate Showers",
            82 => "Violent Showers",
            85 => "Slight Snow Showers",
            86 => "Heavy Snow Showers",
            95 => "Thunderstorm",
            96 => "Thunderstorm w/ Hail",
            99 => "Thunderstorm w/ Heavy Hail",
            _ => "Unknown"
        };

        public static string WMOIcon(int code, bool isDay) => code switch
        {
            0 => isDay ? "☀️" : "🌙",
            1 => isDay ? "🌤️" : "🌤️",
            2 => "⛅",
            3 => "☁️",
            45 or 48 => "🌫️",
            51 or 53 or 55 => "🌦️",
            61 or 63 or 65 => "🌧️",
            71 or 73 or 75 or 77 => "❄️",
            80 or 81 or 82 => "🌦️",
            85 or 86 => "🌨️",
            95 or 96 or 99 => "⛈️",
            _ => "🌡️"
        };

        public static string GetMapUrl(double lat, double lon, string layer = "rain")
        {
            // Use current Windy embed.html format (embed2.html is deprecated)
            string latStr = lat.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
            string lonStr = lon.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
            return $"https://embed.windy.com/embed.html?type=map&location=coordinates&metricRain=mm&metricTemp=%C2%B0F&metricWind=mph&zoom=7&overlay={layer}&product=ecmwf&level=surface&lat={latStr}&lon={lonStr}";
        }
    }
}