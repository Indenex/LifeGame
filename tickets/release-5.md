# release-5: Player launcher up front
Lane: Release & Launcher
Owner: Release & Launcher thread
Status: in_review
Branch: claude/release-8
PR: (see below)

## Goal
Justin: "we need a lifegame player launcher too". The release page leads with two downloads, "LifeGame (play)" (player launcher, Windows and Linux, for friends) and "LifeGame Host" (server plus launcher). The player launcher is a double-click: a "Server address" box on first run (saved to LifeGame/server.cfg, `host:3000` gets `http://`), then it updates itself and the game and plays.

## Progress log
- 2026-10-17 04:40: release notes (make-release.sh) lead with the two downloads; player launcher gets an address box; headless run with the real build-1422 assets reaches "No server address yet" as expected; README updated.

## Open questions / blockers
- The `PLAYER_SERVER_URL` repo variable is still unset (bakes the host's address into the player launcher so friends skip the box). Needs Justin's host address, not a secret.

## Next step
Wait for CI on the PR, send the green sha to the PM, then verify the next release from main.
