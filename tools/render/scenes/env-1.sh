#!/usr/bin/env bash
# env-1 before shots. Camera yaw: the game camera starts at 45 deg; each
# --screenshot-yaw step turns it 45 deg. -3 looks toward +x, 1 toward -x.
set -euo pipefail
cd "$(dirname "$0")/../../.."
ref=${1:-1ade969}
shots=()
for t in 0.5 0.76; do
  shots+=("--time-of-day=$t --rain=0 --cloud=0.2 --debug-position=0,0 --screenshot-yaw=-3")
  shots+=("--time-of-day=$t --rain=0 --cloud=0.2 --debug-position=-20,-2 --screenshot-yaw=1")
  shots+=("--time-of-day=$t --rain=0 --cloud=0.2 --debug-position=-46,-46")
done
shots+=("--time-of-day=0.5 --rain=0 --cloud=0.2 --debug-position=0,0 --render-cam=0.5,40,0,0")
tools/render/render.sh env-1 "$ref" "${shots[@]}"
