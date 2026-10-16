#!/usr/bin/env bash
# ui-9 mock trade window (pretend partner Mara). Its timers run from game start, so
# game time crawls at 5% for the first 45 s while the world loads (--render-slowstart).
set -euo pipefail
cd "$(dirname "$0")/../../.."
ref=${1:-e4319e5}
s="--time-of-day=0.45 --screenshot-hud --render-slowstart=0.05,45"
RENDER_SIZE=1280x720 tools/render/render.sh ui-9 "$ref" \
  "$s --screenshot-trade --screenshot-delay=2.1" \
  "$s --screenshot-trade --screenshot-delay=6" \
  "$s --screenshot-trade=accepted --screenshot-delay=6" \
  "$s --screenshot-trade=changed --screenshot-delay=4.5" \
  "$s --screenshot-trade=asked --screenshot-delay=6"
