# env-24: Far chunks share their batches
Lane: Environment
Owner: Environment thread (session_012hVejTxLoxBpZyX3GQmNL9)
Status: in_review
Branch: claude/environment-far-batching
PR: (opening)

## Goal
The "radius 12 batching" milestone: each chunk was its own handful of MultiMeshes (about 20 draw calls), so far views cost draw calls by the thousand (and the painted trees added more). Past NearChunks (3, the grass tufts' and painted trees' near reach) a chunk now draws only its ground; the trees, rocks and plants of all far chunks in each 4x4-chunk region are drawn as one batch per mesh and material, with only the painted trees' coarse version and no see-through copies. The world looks the same; far views are cheaper.

## Progress log
- 2026-10-17: `environment/FarBatches.cs` (Placed / Dropped / Regroup / PumpRegions / BuildRegion); ChunkWorker keeps each placed chunk's build and puts its plants under a "Plants" child (AddPlants), so crossing the near edge moves them without rebuilding the chunk; regions rebuild within 2 ms a frame. ViewBench flattens "Plants" (hash unchanged: F071B33E239EC47E at (-4,46), same as main) and prints what is drawn with far regions. Bench at (0,10), draw calls (all batches) -> drawn with far regions: radius 5 2666 -> 1215, radius 8 6486 -> 1705, radius 10 9579 -> 2281, radius 12 12981 -> 2877. Real client at radius 5: 121 chunks, 72 far chunks in 15 regions, no errors.

## Open questions / blockers
- (none)

## Next step
Wait for CI; send the green sha to the PM; Render a high Far view to confirm the far forest looks the same.
