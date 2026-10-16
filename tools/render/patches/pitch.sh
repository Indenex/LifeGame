#!/usr/bin/env bash
# Render-tree-only patch (never committed to the game): start the camera at the pitch in
# $RENDER_PITCH degrees (e.g. -16 .. -50) so a screenshot can show each end of the
# right-drag tilt range. Usage: RENDER_PATCH=tools/render/patches/pitch.sh RENDER_PITCH=-16
set -euo pipefail
f=$1/client/scripts/CameraRig.cs
grep -q RENDER_PITCH "$f" && exit 0
sed -i 's|float pitchTarget = DefaultPitch, pitch = DefaultPitch;|float pitchTarget = StartPitch(), pitch = StartPitch();\n    static float StartPitch() => float.TryParse(System.Environment.GetEnvironmentVariable("RENDER_PITCH"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var p) ? p : DefaultPitch;|' "$f"
grep -q RENDER_PITCH "$f"
