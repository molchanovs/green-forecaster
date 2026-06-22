using System.Diagnostics.Metrics;

namespace Forecaster.API.Metrics;

/// <summary>
/// Central place for all custom metrics emitted by the Forecaster API.
/// Register as a singleton and inject wherever measurements are needed.
/// </summary>
public sealed class ForecasterMetrics : IDisposable
{
    public const string MeterName = "Forecaster.API";

    private readonly Meter _meter;

    // Total number of forecast requests, tagged by endpoint
    private readonly Counter<long> _requestsCounter;

    // Distribution of how many forecasts were returned per request
    private readonly Histogram<int> _forecastsReturnedHistogram;

    public ForecasterMetrics()
    {
        _meter = new Meter(MeterName, "1.0.0");

        _requestsCounter = _meter.CreateCounter<long>(
            name: "weather_forecast.requests",
            unit: "{requests}",
            description: "Total number of weather forecast requests");

        _forecastsReturnedHistogram = _meter.CreateHistogram<int>(
            name: "weather_forecast.forecasts_returned",
            unit: "{forecasts}",
            description: "Number of forecast items returned per request");
    }

    /// <summary>Records a completed forecast request.</summary>
    /// <param name="endpoint">Short name identifying the endpoint (e.g. "five-day").</param>
    /// <param name="forecastCount">Number of items included in the response.</param>
    public void RecordRequest(string endpoint, int forecastCount)
    {
        var tag = new KeyValuePair<string, object?>("endpoint", endpoint);
        _requestsCounter.Add(1, tag);
        _forecastsReturnedHistogram.Record(forecastCount, tag);
    }

    public void Dispose() => _meter.Dispose();
}




