#!/usr/bin/env bash
# Render-tree-only patch (never committed): hold the needs gauges at RENDER_NEEDS="food,water,warmth"
# (0..1 each, e.g. "0.45,0.7,0.3") so a still shows the rings part-full in colour.
set -euo pipefail
f=$1/client/scripts/survival/NeedsHud.cs
grep -q RENDER_NEEDS "$f" && exit 0
python3 - "$f" <<'PY'
import sys
p=sys.argv[1]; s=open(p).read()
key="    public override void _Process(double delta)\n    {\n"
assert key in s
s=s.replace(key, key+'        if (System.Environment.GetEnvironmentVariable("RENDER_NEEDS") is { } rn) { var v = rn.Split(\',\'); SetNeeds(float.Parse(v[0], System.Globalization.CultureInfo.InvariantCulture), float.Parse(v[1], System.Globalization.CultureInfo.InvariantCulture), float.Parse(v[2], System.Globalization.CultureInfo.InvariantCulture), false); }\n',1)
open(p,'w').write(s)
PY
grep -q RENDER_NEEDS "$f"
