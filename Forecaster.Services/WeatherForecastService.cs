using Forecaster.Database;
using Forecaster.Database.Entities;
using Forecaster.DataContracts;

namespace Forecaster.Services;

public class WeatherForecastService : IWeatherForecastService
{
    private readonly ForecasterDbContext _dbContext;

    public WeatherForecastService(ForecasterDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private static readonly string[] Summaries =
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    public IEnumerable<WeatherForecast> GetFiveDay()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        }).ToArray();
    }

    public IEnumerable<WeatherForecast> GetThirtyDay()
    {
        return Enumerable.Range(1, 30).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        }).ToArray();
    }

    public IEnumerable<WeatherForecast> GetAll(int days, string filter)
    {
        return _dbContext.WeatherForecasts
            .Where(f => f.Summary == filter)
            .Take(days)
            .Select(f => new WeatherForecast
            {
                Date = f.Date,
                TemperatureC = f.TemperatureC,
                Summary = f.Summary,
                City = f.City
            })
            .ToArray();
    }

    public async Task<WeatherForecast> CreateAsync(WeatherForecast forecast)
    {
        var entity = new WeatherForecastEntity
        {
            Date = forecast.Date,
            TemperatureC = forecast.TemperatureC,
            Summary = forecast.Summary,
            City = forecast.City
        };

        await _dbContext.WeatherForecasts.AddAsync(entity);
        await _dbContext.SaveChangesAsync();

        return new WeatherForecast
        {
            Date = entity.Date,
            TemperatureC = entity.TemperatureC,
            Summary = entity.Summary,
            City = entity.City
        };
    }

    public async Task BulkInsertAsync(IEnumerable<WeatherForecast> forecasts)
    {
        var entities = forecasts.Select(f => new WeatherForecastEntity
        {
            Date = f.Date,
            TemperatureC = f.TemperatureC,
            Summary = f.Summary,
            City = f.City
        });

        await _dbContext.WeatherForecasts.AddRangeAsync(entities);
        await _dbContext.SaveChangesAsync();
    }
}

