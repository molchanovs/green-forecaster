using Forecaster.Services;

namespace Forecaster.IntegrationTests;

public class WeatherForecastServiceTests : IClassFixture<DatabaseFixture>
{
    private readonly WeatherForecastService _sut;

    public WeatherForecastServiceTests(DatabaseFixture fixture)
    {
        fixture.Seed30WeatherForecastRecords();
        _sut = fixture.Service;
    }

    // ── GetFiveDay ────────────────────────────────────────────────────────────

    [Fact]
    public void GetFiveDay_ReturnsExactlyFiveForecasts()
    {
        var result = _sut.GetFiveDay().ToList();

        Assert.Equal(5, result.Count);
    }

    // ── GetThirtyDay ──────────────────────────────────────────────────────────

    [Fact]
    public void GetThirtyDay_ReturnsExactlyThirtyForecasts()
    {
        var result = _sut.GetThirtyDay().ToList();

        Assert.Equal(30, result.Count);
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public void GetAll_FetchesRecordsFromDatabase()
    {
        var result = _sut.GetAll(10, "Warm").ToList();

        Assert.Equal(10, result.Count);
        Assert.All(result, r =>
        {
            Assert.Equal("Warm", r.Summary);
            Assert.Equal("Kyiv", r.City);
            Assert.Equal(20, r.TemperatureC);
        });
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_PersistsAndReturnsRecord()
    {
        var input = new DataContracts.WeatherForecast
        {
            Date = new DateOnly(2026, 6, 28),
            TemperatureC = 25,
            Summary = "Warm",
            City = "Lviv"
        };

        var result = await _sut.CreateAsync(input);

        Assert.Equal(input.Date, result.Date);
        Assert.Equal(input.TemperatureC, result.TemperatureC);
        Assert.Equal(input.Summary, result.Summary);
        Assert.Equal(input.City, result.City);
    }
}

