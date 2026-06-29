#!/bin/sh
# probe.sh — runs the Forecaster smoke tests on a configurable interval.
#
# Environment variables:
#   FORECASTER_BASE_URL        URL of the API to probe (required)
#   PROBE_INTERVAL_SECONDS     Seconds between probe runs (default: 60)
#                              Set to 0 to run once and exit — useful for CI.

BASE_URL=${FORECASTER_BASE_URL:?"FORECASTER_BASE_URL must be set"}
INTERVAL=${PROBE_INTERVAL_SECONDS:-60}

echo "Smoke-test target : $BASE_URL"
echo "Probe interval    : ${INTERVAL}s  (0 = run once and exit)"
echo ""

# ── Wait until the API health endpoint responds ───────────────────────────────
echo "Waiting for API to become ready..."
until curl -sf "${BASE_URL}/health/live" > /dev/null 2>&1; do
  echo "  $(date -u '+%H:%M:%S')  Not ready — retrying in 5 s..."
  sleep 5
done
echo "  $(date -u '+%H:%M:%S')  API is up."
echo ""

# ── Probe function ────────────────────────────────────────────────────────────
run_probes() {
  echo "=== $(date -u '+%Y-%m-%d %H:%M:%S UTC')  Running smoke tests ==="
  dotnet test /src/Forecaster.SmokeTests/Forecaster.SmokeTests.csproj \
    --no-build \
    --configuration Release \
    --logger "console;verbosity=normal"
  result=$?
  if [ $result -eq 0 ]; then
    echo "=== PASSED ==="
  else
    echo "=== FAILED (exit $result) ==="
  fi
  return $result
}

# ── Run once or loop ──────────────────────────────────────────────────────────
if [ "$INTERVAL" = "0" ]; then
  run_probes
  exit $?
fi

while true; do
  run_probes || true   # keep looping even when tests fail
  echo ""
  echo "Next probe in ${INTERVAL}s..."
  sleep "$INTERVAL"
done

