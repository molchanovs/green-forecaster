using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Forecaster.Database.Entities;

[Table("weather_forecasts")]
public class WeatherForecastEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("date")]
    public DateOnly Date { get; set; }

    [Column("temperature_c")]
    public int TemperatureC { get; set; }

    [Column("summary")]
    [MaxLength(500)]
    public string? Summary { get; set; }

    [Column("city")]
    [MaxLength(200)]
    public string? City { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

