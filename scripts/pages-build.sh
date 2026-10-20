#!/usr/bin/env bash
# Cloudflare Pages build entry point for the Blazor WebAssembly client.
#
# The Pages build image has no .NET 10 SDK, so the SDK is installed on the fly.
# Environment variables (Pages > Settings > Environment variables):
#   API_BASE_URL  Public URL of the API, e.g. https://fittrack-api.fly.dev
#   DOTNET_VERSION  Optional, defaults to 10.0
set -euo pipefail

DOTNET_VERSION="${DOTNET_VERSION:-10.0}"
API_BASE_URL="${API_BASE_URL:-}"

curl -sSL https://dot.net/v1/dotnet-install.sh \
  | bash -s -- --channel "$DOTNET_VERSION" --install-dir "$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

if [ -n "$API_BASE_URL" ]; then
  # appsettings.json is listed in the service-worker asset manifest with its hash, so it
  # must be rewritten before publish. Editing the published file would break the
  # offline cache install (integrity mismatch).
  printf '{\n  "ApiBaseUrl": "%s"\n}\n' "$API_BASE_URL" \
    > src/FitTrack.Web/wwwroot/appsettings.json
fi

dotnet publish src/FitTrack.Web -c Release -o out --nologo
