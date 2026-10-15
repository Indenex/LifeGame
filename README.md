# LifeGame

An online multiplayer 2.5D survival game where players wake up in the woods with nothing and rebuild society from scratch.

Design scope: https://claude.ai/code/artifact/8064d6ad-709a-42b9-8136-ad85e16111a8

Agents working on this repo: start with [`CLAUDE.md`](CLAUDE.md) and [`STATUS.md`](STATUS.md).

## Layout

| Path | What it is |
| --- | --- |
| `server/` | SpacetimeDB module in C#: tables and reducers for players, movement, digging and items |
| `client/` | Godot 4.6 (.NET) game client |
| `client/module_bindings/` | Client bindings generated from the server module by `tools/gen-bindings.sh`; not committed |
| `shared/` | C# shared by server and client: world generation, chunk layout, gameplay limits |
| `tools/LoadTest/` | Simulated players for measuring server cost and bandwidth |
| `tools/CraftCheck/` | One bot that gathers and runs every crafting process end to end (runs in CI) |
| `tools/BuildTest/` | Two simulated players building a lean-to and a clay hut together |

## Requirements

- .NET 8 SDK
- [SpacetimeDB CLI](https://spacetimedb.com/install) 2.12
- [Godot 4.6 .NET](https://godotengine.org/download) (the ".NET" build, not the standard one)

## Run it locally

```sh
# 1. Start a local SpacetimeDB server (leave it running)
spacetime start

# 2. Build and publish the server module
cd server
spacetime publish lifegame --server local --yes

# 3. Generate the client bindings (not committed; rerun whenever the server module changes)
cd ..
tools/gen-bindings.sh

# 4. Open client/project.godot in Godot 4.6 .NET and press Play
```

The client connects to `http://127.0.0.1:3000`, database `lifegame`. To point it somewhere else, pass arguments after `--`:

```sh
godot --path client -- --server=http://my-server:3000 --db=lifegame
```

Run two clients side by side with separate identities using `--profile=a` and `--profile=b`.

Controls: WASD to walk, Q and E to turn the camera, mouse wheel to zoom, F to dig, Space to pick up, C to open crafting, T to put fuel on the nearest fire, J to open the journal; G reads a note on the ground; Enter talks; K opens your claim; G at your claim marker tends it.

## Crafting

Items and processes are plain data in `shared/Items.cs` and `shared/Processes.cs`, compiled into both the server and the client. A process consumes inputs, may need a tool (matched by property, such as Hammer), takes time, and makes an item or a fire. Key steps have a short minigame: spinning a hand drill for fire (alternate A and D), and knapping stone (strike with Space as the mark crosses the bright spot). The minigame score and the input quality set the result's quality, capped by the crafter's skill.

The server runs each process as a job with a finish time (`server/spacetimedb/Crafting.cs`). Process ids such as `knap.flake` are stable: skills and the journal key on them. The skills system plugs in through two partial methods, `OnProcessPerformed` and `QualityCapFor`. Other systems read warmth with `Module.WarmthAt(ctx, x, z)`.

Item ids are stored as numbers, so never renumber or reuse one; add new items at the end.

## Chat, land claims and PvP

- Chat is heard within 24 m of the speaker. Type `/name Something` in chat to rename yourself.
- A claim marker is laid out from the build menu (B, then Tab to it) and built with 4 sticks and 2 fiber like any structure. Once it stands it protects land within 20 m of it. Outsiders cannot dig there or pick up what lies on the ground. The owner can invite people standing nearby; members can dig, pick up, tend the marker and use the stash at its foot.
- Fighting is allowed in the wild. Inside a standing claim it needs the owner's permission (off by default). There is no combat yet; `Module.RequirePvpAllowed` is the rule combat code should call.
- A marker stands for 3 days after it was last tended, then falls and spills its stash. To watch that happen in a playtest, shorten it as the database owner: `spacetime call lifegame set_claim_lifetime 120`.
- `tools/SocialTest` checks all of this with two simulated players against a running server (CI runs it).

Building: press B to lay out a footprint (Tab picks a lean-to or clay hut, T turns it, click to place, Esc cancels). Stand next to a site and hold H to add what you carry; anyone can help. Each structure goes up in four stages, and a finished one shelters whoever stands inside. To look at the buildings without a server, run the client with `--building-showcase` (add `--building-closeup` to zoom in).

Survival: G picks berries from a ripe bush, X eats, R drinks when you stand by water or from a pot of water you carry. Food, water and warmth show as three rings in the bottom-right corner; stand by a fire to warm up. If any of them runs out you die, drop what you carry, and wake naked at the edge of the wilderness.

### Day, night and weather

An in-game day lasts 90 real minutes. The server keeps the clock and the weather (`server/spacetimedb/WorldClock.cs`), so every player sees the same sky. The client draws it in `client/scripts/atmosphere/`, and synthesizes all the sound as it plays: wind, rain, birds, crickets, footsteps and the music.

Preview keys in debug builds (running from the editor) change only what you see and hear: hold F5 to fast-forward time (a whole day in about 45 seconds), press F6 to step through clear, drizzle, light rain, steady rain, a storm and back to the server's weather, and M to turn the music off or on. The same can be set from the command line, which is handy for screenshots: `--time-of-day=0.75` (0 midnight, 0.25 sunrise, 0.5 noon, 0.75 sunset), `--rain=1`, `--wetness=1`, `--cloud=0.6`, `--wind=0.5`, and `--day-minutes=4` for a shorter day.

To listen to the soundscape without the game, render it to a file: `cd tools/SoundPreview && dotnet run -- out.wav` (day, rain, dusk and night, with footsteps).

Other server code can ask about the weather: `TimeOfDay(ctx)`, `CurrentWeather(ctx)` (cloud, rain, wind and ground wetness, each 0 to 1), `DryingRate(ctx)` (1 on a dry day, about half in rain) and `AirTemperature(ctx)` in degrees Celsius.

## After changing the server module

Tables and reducers live in `server/spacetimedb/`. After changing them, republish and regenerate the client bindings:

```sh
(cd server && spacetime publish lifegame --server local --yes)   # add --delete-data=always if the schema change is breaking
tools/gen-bindings.sh
```

## Load test

```sh
cd tools/LoadTest
dotnet run -c Release -- 50 60      # 50 simulated players for 60 seconds
```

It prints server CPU, memory, per-player bandwidth and how long a move takes to reach other players. Run it on the same machine as the server, since it reads the server's CPU and memory from `/proc` (Linux only).

### Phase 0 results

Measured on a 4-core cloud VM, with the load test running on the same machine as the server. All bots wander inside the same 128 m square, so every player sees every other player. That is the worst case.

| Players | Server CPU | Server memory | Traffic per player | Move seen by others (p50 / p95) |
| --- | --- | --- | --- | --- |
| 50 | 47% of one core | 253 MB | 22 KB/s | 69 / 76 ms |
| 150 | 129% of one core | 394 MB | 40 KB/s | 175 / 429 ms |

At 150 bots the single-threaded load-test harness itself becomes a bottleneck, so the 150-player latency is pessimistic.

Two changes got traffic down from 94 KB/s per player in the first run: position updates now live in a small `player_pos` table separate from player identity, and moves are applied in one batch every 100 ms instead of one transaction per move.

## Building test

Two simulated players gather materials, build a lean-to and then a clay hut together, stand inside, and reconnect to check the buildings persisted. Run it against a freshly published local server:

```sh
cd tools/BuildTest
dotnet run -- both        # or lean-to, or clay-hut
```

## Spawn check

A player walks well past the area around the origin, logs off and logs back in, using the client's own subscriptions (`client/scripts/net/Subscriptions.cs`). It checks they get their own position back and the area around it. Run it against a freshly published local server:

```sh
cd tools/SpawnCheck
dotnet run
```

## Upgrade check

Publishes the base branch's module to a local server, plays on it briefly, then publishes this checkout's module over it without `--delete-data`, as a host updates a running world. If that is refused, merging needs a world wipe. CI runs it on every pull request; locally, with a server running:

```sh
tools/upgrade-check.sh origin/main
```

## Screenshots without a display

The client can render one frame and quit, which is handy on servers and in CI:

```sh
godot --path client --resolution 1600x900 -- --screenshot=shot.png --screenshot-delay=8
```
