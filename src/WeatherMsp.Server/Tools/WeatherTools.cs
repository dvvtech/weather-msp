using System.ComponentModel;
using System.Globalization;
using System.Text;
using ModelContextProtocol.Server;
using WeatherMsp.Server.Models;
using WeatherMsp.Server.Services;

namespace WeatherMsp.Server.Tools;

/// <summary>
/// Набор MCP-инструментов для получения погоды через OpenWeatherMap.
/// Регистрируется через WithToolsFromAssembly() в Program.cs.
/// </summary>
[McpServerToolType]
public sealed class WeatherTools
{
    private readonly IWeatherService _weather;

    public WeatherTools(IWeatherService weather) => _weather = weather;

    [McpServerTool(Name = "get_current_weather")]
    [Description("Возвращает текущую погоду для указанного города: температуру, ощущается как, " +
                 "влажность, давление, ветер, облачность и описание.")]
    public async Task<string> GetCurrentWeatherAsync(
        [Description("Название города, например 'Москва' или 'London,GB'.")] string city,
        [Description("Единицы измерения: metric (°C), imperial (°F) или standard (K). По умолчанию metric.")]
        string? units = null,
        [Description("Язык описания погоды (ISO 639-1), например 'ru' или 'en'. По умолчанию ru.")]
        string? language = null,
        CancellationToken cancellationToken = default)
    {
        var loc = await _weather.GeocodeAsync(city, cancellationToken);
        if (loc is null)
            return $"Город '{city}' не найден.";

        var w = await _weather.GetCurrentByCoordsAsync(loc.Lat, loc.Lon, units, language, cancellationToken);
        return FormatCurrent(loc, w, units);
    }

    [McpServerTool(Name = "get_weather_forecast")]
    [Description("Возвращает прогноз погоды на ближайшие дни (с шагом 3 часа, до 5 дней) " +
                 "для указанного города.")]
    public async Task<string> GetForecastAsync(
        [Description("Название города, например 'Москва' или 'London,GB'.")] string city,
        [Description("Количество дней прогноза от 1 до 5. По умолчанию 3.")] int days = 3,
        [Description("Единицы измерения: metric (°C), imperial (°F) или standard (K). По умолчанию metric.")]
        string? units = null,
        [Description("Язык описания погоды (ISO 639-1), например 'ru' или 'en'. По умолчанию ru.")]
        string? language = null,
        CancellationToken cancellationToken = default)
    {
        days = Math.Clamp(days, 1, 5);

        var loc = await _weather.GeocodeAsync(city, cancellationToken);
        if (loc is null)
            return $"Город '{city}' не найден.";

        var forecast = await _weather.GetForecastByCoordsAsync(loc.Lat, loc.Lon, units, language, cancellationToken);
        return FormatForecast(loc, forecast, days, units);
    }

    // ----- форматирование вывода -----

    private static string TempUnit(string? units) => units switch
    {
        "imperial" => "°F",
        "standard" => "K",
        _ => "°C",
    };

    private static string SpeedUnit(string? units) => units == "imperial" ? "миль/ч" : "м/с";

    private static string FormatCurrent(GeoLocation loc, CurrentWeatherResponse w, string? units)
    {
        var tu = TempUnit(units);
        var su = SpeedUnit(units);
        var desc = w.Weather.FirstOrDefault()?.Description ?? "нет данных";
        var place = Place(loc);

        var sb = new StringBuilder();
        sb.AppendLine($"Текущая погода — {place}:");
        sb.AppendLine($"  Состояние:     {Capitalize(desc)}");
        sb.AppendLine($"  Температура:   {w.Main.Temp:0.#}{tu} (ощущается как {w.Main.FeelsLike:0.#}{tu})");
        sb.AppendLine($"  Мин / Макс:    {w.Main.TempMin:0.#}{tu} / {w.Main.TempMax:0.#}{tu}");
        sb.AppendLine($"  Влажность:     {w.Main.Humidity}%");
        sb.AppendLine($"  Давление:      {w.Main.Pressure} гПа");
        if (w.Wind is not null)
            sb.AppendLine($"  Ветер:         {w.Wind.Speed:0.#} {su}, {WindDir(w.Wind.Deg)}");
        if (w.Clouds is not null)
            sb.AppendLine($"  Облачность:    {w.Clouds.All}%");
        if (w.Visibility is not null)
            sb.AppendLine($"  Видимость:     {w.Visibility / 1000.0:0.#} км");
        return sb.ToString().TrimEnd();
    }

    private static string FormatForecast(GeoLocation loc, ForecastResponse f, int days, string? units)
    {
        var tu = TempUnit(units);
        var place = Place(loc);
        var tzOffset = TimeSpan.FromSeconds(f.City.Timezone);

        var groups = f.List
            .Select(i => new
            {
                Item = i,
                Local = DateTimeOffset.FromUnixTimeSeconds(i.Dt).ToOffset(tzOffset)
            })
            .GroupBy(x => x.Local.Date)
            .Take(days);

        var sb = new StringBuilder();
        sb.AppendLine($"Прогноз погоды — {place} (на {days} дн.):");

        foreach (var day in groups)
        {
            var temps = day.Select(x => x.Item.Main.Temp).ToList();
            var min = temps.Min();
            var max = temps.Max();
            var midday = day
                .OrderBy(x => Math.Abs(x.Local.Hour - 12))
                .First();
            var desc = midday.Item.Weather.FirstOrDefault()?.Description ?? "нет данных";
            var pop = day.Max(x => x.Item.Pop ?? 0) * 100;

            sb.AppendLine();
            sb.AppendLine($"  {day.Key:dd.MM (ddd)}:");
            sb.AppendLine($"    {Capitalize(desc)}");
            sb.AppendLine($"    Температура: {min:0.#}…{max:0.#}{tu}");
            sb.AppendLine($"    Вероятность осадков: {pop:0}%");
        }

        return sb.ToString().TrimEnd();
    }

    private static string Place(GeoLocation loc)
    {
        var parts = new List<string> { loc.Name };
        if (!string.IsNullOrWhiteSpace(loc.State)) parts.Add(loc.State!);
        if (!string.IsNullOrWhiteSpace(loc.Country)) parts.Add(loc.Country!);
        return string.Join(", ", parts);
    }

    private static string WindDir(int deg)
    {
        string[] dirs = { "С", "СВ", "В", "ЮВ", "Ю", "ЮЗ", "З", "СЗ" };
        var idx = (int)Math.Round(deg / 45.0) % 8;
        return $"{dirs[idx]} ({deg}°)";
    }

    private static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0], CultureInfo.CurrentCulture) + s[1..];
}
