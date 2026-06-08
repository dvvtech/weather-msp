using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WeatherMsp.Server.Configuration;
using WeatherMsp.Server.Models;
using WeatherMsp.Server.Services;

namespace Weather.Tests;

public sealed class OpenWeatherMapServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private static OpenWeatherMapOptions DefaultOptions => new()
    {
        ApiKey = "test-key-123",
        BaseUrl = "https://api.openweathermap.org/",
        DefaultUnits = "metric",
        DefaultLanguage = "ru"
    };

    // ===== Geocode =====

    [Fact]
    public async Task GeocodeAsync_ShouldReturnLocation_WhenCityExists()
    {
        var geo = new[]
        {
            new GeoLocation { Name = "Moscow", Lat = 55.75, Lon = 37.62, Country = "RU", State = "Moscow" }
        };
        using var handler = MockHandler(HttpStatusCode.OK, geo);
        var service = CreateService(handler);

        var result = await service.GeocodeAsync("Moscow");

        Assert.NotNull(result);
        Assert.Equal("Moscow", result!.Name);
        Assert.Equal(55.75, result.Lat);
        Assert.Equal(37.62, result.Lon);
    }

    [Fact]
    public async Task GeocodeAsync_ShouldReturnNull_WhenCityNotFound()
    {
        using var handler = MockHandler(HttpStatusCode.OK, Array.Empty<GeoLocation>());
        var service = CreateService(handler);

        var result = await service.GeocodeAsync("NonexistentCity12345");

        Assert.Null(result);
    }

    [Fact]
    public async Task GeocodeAsync_ShouldThrow_WhenCityIsEmpty()
    {
        var service = CreateService(MockHandler(HttpStatusCode.OK, "null"));

        await Assert.ThrowsAsync<ArgumentException>(() => service.GeocodeAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GeocodeAsync("   "));
    }

    // ===== Current weather =====

    [Fact]
    public async Task GetCurrentByCoordsAsync_ShouldReturnWeather()
    {
        var weather = new CurrentWeatherResponse
        {
            Name = "Moscow",
            Dt = 1700000000,
            Timezone = 10800,
            Weather = [new() { Main = "Clear", Description = "ясно" }],
            Main = new() { Temp = 20.5, FeelsLike = 18.0, TempMin = 19.0, TempMax = 22.0, Humidity = 65, Pressure = 1013 },
            Wind = new() { Speed = 3.5, Deg = 180 },
            Visibility = 10000
        };
        using var handler = MockHandler(HttpStatusCode.OK, weather);
        var service = CreateService(handler);

        var result = await service.GetCurrentByCoordsAsync(55.75, 37.62, "metric", "ru");

        Assert.NotNull(result);
        Assert.Equal("Moscow", result.Name);
        Assert.Equal(20.5, result.Main.Temp);
        Assert.Equal("Clear", result.Weather[0].Main);
    }

    // ===== Forecast =====

    [Fact]
    public async Task GetForecastByCoordsAsync_ShouldReturnForecast()
    {
        var forecast = new ForecastResponse
        {
            City = new() { Name = "Moscow", Country = "RU", Timezone = 10800 },
            List =
            [
                new()
                {
                    Dt = 1700000000,
                    DtTxt = "2026-06-08 12:00:00",
                    Main = new() { Temp = 22.0, TempMin = 20.0, TempMax = 24.0, Humidity = 60, Pressure = 1010 },
                    Weather = [new() { Main = "Clouds", Description = "облачно" }],
                    Pop = 0.3
                }
            ]
        };
        using var handler = MockHandler(HttpStatusCode.OK, forecast);
        var service = CreateService(handler);

        var result = await service.GetForecastByCoordsAsync(55.75, 37.62, "metric", "ru");

        Assert.NotNull(result);
        Assert.Equal("Moscow", result.City.Name);
        Assert.Single(result.List);
        Assert.Equal(22.0, result.List[0].Main.Temp);
    }

    // ===== Error handling =====

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "401")]
    [InlineData(HttpStatusCode.NotFound, "404")]
    public async Task ShouldThrowWeatherServiceException_OnHttpError(HttpStatusCode statusCode, string expectedPart)
    {
        using var handler = MockHandler(statusCode, "");
        var service = CreateService(handler);

        var ex = await Assert.ThrowsAsync<WeatherServiceException>(
            () => service.GetCurrentByCoordsAsync(55, 37, null, null));

        Assert.Contains(expectedPart, ex.Message);
    }

    [Fact]
    public async Task ShouldThrowWeatherServiceException_OnTooManyRequests()
    {
        using var handler = MockHandler((HttpStatusCode)429, "");
        var service = CreateService(handler);

        var ex = await Assert.ThrowsAsync<WeatherServiceException>(
            () => service.GetCurrentByCoordsAsync(55, 37, null, null));

        Assert.Contains("429", ex.Message);
    }

    [Fact]
    public async Task ShouldThrowWeatherServiceException_OnEmptyResponse()
    {
        using var handler = new MockHttpMessageHandler(HttpStatusCode.OK, null, JsonOptions);
        var service = CreateService(handler);

        var ex = await Assert.ThrowsAsync<WeatherServiceException>(
            () => service.GetCurrentByCoordsAsync(55, 37, null, null));

        Assert.Contains("Пустой ответ", ex.Message);
    }

    [Fact]
    public async Task ShouldThrowWeatherServiceException_OnHttpRequestException()
    {
        var innerHandler = new ExceptionHandler();
        using var handler = new MockHttpMessageHandler(innerHandler);
        var service = CreateService(handler);

        var ex = await Assert.ThrowsAsync<WeatherServiceException>(
            () => service.GeocodeAsync("Moscow"));

        Assert.Contains("Не удалось получить данные", ex.Message);
    }

    // ===== Helpers =====

    private static MockHttpMessageHandler MockHandler<T>(HttpStatusCode statusCode, T body) where T : notnull
    {
        return new MockHttpMessageHandler(statusCode, body, JsonOptions);
    }

    private static OpenWeatherMapService CreateService(HttpMessageHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri(DefaultOptions.BaseUrl) };
        return new OpenWeatherMapService(
            http,
            Options.Create(DefaultOptions),
            NullLogger<OpenWeatherMapService>.Instance);
    }

    private sealed class ExceptionHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => throw new HttpRequestException("Connection refused");
    }
}

internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly HttpContent? _content;

    public MockHttpMessageHandler(HttpStatusCode statusCode, object body, JsonSerializerOptions options)
    {
        _statusCode = statusCode;
        if (body is string s)
            _content = new StringContent(s, System.Text.Encoding.UTF8, "application/json");
        else if (body is null)
            _content = JsonContent.Create<object?>(null, options: options);
        else
            _content = JsonContent.Create(body, options: options);
    }

    public MockHttpMessageHandler(HttpMessageHandler inner) : base() { }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        await Task.CompletedTask;
        return new HttpResponseMessage(_statusCode) { Content = _content };
    }
}
