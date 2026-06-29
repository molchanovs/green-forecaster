using System.Net;
using System.Net.Http.Json;

namespace Forecaster.SmokeTests.WeatherForecast;

/// <summary>
/// Smoke tests for the WeatherForecast endpoints.
/// These run against a live API (local or production) pointed to by FORECASTER_BASE_URL.
/// No mocks, no in-process hosting — real HTTP over the network.
/// </summary>
public sealed class WeatherForecastSmokeTests : IDisposable
{
    private readonly HttpClient _client = new()
    {
        BaseAddress = new Uri(SmokeTestSettings.BaseUrl),
        Timeout = TimeSpan.FromSeconds(10)
    };

    // ── GET /weather-forecasts/five-day ──────────────────────────────────────

    [Fact]
    public async Task GetFiveDay_Returns200_WithExactlyFiveForecasts()
    {
        var response = await _client.GetAsync("/weather-forecasts/five-day");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var forecasts = await response.Content
            .ReadFromJsonAsync<List<WeatherForecastDto>>();

        Assert.NotNull(forecasts);
        Assert.Equal(5, forecasts.Count);
    }

    // ── GET /weather-forecasts/all ────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Returns200()
    {
        var response = await _client.GetAsync("/weather-forecasts/all?days=5&filter=Warm");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public void Dispose() => _client.Dispose();

    // ── DTOs ──────────────────────────────────────────────────────────────────

    private sealed record WeatherForecastDto(
        DateOnly Date,
        int TemperatureC,
        int TemperatureF,
        string? Summary,
        string? City = null);
}

