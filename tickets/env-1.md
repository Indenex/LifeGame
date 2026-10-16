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
- 2026-10-16 17:40: Render requested 1ade969 (baseline: spawn, pond at (-28,0), dense forest (-46,-46), overhead; noon and dusk). Directions sent to PM. Atmosphere messaged.
- 2026-10-16 17:35: read the world code. Findings: `shared/World.cs` gives height (`BaseHeight`, two value-noise octaves) and clay; `client/scripts/Terrain.cs` plants everything per 1 m cell in `AddVegetation`: trees by `roll < 0.03 + 0.10*Noise(24,33)` (even-ish scatter, no clusters), a 12 m bare spawn clearing, rare boulders (0.6%), a grass tuft on nearly every dry tile, nothing at water edges. Trees and boulders are client-only visuals; the server does not know them (gatherable sticks/stones/clay are server items, untouched). Atmosphere's `Woodedness()` in `client/scripts/atmosphere/Atmosphere.cs` copies the forest density formula for ambience: any density change must keep both in sync, so plan a shared `ForestDensity(x, z)` both call.

## Open questions / blockers
- Sent to PM 2026-10-16 for Justin: scope of the pass. A (recommended, default) groves and glades, visual only; B dress today's layout only; C A plus reshaped land (more ponds, stream) needing a world wipe. Working on A.
- Sent to Atmosphere 2026-10-16: working agreement (I stay out of `Look.cs`/atmosphere files; wind global shader param; shared `Forest.Density` replacing their `Woodedness` copy; I do shore only, not water surface; canopy tint split). Awaiting reply.

## Next step
Build direction A: add `client/scripts/environment/Forest.cs` (deterministic grove/glade density) and switch `Terrain.AddVegetation` tree placement to it; then undergrowth, logs, rocks, reeds.
