#!/usr/bin/env bash
# Render-tree-only patch (never committed): stop the first-join look picker from opening over
# the screen when the server has no look for the camera player. --screenshot-look still works.
set -euo pipefail
f=$1/client/scripts/ui/LookPicker.cs
[ -f "$f" ] || exit 0
grep -q RENDER_NOLOOK "$f" && exit 0
# bugfix-41 and later: once-only guard, then the picker opens
sed -i 's|if (c.Db.PlayerLook.PlayerId.Find(MyId()) == null \&\& !Settings.Get("look", "asked", false))|if (false /* RENDER_NOLOOK */)|' "$f"
sed -i 's|if (c.Db.PlayerLook.PlayerId.Find(MyId()) == null) Open(first: true);|/* RENDER_NOLOOK */|' "$f"
grep -q RENDER_NOLOOK "$f"
