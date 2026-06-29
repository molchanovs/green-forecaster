# Stage: restore & build the smoke-test project
FROM mcr.microsoft.com/dotnet/sdk:8.0

# curl is needed by probe.sh to poll /health/live
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src

# Restore (layer-cached until the .csproj changes)
COPY Forecaster.SmokeTests/Forecaster.SmokeTests.csproj Forecaster.SmokeTests/
RUN dotnet restore Forecaster.SmokeTests/Forecaster.SmokeTests.csproj

# Build (layer-cached until source changes)
COPY Forecaster.SmokeTests/ Forecaster.SmokeTests/
RUN dotnet build Forecaster.SmokeTests/Forecaster.SmokeTests.csproj \
    --no-restore \
    -c Release

# Strip Windows CR characters in case the file was checked out with CRLF line endings,
# then make it executable.
RUN sed -i 's/\r$//' /src/Forecaster.SmokeTests/probe.sh \
    && chmod +x /src/Forecaster.SmokeTests/probe.sh

ENTRYPOINT ["sh", "/src/Forecaster.SmokeTests/probe.sh"]

