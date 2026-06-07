using System.Text.Json.Serialization;

namespace WeatherMsp.Server.Models;

// --- Geocoding API (/geo/1.0/direct) ---

public sealed class GeoLocation
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("lat")] public double Lat { get; set; }
    [JsonPropertyName("lon")] public double Lon { get; set; }
    [JsonPropertyName("country")] public string? Country { get; set; }
    [JsonPropertyName("state")] public string? State { get; set; }

    [JsonExtensionData] public Dictionary<string, object>? LocalNames { get; set; }
}

// --- Current Weather API (/data/2.5/weather) ---

public sealed class CurrentWeatherResponse
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("dt")] public long Dt { get; set; }
    [JsonPropertyName("timezone")] public int Timezone { get; set; }
    [JsonPropertyName("weather")] public List<WeatherDescription> Weather { get; set; } = new();
    [JsonPropertyName("main")] public MainBlock Main { get; set; } = new();
    [JsonPropertyName("wind")] public WindBlock? Wind { get; set; }
    [JsonPropertyName("clouds")] public CloudsBlock? Clouds { get; set; }
    [JsonPropertyName("visibility")] public int? Visibility { get; set; }
    [JsonPropertyName("sys")] public SysBlock? Sys { get; set; }
}

// --- 5 day / 3 hour Forecast API (/data/2.5/forecast) ---

public sealed class ForecastResponse
{
    [JsonPropertyName("city")] public CityBlock City { get; set; } = new();
    [JsonPropertyName("list")] public List<ForecastItem> List { get; set; } = new();
}

public sealed class CityBlock
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("country")] public string? Country { get; set; }
    [JsonPropertyName("timezone")] public int Timezone { get; set; }
}

public sealed class ForecastItem
{
    [JsonPropertyName("dt")] public long Dt { get; set; }
    [JsonPropertyName("dt_txt")] public string? DtTxt { get; set; }
    [JsonPropertyName("main")] public MainBlock Main { get; set; } = new();
    [JsonPropertyName("weather")] public List<WeatherDescription> Weather { get; set; } = new();
    [JsonPropertyName("wind")] public WindBlock? Wind { get; set; }
    [JsonPropertyName("clouds")] public CloudsBlock? Clouds { get; set; }
    [JsonPropertyName("pop")] public double? Pop { get; set; }
}

// --- Shared blocks ---

public sealed class WeatherDescription
{
    [JsonPropertyName("main")] public string Main { get; set; } = string.Empty;
    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
    [JsonPropertyName("icon")] public string? Icon { get; set; }
}

public sealed class MainBlock
{
    [JsonPropertyName("temp")] public double Temp { get; set; }
    [JsonPropertyName("feels_like")] public double FeelsLike { get; set; }
    [JsonPropertyName("temp_min")] public double TempMin { get; set; }
    [JsonPropertyName("temp_max")] public double TempMax { get; set; }
    [JsonPropertyName("pressure")] public int Pressure { get; set; }
    [JsonPropertyName("humidity")] public int Humidity { get; set; }
}

public sealed class WindBlock
{
    [JsonPropertyName("speed")] public double Speed { get; set; }
    [JsonPropertyName("deg")] public int Deg { get; set; }
    [JsonPropertyName("gust")] public double? Gust { get; set; }
}

public sealed class CloudsBlock
{
    [JsonPropertyName("all")] public int All { get; set; }
}

public sealed class SysBlock
{
    [JsonPropertyName("country")] public string? Country { get; set; }
    [JsonPropertyName("sunrise")] public long? Sunrise { get; set; }
    [JsonPropertyName("sunset")] public long? Sunset { get; set; }
}
