using Forecaster.DataContracts;

namespace Forecaster.Services;

public interface IWeatherForecastService
{
    IEnumerable<WeatherForecast> GetFiveDay();
    IEnumerable<WeatherForecast> GetThirtyDay();
    IEnumerable<WeatherForecast> GetAll(int days, string filter);
    Task<WeatherForecast> CreateAsync(WeatherForecast forecast);
    Task BulkInsertAsync(IEnumerable<WeatherForecast> forecasts);
}

