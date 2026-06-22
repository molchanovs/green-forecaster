namespace Forecaster.API.Requests;

public class CreateWeatherForecastRequest
{
    public DateOnly Date { get; set; }

    public int TemperatureC { get; set; }

    public string? Summary { get; set; }

    public string? City { get; set; }
}

