namespace Forecaster.API.FeatureFlags;

/// <summary>
/// Strongly-typed constants for all feature flag names.
/// Must match the key names in appsettings.json FeatureManagement section
/// and in Azure App Configuration Feature Manager.
/// </summary>
public static class FeatureFlagNames
{
    public const string CreateForecast = "CreateForecast";
    public const string AllForecast = "AllForecast";
}


