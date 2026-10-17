#!/usr/bin/env bash
# Kills a render.sh Godot that keeps running more than 90 s after its --screenshot PNG was written (it sometimes hangs on quit).
while true; do
  for pid in $(pgrep -f "Godot_v.*--screenshot="); do
    out=$(tr '\0' '\n' < /proc/$pid/cmdline 2>/dev/null | sed -n 's/^--screenshot=//p' | head -1)
    [ -n "$out" ] && [ -f "$out" ] || continue
    age=$(( $(date +%s) - $(stat -c %Y "$out") ))
    [ $age -gt 90 ] && { echo "$(date +%T) wd: killing hung Godot $pid ($out)"; kill $pid; }
  done
  sleep 20
done
