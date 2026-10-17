#!/usr/bin/env bash
# Render-tree-only patch (never committed): open the Town panel from a timer (claims-19), since a
# keypress and the claim panel's Town button are unreliable at ~0.5 fps. RENDER_TOWN_AT=<seconds>.
set -euo pipefail
f=$1/client/scripts/social/SocialLayer.Town.cs
[ -f "$f" ] || exit 0
grep -q RENDER_TOWN "$f" && exit 0
sed -i 's|^\(\s*\)town.Conn = c;|\1town.Conn = c;\n\1if (System.Environment.GetEnvironmentVariable("RENDER_TOWN") is { } rt) GetTree().CreateTimer(double.Parse(System.Environment.GetEnvironmentVariable("RENDER_TOWN_AT") ?? "50")).Timeout += OpenTown;|' "$f"
grep -q RENDER_TOWN "$f"
