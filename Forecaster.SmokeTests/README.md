# Forecaster.SmokeTests

Black-box smoke tests that probe a **live, running** Forecaster API over real HTTP.  
No in-process hosting, no mocks — if the API and database are broken, these tests fail.

---

## Configuration

The base URL of the API under test is controlled by a single environment variable:

| Variable | Description | Default (seed script) |
|---|---|---|
| `FORECASTER_BASE_URL` | Full base URL of the target API | `http://localhost:5171` |
| `PROBE_INTERVAL_SECONDS` | Seconds between probe runs in loop mode. Set to `0` to run once and exit. | `60` |

Run `seed-env-vars.ps1` once to register the defaults for local development:

```powershell
.\seed-env-vars.ps1
```

---

## Running the Tests

### Option 1 — dotnet test (direct, no Docker)

Start the API first, then run the tests from the solution root:

```powershell
# Terminal 1 — start the API
dotnet run --project Forecaster.API --launch-profile http

# Terminal 2 — run smoke tests once
dotnet test Forecaster.SmokeTests
```

The `FORECASTER_BASE_URL` variable set by `seed-env-vars.ps1` is picked up automatically.  
Override it inline to target a different environment:

```powershell
$env:FORECASTER_BASE_URL = "<prod-url>"
dotnet test Forecaster.SmokeTests
```

---

### Option 2 — Docker Compose, continuous loop (local synthetic monitoring)

The `smoke-tests` service is defined under the `smoke` profile so it does not start with a plain `docker compose up`.

Start the API outside Docker (from the IDE or a terminal), then launch the monitor:

```powershell
docker compose --profile smoke up smoke-tests
```

The container will:
1. Poll `$FORECASTER_BASE_URL/health/live` every 5 s until the API responds.
2. Run `dotnet test` and print results to stdout.
3. Sleep for `PROBE_INTERVAL_SECONDS` (default: 60) and repeat.

Follow the output:

```powershell
docker compose logs -f smoke-tests
```

---

### Option 3 — Docker Compose, run once and exit (CI / post-deploy gate)

```powershell
docker compose --profile smoke run --rm `
  -e PROBE_INTERVAL_SECONDS=0 `
  smoke-tests
```

The container exits with code `0` on success and non-zero on any test failure,  
making it suitable as a quality gate in a deployment pipeline.

---

### Option 4 — Point at production

Override `FORECASTER_DOCKER_BASE_URL` at runtime without touching any config files:

```powershell
# One-shot against production
docker compose --profile smoke run --rm `
  -e FORECASTER_DOCKER_BASE_URL=<prod-url> `
  -e PROBE_INTERVAL_SECONDS=0 `
  smoke-tests
```

```powershell
# Continuous loop against production
docker compose --profile smoke run --rm `
  -e FORECASTER_DOCKER_BASE_URL=<prod-url> `
  smoke-tests
```

---

## Notes

- `host.docker.internal` is configured in `docker-compose.yml` via `extra_hosts` so the container can reach an API running on the host machine. This is automatic on Docker Desktop (Windows/Mac) and requires the `host-gateway` alias on Linux.
- `localhost` inside Docker refers to the container itself, **not** the host. The compose service always uses `FORECASTER_DOCKER_BASE_URL` (defaulting to `http://host.docker.internal:5171`) so you never need to change `FORECASTER_BASE_URL` for Docker.
- The probe loop continues even when tests fail — a failed run is logged and the container sleeps before retrying, matching the behaviour of a cloud synthetic monitor.
- To change the probe interval without rebuilding the image, pass `PROBE_INTERVAL_SECONDS` as an environment variable at `docker compose run` time.

