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

## Open questions / blockers
- (none yet)

## Next step
Implement version stamping (tools/release/launcher-version.sh, release.yml, make-release.sh), then self-update, shortcut, server start before Play, install button in launcher.gd.
