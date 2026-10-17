#!/usr/bin/env bash
# art-rnd-1 turntable: five angles of one chibi look, standalone Showcase scene (no server).
# Usage: art-rnd-1-turntable.sh <ref> <name> [extra Showcase args...]
#   e.g. art-rnd-1-turntable.sh origin/claude/model-11-pebble-shell before-scutes --style=1
# Renders a detached worktree of <ref> into $RENDER_OUT/<name>.png (default ./renders).
# If <ref> has no Turntable() view yet, bdfa9163's Showcase.cs diff is applied (render checkout only).
set -euo pipefail
here=$(cd "$(dirname "$0")" && pwd); repo=$(cd "$here/../../.." && pwd)
ref=${1:?ref}; name=${2:?name}; shift 2
export PATH="$HOME/.dotnet:$HOME/stdb:$PATH" DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
godot=${GODOT:-$(ls "$HOME"/tools/godot/*/Godot_v*_mono_linux.x86_64 | head -1)}
RENDER_OUT=${RENDER_OUT:-$repo/renders}; mkdir -p "$RENDER_OUT"; RENDER_OUT=$(cd "$RENDER_OUT" && pwd)
sha=$(git -C "$repo" rev-parse --short=7 "$ref^{commit}")
work=${RENDER_WORK:-${TMPDIR:-/tmp}/lifegame-render}; tree=$work/turntable-$sha; log=$tree.log
mkdir -p "$work"
[ -d "$tree" ] || git -C "$repo" worktree add --detach "$tree" "$sha" >>"$log" 2>&1
if ! grep -q 'Turntable()' "$tree/client/scripts/models/Showcase.cs"; then
  git -C "$repo" show bdfa9163 -- client/scripts/models/Showcase.cs | git -C "$tree" apply >>"$log" 2>&1
fi
"$tree/tools/gen-bindings.sh" >>"$log" 2>&1
(cd "$tree/client" && dotnet build -c Debug) >>"$log" 2>&1 || { echo "client build failed, see $log"; exit 1; }
[ -d "$tree/client/.godot/imported" ] || (cd "$tree/client" && timeout 600 "$godot" --headless --path . --import) >>"$log" 2>&1 || true
out=$RENDER_OUT/$name.png
(cd "$tree/client" && timeout 1200 xvfb-run -a -s "-screen 0 1600x900x24" "$godot" --path . --resolution 1600x900 \
  res://scripts/models/Showcase.tscn -- --view=turntable "$@" "--shot=$out") >>"$log" 2>&1 || true
[ -s "$out" ] && echo "saved $out" || { echo "no image (see $log)"; exit 1; }
