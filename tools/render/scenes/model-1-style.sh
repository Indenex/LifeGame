#!/usr/bin/env bash
# model-1 style options (37a5656): the standalone Showcase scene, one PNG per view.
set -euo pipefail
here=$(cd "$(dirname "$0")/.." && pwd)
ticket=model-1-style; ref=${1:-37a5656}
source "$here/lib.sh"
n=0
for view in row people faces sheep walk; do
  n=$((n + 1)); out=$RENDER_OUT/$ticket-$sha-$n-$view.png
  (cd "$tree/client" && timeout 1200 xvfb-run -a -s "-screen 0 1600x900x24" "$godot" --path . --resolution 1600x900 \
    res://scripts/models/Showcase.tscn -- --view=$view "--shot=$out") >>"$log" 2>&1 || true
  if [ -s "$out" ]; then echo "  saved $out"; else echo "  no image for $view (see $log)"; fi
done
