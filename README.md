# green-forecaster
A demo ASP.NET Core 8 Web API project showcasing production-ready patterns for containerised microservices hosted on Azure Container Apps.

## Summary

| Concern | Implementation |
|---|---|
| **API** | ASP.NET Core 8 Web API — weather forecast endpoints (`five-day`, `thirty-day`, `all`) |
| **Logging** | [Serilog](https://serilog.net/) with structured JSON output via `CompactJsonFormatter` |
| **Metrics** | Custom instruments (`Counter`, `Histogram`) exported via [OpenTelemetry](https://opentelemetry.io/) |
| **Error handling** | Global `ExceptionMiddleware` — catches unhandled exceptions and returns a consistent JSON error envelope |
| **Health checks** | `/health/live` and `/health/ready` endpoints via `Microsoft.Extensions.Diagnostics.HealthChecks` |
| **Feature flags** | [`Microsoft.FeatureManagement`](https://learn.microsoft.com/en-us/azure/azure-app-configuration/feature-management-overview) — local JSON fallback in development, [Azure App Configuration](https://learn.microsoft.com/en-us/azure/azure-app-configuration/overview) in production |
| **Testing** | Component tests via `Microsoft.AspNetCore.Mvc.Testing` — real HTTP calls, no mocking |
| **Database migrations** | EF Core migrations in `Forecaster.Database`, applied via `dotnet ef migrations bundle` |
| **Containerisation** | Multi-stage `Dockerfile`, published to GHCR |
| **CI/CD** | GitHub Actions deploying to Azure Container Apps |

---

## Component Tests

The `Forecaster.ComponentTests` project contains component-level tests that verify the API behaviour end-to-end via real HTTP calls — without any mocking — using [`Microsoft.AspNetCore.Mvc.Testing`](https://learn.microsoft.com/aspnet/core/test/integration-tests).

### How it works

| Building block | Purpose |
|---|---|
| `ForecasterApiFactory` | Extends `WebApplicationFactory<Program>` to boot the real API **in-process** and register test-only controllers from the test assembly |
| `ForecasterCollection` | xUnit `ICollectionFixture` that shares a **single** factory (and test server) across all test classes, avoiding Serilog bootstrap-logger re-initialisation issues |
| `TestControllers/ThrowingController` | Test-only controller exposing `/test/throw` endpoints to trigger unhandled exceptions without touching production code |

### Test coverage

| Test class | Endpoints covered | Tests |
|---|---|---|
| `ExceptionMiddlewareTests` | `GET /test/throw`, `GET /test/throw-argument` | 500 status code, `application/json` content-type, generic error message in body, `statusCode` field in body, consistent behaviour for different exception types, happy-path returns 200 |
| `WeatherForecastControllerTests` | `GET /weather-forecasts/five-day`, `GET /weather-forecasts/thirty-day`, `GET /weather-forecasts/all` | Response status, content-type, exact item counts, future dates, chronological ordering, TemperatureF conversion formula, summary filter behaviour, empty result for unknown filter |
| `HealthCheckTests` | `GET /health/live`, `GET /health/ready` | 200 status code, `Healthy` response body |

### Running the tests

```powershell
dotnet test Forecaster.ComponentTests
```

---

## Metrics

The API is instrumented with custom metrics using the built-in `System.Diagnostics.Metrics` API, exported via [OpenTelemetry](https://opentelemetry.io/) to the console.

### Instruments

All instruments live in the `Forecaster.API` meter (see `Metrics/ForecasterMetrics.cs`):

| Instrument | Name | Type | Tag |
|---|---|---|---|
| Forecast requests | `weather_forecast.requests` | `Counter<long>` | `endpoint` |
| Forecasts returned | `weather_forecast.forecasts_returned` | `Histogram<int>` | `endpoint` |

The `endpoint` tag is one of `five-day`, `thirty-day`, or `all`.

### How it works

```
WeatherForecastController
        │  calls RecordRequest(endpoint, count)
        ▼
ForecasterMetrics  (singleton)
        │  adds to Counter / records in Histogram
        ▼
OpenTelemetry SDK  (AddMeter("Forecaster.API"))
        │
        ▼
Console Exporter  →  stdout
```

### Exporter output

When the app is running, the console exporter periodically flushes collected values to stdout, for example:

```
Metric Name: weather_forecast.requests, Unit: {requests}
  endpoint=five-day     Value: 12
  endpoint=thirty-day   Value: 4
  endpoint=all          Value: 7

Metric Name: weather_forecast.forecasts_returned, Unit: {forecasts}
  endpoint=five-day     Sum: 60,  Count: 12
  endpoint=thirty-day   Sum: 120, Count: 4
  endpoint=all          Sum: 18,  Count: 7
```

### Swapping the exporter

The exporter is registered in `Program.cs`. To switch from console to Prometheus (for scraping by a Prometheus server) replace `AddConsoleExporter()` with the Prometheus exporter:

```csharp
// Install: dotnet add package OpenTelemetry.Exporter.Prometheus.AspNetCore
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMeter(ForecasterMetrics.MeterName)
        .AddPrometheusExporter());

// In pipeline:
app.MapPrometheusScrapingEndpoint(); // exposes /metrics
```

### Local debugging with dotnet-counters

No code changes needed — `dotnet-counters` subscribes directly to any running process:

```powershell
dotnet-counters monitor --process-id <pid> --counters Forecaster.API
```

---

## Database Migrations

Migrations live in `Forecaster.Database/Migrations` and are managed with EF Core. They are applied using a **migrations bundle** — a self-contained executable that runs all pending migrations against a target database without requiring the .NET SDK or EF tools to be installed in the deployment environment.

### Prerequisites

Install the `dotnet-ef` global tool (one-time):

```powershell
dotnet tool install --global dotnet-ef
```

Or update if already installed:

```powershell
dotnet tool update --global dotnet-ef
```

---

### Adding a new migration

```powershell
dotnet ef migrations add <MigrationName> --project Forecaster.Database --output-dir Migrations
```

---

### Building the migrations bundle

Run from the solution root. The bundle targets the host OS by default; use `--self-contained` to include the .NET runtime for environments where it is not pre-installed.

```powershell
# Self-contained (no .NET runtime required on the target machine)
dotnet ef migrations bundle --project Forecaster.Database --output efbundle.exe --self-contained
```

---

### Running the bundle

Pass the connection string via the `--connection` flag or, when `ConnectionStrings__DefaultConnection`
is already set in the environment (e.g. after running `seed-env-vars.ps1`), reference it directly:

**Via environment variable (preferred for CI/CD):**
```powershell
.\efbundle.exe --connection $env:ConnectionStrings__DefaultConnection
```

The bundle is idempotent — it only applies migrations that have not yet been recorded in the `__EFMigrationsHistory` table.

---

### Local development

Start the database with Docker Compose (the service is named `postgres`), then run the bundle.  
The connection string is already available via `ConnectionStrings__DefaultConnection` seeded by
`seed-env-vars.ps1`, so pass it directly with `--connection`:

```powershell
docker compose up -d postgres
.\efbundle.exe --connection $env:ConnectionStrings__DefaultConnection
```

---

## CI/CD Setup: Azure Container Apps + GitHub Actions

### Prerequisites
- An Azure account with an active subscription
- A GitHub repository
- Docker image published to GHCR (GitHub Container Registry)

---

### 1. Install Azure CLI

**Windows (PowerShell as Administrator):**
```powershell
winget install --exact --id Microsoft.AzureCLI
```

Verify the installation:
```powershell
az version
```

---

### 2. Login and Select Subscription

```powershell
az login
```

List your subscriptions and set the one you want to use:
```powershell
az account list --output table
az account set --subscription "<your-subscription-id>"
```

---

### 3. Generate Azure Credentials JSON

Create a service principal with `Contributor` role scoped to your subscription:

```powershell
az ad sp create-for-rbac `
  --name "green-forecaster-sp" `
  --role contributor `
  --scopes /subscriptions/<your-subscription-id>/resourceGroups/forecaster-rg `
  --sdk-auth
```

This outputs a JSON block like:
```json
{
  "clientId": "...",
  "clientSecret": "...",
  "subscriptionId": "...",
  "tenantId": "...",
  "activeDirectoryEndpointUrl": "...",
  "resourceManagerEndpointUrl": "...",
  ...
}
```

> ⚠️ Copy the **entire JSON output** — you will need it in the next step.

---

### 4. Add the Secret to GitHub

1. Go to your GitHub repository
2. Navigate to **Settings → Secrets and variables → Actions**
3. Click **New repository secret**
4. Set the name to `AZURE_CREDENTIALS`
5. Paste the full JSON from the previous step as the value
6. Click **Add secret**

Optionally add these additional secrets for convenience:

| Secret Name | Value                    |
|---|--------------------------|
| `AZURE_SUBSCRIPTION_ID` | Your subscription ID     |
| `AZURE_RESOURCE_GROUP` | Your resource group name |

---

## Feature Flags

Feature flags are managed via `Microsoft.FeatureManagement`. The behaviour differs between environments:

| Environment | Flag source |
|---|---|
| Local development | `appsettings.Development.json` → `FeatureManagement` section |
| Component tests | `appsettings.Test.json` → `FeatureManagement` section |
| Azure Container Apps | Azure App Configuration store (polled every 30 s via Managed Identity) |

### Current flags

| Flag | Constant | Gates |
|---|---|---|
| `ThirtyDayForecast` | `FeatureFlagNames.ThirtyDayForecast` | `GET /weather-forecasts/thirty-day` |
| `AllForecast` | `FeatureFlagNames.AllForecast` | `GET /weather-forecasts/all` |

> When a flag is **disabled** the endpoint returns `404 Not Found`.  
> `GET /weather-forecasts/five-day` is always available — it is not gated.

### Toggling flags locally

Edit `appsettings.Development.json` and save — `IConfiguration` reloads automatically within ~1 second with no restart required:

```json
"FeatureManagement": {
  "ThirtyDayForecast": true,
  "AllForecast": false
}
```

---

## Wiring up Azure App Configuration

Follow these steps once to connect the Container App to an Azure App Configuration store. After that, flags can be toggled instantly from the Azure portal or CLI with no redeployment.

### Prerequisites
- Azure CLI installed and logged in (`az login`)
- An existing Azure Container Apps environment and Container App
- The Container App must have a **system-assigned managed identity** enabled

---

### 1. Create an App Configuration store

```powershell
az appconfig create `
  --name forecaster-config `
  --resource-group <your-resource-group> `
  --location <your-location> `
  --sku Free
```

> The **Free** tier is sufficient for feature flags. Upgrade to **Standard** if you need geo-replication or soft-delete.

---

### 2. Enable system-assigned managed identity on the Container App

```powershell
az containerapp identity assign `
  --name <your-container-app-name> `
  --resource-group <your-resource-group> `
  --system-assigned
```

Note the `principalId` from the output — you need it in the next step.

---

### 3. Grant the identity read access to App Configuration

```powershell
$scope = az appconfig show `
  --name forecaster-config `
  --resource-group <your-resource-group> `
  --query id --output tsv

az role assignment create `
  --assignee <principalId-from-step-2> `
  --role "App Configuration Data Reader" `
  --scope $scope
```

---

### 4. Create the feature flags in the store

```powershell
# Create flags (disabled by default)
az appconfig feature set `
  --name forecaster-config `
  --feature ThirtyDayForecast --yes

az appconfig feature set `
  --name forecaster-config `
  --feature AllForecast --yes

# Enable them
az appconfig feature enable `
  --name forecaster-config `
  --feature ThirtyDayForecast

az appconfig feature enable `
  --name forecaster-config `
  --feature AllForecast
```

---

### 5. Set the App Configuration endpoint on the Container App

```powershell
$endpoint = az appconfig show `
  --name forecaster-config `
  --resource-group <your-resource-group> `
  --query endpoint --output tsv

az containerapp update `
  --name <your-container-app-name> `
  --resource-group <your-resource-group> `
  --set-env-vars "AzureAppConfiguration__Endpoint=$endpoint"
```

> This triggers a new revision (one-time restart). All subsequent flag changes are picked up without restarting.

---

### 6. Toggle a flag (zero-downtime, no restart)

**Azure portal:** App Configuration → Feature Manager → toggle on/off

**Azure CLI:**
```powershell
# Disable
az appconfig feature disable `
  --name forecaster-config `
  --feature ThirtyDayForecast

# Enable
az appconfig feature enable `
  --name forecaster-config `
  --feature AllForecast
```

All running Container App replicas pick up the change within **30 seconds**.

---

### Architecture

```
Azure Container Apps  (N replicas)
  ├─ Replica 1 ──┐
  ├─ Replica 2 ──┼── poll every 30 s ──▶  Azure App Configuration
  └─ Replica N ──┘   (Managed Identity)        (feature flags)
                                                      ▲
                                               Portal / CLI toggle
```

---
