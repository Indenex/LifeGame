#!/usr/bin/env bash
# Render-tree-only patch (never committed): stop the first-join look picker from opening over
# the screen when the server has no look for the camera player. --screenshot-look still works.
set -euo pipefail
f=$1/client/scripts/ui/LookPicker.cs
[ -f "$f" ] || exit 0
grep -q RENDER_NOLOOK "$f" && exit 0
sed -i 's|if (c.Db.PlayerLook.PlayerId.Find(MyId()) == null) Open(first: true);|/* RENDER_NOLOOK */|' "$f"
# newer form: if (PlayerLook ... == null && !Settings.Get("look", "asked", false)) { ... Open(first: true); }
sed -i 's|if (c.Db.PlayerLook.PlayerId.Find(MyId()) == null \&\& |/* RENDER_NOLOOK */ if (false \&\& |' "$f"
grep -q RENDER_NOLOOK "$f"
