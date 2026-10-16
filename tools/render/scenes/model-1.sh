#!/usr/bin/env bash
# model-1 baseline: player close-ups, walk, sheep close-up, flock, huts.
set -euo pipefail
cd "$(dirname "$0")/../../.."
ref=${1:-origin/claude/model-1}
tools/render/render.sh model-1 "$ref" \
  "--time-of-day=0.5 --rain=0 --cloud=0.1 --render-cam=3,1.3,150,1.0" \
  "--time-of-day=0.5 --rain=0 --cloud=0.1 --render-cam=3,1.3,210,1.0" \
  "--time-of-day=0.5 --rain=0 --cloud=0.1 --render-cam=3,1.0,0,1.0" \
  "--time-of-day=0.5 --rain=0 --cloud=0.1 --render-cam=3,1.1,60,0.6 --render-cam-target=sheep" \
  "--time-of-day=0.5 --rain=0 --cloud=0.1 --render-cam=3,1.1,-60,0.6 --render-cam-target=sheep" \
  "--time-of-day=0.5 --rain=0 --cloud=0.1" \
  "--time-of-day=0.7 --rain=0 --cloud=0.1 --building-showcase" \
  "--time-of-day=0.7 --rain=0 --cloud=0.1 --building-showcase --screenshot-yaw=1"
tools/render/scene.sh model-1-walk "$ref" tools/render/scenes/model-1-walk.txt
