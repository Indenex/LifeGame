#!/usr/bin/env bash
# Render-tree-only patch (never committed): the build menu switches tab when the file
# /tmp/render-tab appears (its text is the tab name; the menu deletes it), for scenes where clicking
# tabs is unreliable at ~0.5 fps. A scene step `sh echo Storage > /tmp/render-tab` does it.
set -euo pipefail
f=$1/client/scripts/ui/BuildMenu.cs
grep -q render-tab "$f" && exit 0
sed -i 's|^\(\s*\)public void Open()|\1public override void _Process(double delta)\n\1{\n\1    if (System.IO.File.Exists("/tmp/render-tab")) { var want = System.IO.File.ReadAllText("/tmp/render-tab").Trim(); System.IO.File.Delete("/tmp/render-tab"); if (Order.FirstOrDefault(c => c.Equals(want, System.StringComparison.OrdinalIgnoreCase)) is { } hit) { category = hit; Refresh(); } }\n\1}\n\n&|' "$f"
grep -q render-tab "$f"
