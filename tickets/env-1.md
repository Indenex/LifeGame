# env-1: Cosy forest pass
Lane: Environment
Owner: Environment session (Opus)
Status: todo
Branch: (record the harness-assigned branch here)
PR: (once opened)

## Goal
Make the forest region feel lived-in, varied and cosy (D-078), together with the Atmosphere lane. Done means: ground cover and grass that vary by area, clusters of trees and undergrowth instead of even scatter, rocks and fallen logs, soft clearings and water edges (reeds, stones), all deterministic so every player sees the same world, and cheap enough for 32 players (CI load test). Gameplay objects (gatherable sticks, stones, trees) keep working; coordinate any change to them with their lane. Justin sees before/after renders of spawn, a clearing, a pond edge and dense forest, by day and at dusk.

## Progress log
- 2026-10-16: ticket written by the PM (D-078).

## Open questions / blockers
- (none)

## Next step
Read `client/scripts/Terrain.cs`, `shared/World.cs` and how trees/resources are placed; ask the Render lane for current shots of spawn, a clearing and a pond; send the PM 2 to 3 directions as options.
