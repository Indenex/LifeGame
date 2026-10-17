#!/usr/bin/env bash
# Composite an art-look image side by side with a docs/art-reference image (PM rule 2026-10-17):
# ours on the left, the reference on the right, both scaled to the same height.
#   tools/render/pair.sh <ours.png> <reference name, e.g. 02-forest-meadow> [out.png]
set -euo pipefail
ours=$1; ref=$2
out=${3:-${ours%.png}-vs-$ref.png}
repo=$(cd "$(dirname "$0")/../.." && pwd)
tmp=$(mktemp -d)
git -C "$repo" show "origin/main:docs/art-reference/$ref.webp" > "$tmp/ref.webp"
h=$(identify -format %h "$ours")
convert "$tmp/ref.webp" -resize "x$h" "$tmp/ref.png"
convert "$ours" "$tmp/ref.png" +append "$out"
rm -rf "$tmp"
echo "$out"
