#!/usr/bin/env bash
# Cloudflare Workers build entry point for the Blazor WebAssembly client.
#
# The Workers build image has no .NET 10 SDK, so the SDK is installed on the fly.
# Environment variables (Workers > Settings > Build > Build variables and secrets):
#   API_BASE_URL    Optional, defaults to the Fly.io API below
#   DOTNET_VERSION  Optional, defaults to 10.0
set -euo pipefail

DOTNET_VERSION="${DOTNET_VERSION:-10.0}"
# Public API URL. Override with a build variable in Cloudflare Workers if it ever changes.
API_BASE_URL="${API_BASE_URL:-https://fittrack-api-claudiovolnei.fly.dev}"

curl -sSL https://dot.net/v1/dotnet-install.sh \
  | bash -s -- --channel "$DOTNET_VERSION" --install-dir "$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

# appsettings.json is listed in the service-worker asset manifest with its hash, so it
# must be rewritten before publish. Editing the published file would break the
# offline cache install (integrity mismatch).
printf '{\n  "ApiBaseUrl": "%s"\n}\n' "$API_BASE_URL" \
  > src/FitTrack.Web/wwwroot/appsettings.json
echo "API base URL: $API_BASE_URL"

dotnet publish src/FitTrack.Web -c Release -o out --nologo

# Workers serve the assets directory as-is, so index.html must be at its root.
# `dotnet publish -o out` puts the static site in out/wwwroot, which is what
# wrangler.jsonc points at.
SITE_DIR="out/wwwroot"
if [ ! -f "$SITE_DIR/index.html" ]; then
  echo "ERROR: $SITE_DIR/index.html was not produced. Check the publish output above." >&2
  exit 1
fi

echo "Static site ready at $SITE_DIR:"
find "$SITE_DIR" -maxdepth 1 -mindepth 1 | sort
