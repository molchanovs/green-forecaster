using Azure.Identity;
using FluentValidation;
using Forecaster.API.FeatureFlags;
using Forecaster.API.Metrics;
using Forecaster.API.Middleware;
using Forecaster.API.Validators;
using Forecaster.Database;
using Forecaster.Services;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.FeatureManagement;
using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Events;

namespace Forecaster.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter())
                .CreateBootstrapLogger();

            try
            {
                Log.Information("Starting application");
                RunApp(args);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void RunApp(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ── Azure App Configuration (production) ──────────────────────────────
            // When running in Azure Container Apps the endpoint is injected as an
            // environment variable.  Locally the variable is absent and the app
            // falls back to the FeatureManagement section in appsettings.json.
            var appConfigEndpoint = builder.Configuration["AzureAppConfiguration:Endpoint"];
            var useAzureAppConfig = !string.IsNullOrEmpty(appConfigEndpoint);

            if (useAzureAppConfig)
            {
                builder.Configuration.AddAzureAppConfiguration(options =>
                    options
                        .Connect(new Uri(appConfigEndpoint!), new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned))
                        .UseFeatureFlags(ff =>
                            ff.SetRefreshInterval(TimeSpan.FromSeconds(30))));

                builder.Services.AddAzureAppConfiguration();
            }

            builder.Host.UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter()));

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddValidatorsFromAssemblyContaining<CreateWeatherForecastRequestValidator>();

            builder.Services.AddHealthChecks();

            builder.Services.AddSingleton<ForecasterMetrics>();

            builder.Services.AddScoped<IWeatherForecastService, WeatherForecastService>();

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddForecasterDatabase(connectionString);

            builder.Services.AddFeatureManagement();

            builder.Services.AddOpenTelemetry()
                .WithMetrics(metrics => metrics
                    .AddMeter(ForecasterMetrics.MeterName)
                    .AddConsoleExporter());

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Note: UseAzureAppConfiguration() middleware was removed in v8.x.
            // Background refresh is handled automatically by the hosted service
            // registered via AddAzureAppConfiguration() above.

            app.UseMiddleware<ExceptionMiddleware>();

            app.UseSerilogRequestLogging();

            app.UseAuthorization();


            app.MapControllers();

            app.MapHealthChecks("/health/live");
            app.MapHealthChecks("/health/ready");

            app.Run();
        }
    }
}
