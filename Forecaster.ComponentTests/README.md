# Forecaster.ComponentTests

Component tests for the Forecaster API. These tests exercise the **full HTTP pipeline** in-process using `WebApplicationFactory<Program>` — routing, model binding, validation middleware, response serialisation and status codes — without any real infrastructure.

## Intent

The goal is to validate the **HTTP contract** of the API: that the right status codes, content types, response shapes, and validation error structures are returned for a given request. Business logic and data access are intentionally kept out of scope.

> If it lives in the HTTP layer, it belongs here.  
> If it lives in a service or a database, it belongs in a unit or integration test.

## What is tested

| Area | Examples |
|------|---------|
| Status codes | `201 Created`, `400 Bad Request`, `200 OK` |
| Response body shape | Field names, types, computed values (e.g. `TemperatureF`) |
| `Content-Type` header | `application/json` |
| Validation errors | Required fields, range bounds, allowed values, max length |
| Multiple validation failures | All errors returned in a single response |
| Exception middleware | Unhandled exceptions → `500`, argument exceptions → `500` |
| Health checks | `/health/live`, `/health/ready` → `200` |
| Feature-flagged endpoints | Endpoints gated by `FeatureManagement` configuration |

## Service mocking

The real `IWeatherForecastService` implementation — which depends on PostgreSQL — is replaced with a **Moq mock** registered in `ForecasterApiFactory`. This keeps the tests fast, hermetic, and runnable without any external dependencies.

```
[Test process]
    │
    ▼
WebApplicationFactory<Program>        (real ASP.NET Core pipeline)
    │
    ├── Controllers                   (real — routing, binding, validation)
    ├── Middleware                    (real — ExceptionMiddleware, Serilog)
    ├── FluentValidation validators   (real)
    │
    └── IWeatherForecastService  ──►  Mock<IWeatherForecastService>   (Moq)
                                          • GetFiveDay()   → 5 forecasts
                                          • GetThirtyDay() → 30 forecasts
                                          • CreateAsync()  → echoes input back
```

Default setups are defined once in `ForecasterApiFactory`. Individual tests can retrieve `factory.WeatherForecastServiceMock` to override behaviour for a specific scenario.

## Running the tests

```bash
dotnet test Forecaster.ComponentTests/Forecaster.ComponentTests.csproj
```

No database, no Docker, no environment variables required.

