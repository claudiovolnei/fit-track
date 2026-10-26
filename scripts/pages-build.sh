#!/usr/bin/env bash
# Cloudflare Workers build entry point for the Blazor WebAssembly client.
#
# The Workers build image has no .NET 10 SDK, so the SDK is installed on the fly.
# Environment variables (Workers > Settings > Build > Build variables and secrets):
#   API_BASE_URL    Optional, defaults to the Render API below
#   DOTNET_VERSION  Optional, defaults to 10.0
set -euo pipefail

DOTNET_VERSION="${DOTNET_VERSION:-10.0}"
# Public API URL. The Render blueprint names the service after this subdomain; override with a
# build variable in Cloudflare Workers if the host assigns another URL or a custom domain.
API_BASE_URL="${API_BASE_URL:-https://fittrack-api-claudiovolnei.onrender.com}"

# The build runner occasionally drops the connection to Microsoft's CDN mid-download
# (curl 56 killed a deploy on 2026-10-23), so both fetch steps retry before failing.
# -L is required: https://dot.net/v1/dotnet-install.sh is a 301 with an empty body.
# Without following the redirect, curl writes a 0-byte file, bash succeeds, and
# `dotnet` is missing from PATH.
INSTALLER="${TMPDIR:-/tmp}/dotnet-install.sh"
curl -fsSL --retry 5 --retry-delay 2 --retry-all-errors \
  https://dot.net/v1/dotnet-install.sh -o "$INSTALLER"
if [ ! -s "$INSTALLER" ]; then
  echo "Installer download was empty; fetching CDN URL directly." >&2
  curl -fsSL --retry 5 --retry-delay 2 --retry-all-errors \
    https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.sh \
    -o "$INSTALLER"
fi
if [ ! -s "$INSTALLER" ]; then
  echo "ERROR: could not download dotnet-install.sh." >&2
  exit 1
fi

for attempt in 1 2 3; do
  if bash "$INSTALLER" --channel "$DOTNET_VERSION" --install-dir "$HOME/.dotnet"; then
    break
  fi

  [ "$attempt" -eq 3 ] && exit 1
  echo "SDK download attempt $attempt failed; retrying..." >&2
  sleep 5
done
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

if [ ! -x "$DOTNET_ROOT/dotnet" ]; then
  echo "ERROR: .NET SDK was not installed at $DOTNET_ROOT/dotnet." >&2
  exit 1
fi

# appsettings.json is listed in the service-worker asset manifest with its hash, so it
# must be rewritten before publish. Editing the published file would break the
# offline cache install (integrity mismatch).
printf '{\n  "ApiBaseUrl": "%s"\n}\n' "$API_BASE_URL" \
  > src/FitTrack.Web/wwwroot/appsettings.json
echo "API base URL: $API_BASE_URL"

"$DOTNET_ROOT/dotnet" publish src/FitTrack.Web -c Release -o out --nologo

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
