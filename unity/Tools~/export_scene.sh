#!/usr/bin/env bash
# Exports the Godot scene for the Unity spike (unity-1).
#   unity/Tools~/export_scene.sh <render checkout client dir> <out dir> [client options...]
# The render checkout comes from tools/render/render.sh (it builds the client);
# this adds the spike_export.gd autoload and runs one offline client.
# Example:
#   unity/Tools~/export_scene.sh /tmp/lifegame-render/1321e72/client unity/Assets/StreamingAssets/spike/noon --time-of-day=0.5
set -euo pipefail
client=$1; out=$(mkdir -p "$2" && cd "$2" && pwd); shift 2
here=$(cd "$(dirname "$0")" && pwd)
export PATH="$HOME/.dotnet:$PATH" DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
godot=${GODOT:-$(ls "$HOME"/tools/godot/*/Godot_v*_mono_linux.x86_64 | head -1)}
cp "$here/spike_export.gd" "$client/spike_export.gd"
# render_cam.gd gives --render-cam=distance,height,yaw[,look] for other views.
cp "$here/../../tools/render/render_cam.gd" "$client/render_cam.gd"
printf '[physics]\n\ncommon/physics_ticks_per_second=20\ncommon/max_physics_steps_per_frame=60\n\n[autoload]\n\nRenderCam="*res://render_cam.gd"\nSpikeExport="*res://spike_export.gd"\n' > "$client/override.cfg"
size=${RENDER_SIZE:-1600x900}
(cd "$client" && timeout 1500 xvfb-run -a -s "-screen 0 ${size}x24" "$godot" --path . --resolution "$size" -- \
  --server=http://127.0.0.1:1 --db=lifegame --profile=spike --rain=0 --cloud=0.2 --debug-position=0,0 \
  "--spike-export-dir=$out" --spike-export-delay=${SPIKE_DELAY:-150} "$@") > "$out.log" 2>&1 || true
grep "spike-export" "$out.log" || { echo "export failed, see $out.log"; exit 1; }
