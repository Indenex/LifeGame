#!/usr/bin/env bash
# Bramble's and Pebble's heads and face atlases (art-rnd-1): model and bake in Blender,
# paint, and copy the game files to client/assets/art/heads/. Needs Blender 4.2
# (BLENDER=path, or ~/tools/blender/blender-4.2.3-linux-x64/blender) and python3 with
# numpy and Pillow. About a minute and a half.
set -euo pipefail
here=$(cd "$(dirname "$0")" && pwd)
repo=$(cd "$here/../../.." && pwd)
blender=${BLENDER:-$HOME/tools/blender/blender-4.2.3-linux-x64/blender}
work=$(mktemp -d)
"$blender" -b --factory-startup -P "$here/blender_build.py" -- "$work" 2>&1 | grep -E "baked|exported|Error" || true
python3 -I "$here/paint.py" "$work" "$work/tex"
dest=$repo/client/assets/art/heads
mkdir -p "$dest"
for name in bramble pebble; do
  cp "$work/${name}_head.glb" "$work/${name}_face.glb" "$work/tex/${name}_head.png" "$work/tex/${name}_face.png" "$dest/"
done
rm -rf "$work"
echo "heads -> $dest"
