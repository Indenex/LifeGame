#!/usr/bin/env bash
# Installs Blender 4.2 LTS (headless use) to ~/tools/blender/blender-4.2.3-linux-x64 for the
# art build scripts under tools/art/. Run once per container; prints the binary's path.
set -euo pipefail
version=4.2.3
dir=$HOME/tools/blender
bin=$dir/blender-$version-linux-x64/blender
if [ ! -x "$bin" ]; then
    mkdir -p "$dir"
    curl -sSL "https://download.blender.org/release/Blender4.2/blender-$version-linux-x64.tar.xz" | tar -xJ -C "$dir"
fi
"$bin" --version | head -1
echo "$bin"
