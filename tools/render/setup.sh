#!/usr/bin/env bash
# Installs what tools/render/render.sh needs on a fresh Linux container
# (versions match .github/workflows/build.yml). Safe to re-run.
set -euo pipefail
GODOT_VERSION=4.6.2
SPACETIME_VERSION=2.12.0

if [ ! -x "$HOME/.dotnet/dotnet" ]; then
  curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel 8.0 --install-dir "$HOME/.dotnet"
fi
if [ ! -x "$HOME/stdb/spacetimedb-cli" ]; then
  mkdir -p "$HOME/stdb"
  curl -sSL "https://github.com/clockworklabs/SpacetimeDB/releases/download/v${SPACETIME_VERSION}/spacetime-x86_64-unknown-linux-gnu.tar.gz" | tar xz -C "$HOME/stdb"
fi
if ! ls "$HOME"/tools/godot/*/Godot_v*_mono_linux.x86_64 >/dev/null 2>&1; then
  mkdir -p "$HOME/tools"
  curl -sSL -o /tmp/godot.zip "https://github.com/godotengine/godot/releases/download/${GODOT_VERSION}-stable/Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64.zip"
  unzip -qo /tmp/godot.zip -d "$HOME/tools/godot"
fi
# Forward+ (what Justin sees) in software needs Mesa's Vulkan driver.
if ! dpkg -s mesa-vulkan-drivers >/dev/null 2>&1 || ! command -v xvfb-run >/dev/null || ! command -v xdotool >/dev/null; then
  apt-get update -q && apt-get install -y -q mesa-vulkan-drivers xvfb xdotool imagemagick
fi
echo "render setup done"
