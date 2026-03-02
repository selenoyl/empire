#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION="${CONFIGURATION:-Release}"
RUNTIME="${RUNTIME:-win-x64}"
VERSION="${VERSION:-1.0.0}"
SELF_CONTAINED="${SELF_CONTAINED:-false}"

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ARTIFACTS_ROOT="$REPO_ROOT/artifacts"
PUBLISH_DIR="$ARTIFACTS_ROOT/publish/$RUNTIME"
ZIP_PATH="$ARTIFACTS_ROOT/Empire-$VERSION-$RUNTIME.zip"

mkdir -p "$ARTIFACTS_ROOT"

echo "==> Restoring"
dotnet restore "$REPO_ROOT/Empire.sln"

echo "==> Testing"
dotnet test "$REPO_ROOT/Empire.sln" -c "$CONFIGURATION"

echo "==> Publishing Game (.exe)"
dotnet publish "$REPO_ROOT/Game/Game.csproj" \
  -c "$CONFIGURATION" \
  -r "$RUNTIME" \
  --self-contained "$SELF_CONTAINED" \
  -p:PublishSingleFile=false \
  -p:Version="$VERSION" \
  -o "$PUBLISH_DIR"

echo "==> Preparing logs folder"
mkdir -p "$PUBLISH_DIR/logs"

echo "==> Creating zip: $ZIP_PATH"
if command -v powershell >/dev/null 2>&1; then
  powershell -NoProfile -Command "Compress-Archive -Path '$PUBLISH_DIR/*' -DestinationPath '$ZIP_PATH' -Force"
else
  (cd "$PUBLISH_DIR" && zip -r "$ZIP_PATH" . >/dev/null)
fi

echo "Done. Launch: $PUBLISH_DIR/Game.exe"
