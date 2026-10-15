# LifeGame

An online multiplayer 2.5D survival game where players wake up in the woods with nothing and rebuild society from scratch.

Design scope: https://claude.ai/code/artifact/8064d6ad-709a-42b9-8136-ad85e16111a8

Agents working on this repo: start with [`CLAUDE.md`](CLAUDE.md) and [`STATUS.md`](STATUS.md).

## Layout

| Path | What it is |
| --- | --- |
| `server/` | SpacetimeDB module in C#: tables and reducers for players, movement, digging and items |
| `client/` | Godot 4.6 (.NET) game client |
| `client/module_bindings/` | Client bindings generated from the server module; do not edit by hand |
| `shared/` | C# shared by server and client: world generation, chunk layout, gameplay limits |
| `tools/LoadTest/` | Simulated players for measuring server cost and bandwidth |
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

# 3. Open client/project.godot in Godot 4.6 .NET and press Play
```

The client connects to `http://127.0.0.1:3000`, database `lifegame`. To point it somewhere else, pass arguments after `--`:

```sh
godot --path client -- --server=http://my-server:3000 --db=lifegame
```

Run two clients side by side with separate identities using `--profile=a` and `--profile=b`.

Controls: WASD to walk, Q and E to turn the camera, mouse wheel to zoom, F to dig, Space to pick up, Enter to talk, K at a claim marker to open it.

## Chat, land claims and PvP

- Chat is heard within 24 m of the speaker. Type `/name Something` in chat to rename yourself.
- A claim marker is laid out from the build menu (B, then Tab to it) and built with 4 sticks and 2 fiber like any structure. Once it stands it protects land within 20 m of it. Outsiders cannot dig there or pick up what lies on the ground. The owner can invite people standing nearby; members can dig, pick up, tend the marker and use the stash at its foot.
- Fighting is allowed in the wild. Inside a standing claim it needs the owner's permission (off by default). There is no combat yet; `Module.RequirePvpAllowed` is the rule combat code should call.
- A marker stands for 3 days after it was last tended, then falls and spills its stash. To watch that happen in a playtest, shorten it as the database owner: `spacetime call lifegame set_claim_lifetime 120`.
- `tools/SocialTest` checks all of this with two simulated players against a running server (CI runs it).

Building: press B to lay out a footprint (Tab picks a lean-to or clay hut, T turns it, click to place, Esc cancels). Stand next to a site and hold H to add what you carry; anyone can help. Each structure goes up in four stages, and a finished one shelters whoever stands inside. To look at the buildings without a server, run the client with `--building-showcase` (add `--building-closeup` to zoom in).

Survival: G picks berries from a ripe bush, C eats, R drinks when you stand by water. Food, water and warmth show as three rings in the bottom-right corner; stand by a fire to warm up. If any of them runs out you die, drop what you carry, and wake naked at the edge of the wilderness.

## After changing the server module

Tables and reducers live in `server/spacetimedb/`. After changing them, republish and regenerate the client bindings:

```sh
cd server
spacetime publish lifegame --server local --yes        # add --delete-data=always if the schema change is breaking
spacetime generate --lang csharp --out-dir ../client/module_bindings --module-path ./spacetimedb
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

## Screenshots without a display

The client can render one frame and quit, which is handy on servers and in CI:

```sh
godot --path client --resolution 1600x900 -- --screenshot=shot.png --screenshot-delay=8
```
