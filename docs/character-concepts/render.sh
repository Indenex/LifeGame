#!/bin/sh
# Render out/*.svg to PNG with headless Chromium (storyboard only).
set -e
cd "$(dirname "$0")"
python3 make_concepts.py
python3 make_round2.py
python3 make_round3.py
CH=/opt/pw-browsers/chromium-1194/chrome-linux/chrome
for f in out/*.svg; do
  w=$(sed -n 's/.*<svg[^>]* width="\([0-9]*\)".*/\1/p' "$f" | head -1)
  h=$(sed -n 's/.*<svg[^>]* height="\([0-9]*\)".*/\1/p' "$f" | head -1)
  "$CH" --headless --no-sandbox --disable-gpu --hide-scrollbars --allow-file-access-from-files --force-device-scale-factor=1 \
    --window-size=$w,$((h+200)) --screenshot="$PWD/${f%.svg}.png" "file://$PWD/$f" >/dev/null 2>&1
  convert "${f%.svg}.png" -crop ${w}x${h}+0+0 +repage "${f%.svg}.png"
done
ls out/*.png | wc -l
