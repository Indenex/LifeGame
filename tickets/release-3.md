# release-3: Two launcher builds (host and player)
Lane: Release & Launcher (D-131)
Owner: Release & Launcher lane (Sonnet, D-159)
Status: todo
Branch: claude/release-3 (create from main after #246 merges)
PR: (none yet)

## Goal
Justin chose "two launcher builds", both from the same release:
1. **Host launcher** (today's): installs, starts and updates SpacetimeDB and the module on the host's PC, then plays. Unchanged behaviour.
2. **Player launcher**: no server parts at all (no SpacetimeDB install or start, no world update, no wipe question, no module publish). It updates the game client only and connects to the host's server address. The address lives in one place and is never a secret in the repo.

A friend downloads the player launcher once; it updates itself and the game from each release and plays on Justin's server. Never touch Justin's real server while building or testing this.

## Plan (proposed to the PM, not yet confirmed)
- launcher.gd already has `--no-server` and `--server=URL`. Add a `mode` (host | player) read from a small `mode.cfg` baked in at export, so a double-click needs no command-line options. `--no-server` stays as an override.
- Player mode hides: world button, wipe overlay, SpacetimeDB install, server start, "World:" build line (shows the server address instead). It keeps: game update, self-update, shortcut, Play.
- Server address, one place: `server.cfg` next to the player launcher (editable by hand), written at export from the repository variable `PLAYER_SERVER_URL` (Settings > Variables, set by Justin; not a secret, not committed). Order: `--server` > `server.cfg` > none. With no address the player launcher says plainly "No server address yet" and does not guess.
- Export: release.yml `launcher` job exports each preset twice (host, player) with a different `mode.cfg`; assets become `LifeGameLauncher-{Linux,Windows}.zip` (host, same names as today so existing hosts self-update) and `LifeGamePlayer-{Linux,Windows}.zip`. release.json gets a `player` section (version, sha256s). Player and host share one `launcher.version` hash so they update together.
- Self-update picks its own flavour's asset (mode decides which section of release.json).
- Shortcut names: "LifeGame" (host) and "LifeGame (player)" so both can coexist.
- Tests (Linux, fake API + local spacetime on a test port only): player mode with `--auto --no-play --quit` updates the client and never runs `spacetime`; with a fake server address Play passes `--server=`; no address shows the message; self-update stays inside the flavour.
- Docs: `launcher/README.md` and PLAYING.md get a short "Joining Justin's server" section; ask Wiki lane for player-facing wording if wanted.

## Progress log
- 2026-10-16 20:40: ticket written; waiting on #246 and the wipe release first.

## Open questions / blockers
- Server address source: repository variable `PLAYER_SERVER_URL` set by Justin (recommended: nothing in the repo, rebuilt into each player launcher), or a hand-edited `server.cfg` only. Needs the PM to ask Justin to set the variable once, no secret involved.
- Does Justin's server need to be reachable by hostname/port (his router or a tunnel)? Not the launcher's job; the PM should confirm how friends reach it.

## Next step
After #246 merges and the wipe release is verified: branch claude/release-3 from main, add `mode`/`server.cfg` handling to launcher/launcher.gd with player-mode hiding, then the two-flavour export in release.yml and make-release.sh.
