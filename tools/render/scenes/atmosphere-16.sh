#!/usr/bin/env bash
# atmosphere-16 seasons (c41b1e6): woods at the glade edge and the spawn meadow through
# the year, then a snowfall clip. Also greps the client log for shader errors.
set -euo pipefail
cd "$(dirname "$0")/../../.."
ref=${1:-c41b1e6}
w="--time-of-day=0.45 --rain=0 --debug-position=36,-36"
m="--time-of-day=0.45 --rain=0 --debug-position=0,0"
tools/render/render.sh atmosphere-16 "$ref" \
  "$w --cloud=0.2 --season=0.3" "$w --cloud=0.2 --season=0.6" "$w --cloud=0.2 --season=0.85" \
  "$w --season=0.85 --snow-cover=1 --cloud=0.9" "$w --season=0.85 --snow=1 --snow-cover=0.6 --cloud=1" \
  "$w --cloud=0.2 --season=0.02" \
  "$m --cloud=0.2 --season=0.3" "$m --cloud=0.2 --season=0.85 --snow-cover=1"
sha=$(git rev-parse --short=7 "$ref^{commit}")
echo "shader errors in the client log:"
grep -E "SHADER ERROR|Shader compilation failed|error\(.*\): " "${RENDER_WORK:-/tmp/lifegame-render}/$sha.log" | sort -u | head -20 || echo "  none"
tools/render/scene.sh atmosphere-16-snow "$ref" tools/render/scenes/atmosphere-16-snow.txt
