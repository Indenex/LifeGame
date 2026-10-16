#!/usr/bin/env bash
# ui-13 (station panel): CraftCheck claims land and builds a crafting table; a separate
# camera client stands at it, is given plant fiber by owner SQL, and opens the station
# panel with G: first with too little fiber (2), then with enough (12).
#   tools/render/scenes/ui-13.sh [ref]
set -euo pipefail
here=$(cd "$(dirname "$0")/.." && pwd)
ref=${1:-2cdbc1c}
ticket=ui-13
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

size=1280x720
disp=:$((90 + port % 9))
Xvfb "$disp" -screen 0 "${size}x24" -nolisten tcp >>"$log" 2>&1 &
xvfb=$!
rm -f "$HOME/.local/share/godot/app_userdata/LifeGame/token_camera.txt"
(cd "$tree/client" && DISPLAY=$disp exec "$godot" --path . --resolution "$size" -- \
  "--server=http://127.0.0.1:$port" --db=lifegame --profile=camera --time-of-day=0.45 --rain=0 --cloud=0.2 \
  "--debug-position=$px,$pz" --render-cam=3.0,1.4,0,0.2) >>"$log" 2>&1 &
client=$!
n=0
snap() { n=$((n + 1)); DISPLAY=$disp import -window root "$RENDER_OUT/$ticket-$sha-$n.png" && echo "saved $RENDER_OUT/$ticket-$sha-$n.png"; }
key() { DISPLAY=$disp xdotool search --name LifeGame windowfocus key "$1"; }
sql() { spacetimedb-cli sql --server "http://127.0.0.1:$port" lifegame "$1" >>"$log" 2>&1; }
sleep 45
# The camera player is the newest one; give it 2 plant fiber (item 3).
cam=$(spacetimedb-cli sql --server "http://127.0.0.1:$port" lifegame "SELECT identity, player_id FROM player" 2>/dev/null | grep -o '0x[0-9a-f]*' | tail -1)
sql "INSERT INTO inventory_item (id, owner, item, quality, count) VALUES (0, $cam, 3, 50, 2)"
sleep 4
snap                      # 1. the E prompt at the table
key e; sleep 5; snap      # 3. too little fiber: amber line, Make greyed
key Escape; sleep 2
sql "INSERT INTO inventory_item (id, owner, item, quality, count) VALUES (0, $cam, 3, 50, 10)"
sleep 4
key e; sleep 5; snap      # 2. enough fiber: counts and Make
cat "$botlog" >>"$log"
