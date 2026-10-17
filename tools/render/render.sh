#!/usr/bin/env bash
# Render lane capture (D-059): builds one commit, runs a local server and the
# real client, and saves one PNG per shot.
#
#   tools/render/render.sh <ticket> <git-ref> [shot ...]
#
# Each shot is a quoted string of client options (see the top of
# client/scripts/Main.cs and atmosphere/Atmosphere.cs), for example:
#
#   tools/render/render.sh render-1 origin/main \
#     "--time-of-day=0.45" "--time-of-day=0.02"
#
# RENDER_SIZE=1280x720 changes the window size (default 1600x900).
# Useful shot options: --time-of-day=0..1 (0 midnight, 0.5 noon), --rain=0..1,
# --cloud=, --wind=, --wetness=, --debug-position=x,z, --screenshot-yaw=steps,
# --screenshot-delay=seconds (default 30 here), --screenshot-fire,
# --screenshot-hud, --camera-zoom=-8, --offline (no server; the client
# shows the world without other players).
#
# Images go to $RENDER_OUT (default /mnt/project-files/screenshots if it is
# writable, else renders/ in this repo) as <ticket>-<short sha>-<n>.png.
# Needs: dotnet 8, spacetimedb-cli 2.12, Godot 4.6.2 mono (tools/render/setup.sh
# installs them), xvfb-run and mesa-vulkan-drivers for Forward+ in software.
set -euo pipefail

[ $# -ge 2 ] || { sed -n 2,20p "$0"; exit 2; }
ticket=$1; ref=$2; shift 2
shots=("$@"); [ ${#shots[@]} -gt 0 ] || shots=("--time-of-day=0.45")

source "$(dirname "$0")/lib.sh"
size=${RENDER_SIZE:-1600x900}   # e.g. RENDER_SIZE=1280x720

# 4. One client run per shot. Forward+ on llvmpipe: about a minute per shot; run one render at a time.
n=0
for shot in "${shots[@]}"; do
  n=$((n + 1))
  out=$RENDER_OUT/$ticket-$sha-$n.png
  read -r -a opts <<<"$shot"
  server_opt="--server=http://127.0.0.1:$port"
  extra=(); engine=(); shotarg=("--screenshot=$out")
  for o in "${opts[@]}"; do
    case "$o" in
      --offline) server_opt="--server=http://127.0.0.1:1" ;;
      --when-loaded) shotarg=("--screenshot-when-loaded=$out") ;;   # wait until every chunk in view is loaded
      --engine=*) engine+=(--rendering-method "${o#--engine=}") ;;   # e.g. --engine=gl_compatibility
      *) extra+=("$o") ;;
    esac
  done
  case " $shot " in *" --screenshot-delay="*) ;; *) extra+=("--screenshot-delay=30") ;; esac
  echo "shot $n: $shot"
  (cd "$tree/client" && timeout "${RENDER_SHOT_TIMEOUT:-2400}" xvfb-run -a -s "-screen 0 ${size}x24" "$godot" --path . --resolution "$size" "${engine[@]}" -- \
    "$server_opt" --db=lifegame --profile=render$n "${shotarg[@]}" "${extra[@]}") >>"$log" 2>&1 || true
  if [ -s "$out" ]; then echo "  saved $out"; else echo "  no image (see $log)"; fi
done
