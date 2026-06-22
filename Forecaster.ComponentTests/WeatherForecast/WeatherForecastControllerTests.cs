using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Forecaster.API.FeatureFlags;
using Forecaster.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using WeatherForecastModel = Forecaster.DataContracts.WeatherForecast;

namespace Forecaster.ComponentTests.WeatherForecast;

[Collection(ForecasterCollection.Name)]
public class WeatherForecastControllerTests
{
    private readonly HttpClient _client;
    private readonly ForecasterApiFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public WeatherForecastControllerTests(ForecasterApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── GET /weather-forecasts/five-day ──────────────────────────────────────

    [Fact]
    public async Task GetFiveDay_Returns200()
    {
        var response = await _client.GetAsync("/weather-forecasts/five-day");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetFiveDay_ReturnsApplicationJson()
    {
        var response = await _client.GetAsync("/weather-forecasts/five-day");

        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetFiveDay_ReturnsExactlyFiveForecasts()
    {
        var forecasts = await _client.GetFromJsonAsync<List<WeatherForecastDto>>(
            "/weather-forecasts/five-day", JsonOptions);

        Assert.NotNull(forecasts);
        Assert.Equal(5, forecasts.Count);
    }

    [Fact]
    public async Task GetFiveDay_EachForecastHasRequiredFields()
    {
        var forecasts = await _client.GetFromJsonAsync<List<WeatherForecastDto>>(
            "/weather-forecasts/five-day", JsonOptions);

        Assert.NotNull(forecasts);
        Assert.All(forecasts, f =>
        {
            Assert.False(string.IsNullOrWhiteSpace(f.Summary));
            Assert.True(f.Date > DateOnly.FromDateTime(DateTime.UtcNow),
                "Date should be in the future");
        });
    }

    [Fact]
    public async Task GetFiveDay_TemperatureFMatchesConversionFormula()
    {
        var forecasts = await _client.GetFromJsonAsync<List<WeatherForecastDto>>(
            "/weather-forecasts/five-day", JsonOptions);

        Assert.NotNull(forecasts);
        Assert.All(forecasts, f =>
        {
            var expected = 32 + (int)(f.TemperatureC / 0.5556);
            Assert.Equal(expected, f.TemperatureF);
        });
    }

    // ── GET /weather-forecasts/thirty-day ────────────────────────────────────

    [Fact]
    public async Task GetThirtyDay_Returns200()
    {
        var response = await _client.GetAsync("/weather-forecasts/thirty-day");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetThirtyDay_ReturnsExactlyThirtyForecasts()
    {
        var forecasts = await _client.GetFromJsonAsync<List<WeatherForecastDto>>(
            "/weather-forecasts/thirty-day", JsonOptions);

        Assert.NotNull(forecasts);
        Assert.Equal(30, forecasts.Count);
    }

    [Fact]
    public async Task GetThirtyDay_DatesAreChronologicallyOrdered()
    {
        var forecasts = await _client.GetFromJsonAsync<List<WeatherForecastDto>>(
            "/weather-forecasts/thirty-day", JsonOptions);

        Assert.NotNull(forecasts);
        var dates = forecasts.Select(f => f.Date).ToList();
        Assert.Equal(dates.OrderBy(d => d), dates);
    }

    // ── GET /weather-forecasts/all ────────────────────────────────────────────

    [Fact]
    public async Task GetAll_WithValidDaysAndFilter_Returns200()
    {
        var response = await _client.GetAsync("/weather-forecasts/all?days=10&filter=Warm");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsOnlyForecastsMatchingFilter()
    {
        const string filter = "Freezing";

        var forecasts = await _client.GetFromJsonAsync<List<WeatherForecastDto>>(
            $"/weather-forecasts/all?days=100&filter={filter}", JsonOptions);

        Assert.NotNull(forecasts);
        Assert.All(forecasts, f => Assert.Equal(filter, f.Summary));
    }

    [Fact]
    public async Task GetAll_WithUnknownFilter_ReturnsEmptyArray()
    {
        var forecasts = await _client.GetFromJsonAsync<List<WeatherForecastDto>>(
            "/weather-forecasts/all?days=50&filter=NonExistentSummary", JsonOptions);

        Assert.NotNull(forecasts);
        Assert.Empty(forecasts);
    }

    [Fact]
    public async Task GetAll_ReturnsNoMoreForecastsThanRequestedDays()
    {
        const int days = 7;

        var forecasts = await _client.GetFromJsonAsync<List<WeatherForecastDto>>(
            $"/weather-forecasts/all?days={days}&filter=Mild", JsonOptions);

        Assert.NotNull(forecasts);
        Assert.True(forecasts.Count <= days,
            $"Expected at most {days} forecasts, got {forecasts.Count}");
    }

    // ── POST /weather-forecasts — Happy path ──────────────────────────────────

    [Fact]
    public async Task Post_WithValidRequest_Returns201Created()
    {
        var response = await _client.PostAsJsonAsync("/weather-forecasts", ValidRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithValidRequest_ReturnsApplicationJson()
    {
        var response = await _client.PostAsJsonAsync("/weather-forecasts", ValidRequest());

        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Post_WithValidRequest_ReturnsCreatedForecast()
    {
        var request = ValidRequest();

        var response = await _client.PostAsJsonAsync("/weather-forecasts", request);
        var body = await response.Content.ReadFromJsonAsync<WeatherForecastDto>(JsonOptions);

        Assert.NotNull(body);
        Assert.Equal(request.Date, body.Date);
        Assert.Equal(request.TemperatureC, body.TemperatureC);
        Assert.Equal(request.Summary, body.Summary);
        Assert.Equal(request.City, body.City);
    }

    [Fact]
    public async Task Post_WithValidRequest_TemperatureFMatchesConversionFormula()
    {
        var request = ValidRequest() with { TemperatureC = 20 };

        var response = await _client.PostAsJsonAsync("/weather-forecasts", request);
        var body = await response.Content.ReadFromJsonAsync<WeatherForecastDto>(JsonOptions);

        Assert.NotNull(body);
        var expected = 32 + (int)(body.TemperatureC / 0.5556);
        Assert.Equal(expected, body.TemperatureF);
    }

    // ── POST /weather-forecasts — Date validation ─────────────────────────────

    [Fact]
    public async Task Post_WithPastDate_Returns400()
    {
        var request = ValidRequest() with { Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)) };

        var response = await _client.PostAsJsonAsync("/weather-forecasts", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithPastDate_ReturnsDateValidationError()
    {
        var request = ValidRequest() with { Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)) };

        var response = await _client.PostAsJsonAsync("/weather-forecasts", request);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsDto>(JsonOptions);

        Assert.NotNull(body);
        Assert.True(body.Errors.ContainsKey("Date"));
    }

    // ── POST /weather-forecasts — Temperature validation ──────────────────────

    [Theory]
    [InlineData(-61)]
    [InlineData(61)]
    public async Task Post_WithTemperatureOutOfRange_Returns400(int temperatureC)
    {
        var request = ValidRequest() with { TemperatureC = temperatureC };

        var response = await _client.PostAsJsonAsync("/weather-forecasts", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(-60)]
    [InlineData(0)]
    [InlineData(60)]
    public async Task Post_WithTemperatureAtBoundary_Returns201(int temperatureC)
    {
        var request = ValidRequest() with { TemperatureC = temperatureC };

        var response = await _client.PostAsJsonAsync("/weather-forecasts", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // ── POST /weather-forecasts — Summary validation ──────────────────────────

    [Fact]
    public async Task Post_WithNullSummary_Returns400()
    {
        var request = ValidRequest() with { Summary = null };

        var response = await _client.PostAsJsonAsync("/weather-forecasts", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithUnknownSummary_Returns400()
    {
        var request = ValidRequest() with { Summary = "Unknown" };

        var response = await _client.PostAsJsonAsync("/weather-forecasts", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithUnknownSummary_ReturnsSummaryValidationError()
    {
        var request = ValidRequest() with { Summary = "Unknown" };

        var response = await _client.PostAsJsonAsync("/weather-forecasts", request);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsDto>(JsonOptions);

        Assert.NotNull(body);
        Assert.True(body.Errors.ContainsKey("Summary"));
    }

    // ── POST /weather-forecasts — City validation ─────────────────────────────

    [Fact]
    public async Task Post_WithNullCity_Returns400()
    {
        var request = ValidRequest() with { City = null };

        var response = await _client.PostAsJsonAsync("/weather-forecasts", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithCityExceeding100Characters_Returns400()
    {
        var request = ValidRequest() with { City = new string('A', 101) };

        var response = await _client.PostAsJsonAsync("/weather-forecasts", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithCityExactly100Characters_Returns201()
    {
        var request = ValidRequest() with { City = new string('A', 100) };

        var response = await _client.PostAsJsonAsync("/weather-forecasts", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // ── POST /weather-forecasts — Multiple validation errors ──────────────────

    [Fact]
    public async Task Post_WithMultipleInvalidFields_ReturnsAllValidationErrors()
    {
        var request = new CreateWeatherForecastRequestDto(
            Date: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            TemperatureC: 999,
            Summary: null,
            City: null);

        var response = await _client.PostAsJsonAsync("/weather-forecasts", request);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.True(body.Errors.Count >= 3);
    }

    // ── POST /weather-forecasts — Feature flag ────────────────────────────────

    [Fact]
    public async Task Post_WhenCreateForecastFlagDisabled_Returns500()
    {
        var client = _factory.CreateClient(
            featureFlags: new Dictionary<string, bool> { [FeatureFlagNames.CreateForecast] = false });

        var response = await client.PostAsJsonAsync("/weather-forecasts", ValidRequest());

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Post_WhenCreateForecastFlagDisabled_DoesNotCreateForecast()
    {
        var ownMock = new Mock<IWeatherForecastService>();
        var client = _factory.CreateClient(
            configureServices: services =>
            {
                services.RemoveAll<IWeatherForecastService>();
                services.AddSingleton(ownMock.Object);
            },
            featureFlags: new Dictionary<string, bool> { [FeatureFlagNames.CreateForecast] = false });

        await client.PostAsJsonAsync("/weather-forecasts", ValidRequest());

        ownMock.Verify(
            s => s.CreateAsync(It.IsAny<WeatherForecastModel>()),
            Times.Never);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static CreateWeatherForecastRequestDto ValidRequest() => new(
        Date: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
        TemperatureC: 22,
        Summary: "Warm",
        City: "Kyiv");

    // ── DTOs ──────────────────────────────────────────────────────────────────

    private sealed record WeatherForecastDto(
        DateOnly Date,
        int TemperatureC,
        int TemperatureF,
        string? Summary,
        string? City = null);

    private sealed record CreateWeatherForecastRequestDto(
        DateOnly Date,
        int TemperatureC,
        string? Summary,
        string? City);

    private sealed record ValidationProblemDetailsDto(
        Dictionary<string, string[]> Errors);
}
