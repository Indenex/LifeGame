#!/usr/bin/env bash
# atmosphere-13 before (#66 head 47a151d) / after (dc26f57): light through the forest.
set -euo pipefail
cd "$(dirname "$0")/../../.."
w="--debug-position=-44,-46 --rain=0"
shots=(
  "$w --cloud=0.1 --time-of-day=0.27" "$w --cloud=0.1 --time-of-day=0.27 --screenshot-yaw=4"
  "$w --cloud=0.1 --time-of-day=0.35" "$w --cloud=0.1 --time-of-day=0.5"
  "$w --cloud=0.1 --time-of-day=0.72" "$w --cloud=0.1 --time-of-day=0.72 --screenshot-yaw=4"
  "$w --cloud=0.9 --time-of-day=0.3"
  "--debug-position=0,0 --rain=0 --cloud=0.1 --time-of-day=0.3"
)
tools/render/render.sh atmosphere-13-before 47a151d "${shots[@]}"
tools/render/render.sh atmosphere-13-after dc26f57 "${shots[@]}"
