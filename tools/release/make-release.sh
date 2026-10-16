#!/usr/bin/env bash
# Packs one green main build into release assets (release-1, D-131).
#
# Usage: tools/release/make-release.sh <artifacts-dir> <out-dir> <build-number> <sha> [world-wipe=false] [pr-number] [pr-title]
# <artifacts-dir> holds the Build run's artifacts as actions/download-artifact lays them out:
#   LifeGame-Server/StdbModule.wasm, LifeGame-Linux/linux/..., LifeGame-Windows/windows/...
# Writes to <out-dir>: LifeGame-Linux.zip, LifeGame-Windows.zip, StdbModule.wasm,
# release.json, and notes.md (the release body; the launcher reads its world-wipe marker).
set -euo pipefail
art=$1 out=$2 build=$3 sha=$4 wipe=${5:-false} pr=${6:-} title=${7:-}
mkdir -p "$out"
out=$(cd "$out" && pwd)

cp "$art/LifeGame-Server/StdbModule.wasm" "$out/StdbModule.wasm"
# Each zip holds the platform folder's contents at its root, so the launcher unpacks it as is.
(cd "$art/LifeGame-Linux/linux" && chmod +x LifeGame.x86_64 && zip -qry "$out/LifeGame-Linux.zip" .)
(cd "$art/LifeGame-Windows/windows" && zip -qry "$out/LifeGame-Windows.zip" .)
for l in Linux Windows; do
  if [ -d "$art/LifeGameLauncher-$l" ]; then
    (cd "$art/LifeGameLauncher-$l" && zip -qry "$out/LifeGameLauncher-$l.zip" .)
  fi
done

sha256() { sha256sum "$1" | cut -d' ' -f1; }
cat > "$out/release.json" <<JSON
{
  "build": $build,
  "sha": "$sha",
  "world_wipe": $wipe,
  "pr": "${pr}",
  "server": { "file": "StdbModule.wasm", "sha256": "$(sha256 "$out/StdbModule.wasm")" },
  "client": {
    "linux": { "file": "LifeGame-Linux.zip", "exe": "LifeGame.x86_64", "sha256": "$(sha256 "$out/LifeGame-Linux.zip")" },
    "windows": { "file": "LifeGame-Windows.zip", "exe": "LifeGame.exe", "sha256": "$(sha256 "$out/LifeGame-Windows.zip")" }
  }
}
JSON

{
  echo "LifeGame build $build, from main ${sha:0:7}${pr:+ (#$pr ${title})}."
  echo
  if [ "$wipe" = true ]; then
    echo "<!-- world-wipe -->"
    echo "**This build wipes the world.** The launcher asks before publishing it."
  else
    echo "Updates the world in place; nothing is wiped."
  fi
  echo
  echo "Use the LifeGame launcher to update, or see PLAYING.md for the manual steps."
} > "$out/notes.md"
ls -l "$out"
