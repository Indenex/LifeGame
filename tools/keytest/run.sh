#!/usr/bin/env bash
# Runs the real client headless against a local server and presses I (see keytest.gd).
#   tools/keytest/run.sh [server=http://127.0.0.1:3000] [db=lifegame]
# Needs the Godot 4.6.2 mono binary (GODOT, or ~/tools/godot), a built client (dotnet build in client/),
# and a server with the module published. Exit code 0 means the bag opened and closed.
set -euo pipefail
server=${1:-http://127.0.0.1:3000}; db=${2:-lifegame}
root=$(cd "$(dirname "$0")/../.." && pwd)
godot=${GODOT:-$(ls "$HOME"/tools/godot/*/Godot_v*_mono_linux.x86_64 2>/dev/null | head -1)}
[ -n "$godot" ] || { echo "no Godot binary (set GODOT)"; exit 2; }
cp "$root/tools/keytest/keytest.gd" "$root/client/keytest.gd"
printf '[autoload]\n\nKeyTest="*res://keytest.gd"\n' > "$root/client/override.cfg"
trap 'rm -f "$root/client/keytest.gd" "$root/client/override.cfg"' EXIT
cd "$root/client"
out=$(timeout 180 "$godot" --headless --path . -- "--server=$server" "--db=$db" "--profile=keytest$$" 2>&1) || true
grep KEYTEST <<<"$out" || true
grep -q "KEYTEST PASS" <<<"$out"
