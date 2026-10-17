#!/usr/bin/env bash
# Render-tree-only patch (never committed): lets RENDER_TAB="Fences and gates" pick the build
# menu tab for --screenshot-buildmenu (a tab name with spaces can't go through render.sh's shot
# string). Run render.sh once per tab with RENDER_TAB set.
set -euo pipefail
f=$1/client/scripts/ui/BuildMenu.cs
grep -q RENDER_TAB "$f" && exit 0
sed -i 's|string wanted = arg.Split(.=.).ElementAtOrDefault(1) ?? "";|string wanted = System.Environment.GetEnvironmentVariable("RENDER_TAB") ?? arg.Split(\x27=\x27).ElementAtOrDefault(1) ?? "";|' "$f"
grep -q RENDER_TAB "$f"
