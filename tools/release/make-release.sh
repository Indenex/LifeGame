#!/usr/bin/env bash
# Packs one green main build into release assets (release-1, D-131).
#
# Usage: [LAUNCHER_VERSION=v] tools/release/make-release.sh <artifacts-dir> <out-dir> <build-number> <sha> [prs.json]
# [prs.json] is tools/release/merged-prs.sh's output: the PRs this release covers. If any is
# labelled world-wipe the release is marked as a wipe. Without it the release covers no PRs.
# <artifacts-dir> holds the Build run's artifacts as actions/download-artifact lays them out:
#   LifeGame-Server/StdbModule.wasm, LifeGame-Linux/linux/..., LifeGame-Windows/windows/...
# Writes to <out-dir>: LifeGame-Linux.zip, LifeGame-Windows.zip, StdbModule.wasm,
# release.json, and notes.md (the release body; the launcher reads its world-wipe marker).
set -euo pipefail
art=$1 out=$2 build=$3 sha=$4 prs_file=${5:-}
mkdir -p "$out"
out=$(cd "$out" && pwd)
prs=$( [ -n "$prs_file" ] && cat "$prs_file" || echo '[]')
wipe=$(jq -r 'any(.[]; .wipe)' <<<"$prs")

# Never ship a game without its .NET assemblies (builds 1092 to 1096 did).
"$(dirname "$0")/../ci/check-export.sh" "$art/LifeGame-Linux/linux" "$art/LifeGame-Windows/windows"

cp "$art/LifeGame-Server/StdbModule.wasm" "$out/StdbModule.wasm"
# Each zip holds the platform folder's contents at its root, so the launcher unpacks it as is.
(cd "$art/LifeGame-Linux/linux" && chmod +x LifeGame.x86_64 && zip -qry "$out/LifeGame-Linux.zip" .)
(cd "$art/LifeGame-Windows/windows" && zip -qry "$out/LifeGame-Windows.zip" .)
for l in Linux Windows; do
  if [ -d "$art/LifeGameLauncher-$l" ]; then
    # Artifacts lose the exec bit; restore it so the unzipped launcher runs on a double-click.
    chmod +x "$art/LifeGameLauncher-$l"/LifeGameLauncher.* 2>/dev/null || true
    (cd "$art/LifeGameLauncher-$l" && zip -qry "$out/LifeGameLauncher-$l.zip" .)
  fi
done

sha256() { sha256sum "$1" | cut -d' ' -f1; }
# The launcher section, when this release carries the launcher (LAUNCHER_VERSION from
# tools/release/launcher-version.sh; the launcher updates itself when it differs).
launcher_json() {
  [ -f "$out/LifeGameLauncher-Linux.zip" ] && [ -f "$out/LifeGameLauncher-Windows.zip" ] || return 0
  printf ',\n  "launcher": {\n    "version": "%s",\n' "${LAUNCHER_VERSION:-unknown}"
  printf '    "linux": { "file": "LifeGameLauncher-Linux.zip", "exe": "LifeGameLauncher.x86_64", "sha256": "%s" },\n' "$(sha256 "$out/LifeGameLauncher-Linux.zip")"
  printf '    "windows": { "file": "LifeGameLauncher-Windows.zip", "exe": "LifeGameLauncher.exe", "sha256": "%s" }\n  }' "$(sha256 "$out/LifeGameLauncher-Windows.zip")"
}
cat > "$out/release.json" <<JSON
{
  "build": $build,
  "sha": "$sha",
  "world_wipe": $wipe,
  "prs": $(jq -c 'map({number, title, wipe})' <<<"$prs"),
  "server": { "file": "StdbModule.wasm", "sha256": "$(sha256 "$out/StdbModule.wasm")" },
  "client": {
    "linux": { "file": "LifeGame-Linux.zip", "exe": "LifeGame.x86_64", "sha256": "$(sha256 "$out/LifeGame-Linux.zip")" },
    "windows": { "file": "LifeGame-Windows.zip", "exe": "LifeGame.exe", "sha256": "$(sha256 "$out/LifeGame-Windows.zip")" }
  }$(launcher_json)
}
JSON

{
  echo "LifeGame build $build, from main ${sha:0:7}."
  echo
  jq -r '.[] | "- #\(.number) \(.title)\(if .wipe then " (world wipe)" else "" end)"' <<<"$prs"
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
