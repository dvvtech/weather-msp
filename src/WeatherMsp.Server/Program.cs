using WeatherMsp.Server.Configuration;
using WeatherMsp.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Конфигурация OpenWeatherMap ---

if (!builder.Environment.IsDevelopment())
{
    builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);
}

builder.Services
    .AddOptions<OpenWeatherMapOptions>()
    .Bind(builder.Configuration.GetSection(OpenWeatherMapOptions.SectionName))
    .ValidateOnStart();

// --- Типизированный HttpClient для сервиса погоды ---
builder.Services.AddHttpClient<IWeatherService, OpenWeatherMapService>((sp, client) =>
{
    var opts = builder.Configuration
        .GetSection(OpenWeatherMapOptions.SectionName)
        .Get<OpenWeatherMapOptions>() ?? new OpenWeatherMapOptions();

    client.BaseAddress = new Uri(opts.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("User-Agent", "WeatherMcpServer/1.0");
});

// --- MCP сервер: HTTP-транспорт (Streamable HTTP + SSE) + автообнаружение инструментов ---
builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new() { Name = "weather-mcp-server", Version = "1.0.0" };
    })
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

// Проверка работоспособности.
app.MapGet("/", () => Results.Text(
    "Weather MCP Server is running. MCP endpoint: /mcp", "text/plain"));

// MCP endpoint доступен по адресу /mcp (Streamable HTTP) и /mcp/sse (legacy SSE).
app.MapMcp("/mcp");

app.Run();
