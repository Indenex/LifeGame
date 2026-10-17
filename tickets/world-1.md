# world-1: Biomes design and per-chunk resource seeding
Lane: World
Owner: World thread (Sonnet)
Status: in_review
Branch: claude/world-biomes-dvtuio
PR: none yet (design only)

## Goal
Biome map (forest, meadow, marsh, rocky highland, birch hills) with special materials, and resources seeded per chunk as players reach it instead of only within World.SpawnRadiusChunks. Justin sees the design doc first, then a debug biome map, then materials.

## Progress log
- 2026-10-17: read Land.cs, World.cs, Forest.cs, Features.cs, seeding in Reducers.cs/Survival.cs. Wrote /mnt/project-files/world/biomes-design.md and sent it to the PM for Justin's approval. No code yet.

## Open questions / blockers
- Waiting on Justin's answers to the 5 questions at the end of biomes-design.md (biomes first, size, wake-up, first materials, marsh mud). Defaults: highland + marsh first, 300 to 800 m, wake in forest, flint/copper/reeds/peat, slight mud slowdown.

## Next step
When Justin approves: add shared/Biomes.cs (pure noise biome map) with a map preview tool; no server changes until that is reviewed.
