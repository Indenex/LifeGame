# release-4: Launcher verifies the game install
Lane: Release & Launcher (D-131)
Owner: Release & Launcher lane (Sonnet, D-159)
Status: in_review
Branch: claude/release-4
PR: (see below once opened)

## Goal
Justin (Linux, build 980) started the game and Godot said "Unable to find the .NET assemblies directory ... game/data_LifeGame_linuxbsd_x86_64". The launcher must never start, or leave in place, a game folder that is missing LifeGame.pck or the data_* folder: it checks, and reinstalls automatically.

## Progress log
- 2026-10-16 23:45: reproduced the NORMAL path with the real build-980 LifeGame-Linux.zip in a scratch home (headless Godot 4.6.2, fake releases API): game/ is complete (exe, pck, data_* with 189 files), so the zip and unzip are fine. The hole: at start the launcher only checked that game/LifeGame.x86_64 exists, so a game folder with client_build 980 in state.json (hand-edited, or left half-copied by a manual install) and no data_* played as "Play" and failed in Godot. _unzip also returned true after a short write.
- 2026-10-16 23:50: fix in launcher/launcher.gd: `_client_ok(dir)` (exe + LifeGame.pck + non-empty data_*) used at start (incomplete resets client_build, button becomes "Update and play"), after unzip before swapping game.new in (incomplete unpack rejected, old game kept), and before Play (reinstall, re-download if needed); `_unzip` fails on short writes. Tested: exe-only game with state 980 gets reinstalled; a zip without the data folder is rejected and the old game stays (state unchanged, exit 1); the normal path still installs.

## Open questions / blockers
- (none)

## Next step
When CI on the PR is green, send the PM the sha; after it merges, check the next release's launcher.version changed and that an old launcher self-updates to it.
