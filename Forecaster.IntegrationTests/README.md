# Forecaster.IntegrationTests

## Intent

This project verifies **gold path database integration scenarios** for the Forecaster application.

The tests run against a real PostgreSQL instance and exercise the service layer end-to-end through an actual `ForecasterDbContext`, ensuring that:

- Data is correctly persisted to and retrieved from the database.
- Migrations are applied successfully before any test executes.

## Scope

These tests are intentionally narrow in scope — they cover only the **happy path** (gold path) flows.
Edge cases, error handling, and boundary conditions are covered by unit tests and component tests in their respective projects.

## Prerequisites

A running PostgreSQL instance is required. Set the connection string via the environment variable before running tests:

```powershell
# From the repo root
.\seed-env-vars.ps1
```

The `DatabaseFixture` will automatically apply any pending EF Core migrations on startup.

## Running the Tests

```powershell
dotnet test .\Forecaster.IntegrationTests\Forecaster.IntegrationTests.csproj
```

