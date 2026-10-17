#!/usr/bin/env bash
# Render-tree-only patch (never committed): while /tmp/render-ghost holds "x,z", the camera
# client lays out structure kind $RENDER_GHOST_KIND (default 2, claim marker) with its ghost
# pinned at that world point (still clamped to place range), so a scene can slide the
# ghost by rewriting the file (puppet: sh echo 44,-40 > /tmp/render-ghost).
set -euo pipefail
f=$1/client/scripts/building/BuildingSystem.cs
grep -q RENDER_GHOST "$f" && exit 0
sed -i 's|var target = PointUnderMouse() ?? me.Position + cameraRig.Forward \* 3.5f;|var target = RenderGhost() ?? PointUnderMouse() ?? me.Position + cameraRig.Forward * 3.5f;|' "$f"
sed -i 's|        if (placing) UpdateGhost(dt);|        if (!placing \&\& System.IO.File.Exists("/tmp/render-ghost")) StartPlacing(int.Parse(System.Environment.GetEnvironmentVariable("RENDER_GHOST_KIND") ?? "2"));\n        if (placing) UpdateGhost(dt);|' "$f"
sed -i 's|^    void UpdateGhost(float dt)$|    // RENDER_GHOST\n    static Vector3? RenderGhost()\n    {\n        try\n        {\n            var p = System.IO.File.ReadAllText("/tmp/render-ghost").Trim().Split(\x27,\x27);\n            return new Vector3(float.Parse(p[0], System.Globalization.CultureInfo.InvariantCulture), 0, float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture));\n        }\n        catch { return null; }\n    }\n\n    void UpdateGhost(float dt)|' "$f"
grep -q "RenderGhost() ??" "$f" && grep -q 'render-ghost")) StartPlacing' "$f" && grep -q "static Vector3? RenderGhost" "$f"
