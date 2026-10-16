#!/usr/bin/env bash
# crafting-15 (crafting table): the Crafting lane's CraftCheck bot claims land and builds
# a table; then a separate camera client stands by it (client-side --debug-position) for
# a still from ~3 m and a still with the C panel open.
#   tools/render/scenes/crafting-15.sh [ref]
set -euo pipefail
here=$(cd "$(dirname "$0")/.." && pwd)
ref=${1:-859f25e}
ticket=crafting-15
source "$here/lib.sh"

(cd "$tree/tools/CraftCheck" && dotnet build -c Release) >>"$log" 2>&1 || { echo "CraftCheck build failed, see $log"; exit 1; }
botlog=$work/craftcheck.log
(cd "$tree/tools/CraftCheck" && exec dotnet run -c Release --no-build -- "http://127.0.0.1:$port" lifegame) >"$botlog" 2>&1 &
bot=$!
client=$bot; xvfb=$bot
trap 'kill $bot $client $xvfb $server 2>/dev/null || true' EXIT
until grep -q "Built a crafting table at" "$botlog"; do
  kill -0 $bot 2>/dev/null || { echo "CraftCheck ended before the table:"; tail -5 "$botlog"; exit 1; }
  sleep 2
done
read -r tx tz < <(grep -o "Built a crafting table at ([-0-9.]*, [-0-9.]*)" "$botlog" | head -1 | sed 's/.*(\(.*\), \(.*\))/\1 \2/')
echo "table at $tx, $tz"
px=$(python3 -c "print($tx + 1.6)"); pz=$(python3 -c "print($tz + 1.6)")

size=1600x900
disp=:$((90 + port % 9))
Xvfb "$disp" -screen 0 "${size}x24" -nolisten tcp >>"$log" 2>&1 &
xvfb=$!
rm -f "$HOME/.local/share/godot/app_userdata/LifeGame/token_camera.txt"
(cd "$tree/client" && DISPLAY=$disp exec "$godot" --path . --resolution "$size" -- \
  "--server=http://127.0.0.1:$port" --db=lifegame --profile=camera --time-of-day=0.45 --rain=0 --cloud=0.2 \
  "--debug-position=$px,$pz" --render-cam=3.4,1.6,0,0.2) >>"$log" 2>&1 &
client=$!
n=0
snap() { n=$((n + 1)); DISPLAY=$disp import -window root "$RENDER_OUT/$ticket-$sha-$n.png" && echo "saved $RENDER_OUT/$ticket-$sha-$n.png"; }
sleep 45
snap; sleep 2; snap
DISPLAY=$disp xdotool search --name LifeGame windowfocus key c
sleep 4; snap
DISPLAY=$disp xdotool search --name LifeGame windowfocus key Tab
sleep 4; snap
DISPLAY=$disp xdotool search --name LifeGame windowfocus key Tab
sleep 4; snap
cat "$botlog" >>"$log"
