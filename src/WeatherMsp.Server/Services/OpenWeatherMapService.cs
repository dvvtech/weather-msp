using System.Net;
using Microsoft.Extensions.Options;
using WeatherMsp.Server.Configuration;
using WeatherMsp.Server.Models;

namespace WeatherMsp.Server.Services;

/// <summary>
/// Клиент OpenWeatherMap поверх типизированного HttpClient.
/// </summary>
public sealed class OpenWeatherMapService : IWeatherService
{
    private readonly HttpClient _http;
    private readonly OpenWeatherMapOptions _options;
    private readonly ILogger<OpenWeatherMapService> _logger;

    public OpenWeatherMapService(
        HttpClient http,
        IOptions<OpenWeatherMapOptions> options,
        ILogger<OpenWeatherMapService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GeoLocation?> GeocodeAsync(string city, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("Название города не должно быть пустым.", nameof(city));

        var url = $"geo/1.0/direct?q={Uri.EscapeDataString(city)}&limit=1&appid={ApiKey}";
        var results = await SendAsync<List<GeoLocation>>(url, ct);
        return results is { Count: > 0 } ? results[0] : null;
    }

    public Task<CurrentWeatherResponse> GetCurrentByCoordsAsync(
        double lat, double lon, string? units, string? lang, CancellationToken ct = default)
    {
        var url = $"data/2.5/weather?lat={Fmt(lat)}&lon={Fmt(lon)}" +
                  $"&units={Units(units)}&lang={Lang(lang)}&appid={ApiKey}";
        return SendAsync<CurrentWeatherResponse>(url, ct)!;
    }

    public Task<ForecastResponse> GetForecastByCoordsAsync(
        double lat, double lon, string? units, string? lang, CancellationToken ct = default)
    {
        var url = $"data/2.5/forecast?lat={Fmt(lat)}&lon={Fmt(lon)}" +
                  $"&units={Units(units)}&lang={Lang(lang)}&appid={ApiKey}";
        return SendAsync<ForecastResponse>(url, ct)!;
    }

    private string ApiKey =>
        !string.IsNullOrWhiteSpace(_options.ApiKey)
            ? _options.ApiKey
            : throw new InvalidOperationException(
                "OpenWeatherMap API-ключ не задан. Укажите OpenWeatherMap:ApiKey в appsettings.json " +
                "или переменную окружения OpenWeatherMap__ApiKey.");

    private string Units(string? units) => string.IsNullOrWhiteSpace(units) ? _options.DefaultUnits : units;
    private string Lang(string? lang) => string.IsNullOrWhiteSpace(lang) ? _options.DefaultLanguage : lang;
    private static string Fmt(double v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private async Task<T?> SendAsync<T>(string relativeUrl, CancellationToken ct)
    {
        try
        {
            using var response = await _http.GetAsync(relativeUrl, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new WeatherServiceException("Неверный или неактивный API-ключ OpenWeatherMap (401).");

            if (response.StatusCode == HttpStatusCode.NotFound)
                throw new WeatherServiceException("Запрошенные данные не найдены (404).");

            if (response.StatusCode == (HttpStatusCode)429)
                throw new WeatherServiceException("Превышен лимит запросов OpenWeatherMap (429).");

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
            if (result is null)
                throw new WeatherServiceException("Пустой ответ от OpenWeatherMap.");
            return result;
        }
        catch (WeatherServiceException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Ошибка обращения к OpenWeatherMap: {Url}", relativeUrl);
            throw new WeatherServiceException($"Не удалось получить данные о погоде: {ex.Message}", ex);
        }
    }
}

/// <summary>Доменная ошибка сервиса погоды (передаётся клиенту MCP в читаемом виде).</summary>
public sealed class WeatherServiceException : Exception
{
    public WeatherServiceException(string message, Exception? inner = null) : base(message, inner) { }
}
