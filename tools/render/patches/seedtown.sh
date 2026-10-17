#!/usr/bin/env bash
# Render-tree-only patch (never committed): adds the SeedTown and SeedInvite reducers (claims-19
# stills) and the opentown.sh timer. Run the scene with `act SeedTown` / `act SeedInvite`.
set -euo pipefail
here=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
cp "$here/RenderSeed.cs.txt" "$1/server/spacetimedb/RenderSeed.cs"
"$here/opentown.sh" "$1"
