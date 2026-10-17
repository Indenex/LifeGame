#!/usr/bin/env bash
# Pebble's shell (art-rnd-1): model and bake in Blender, paint, and copy the game files
# to client/assets/models/pebble_shell/. Needs Blender 4.2 (BLENDER=path, or
# ~/tools/blender/blender-4.2.3-linux-x64/blender) and python3 with numpy and Pillow.
set -euo pipefail
here=$(cd "$(dirname "$0")" && pwd)
repo=$(cd "$here/../../.." && pwd)
# tools/art/get-blender.sh installs it.
blender=${BLENDER:-$HOME/tools/blender/blender-4.2.3-linux-x64/blender}
[ -x "$blender" ] || { echo "Blender not found at $blender; run tools/art/get-blender.sh or set BLENDER" >&2; exit 1; }
work=$(mktemp -d)
"$blender" -b --factory-startup -P "$here/blender_build.py" -- "$work" 2>&1 | grep -E "baked|exported|Error" || true
python3 -I "$here/paint.py" "$work" "$work/tex"
dest=$repo/client/assets/models/pebble_shell
mkdir -p "$dest"
cp "$work/pebble_shell.glb" "$work/tex/shell_scutes.png" "$work/tex/shell_worn.png" "$work/tex/shell_normal.png" "$dest/"
rm -rf "$work"
echo "pebble shell -> $dest"
