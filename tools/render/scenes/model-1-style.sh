#!/usr/bin/env bash
# model-1 style options (eb8b399): the standalone Showcase scene, one PNG per view.
set -euo pipefail
here=$(cd "$(dirname "$0")/.." && pwd)
ticket=${TICKET:-model-1-style}; ref=${1:-eb8b399}
source "$here/lib.sh"
n=0
views=${VIEWS:-row,people,faces,sheep,walk,items,items --old,held,clothes}
sep=,; case "$views" in *";"*) sep=";" ;; esac   # use ";" between views when an argument holds commas
IFS=$sep read -r -a specs <<<"$views"
for spec in "${specs[@]}"; do
  read -r view extra <<<"$spec"
  n=$((n + 1)); out=$RENDER_OUT/$ticket-$sha-$n-$view${extra:+-${extra//[^a-z0-9]/}}.png
  (cd "$tree/client" && timeout 1200 xvfb-run -a -s "-screen 0 1600x900x24" "$godot" --path . --resolution 1600x900 \
    res://scripts/models/Showcase.tscn -- --view=$view $extra "--shot=$out") >>"$log" 2>&1 || true
  if [ -s "$out" ]; then echo "  saved $out"; else echo "  no image for $view (see $log)"; fi
done
