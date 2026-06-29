namespace Forecaster.SmokeTests;

/// <summary>
/// Reads smoke-test configuration from environment variables.
/// Set FORECASTER_BASE_URL via seed-env-vars.ps1 for local runs,
/// or via your deployment pipeline for production runs.
/// </summary>
public static class SmokeTestSettings
{
    /// <summary>
    /// Base URL of the Forecaster API under test.
    /// Driven by the FORECASTER_BASE_URL environment variable.
    /// </summary>
    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("FORECASTER_BASE_URL")
        ?? throw new InvalidOperationException(
            "FORECASTER_BASE_URL environment variable is not set. " +
            "Run seed-env-vars.ps1 to set it for local development.");
}

