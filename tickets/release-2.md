# release-2: Launcher polish: shortcut, self-update, server start, SpacetimeDB install
Lane: Release & Launcher (D-131)
Owner: Release & Launcher thread (session_015Je3JR5vZrzA7TwQYBSPgM)
Status: in_progress
Branch: claude/release-1 (restarted from main after #161; release-2 work continues here)
PR: (not yet)

## Goal
Justin does zero work after downloading the launcher once (PM, D-145 never idle):
1. First run puts a LifeGame shortcut on his desktop and in the app menu (Linux .desktop, Windows .lnk).
2. The launcher updates itself: each release carries a launcher version (hash of `launcher/`); when it differs from the running launcher, the launcher downloads the new one, swaps itself in place and restarts. Unchanged launcher, no download.
3. The local SpacetimeDB server is started automatically, before updating the world and before Play.
4. If SpacetimeDB isn't installed, the launcher says so plainly and offers one button that runs SpacetimeDB's official installer.

## Progress log
- 2026-10-16 20:35: started. Read SpacetimeDB's official installers: Linux `curl -sSf https://install.spacetimedb.com | sh -s -- --yes` (installs to ~/.local/bin/spacetime); Windows `iwr https://windows.spacetimedb.com -useb | iex` (installs to %LOCALAPPDATA%\SpacetimeDB, adds it to PATH, shows its own installer window). Both install the latest SpacetimeDB, not 2.12.

- 2026-10-16 20:45: version stamp: `tools/release/launcher-version.sh` (hash of tracked `launcher/` files) goes into `launcher/version.txt` at export and into release.json `launcher.version`; make-release.sh adds the launcher section. Icon (`launcher/icon.png`, `icon.ico`; two pines and a low sun), set as the project icon and the Windows exe icon.
- 2026-10-16 20:55: launcher.gd: first-run shortcuts (Linux: lifegame.desktop in ~/.local/share/applications and the XDG desktop folder, chmod +x, gio trusted; Windows: LifeGame.lnk on the Desktop and in Start Menu Programs via PowerShell WScript.Shell); self-update (download, sha256 check, rename the running exe to .old, place the new one, restart with the same options plus --updated-from, which stops loops; the .old file is removed on the next start); `_ensure_server()` before publishing and before Play; when `spacetime` is missing the main button becomes "Install SpacetimeDB and play" and runs the official installer, then carries on.
- 2026-10-16 21:00: tested on Linux: shortcuts written to a fake HOME (both .desktop files, executable); launcher v1 to v2 self-update against a fake release with launcher version v2: swapped, restarted itself (--headless carried over) and updated world and game to 481; SpacetimeDB installer from the launcher in an empty HOME installed 2.12.0 to ~/.local/bin and the launcher found it and carried on (the publish then failed only because that fresh test login doesn't own the test database, as expected). Windows paths (shortcut, exe swap) are untested here.
- 2026-10-16 21:05: PM: releases kept being skipped because main Builds were cancelled by newer merges (only 639, 680 green since 19:00). release.yml now has a `pick` job plus `schedule` (every 30 min) and `workflow_dispatch`: they release the newest green push Build of main unless its commit already has a release. Tested the pick query read-only on the real repo (Build 680, 02153f2, not yet released).

- 2026-10-16 21:15: first real release: https://github.com/Indenex/LifeGame/releases/tag/build-680-02153f2 (Release run 38956847322 green, from Build 680). Assets and release.json sha256 match. Found: upload-artifact drops the exec bit, so LifeGameLauncher.x86_64 in that zip isn't executable; make-release.sh now restores it (checked). With the bit set by hand, the release's own Linux launcher started the local server, updated the existing world to 680 (data kept), and installed the game.

## Open questions / blockers
- (none yet)

## Next step
Wait for CI on the release-2 PR; send the PM the green sha; after merge check that the next release's launcher zip is executable and carries launcher.version.
