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

# Microsoft.AspNetCore.SignalR.Core 1.2.0 and the ASP.NET Core 2.3.0 packages it
# pulls in transitively still carry the pre-relicense `licenseUrl` pointing at the
# Apache-2.0 LICENSE.txt from the aspnet/AspNetCore repo's 2.0.0 tag. That is the
# license these specific package versions were actually published under, even
# though current releases of the same project are MIT.
IGNORED_PACKAGES="Microsoft.AspNetCore.Authorization;Microsoft.AspNetCore.Connections.Abstractions;Microsoft.AspNetCore.Http.Features;Microsoft.AspNetCore.SignalR.Common;Microsoft.AspNetCore.SignalR.Core;Microsoft.AspNetCore.SignalR.Protocols.Json"

dotnet tool restore

for project in "${PROJECTS[@]}"; do
  echo "Checking licenses for $project"
  dotnet tool run nuget-license \
    --input "$project" \
    --include-transitive \
    --allowed-license-types "MIT" \
    --ignored-packages "$IGNORED_PACKAGES"
done
