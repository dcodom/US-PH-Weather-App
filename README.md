# 🌦️ Weather Center — US & Philippines

A modern C# WinForms desktop application for real-time weather data with animated weather maps.

## ✨ Features

### Both Forms Include:
- **Real-time weather data** via Open-Meteo API (free, no API key needed)
- **Animated weather maps** powered by Windy embed (precipitation, wind, temperature, clouds, radar)
- **7-day forecast** with daily high/low, weather icons & descriptions
- **6 weather stat cards**: Humidity · Wind Speed/Direction · UV Index · Pressure · Visibility · Precipitation
- **Live aurora/glow background animation**
- **Modern dark UI** with rounded panels, glowing buttons, and styled inputs

### 🇺🇸 US Weather Form
- Search by **City + State abbreviation** (e.g. "Chicago, IL")
- Temperatures in **°F**, wind in **mph**
- Deep navy / arctic blue theme

### 🇵🇭 Philippines Weather Form
- Search by **City + Province/Region** (e.g. "Cebu City, Cebu")
- **Quick-access buttons** for 14 major PH cities (Manila, Cebu, Davao, Boracay, etc.)
- Temperatures in **°C**, wind in **km/h**
- **PAGASA advisory banner** linking to official typhoon bulletins
- Tropical sunset coral/teal theme

### 🚀 Launcher Splash Screen
- Opens on startup — choose US or PH from a stylish card-based launcher
- Both forms can open simultaneously

---

## 🛠️ Requirements

- **Windows 10/11** (required for WinForms)
- **.NET 8.0 SDK** — download from https://dotnet.microsoft.com/download/dotnet/8.0
- **Internet connection** for weather data and maps

---

## 📦 Setup & Build

### 1. Install .NET 8.0 SDK
Download and install from: https://dotnet.microsoft.com/download/dotnet/8.0

### 2. Create project folder
```
mkdir WeatherApp
cd WeatherApp
```

### 3. Copy all files into the WeatherApp folder:
- `WeatherApp.csproj`
- `Program.cs`
- `WeatherService.cs`
- `UIControls.cs`
- `USWeatherForm.cs`
- `PHWeatherForm.cs`

### 4. Restore & Build
```bash
dotnet restore
dotnet build
```

### 5. Run
```bash
dotnet run
```

### 6. Publish as standalone EXE (optional)
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
The EXE will be in `bin\Release\net8.0-windows\win-x64\publish\`

---

## 🗂️ Project Structure

```
WeatherApp/
├── WeatherApp.csproj     # Project file (.NET 8.0 WinForms)
├── Program.cs            # Entry point + Launcher splash form
├── WeatherService.cs     # API service (Open-Meteo geocoding + weather)
├── UIControls.cs         # Theme, RoundedPanel, GlowButton, StatCard, ForecastCard
├── USWeatherForm.cs      # United States weather form
└── PHWeatherForm.cs      # Philippines weather form
```

---

## 🌐 APIs Used

| API | Purpose | Cost |
|-----|---------|------|
| [Open-Meteo Geocoding](https://open-meteo.com/en/docs/geocoding-api) | City search → lat/lon | FREE |
| [Open-Meteo Weather](https://open-meteo.com/en/docs) | Current weather + forecast | FREE |
| [Windy Embed](https://embed.windy.com/) | Animated weather maps | FREE |

No API keys required!

---

## 🗺️ Map Layers Available

Select from the dropdown on each form:
- **Precipitation** — rainfall overlay
- **Wind Speed** — animated wind streams
- **Temperature** — heat map
- **Clouds** — cloud coverage
- **Radar** — weather radar (where available)

---

## 🎨 Themes

| Form | Background | Accent | Temp Unit |
|------|-----------|--------|-----------|
| US | Deep navy `#0A0F23` | Arctic blue `#29B6F6` | °F |
| PH | Deep teal `#081C23` | Coral sunset `#FF6F3C` | °C |

---

## 💡 Usage Tips

- Press **Enter** after typing a city to search instantly
- Use the **map layer dropdown** to switch between precipitation, wind, temperature overlays
- PH form has **quick city buttons** at the top for popular destinations
- Both forms can run **simultaneously** — open US then click "Open PH Weather"
- The map will **auto-center** on the searched city each time

---

## 🔧 Customization

### Add more PH quick-city buttons
In `PHWeatherForm.cs`, find `PHCities` array and add entries:
```csharp
("Vigan", "Ilocos Sur"),
("Palawan", "Puerto Princesa"),
```

### Change default startup city
In `USWeatherForm.cs` → `LoadDefaultCity()`:
```csharp
_cityBox.InnerBox.Text = "Los Angeles";
_stateBox.InnerBox.Text = "CA";
```

### Add Fahrenheit toggle to PH form
Change `_useCelsius` to `false` in `PHWeatherForm.cs` and pass `_useFahrenheit` to `GetWeatherAsync`.
