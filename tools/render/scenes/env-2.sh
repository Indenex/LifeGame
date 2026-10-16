#!/usr/bin/env bash
# env-2 land reshape, before (main 2a6b8eb) / after (ee46b71). Fresh in-memory local
# server per run, so nothing touches Justin's world. Camera yaw step 1 looks west;
# the default (0) looks south-west.
set -euo pipefail
cd "$(dirname "$0")/../../.."
shots=(
  "--time-of-day=0.5 --rain=0 --cloud=0.2 --debug-position=0,0 --render-cam=0.5,60,0,0"
  "--time-of-day=0.5 --rain=0 --cloud=0.2 --debug-position=-36,-16"
  "--time-of-day=0.76 --rain=0 --cloud=0.2 --debug-position=-36,-16"
  "--time-of-day=0.5 --rain=0 --cloud=0.2 --debug-position=-60,-36"
  "--time-of-day=0.5 --rain=0 --cloud=0.2 --debug-position=0,0 --screenshot-yaw=1"
)
tools/render/render.sh env-2-before 2a6b8eb "${shots[@]}"
tools/render/render.sh env-2-after ee46b71 "${shots[@]}"
