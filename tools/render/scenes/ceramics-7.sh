#!/usr/bin/env bash
# ceramics-7 (cooking pot): the Ceramics lane's own ClayCheck bot cooks berry broth
# (--pot, ~12 min) as the camera client's identity, and the screen is grabbed when it
# logs "STAGE broth-simmering" and "STAGE broth-done".
#   tools/render/scenes/ceramics-7.sh [ref] [time-of-day]
set -euo pipefail
here=$(cd "$(dirname "$0")/.." && pwd)
ref=${1:-1da6609}; tod=${2:-0.45}
ticket=ceramics-7
[ "$tod" = 0.45 ] || ticket=ceramics-7-dusk
source "$here/lib.sh"

(cd "$tree/tools/ClayCheck" && dotnet build -c Release) >>"$log" 2>&1 || { echo "ClayCheck build failed, see $log"; exit 1; }
size=1600x900
disp=:$((90 + port % 9))
Xvfb "$disp" -screen 0 "${size}x24" -nolisten tcp >>"$log" 2>&1 &
xvfb=$!
userdir=$HOME/.local/share/godot/app_userdata/LifeGame
rm -f "$userdir/token_camera.txt"
(cd "$tree/client" && DISPLAY=$disp exec "$godot" --path . --resolution "$size" -- \
  "--server=http://127.0.0.1:$port" --db=lifegame --profile=camera "--time-of-day=$tod" --rain=0 --cloud=0.2 --camera-zoom=-8) >>"$log" 2>&1 &
client=$!
bot=$xvfb
trap 'kill $bot $client $xvfb $server 2>/dev/null || true' EXIT
for _ in $(seq 1 120); do [ -s "$userdir/token_camera.txt" ] && break; sleep 1; done
sleep 30   # let the camera client load its world

botlog=$work/claycheck.log
(cd "$tree/tools/ClayCheck" && exec dotnet run -c Release --no-build -- "http://127.0.0.1:$port" lifegame --pot "--token=$userdir/token_camera.txt") >"$botlog" 2>&1 &
bot=$!
n=0
snap() { n=$((n + 1)); DISPLAY=$disp import -window root "$RENDER_OUT/$ticket-$sha-$n.png" && echo "saved $RENDER_OUT/$ticket-$sha-$n.png"; }
until grep -q "STAGE broth-simmering" "$botlog"; do
  kill -0 $bot 2>/dev/null || { echo "ClayCheck ended early:"; tail -5 "$botlog"; exit 1; }
  sleep 2
done
sleep 8; for _ in 1 2 3 4; do snap; sleep 4; done
until grep -q "STAGE broth-done" "$botlog"; do kill -0 $bot 2>/dev/null || break; sleep 1; done
for _ in 1 2 3; do snap; sleep 1.5; done
cat "$botlog" >>"$log"
