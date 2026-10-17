# env-8: Modeling's props in the world (seeded shape variants)
Lane: Environment
Owner: Environment session (Opus), session_012hVejTxLoxBpZyX3GQmNL9
Status: done
Branch: claude/environment-props
PR: #284 (merged 2026-10-16)

## Goal
Swap Modeling's props (#197, #235, `client/scripts/models/PropMeshes.cs`) into the world so repeated things don't look like copies (D-166). Ordinary trunks get flared roots; boulders, river stones, birches, heather, gorse, fallen logs and outcrop formations come in several real shapes, chosen per instance from the seed, with Environment's own per-instance scale, tilt and tint on top. Cattails at pond edges. Collision follows (outcrop formations and boulders block; logs and small stones don't).

## Progress log
- 2026-10-17 01:05: `Terrain.cs`: trunkMesh = PropMeshes.Trunk (old cylinder and boulder meshes removed). Boulders use Rock(4), with the variant regional (30 m noise), so neighbours share a stone type and a chunk holds 1 or 2 batches. Shore pebbles use RiverStone(6), regional. Birch trunks use Birch(3) (the canopy stays the leafy mesh, narrowed). Fallen logs use FallenLog(2). Heather(3) and Gorse(3) carry their own colours with a near-white tint (heather keeps w = 3). 30% of in-water reed clumps are Cattails. `LandmarkProps.cs`: a per-(mesh, material) batcher `Lists.Put`, plus `Lists.Blocks` (blocking circles for collision). Outcrop hearts and slabs use Outcrop(6) "formations" (radius 1.25 x scale), the other stones Rock(4). Ford stones use RiverStone(6), log crossings and giant-tree limbs use FallenLog(2). `TerrainCollision.cs` reads outcrop stones from `Blocks`. Client and server build.

## Open questions / blockers
- (none)

## Next step
None: merged as #284 (CI run https://github.com/Indenex/LifeGame/actions/runs/38972520564 green on 71e65cf). Renders (boulder patch, heath, wetland birches and cattails, an outcrop, a log ford) are still worth asking for; they never block (D-142).
