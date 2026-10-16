# Playing LifeGame

How to update your game and your world to the newest build without losing the world. These steps are for Linux (Bazzite) and Windows. Run every command from the folder you download into.

## What you need once

- The SpacetimeDB CLI 2.12 (`spacetime`). The install line is at https://spacetimedb.com/install.
- A GitHub login, used only to download builds.

## Update to the newest build

1. **Download the build.** On GitHub, open the repository's **Actions** tab and pick **Build** in the list on the left. Open the newest run on `main` that has a green tick, then scroll to **Artifacts** at the bottom of the run. Download:
   - `LifeGame-Server` (the server module, `StdbModule.wasm`)
   - `LifeGame-Linux` or `LifeGame-Windows` (the game)

   Unzip both. On Linux, downloads lose the "can run" flag, so restore it once:

   ```sh
   chmod +x linux/LifeGame.x86_64
   ```

2. **Start the server** if it isn't already running. Leave this terminal open:

   ```sh
   spacetime start
   ```

3. **Update the world.** In a second terminal, from the folder holding `StdbModule.wasm`:

   ```sh
   spacetime publish lifegame --server local --bin-path StdbModule.wasm --delete-data=never --yes=migrate,break-clients,skip-login
   ```

   `--delete-data=never` is what keeps your world. Never use plain `--yes` or `--delete-data=always` here.

   If the publish refuses because the update needs a world wipe, stop and tell the Project Manager thread. Your world is untouched when this happens. CI checks each change for this before it merges, so it should be rare.

4. **Play.** Run `linux/LifeGame.x86_64`, or `windows/LifeGame.exe` on Windows. It connects to `http://127.0.0.1:3000`, database `lifegame`.

## Make yourself an admin (once per world)

Admins can change the time of day and weather from the game. The command has to come from the same computer and `spacetime` login that published the world. It needs your in-game name, so join once first; type `/name Justin` in chat if you haven't named yourself.

```sh
spacetime call lifegame make_admin '"Justin"'
```

This survives updates. You only need to run it again if the world is wiped.

## When something goes wrong

- **"Offline" in the corner:** the server isn't running. Do step 2, then restart the game.
- **Server log:** `spacetime logs lifegame -n 100` shows recent errors, which helps when reporting a problem.
