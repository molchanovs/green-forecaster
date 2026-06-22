using System.Net;

namespace Forecaster.ComponentTests.HealthChecks;

[Collection(ForecasterCollection.Name)]
public class HealthCheckTests
{
    private readonly HttpClient _client;

    public HealthCheckTests(ForecasterApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── GET /health/live ─────────────────────────────────────────────────────

    [Fact]
    public async Task LivenessProbe_Returns200()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LivenessProbe_ReturnsHealthyBody()
    {
        var response = await _client.GetAsync("/health/live");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal("Healthy", body);
    }

    // ── GET /health/ready ────────────────────────────────────────────────────

    [Fact]
    public async Task ReadinessProbe_Returns200()
    {
        var response = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReadinessProbe_ReturnsHealthyBody()
    {
        var response = await _client.GetAsync("/health/ready");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal("Healthy", body);
    }
}

