#!/usr/bin/env bash
# Painted trees (art-rnd-1): model and bake both trees in Blender, paint their albedo,
# and copy the game files to client/assets/art/trees/. Needs Blender 4.2 (BLENDER=path,
# or ~/tools/blender/blender-4.2.3-linux-x64/blender) and python3 with numpy and Pillow.
# About a minute on 4 CPU cores.
set -euo pipefail
here=$(cd "$(dirname "$0")" && pwd)
repo=$(cd "$here/../../.." && pwd)
blender=${BLENDER:-$HOME/tools/blender/blender-4.2.3-linux-x64/blender}
work=$(mktemp -d)
dest=$repo/client/assets/art/trees
mkdir -p "$dest"
for tree in birch pine; do
  "$blender" -b --factory-startup -P "$here/blender_build.py" -- "$work" "$tree" 2>&1 | grep -E "^(baked|exported)|Error" || true
  python3 -I "$here/paint.py" "$work" "$work" "$tree"
  cp "$work/$tree.glb" "$work/${tree}_albedo.png" "$dest/"
done
rm -rf "$work"
echo "trees -> $dest"
