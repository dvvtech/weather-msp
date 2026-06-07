namespace WeatherMsp.Server.Configuration;

/// <summary>
/// Настройки доступа к OpenWeatherMap API.
/// Заполняются из секции "OpenWeatherMap" в appsettings.json
/// или из переменной окружения OPENWEATHERMAP__APIKEY.
/// </summary>
public sealed class OpenWeatherMapOptions
{
    public const string SectionName = "OpenWeatherMap";

    /// <summary>API-ключ OpenWeatherMap (обязательно).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Базовый адрес API.</summary>
    public string BaseUrl { get; set; } = "https://api.openweathermap.org/";

    /// <summary>Единицы измерения по умолчанию: metric, imperial, standard.</summary>
    public string DefaultUnits { get; set; } = "metric";

    /// <summary>Язык описаний погоды по умолчанию (ISO 639-1), например "ru", "en".</summary>
    public string DefaultLanguage { get; set; } = "ru";
}
