using Forecaster.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Forecaster.Database;

public class ForecasterDbContext : DbContext
{
    public ForecasterDbContext(DbContextOptions<ForecasterDbContext> options)
        : base(options)
    {
    }

    public DbSet<WeatherForecastEntity> WeatherForecasts => Set<WeatherForecastEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WeatherForecastEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.TemperatureC).IsRequired();
            entity.Property(e => e.Summary).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).IsRequired();
        });
    }
}

