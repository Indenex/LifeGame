# release-2: Launcher polish: shortcut, self-update, server start, SpacetimeDB install
Lane: Release & Launcher (D-131)
Owner: Release & Launcher lane; handed off by session_015Je3JR5vZrzA7TwQYBSPgM to a fresh Sonnet session (D-159)
Status: in_review
Branch: claude/release-1 (restarted from main after #161; release-2 work continues here)
PR: #201

## Goal
Justin does zero work after downloading the launcher once (PM, D-145 never idle):
1. First run puts a LifeGame shortcut on his desktop and in the app menu (Linux .desktop, Windows .lnk).
2. The launcher updates itself: each release carries a launcher version (hash of `launcher/`); when it differs from the running launcher, the launcher downloads the new one, swaps itself in place and restarts. Unchanged launcher, no download.
3. The local SpacetimeDB server is started automatically, before updating the world and before Play.
4. If SpacetimeDB isn't installed, the launcher says so plainly and offers one button that runs SpacetimeDB's official installer.

## How releases work (for a fresh session)
- `.github/workflows/release.yml` (this lane owns it; Integration reviews). Job `pick` chooses a Build run: on `workflow_run` the green push Build of main that just finished; on `schedule` (every 30 min) or `workflow_dispatch` the newest green push Build of main. It skips any commit that already has a release (tag suffix `-<short sha>`). Then `launcher` exports `launcher/` for Linux and Windows (stamping `version.txt` from `tools/release/launcher-version.sh`), and `release` downloads the Build artifacts, lists the PRs since the previous release (`tools/release/merged-prs.sh`, marks a wipe if any has the `world-wipe` label, D-130), packs with `tools/release/make-release.sh`, and runs `gh release create build-<run number>-<short sha>`.
- Assets: `LifeGame-{Linux,Windows}.zip`, `StdbModule.wasm`, `LifeGameLauncher-{Linux,Windows}.zip`, `release.json` (sha256s, `world_wipe`, `prs`, `launcher.version`). The notes carry `<!-- world-wipe -->` for wipes; the launcher reads that marker.
- Launcher: `launcher/launcher.gd` (one GDScript file; options listed at its top; `launcher/README.md`). Test it without GitHub: pack with `make-release.sh`, serve with `tools/release/fake-api.py <dir>`, run the exported launcher with `-- --api=http://127.0.0.1:8765 --home=<dir> --auto --no-play --quit` against a local `spacetime start` (exit 0 ok, 1 failed, 2 stopped at a wipe). Export needs the Godot 4.6.2 mono editor and .NET 8 on PATH. Never publish to or wipe Justin's real server.
- Untested on Windows (no Windows machine here): the PowerShell `.lnk` shortcuts, the running-exe swap in self-update (renames LifeGameLauncher.exe to LifeGameLauncher.old.exe, removed on the next start), the SpacetimeDB installer path (`irm https://windows.spacetimedb.com | iex`) and the embedded exe icon. If Justin reports trouble there, start with the launcher's Details log.
- Releases so far: build-680-02153f2 (first; its Linux launcher lacks the exec bit, fixed in #201 for later releases; its launcher predates self-update, so Justin downloads the launcher once more after #201's first release).

## Progress log
- 2026-10-16 20:35: started. Read SpacetimeDB's official installers: Linux `curl -sSf https://install.spacetimedb.com | sh -s -- --yes` (installs to ~/.local/bin/spacetime); Windows `iwr https://windows.spacetimedb.com -useb | iex` (installs to %LOCALAPPDATA%\SpacetimeDB, adds it to PATH, shows its own installer window). Both install the latest SpacetimeDB, not 2.12.

- 2026-10-16 20:45: version stamp: `tools/release/launcher-version.sh` (hash of tracked `launcher/` files) goes into `launcher/version.txt` at export and into release.json `launcher.version`; make-release.sh adds the launcher section. Icon (`launcher/icon.png`, `icon.ico`; two pines and a low sun), set as the project icon and the Windows exe icon.
- 2026-10-16 20:55: launcher.gd: first-run shortcuts (Linux: lifegame.desktop in ~/.local/share/applications and the XDG desktop folder, chmod +x, gio trusted; Windows: LifeGame.lnk on the Desktop and in Start Menu Programs via PowerShell WScript.Shell); self-update (download, sha256 check, rename the running exe to .old, place the new one, restart with the same options plus --updated-from, which stops loops; the .old file is removed on the next start); `_ensure_server()` before publishing and before Play; when `spacetime` is missing the main button becomes "Install SpacetimeDB and play" and runs the official installer, then carries on.
- 2026-10-16 21:00: tested on Linux: shortcuts written to a fake HOME (both .desktop files, executable); launcher v1 to v2 self-update against a fake release with launcher version v2: swapped, restarted itself (--headless carried over) and updated world and game to 481; SpacetimeDB installer from the launcher in an empty HOME installed 2.12.0 to ~/.local/bin and the launcher found it and carried on (the publish then failed only because that fresh test login doesn't own the test database, as expected). Windows paths (shortcut, exe swap) are untested here.
- 2026-10-16 21:05: PM: releases kept being skipped because main Builds were cancelled by newer merges (only 639, 680 green since 19:00). release.yml now has a `pick` job plus `schedule` (every 30 min) and `workflow_dispatch`: they release the newest green push Build of main unless its commit already has a release. Tested the pick query read-only on the real repo (Build 680, 02153f2, not yet released).

- 2026-10-16 21:30: handoff (D-159): everything pushed; #201 waiting on CI at a42b0d5, Integration approved.
- 2026-10-16 21:25: PR #201 open; Integration approved release.yml; took its hardening (workflow_run fields via env). Ran the pick script locally for workflow_run (green main: skips the already-released 02153f2; PR run: skipped) and schedule (newest green Build already released: skipped).
- 2026-10-16 21:15: first real release: https://github.com/Indenex/LifeGame/releases/tag/build-680-02153f2 (Release run 38956847322 green, from Build 680). Assets and release.json sha256 match. Found: upload-artifact drops the exec bit, so LifeGameLauncher.x86_64 in that zip isn't executable; make-release.sh now restores it (checked). With the bit set by hand, the release's own Linux launcher started the local server, updated the existing world to 680 (data kept), and installed the game.

## Open questions / blockers
- (none yet)

## Next step
When CI on PR #201 is green (head a42b0d5 or later), send the PM the head sha; after it merges, check the next release (Actions > Release) has an executable LifeGameLauncher.x86_64 and `launcher.version` in release.json, then tell the PM it is the launcher Justin should download.
