# Shared by render.sh and scene.sh: sets repo, godot, port, RENDER_OUT, sha, tree,
# log; checks out and builds $ref, and starts a fresh server with the module.
# Callers set ticket and ref first.

repo=$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)
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

# Software rendering runs ~0.5 fps. Godot drops game time beyond 8 physics steps
# a frame, so a 30 s screenshot timer would take ~10 real minutes. This untracked
# override (render checkout only) lets game time keep pace with the wall clock.
printf '[physics]\n\ncommon/physics_ticks_per_second=20\ncommon/max_physics_steps_per_frame=60\n' \
  > "$tree/client/override.cfg"

# 3. A fresh in-memory server for this run.
# Its own data dir, so two renders can run at once on different RENDER_PORTs.
rm -rf "$work/stdb-$port"
spacetimedb-cli start --in-memory --data-dir "$work/stdb-$port" --listen-addr "127.0.0.1:$port" >>"$log" 2>&1 &
server=$!
trap 'kill $server 2>/dev/null || true' EXIT
for _ in $(seq 1 30); do curl -sf "http://127.0.0.1:$port/v1/ping" >/dev/null && break; sleep 1; done
(cd "$tree/server" && spacetimedb-cli publish lifegame --server "http://127.0.0.1:$port" --yes) >>"$log" 2>&1 \
  || { echo "publish failed, see $log"; exit 1; }

