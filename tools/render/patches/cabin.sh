#!/usr/bin/env bash
# Render-tree-only patch (never committed): adds the SeedCabin reducer (finished log cabin with a fire inside).
set -euo pipefail
here=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
cp "$here/RenderCabin.cs.txt" "$1/server/spacetimedb/RenderCabin.cs"
