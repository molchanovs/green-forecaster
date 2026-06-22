using Forecaster.Database;
using Forecaster.Database.Entities;
using Forecaster.Services;
using Microsoft.EntityFrameworkCore;

namespace Forecaster.IntegrationTests;

/// <summary>
/// Spins up a real ForecasterDbContext against the local Postgres instance,
/// runs pending migrations.
///
/// Connection string is read from the ConnectionStrings__DefaultConnection
/// environment variable (set by seed-env-vars.ps1).
/// </summary>
public class DatabaseFixture
{
    public ForecasterDbContext DbContext { get; }
    public WeatherForecastService Service { get; }

    public DatabaseFixture()
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException(
                "Environment variable 'ConnectionStrings__DefaultConnection' is not set. " +
                "Run seed-env-vars.ps1 and restart your terminal.");

        var options = new DbContextOptionsBuilder<ForecasterDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        DbContext = new ForecasterDbContext(options);
        DbContext.Database.Migrate();

        Service = new WeatherForecastService(DbContext);
    }

    public void Seed30WeatherForecastRecords()
    {
        DbContext.WeatherForecasts.AddRange(
            Enumerable.Range(1, 30).Select(i => new WeatherForecastEntity
            {
                Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(i)),
                TemperatureC = 20,
                Summary = "Warm",
                City = "Kyiv"
            }));

        DbContext.SaveChanges();
    }
}

