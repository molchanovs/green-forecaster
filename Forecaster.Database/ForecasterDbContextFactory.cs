using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Forecaster.Database;

public class ForecasterDbContextFactory : IDesignTimeDbContextFactory<ForecasterDbContext>
{
    public ForecasterDbContext CreateDbContext(string[] args)
    {
        // Reads the connection string seeded by seed-env-vars.ps1
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<ForecasterDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ForecasterDbContext(optionsBuilder.Options);
    }
}

