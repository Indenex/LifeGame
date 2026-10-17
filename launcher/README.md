# LifeGame launcher

One button that brings the game and your world up to the newest build, then plays.

## For Justin: once

1. Open the newest release on GitHub (Releases, on the right of the repository page) and download `LifeGameLauncher-Linux.zip` or `LifeGameLauncher-Windows.zip`.
2. Unzip it anywhere you like, such as your desktop. It keeps the game in a `LifeGame` folder next to itself.
3. Run `LifeGameLauncher.x86_64` (Linux; if it won't start, right-click it, Properties, allow running as a program) or `LifeGameLauncher.exe` (Windows).

You need nothing else beyond the `spacetime` command you already have: the launcher uses its login, so your world stays yours.

## Every time

Press **Update and play**. The launcher:

1. Finds the newest green build of `main`.
2. Downloads the game and the server module, checking each file.
3. Starts your world server if it isn't running.
4. Updates your world without wiping it (`--delete-data=never`). If a build can't update the world without a wipe, it stops, changes nothing, and says so.
5. Swaps in the new game and starts it.

When everything is current the button just says **Play**. **Update world only** updates the server without touching the game. **Details** shows what it ran.

A build labelled as a world wipe (D-130) is never published without asking: the launcher shows "This update wipes the world" with **Keep my world** as the safe choice.

## Two launchers: host and player (release-3)

Each release carries two builds of this launcher:

- **Host** (`LifeGameLauncher-Linux.zip` / `-Windows.zip`): installs, starts and updates SpacetimeDB and the world on this computer, then plays. This is the one above.
- **Player** (`LifeGamePlayer-Linux.zip` / `-Windows.zip`): no server parts at all (no SpacetimeDB install, no world update, no wipe question). It keeps the game up to date and connects to a server address.

The player launcher finds the address in this order: `--server=URL` on the command line, then the first line of `server.cfg` (next to the launcher, or in its `LifeGame` folder), then the address baked in at export from the repository variable `PLAYER_SERVER_URL` (Settings > Variables; an address, not a secret, never committed). With none, it says "No server address yet" and doesn't start the game. Both builds update themselves from the release (the host from its section of `release.json`, the player from `launcher.player`), have their own shortcut name ("LifeGame" and "LifeGame (player)") and can sit side by side.

## Options

Pass after `--`, e.g. `LifeGameLauncher.x86_64 -- --no-server --server=http://friend:3000` for a friend joining someone else's world (updates the game only). The full list is at the top of `launcher.gd`.

## Developing

- `launcher.gd` is the whole app (GDScript, Godot 4.6). Open `launcher/project.godot` in the Godot 4.6 .NET editor.
- Export: `godot --headless --path launcher --export-release Linux out/LifeGameLauncher.x86_64` (and `Windows out/LifeGameLauncher.exe`). The .NET editor needs the .NET 8 SDK on PATH even though the launcher has no C#.
- Test without GitHub: pack releases with `tools/release/make-release.sh`, serve them with `tools/release/fake-api.py <dir>`, then run the launcher with `-- --api=http://127.0.0.1:8765 --home=/tmp/lg --auto --no-play --quit` against a local `spacetime start`. Exit code 0 ok, 1 failed, 2 stopped at a world wipe.
