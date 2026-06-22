using Forecaster.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using WeatherForecastModel = Forecaster.DataContracts.WeatherForecast;

namespace Forecaster.ComponentTests;

/// <summary>
/// Custom WebApplicationFactory that boots the real API in-process,
/// replaces the database with a Moq mock of IWeatherForecastService,
/// and additionally registers test-only controllers from this assembly.
/// </summary>
public class ForecasterApiFactory : WebApplicationFactory<Forecaster.API.Program>
{
    public Mock<IWeatherForecastService> WeatherForecastServiceMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            // Remove the real service and replace with the Moq mock.
            services.RemoveAll<IWeatherForecastService>();

            WeatherForecastServiceMock
                .Setup(s => s.GetFiveDay())
                .Returns(() => Enumerable.Range(1, 5).Select(i => new WeatherForecastModel
                {
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(i)),
                    TemperatureC = 20,
                    Summary = "Warm"
                }));

            WeatherForecastServiceMock
                .Setup(s => s.GetThirtyDay())
                .Returns(() => Enumerable.Range(1, 30).Select(i => new WeatherForecastModel
                {
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(i)),
                    TemperatureC = 20,
                    Summary = "Warm"
                }));

            WeatherForecastServiceMock
                .Setup(s => s.CreateAsync(It.IsAny<WeatherForecastModel>()))
                .ReturnsAsync((WeatherForecastModel f) => f);

            services.AddSingleton(WeatherForecastServiceMock.Object);

            // Register controllers from the test assembly so we can expose
            // a /test/throw endpoint without touching production code.
            services.AddControllers()
                    .AddApplicationPart(typeof(ForecasterApiFactory).Assembly);
        });
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> with full, optional control over services
    /// and feature flags.  All parameters are optional — omit what you don't need.
    /// <list type="bullet">
    ///   <item><paramref name="configureServices"/> — runs after the base registrations;
    ///     use it to remove, replace, or add any service (mocks, fakes, options, …).</item>
    ///   <item><paramref name="featureFlags"/> — in-memory overrides for
    ///     <c>FeatureManagement</c> keys; pass <c>false</c> to disable a flag.</item>
    /// </list>
    /// When called with no arguments the base <see cref="WebApplicationFactory{T}.CreateClient()"/>
    /// is returned directly, with no extra host rebuild overhead.
    /// </summary>
    /// <example>
    /// <code>
    /// // 1. Default client (no overrides)
    /// var client = _factory.CreateClient();
    ///
    /// // 2. Feature flag only
    /// var client = _factory.CreateClient(
    ///     featureFlags: new() { [FeatureFlagNames.CreateForecast] = false });
    ///
    /// // 3. Own mock + disabled flag
    /// var myMock = new Mock&lt;IWeatherForecastService&gt;();
    /// var client = _factory.CreateClient(
    ///     configureServices: services =>
    ///     {
    ///         services.RemoveAll&lt;IWeatherForecastService&gt;();
    ///         services.AddSingleton(myMock.Object);
    ///     },
    ///     featureFlags: new() { [FeatureFlagNames.CreateForecast] = false });
    ///
    /// // 4. Multiple service overrides
    /// var client = _factory.CreateClient(configureServices: services =>
    /// {
    ///     services.RemoveAll&lt;IWeatherForecastService&gt;();
    ///     services.AddSingleton(forecastMock.Object);
    ///     services.PostConfigure&lt;MyOptions&gt;(o =&gt; o.Timeout = TimeSpan.Zero);
    /// });
    /// </code>
    /// </example>
    public HttpClient CreateClient(
        Action<IServiceCollection>? configureServices = null,
        Dictionary<string, bool>? featureFlags = null)
    {
        if (configureServices is null && featureFlags is null)
            return base.CreateClient();

        return WithWebHostBuilder(builder =>
        {
            if (configureServices is not null)
                builder.ConfigureServices(configureServices);

            if (featureFlags is not null)
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    var overrides = featureFlags.ToDictionary(
                        kvp => $"FeatureManagement:{kvp.Key}",
                        kvp => kvp.Value.ToString());
                    config.AddInMemoryCollection(overrides!);
                });
            }
        }).CreateClient();
    }
}

