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
# Useful shot options: --time-of-day=0..1 (0 midnight, 0.5 noon), --rain=0..1,
# --cloud=, --wind=, --wetness=, --debug-position=x,z, --screenshot-yaw=steps,
# --screenshot-delay=seconds (default 40 here), --screenshot-fire,
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

repo=$(cd "$(dirname "$0")/../.." && pwd)
export PATH="$HOME/.dotnet:$HOME/stdb:$PATH" DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
godot=${GODOT:-$(ls "$HOME"/tools/godot/*/Godot_v*_mono_linux.x86_64 | head -1)}
port=${RENDER_PORT:-3300}

if [ -z "${RENDER_OUT:-}" ]; then
  # Only the real shared folder: never create /mnt/project-files on a machine without it.
  if [ -d /mnt/project-files/screenshots ] && [ -w /mnt/project-files/screenshots ] \
     && mountpoint -q /mnt/project-files 2>/dev/null; then
    RENDER_OUT=/mnt/project-files/screenshots
  else
    RENDER_OUT=$repo/renders
  fi
fi
mkdir -p "$RENDER_OUT"

sha=$(git -C "$repo" rev-parse --short=7 "$ref^{commit}")
work=${RENDER_WORK:-${TMPDIR:-/tmp}/lifegame-render}
tree=$work/$sha
log=$work/$sha.log
mkdir -p "$work"
echo "render: $ticket at $sha -> $RENDER_OUT (log $log)"

# 1. A clean checkout of the commit, kept between runs of the same sha.
if [ ! -d "$tree" ]; then
  git -C "$repo" worktree add --detach "$tree" "$sha" >>"$log" 2>&1
fi

# 2. Server module, bindings, client build.
(cd "$tree/server" && spacetimedb-cli build) >>"$log" 2>&1 || { echo "server build failed, see $log"; exit 1; }
"$tree/tools/gen-bindings.sh" >>"$log" 2>&1
(cd "$tree/client" && dotnet build -c Debug) >>"$log" 2>&1 || { echo "client build failed, see $log"; exit 1; }
if [ ! -d "$tree/client/.godot/imported" ]; then
  (cd "$tree/client" && timeout 600 "$godot" --headless --path . --import) >>"$log" 2>&1 || true
fi

# 3. A fresh in-memory server for this run.
# Its own data dir, so two renders can run at once on different RENDER_PORTs.
rm -rf "$work/stdb-$port"
spacetimedb-cli start --in-memory --data-dir "$work/stdb-$port" --listen-addr "127.0.0.1:$port" >>"$log" 2>&1 &
server=$!
trap 'kill $server 2>/dev/null || true' EXIT
for _ in $(seq 1 30); do curl -sf "http://127.0.0.1:$port/v1/ping" >/dev/null && break; sleep 1; done
(cd "$tree/server" && spacetimedb-cli publish lifegame --server "http://127.0.0.1:$port" --yes) >>"$log" 2>&1 \
  || { echo "publish failed, see $log"; exit 1; }

# 4. One client run per shot. Forward+ on llvmpipe takes a minute or two per shot.
n=0
for shot in "${shots[@]}"; do
  n=$((n + 1))
  out=$RENDER_OUT/$ticket-$sha-$n.png
  read -r -a opts <<<"$shot"
  server_opt="--server=http://127.0.0.1:$port"
  extra=()
  for o in "${opts[@]}"; do
    if [ "$o" = "--offline" ]; then server_opt="--server=http://127.0.0.1:1"; else extra+=("$o"); fi
  done
  case " $shot " in *" --screenshot-delay="*) ;; *) extra+=("--screenshot-delay=40") ;; esac
  echo "shot $n: $shot"
  (cd "$tree/client" && timeout 900 xvfb-run -a -s "-screen 0 1600x900x24" "$godot" --path . --resolution 1600x900 -- \
    "$server_opt" --db=lifegame --profile=render$n "--screenshot=$out" "${extra[@]}") >>"$log" 2>&1 || true
  if [ -s "$out" ]; then echo "  saved $out"; else echo "  no image (see $log)"; fi
done
