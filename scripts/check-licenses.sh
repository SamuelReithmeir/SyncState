#!/usr/bin/env bash
# Validates that every dependency of the published SyncState packages is MIT-licensed.
set -euo pipefail
cd "$(dirname "$0")/.."

PROJECTS=(
  src/SyncState.Abstractions/SyncState.Abstractions.csproj
  src/SyncState.Core/SyncState.Core.csproj
  src/SyncState.Diagnostics/SyncState.Diagnostics.csproj
  src/SyncState.EntityFrameworkCore/SyncState.EntityFrameworkCore.csproj
  src/SyncState.ErrorHandling/SyncState.ErrorHandling.csproj
  src/SyncState.OptionsMonitor/SyncState.OptionsMonitor.csproj
  src/SyncState.ReloadInterval/SyncState.ReloadInterval.csproj
  src/SyncState.SignalR/SyncState.SignalR.csproj
  src/SyncState.StateDeltas/SyncState.StateDeltas.csproj
)

dotnet tool restore

for project in "${PROJECTS[@]}"; do
  echo "Checking licenses for $project"
  dotnet tool run nuget-license \
    --input "$project" \
    --include-transitive \
    --allowed-license-types "MIT"
done
