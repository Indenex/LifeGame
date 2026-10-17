#!/usr/bin/env bash
# Render-tree-only patch (never committed): open the C crafting panel from a timer, for
# scenes where a keypress doesn't reach the game at ~0.5 fps. RENDER_CRAFT=<category steps>,
# RENDER_CRAFT_AT=<seconds of game time, default 60>. The --screenshot-craft-hover and
# --screenshot-craft-search flags still apply (they are read when the panel opens).
set -euo pipefail
f=$1/client/scripts/Main.cs
grep -q RENDER_CRAFT "$f" && exit 0
sed -i 's|^\(\s*\)if (Arg("screenshot") is not { } path) return;|\1if (System.Environment.GetEnvironmentVariable("RENDER_CRAFT") is { } rc \&\& int.TryParse(rc, out var rct))\n\1    GetTree().CreateTimer(double.Parse(System.Environment.GetEnvironmentVariable("RENDER_CRAFT_AT") ?? "60")).Timeout += () => crafting.Open(rct);\n&|' "$f"
grep -q RENDER_CRAFT "$f"
