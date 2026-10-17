#!/usr/bin/env bash
# The sheep (art-rnd-1): model and bake in Blender, paint, and copy the game files to
# client/assets/art/sheep/. Needs Blender 4.2 (BLENDER=path, or
# ~/tools/blender/blender-4.2.3-linux-x64/blender) and python3 with numpy and Pillow.
set -euo pipefail
here=$(cd "$(dirname "$0")" && pwd)
repo=$(cd "$here/../../.." && pwd)
blender=${BLENDER:-$HOME/tools/blender/blender-4.2.3-linux-x64/blender}
work=$(mktemp -d)
"$blender" -b --factory-startup -P "$here/blender_build.py" -- "$work" 2>&1 | grep -E "baked|exported|Error" || true
python3 -I "$here/paint.py" "$work" "$work/tex"
dest=$repo/client/assets/art/sheep
mkdir -p "$dest"
cp "$work/sheep.glb" "$work/tex/wool.png" "$work/tex/skin.png" "$dest/"
rm -rf "$work"
echo "sheep -> $dest"
