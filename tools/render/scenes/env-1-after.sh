#!/usr/bin/env bash
# env-1 after shots (0eaba8b), same spots as env-1.sh plus a glade at (36,-36).
set -euo pipefail
cd "$(dirname "$0")/../../.."
ref=${1:-0eaba8b}
shots=()
for t in 0.5 0.76; do
  shots+=("--time-of-day=$t --rain=0 --cloud=0.2 --debug-position=-2,-3 --screenshot-yaw=-3")
  shots+=("--time-of-day=$t --rain=0 --cloud=0.2 --debug-position=-20,-2 --screenshot-yaw=1")
  shots+=("--time-of-day=$t --rain=0 --cloud=0.2 --debug-position=-46,-46")
  shots+=("--time-of-day=$t --rain=0 --cloud=0.2 --debug-position=36,-36 --screenshot-yaw=1")
done
shots+=("--time-of-day=0.5 --rain=0 --cloud=0.2 --debug-position=0,0 --render-cam=0.5,40,0,0")
tools/render/render.sh env-1-after "$ref" "${shots[@]}"
