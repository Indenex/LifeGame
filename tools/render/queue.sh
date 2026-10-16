#!/usr/bin/env bash
# The Render lane's work queue: one render at a time (4 cores can't run two).
# Run detached: nohup tools/render/queue.sh > $RENDER_WORK/queue.log 2>&1 &
# Jobs are lines in tools/render/queue.jobs ("<name> <command...>"), reread between
# jobs so they can be added or reordered while it runs. Finished names go to
# $RENDER_WORK/queue.done and are skipped, so a rerun after a crash resumes.
cd "$(dirname "$0")/../.."
export RENDER_WORK=${RENDER_WORK:-/tmp/lifegame-render} RENDER_OUT=${RENDER_OUT:-$PWD/renders}
done_file=$RENDER_WORK/queue.done; touch "$done_file"
jobs=tools/render/queue.jobs
# Wait for anything already rendering to finish.
while pgrep -f "^[^ ]*/Godot_v[^ ]*_mono_linux.x86_64" >/dev/null || [ -n "$(ps -eo args | grep -E '^bash tools/render/(render|scene)\.sh')" ]; do sleep 10; done
while true; do
  line=$(grep -v '^\s*#' "$jobs" | grep -v '^\s*$' | while read -r name rest; do grep -qx "$name" "$done_file" || { echo "$name $rest"; break; }; done)
  [ -n "$line" ] || { echo "== queue empty"; sleep 30; continue; }
  read -r name rest <<<"$line"
  echo "== $name"
  eval "$rest" 2>&1
  echo "$name" >> "$done_file"; echo "== done $name"
done
