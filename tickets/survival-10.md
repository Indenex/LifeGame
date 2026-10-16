# survival-10: First evening tuning (M2)
Lane: Survival
Owner: Survival thread (session_014pHdaxJ7uESzsvWekpjvsH)
Status: in_progress
Branch: claude/survival-next
PR: (none yet)

## Goal
Justin picked first-evening tuning on the new land (env-2, #175, lands with the D-143 wipe). A fresh character spawning at dusk or in winter should find berries, water and the makings of a fire, and get a fire lit before the cold becomes a worry, without ever dying in the first hour (D-081). Output: measured numbers for the new land (distance from spawn to the nearest ripe bush, to drinkable water, how long a first fire takes with CraftCheck-style steps, warmth left on arrival at the fire, day and night, summer and winter) and the small tunings that fix any gap (bush density near spawn, BushRegrow, respawn warmth), proposed to the PM as options where players would notice.

## Plan
1. Local toolchain: dotnet 8 at ~/.dotnet, SpacetimeDB 2.12.0 at ~/stdb (as in CI). Build the module, publish locally, run bots.
2. Write tools/FirstEvening: a bot spawns fresh, reads forage_bush and terrain (shared World/Look water level), reports the distances above and simulates warmth over time (Needs + Chill + arrival) for the four season/time cases.
3. Re-run on env-2's land once #175 merges; propose tunings.

## Progress log
- 2026-10-16: assigned (PM, Justin picked option A). Installing local toolchain; first module build running.

## Open questions / blockers
- Waits on env-2 (#175) for the final land; tooling can be built against current main.

## Next step
Finish the local module build, then write tools/FirstEvening.
