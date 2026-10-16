#!/usr/bin/env bash
# D-145 "what's new" tour of the latest main, for when the queue is empty.
set -euo pipefail
cd "$(dirname "$0")/../../.."
ref=${1:-origin/main}
git fetch -q origin main || true
tools/render/render.sh tour "$ref" \
  "--time-of-day=0.45 --rain=0 --cloud=0.2" \
  "--time-of-day=0.75 --rain=0 --cloud=0.2" \
  "--time-of-day=0.3 --rain=0 --cloud=0.1 --debug-position=-44,-46" \
  "--time-of-day=0.5 --rain=0 --cloud=0.2 --debug-position=-36,-16" \
  "--time-of-day=0.5 --rain=0 --cloud=0.2 --debug-position=-60,-36" \
  "--time-of-day=0.45 --cloud=0.9 --season=0.85 --snow-cover=1 --debug-position=36,-36" \
  "--time-of-day=0.45 --screenshot-hud --screenshot-trade=accepted --render-slowstart=0.05,45 --screenshot-delay=4"
