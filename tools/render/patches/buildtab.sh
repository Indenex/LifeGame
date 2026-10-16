#!/usr/bin/env bash
# Render-tree-only patch (never committed): open the build menu on the tab named in
# $RENDER_TAB ("Shelter", "Storage", "Crafting", "Fire", "Fences and gates", "Claims").
set -euo pipefail
f=$1/client/scripts/ui/BuildMenu.cs
grep -q RENDER_TAB "$f" && exit 0
sed -i 's|string category = "";|string category = System.Environment.GetEnvironmentVariable("RENDER_TAB") ?? "";|' "$f"
grep -q RENDER_TAB "$f"
