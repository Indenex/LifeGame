#!/usr/bin/env bash
# The Render lane's work queue: one render at a time (4 cores can't run two).
# Run detached: nohup tools/render/queue.sh > $RENDER_WORK/queue.log 2>&1 &
# Each finished job prints "== done <name>"; it skips jobs listed in $RENDER_WORK/queue.done,
# so a rerun after a crash picks up where it stopped.
cd "$(dirname "$0")/../.."
export RENDER_WORK=${RENDER_WORK:-/tmp/lifegame-render} RENDER_OUT=${RENDER_OUT:-$PWD/renders}
done_file=$RENDER_WORK/queue.done; touch "$done_file"
# Wait for anything already rendering (the previous queue) to finish.
while pgrep -f "^[^ ]*/Godot_v[^ ]*_mono_linux.x86_64" >/dev/null || [ -n "$(ps -eo args | grep -E '^bash tools/render/(render|scene)\.sh' )" ]; do sleep 10; done
job() {
  local name=$1; shift
  grep -qx "$name" "$done_file" && return
  echo "== $name"
  "$@" 2>&1
  echo "$name" >> "$done_file"; echo "== done $name"
}
job ui-9-mock tools/render/scenes/ui-9-mock.sh e4319e5
job crafting-11 tools/render/render.sh crafting-11 11ca4f8 "--time-of-day=0.45 --debug-position=0,0 --screenshot-fire=cold --render-cam=3.5,2.2,0,0.3" "--time-of-day=0.45 --debug-position=0,0 --screenshot-fire --render-cam=3.5,2.2,0,0.3"
job ui-10 env RENDER_SIZE=1280x720 tools/render/render.sh ui-10 1931044 "--time-of-day=0.45 --screenshot-hud --screenshot-equip --render-slowstart=0.05,45 --screenshot-delay=4" "--time-of-day=0.45 --screenshot-hud --screenshot-equip=feet --render-slowstart=0.05,45 --screenshot-delay=4"
job env-1 tools/render/scenes/env-1.sh 1ade969
job env-1-after tools/render/scenes/env-1-after.sh 0eaba8b
job model-1-style tools/render/scenes/model-1-style.sh 37a5656
job atmosphere-13 tools/render/scenes/atmosphere-13.sh
job bugfix-17 tools/render/scene.sh bugfix-17 caaf9ae tools/render/scenes/bugfix-17-pot.txt
job bugfix-17-before tools/render/scene.sh bugfix-17-before 7e79d22 tools/render/scenes/bugfix-17-pot.txt
job animals-8 tools/render/scene.sh animals-8 4233255 tools/render/scenes/animals-8-wool.txt
job animals-8-ground tools/render/scene.sh animals-8-ground 4233255 tools/render/scenes/animals-8-ground.txt
job atmosphere-66-clouds tools/render/scene.sh atmosphere-66-clouds dca2a0f tools/render/scenes/atmosphere-66-clouds.txt
job anim-1-side env RENDER_SIZE=1280x720 tools/render/scene.sh anim-1 4afbb2b tools/render/scenes/anim-1-side.txt
job anim-1-turn env RENDER_SIZE=1280x720 tools/render/scene.sh anim-1-turn 4afbb2b tools/render/scenes/anim-1-turn.txt
job anim-1-work env RENDER_SIZE=1280x720 tools/render/scene.sh anim-1-work 4afbb2b tools/render/scenes/anim-1-work.txt
# Live scenes last: they wait for the Render lane to append steps to $RENDER_WORK/live.txt.
job animals-8-spin tools/render/scene.sh animals-8-spin 4233255 tools/render/scenes/animals-8-spin.txt
job ui-9-liveA tools/render/scene.sh ui-9-liveA 0d7a326 tools/render/scenes/ui-9-live-a.txt
job ui-9-liveB tools/render/scene.sh ui-9-liveB 0d7a326 tools/render/scenes/ui-9-live-b.txt
echo "== queue empty"
