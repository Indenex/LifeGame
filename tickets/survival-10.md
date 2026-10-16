# survival-10: First evening tuning (M2)
Lane: Survival
Owner: Survival thread (session_014pHdaxJ7uESzsvWekpjvsH)
Status: in_progress
Branch: claude/survival-next
PR: (none yet; tools/FirstEvening merged in #185)

## Goal
Justin picked first-evening tuning on the new land (env-2, #175, lands with the D-143 wipe). A fresh character spawning at dusk or in winter should find berries, water and the makings of a fire, and get a fire lit before the cold becomes a worry, without ever dying in the first hour (D-081). Output: measured numbers for the new land (distance from spawn to the nearest ripe bush, to drinkable water, how long a first fire takes with CraftCheck-style steps, warmth left on arrival at the fire, day and night, summer and winter) and the small tunings that fix any gap (bush density near spawn, BushRegrow, respawn warmth), proposed to the PM as options where players would notice.

## Plan
1. Local toolchain: dotnet 8 at ~/.dotnet, SpacetimeDB 2.12.0 at ~/stdb (as in CI). Build the module, publish locally, run bots.
2. Write tools/FirstEvening: a bot spawns fresh, reads forage_bush and terrain (shared World/Look water level), reports the distances above and simulates warmth over time (Needs + Chill + arrival) for the four season/time cases.
3. Re-run on env-2's land once #175 merges; propose tunings.

## Progress log
- 2026-10-16: assigned (PM, Justin picked option A). Installing local toolchain; first module build running.

- 2026-10-16: local toolchain works (dotnet 8.0.426 at ~/.dotnet, SpacetimeDB 2.12.0 at ~/stdb; module builds, SpawnCheck passes). Wrote tools/FirstEvening (read-only; run against a local server). Baseline on current main land (before env-2), 120 bushes, 378 sticks, 190 fiber:
  - First spawn (81 spots in the middle): nearest bush median 6 m / worst 11; water median 18 / worst 28; first fire (2 sticks + 2 fiber walk, shred, drill with fails) median 16 s / worst 18 s. Freeze time with the arrival buff: 32 min mild, 16 min freezing night. Fire uses at most 2% of it.
  - Respawn at the rim (24 spots): bush median 5 / worst 11; water median 38 / worst 60; first fire median 17 s / worst 22 s; freeze 8 min on a freezing night, fire uses 5%.
  - Reading: on this land distance is not the problem; the risk is a player who doesn't yet know the hand-drill recipe (Journal's discovery), not the walk. Re-run on env-2.

- 2026-10-16: tools/FirstEvening merged with #185. How to run it (fresh session): install dotnet 8 (`curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0 --install-dir ~/.dotnet`) and SpacetimeDB 2.12.0 (`mkdir -p ~/stdb && curl -sSL https://github.com/clockworklabs/SpacetimeDB/releases/download/v2.12.0/spacetime-x86_64-unknown-linux-gnu.tar.gz | tar xz -C ~/stdb`); `export PATH=~/stdb:~/.dotnet:$PATH DOTNET_ROOT=~/.dotnet`; `(cd server && spacetimedb-cli build)`; `spacetimedb-cli start --listen-addr 127.0.0.1:3000 &`; `(cd server && spacetimedb-cli publish lifegame --server local --yes --delete-data)`; `tools/gen-bindings.sh`; `cd tools/FirstEvening && dotnet run -c Release`. Compare with the baseline above.

- 2026-10-16: env-2 and seasons merged (main f9b2e00). Ran FirstEvening on a fresh local world: first spawn bush median 5/worst 10 m, water median 38/worst 50 m (was 18/28), first fire 16/18 s, freeze 32 min mild / 16 min freezing night. Respawn: bush 6/11, water 28/40 (was 38/60), freeze 8 min freezing night. Only gap: water walk from the middle. Sent PM options A (leave + Journal hint, recommended), B (spring near spawn, Environment), C (thirst 20% slower first hour).

## Open questions / blockers
- Waiting on the PM's pick among options A/B/C for water distance.

## Next step
When the PM picks an option, implement it (A: send Journal a hint line; C: small tweak in Survival.cs) and re-run tools/FirstEvening.
