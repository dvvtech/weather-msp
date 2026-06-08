using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WeatherMsp.Server.Configuration;
using WeatherMsp.Server.Services;

namespace Weather.Tests;

public sealed class OpenWeatherMapIntegrationTests
{
    private const string ApiKeyPlaceholder = "PUT_YOUR_API_KEY_HERE";

    [Fact]
    public async Task GetCurrentWeather_RealApi_ShouldReturnWeather()
    {
        var apiKey = GetApiKey();
        if (apiKey == ApiKeyPlaceholder)
            return;

        var options = Options.Create(new OpenWeatherMapOptions
        {
            ApiKey = apiKey,
            DefaultUnits = "metric",
            DefaultLanguage = "ru"
        });

        using var http = new HttpClient { BaseAddress = new Uri("https://api.openweathermap.org/") };
        var service = new OpenWeatherMapService(http, options, NullLogger<OpenWeatherMapService>.Instance);

        var result = await service.GetCurrentByCoordsAsync(55.75, 37.62, "metric", "ru");

        Assert.NotNull(result);
        Assert.Equal("Moscow", result.Name);
        Assert.InRange(result.Main.Temp, -50, 60);
    }

    [Fact]
    public async Task GetForecast_RealApi_ShouldReturnForecast()
    {
        var apiKey = GetApiKey();
        if (apiKey == ApiKeyPlaceholder)
            return;

        var options = Options.Create(new OpenWeatherMapOptions
        {
            ApiKey = apiKey,
            DefaultUnits = "metric",
            DefaultLanguage = "ru"
        });

        using var http = new HttpClient { BaseAddress = new Uri("https://api.openweathermap.org/") };
        var service = new OpenWeatherMapService(http, options, NullLogger<OpenWeatherMapService>.Instance);

        var result = await service.GetForecastByCoordsAsync(55.75, 37.62, "metric", "ru");

        Assert.NotNull(result);
        Assert.Equal("Moscow", result.City.Name);
        Assert.NotEmpty(result.List);
    }

    [Fact]
    public async Task Geocode_RealApi_ShouldReturnLocation()
    {
        var apiKey = GetApiKey();
        if (apiKey == ApiKeyPlaceholder)
            return;

        var options = Options.Create(new OpenWeatherMapOptions
        {
            ApiKey = apiKey,
        });

        using var http = new HttpClient { BaseAddress = new Uri("https://api.openweathermap.org/") };
        var service = new OpenWeatherMapService(http, options, NullLogger<OpenWeatherMapService>.Instance);

        var result = await service.GeocodeAsync("Moscow");

        Assert.NotNull(result);
        Assert.Equal("Moscow", result!.Name);
    }

    /// <summary>
    /// Получает API-ключ из user secrets (секция OpenWeatherMap:ApiKey)
    /// или возвращает плейсхолдер, если ключ не задан.
    /// </summary>
    private static string GetApiKey()
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets<OpenWeatherMapIntegrationTests>()
            .Build();

        return config["OpenWeatherMap:ApiKey"] ?? ApiKeyPlaceholder;
    }

    /// <summary>
    /// Установите API-ключ:
    /// dotnet user-secrets set "OpenWeatherMap:ApiKey" "ВАШ_КЛЮЧ" --project tests\Weather.Tests\Weather.Tests.csproj
    /// </summary>
}
