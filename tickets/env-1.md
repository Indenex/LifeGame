# env-1: Cosy forest pass
Lane: Environment
Owner: Environment session (Opus)
Status: in_progress
Branch: claude/environment-env-1
PR: (once opened)

## Goal
Make the forest region feel lived-in, varied and cosy (D-078), together with the Atmosphere lane. Done means: ground cover and grass that vary by area, clusters of trees and undergrowth instead of even scatter, rocks and fallen logs, soft clearings and water edges (reeds, stones), all deterministic so every player sees the same world, and cheap enough for 32 players (CI load test). Gameplay objects (gatherable sticks, stones, trees) keep working; coordinate any change to them with their lane. Justin sees before/after renders of spawn, a clearing, a pond edge and dense forest, by day and at dusk.

## Progress log
- 2026-10-16: ticket written by the PM (D-078).
- 2026-10-16 17:30: Environment session (session_012hVejTxLoxBpZyX3GQmNL9) started on `claude/environment-env-1` (harness assigned none).
- 2026-10-16 17:35: read the world code. Findings: `shared/World.cs` gives height (`BaseHeight`, two value-noise octaves) and clay; `client/scripts/Terrain.cs` plants everything per 1 m cell in `AddVegetation`: trees by `roll < 0.03 + 0.10*Noise(24,33)` (even-ish scatter, no clusters), a 12 m bare spawn clearing, rare boulders (0.6%), a grass tuft on nearly every dry tile, nothing at water edges. Trees and boulders are client-only visuals; the server does not know them (gatherable sticks/stones/clay are server items, untouched). Atmosphere's `Woodedness()` in `client/scripts/atmosphere/Atmosphere.cs` copies the forest density formula for ambience: any density change must keep both in sync, so plan a shared `ForestDensity(x, z)` both call.

## Open questions / blockers
- (none)

## Next step
Read `client/scripts/Terrain.cs`, `shared/World.cs` and how trees/resources are placed; ask the Render lane for current shots of spawn, a clearing and a pond; send the PM 2 to 3 directions as options.
