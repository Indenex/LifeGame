# bugfix-30: Identity survives reinstall
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-30
PR: (to open)

## Goal
Reinstalling or updating the game keeps the player's character.

## Findings (2026-10-17)
- The auth token is `user://token.txt` (`user://token_<profile>.txt`): Godot's per-user data folder (`~/.local/share/godot/app_userdata/LifeGame`, `%APPDATA%\Godot\app_userdata\LifeGame`), keyed by `config/name="LifeGame"`. It is not under the install folder.
- Launcher `_install_client` only swaps `home/game` (`game.new` -> `game`, old kept until swap works); it never touches `user://`. The managed server uses the SpacetimeDB CLI's own data dir, also outside `home`, and publishes with `--delete-data=never` unless wiping is requested.
- Deleting the whole launcher folder removes only `home` (downloads, state.json, game), not the token or server data.
- Change: when a new token replaces a different one on disk, the old one is kept as `token.txt.bak`.
- Caveat: renaming `config/name` in project.godot would move the user folder and lose tokens; don't.

## Next step
Open PR, wait for CI, send PM the green sha.
