using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Forecaster.Database;

public static class DatabaseServiceExtensions
{
    public static IServiceCollection AddForecasterDatabase(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<ForecasterDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}