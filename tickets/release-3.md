# release-3: Two launcher builds (host and player)
Lane: Release & Launcher (D-131)
Owner: Release & Launcher lane (Sonnet, D-159)
Status: in_review
Branch: claude/release-6
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
- 2026-10-17 00:05: built (branch claude/release-6): launcher.gd `player` mode (res://mode.txt = player, or --player): manage_server off, address from --server > server.cfg (exe dir, then home) > res://server.txt > none ("No server address yet", game not started), self-update uses release.json launcher.player, shortcut "LifeGame (player)"; release.yml launcher job matrix has host + player x Linux + Windows (player export gets mode.txt and server.txt from vars.PLAYER_SERVER_URL), artifacts LifeGamePlayer-{Linux,Windows}; make-release.sh packs LifeGamePlayer-*.zip and adds launcher.player to release.json. Tested locally: make-release output + release.json valid; player launcher (headless, --player, fake API) updates the client only (server_build stays 0, spacetime never run), starts the game with --server=<address from server.cfg>, and with no address says so and doesn't start. NOT tested here (needs a real export in CI): the exported player exe reading mode.txt, the player self-update swap, Windows. Verify on the first release after merge.
- 2026-10-17 00:30: Justin asked for a Refresh button ("hit a refresh button to check for updates again"). Added to the launcher (both builds): `Refresh` button re-runs `_refresh` without restarting, disabled while busy, shows "Checking...". Tested headless: first check saw build 1200; a 1201 release added to the fake API appeared after Refresh; button text/disabled during and after as expected; a second press while busy is ignored. Branch claude/release-7.

## Open questions / blockers
- Server address source: repository variable `PLAYER_SERVER_URL` set by Justin (recommended: nothing in the repo, rebuilt into each player launcher), or a hand-edited `server.cfg` only. Needs the PM to ask Justin to set the variable once, no secret involved.
- Does Justin's server need to be reachable by hostname/port (his router or a tunnel)? Not the launcher's job; the PM should confirm how friends reach it.

## Next step
Merge the PR; on the first release after it, check release.json has launcher.player, LifeGamePlayer-Linux.zip is executable, and run the player exe (no server.cfg) to see "No server address yet"; then test the player self-update with an older player build.
