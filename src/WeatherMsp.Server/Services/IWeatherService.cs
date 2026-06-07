using WeatherMsp.Server.Models;

namespace WeatherMsp.Server.Services;

public interface IWeatherService
{
    /// <summary>Геокодирование названия города в координаты.</summary>
    Task<GeoLocation?> GeocodeAsync(string city, CancellationToken ct = default);

    /// <summary>Текущая погода по координатам.</summary>
    Task<CurrentWeatherResponse> GetCurrentByCoordsAsync(
        double lat, double lon, string? units, string? lang, CancellationToken ct = default);

    /// <summary>Прогноз (5 дней / 3 часа) по координатам.</summary>
    Task<ForecastResponse> GetForecastByCoordsAsync(
        double lat, double lon, string? units, string? lang, CancellationToken ct = default);
}
