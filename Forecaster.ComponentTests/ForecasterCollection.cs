namespace Forecaster.ComponentTests;

/// <summary>
/// Shares a single <see cref="ForecasterApiFactory"/> (and therefore a single
/// in-process test server) across every test class that belongs to this
/// collection. This avoids the "Serilog ReloadableLogger is already frozen"
/// error that occurs when multiple WebApplicationFactory instances try to
/// initialise the same static bootstrap logger.
/// </summary>
[CollectionDefinition(Name)]
public class ForecasterCollection : ICollectionFixture<ForecasterApiFactory>
{
    public const string Name = "Forecaster";
}

