#!/usr/bin/env bash
# Render lane scenes (D-059): a real client is the camera, and a puppet player acts
# out the scene and takes the screenshots on cue, so moments that last a few
# seconds (speech bubbles, a flock bolting) land in the frame.
#
#   tools/render/scene.sh <ticket> <git-ref> <scene file>
#
# The scene file's "client" lines give the camera client's options (as for
# render.sh); "client-as-puppet" makes the camera log in as the puppet, so it
# shows the puppet's own inventory and crafting; every other line is a puppet step (see tools/render/Puppet/Program.cs):
#
#   client --time-of-day=0.45
#   name B
#   find
#   wait 25          # let the camera client load its world
#   near 3
#   say hello
#   wait 1
#   snap
#
# Images and clips go to $RENDER_OUT as <ticket>-<short sha>-<n>.png / .mp4.
set -euo pipefail

[ $# -eq 3 ] || { sed -n 2,19p "$0"; exit 2; }
ticket=$1; ref=$2; scene=$(realpath "$3")
here=$(cd "$(dirname "$0")" && pwd)

source "$here/lib.sh"

# The puppet builds against this commit's bindings.
rm -rf "$tree/tools/render/Puppet"
mkdir -p "$tree/tools/render"
cp -r "$here/Puppet" "$tree/tools/render/Puppet"
rm -rf "$tree/tools/render/Puppet/bin" "$tree/tools/render/Puppet/obj"
(cd "$tree/tools/render/Puppet" && dotnet build -c Release) >>"$log" 2>&1 || { echo "puppet build failed, see $log"; exit 1; }

client_opts=()
as_puppet=0
steps=$work/$sha-scene.txt
: >"$steps"
while IFS= read -r line || [ -n "$line" ]; do
  case "$line" in
    client-as-puppet) as_puppet=1 ;;
    client\ *) read -r -a more <<<"${line#client }"; client_opts+=("${more[@]}") ;;
    *) printf '%s\n' "$line" >>"$steps" ;;
  esac
done <"$scene"

# A display of our own, so the puppet knows where to grab the screen.
disp=:$((90 + port % 9))
Xvfb "$disp" -screen 0 1600x900x24 -nolisten tcp >>"$log" 2>&1 &
xvfb=$!
trap 'kill $client $xvfb $server 2>/dev/null || true' EXIT
client=$xvfb
sleep 2

echo "scene: $(basename "$scene") with client ${client_opts[*]:-}"
userdir=$HOME/.local/share/godot/app_userdata/LifeGame
mkdir -p "$userdir"
rm -f "$userdir/token_camera.txt" "$work/puppet-token"

# The puppet joins first; the camera client follows (as the puppet, if asked).
PUPPET_TOKEN_OUT=$work/puppet-token DISPLAY=$disp timeout "${RENDER_SCENE_TIMEOUT:-1800}" \
  "$tree/tools/render/Puppet/bin/Release/net8.0/Puppet" "http://127.0.0.1:$port" lifegame "$steps" "$RENDER_OUT/$ticket-$sha" \
  > "$work/puppet.log" 2>&1 &
puppet=$!
if [ $as_puppet = 1 ]; then
  for _ in $(seq 1 60); do [ -s "$work/puppet-token" ] && break; sleep 0.5; done
  cp "$work/puppet-token" "$userdir/token_camera.txt"
fi
(cd "$tree/client" && DISPLAY=$disp exec "$godot" --path . --resolution 1600x900 -- \
  "--server=http://127.0.0.1:$port" --db=lifegame --profile=camera "${client_opts[@]}") >>"$log" 2>&1 &
client=$!

wait $puppet || true
cat "$work/puppet.log" >>"$log"
grep -E "^puppet: (saved|server said|never|nobody|unknown|connect|.* failed)" "$work/puppet.log" || true
