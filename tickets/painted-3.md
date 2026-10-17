# painted-3: Painted bark and leaves (logs, birches, landmark trees)
Lane: Modeling
Owner: Modeling session (session_01JKX3FNjkrEkAqoTaFJiXAm)
Status: done
Branch: claude/painted-3-props (stacked on painted-1, claude/painted-1-rocks)
PR: #559

## Goal
Part of the painted rollout (painted-1 rocks, -2 bushes, -3 logs and old trees, -4 animals, -5 structures, -6 items). Everything still drawn with Look's tree bark and foliage goes onto the shared painted shader. That covers fallen logs, birches and their canopies, and the landmark trees that PaintedTrees doesn't replace. Justin sees the same flat strokes and stepped light on these as on the painted pines, broadleaves and rocks.

## Progress log
- 2026-10-17 05:20: `Painted.Bark` (violet shade, crevices at 0.3) and `Painted.Leaves` (leaning to the painted broadleaf grey-green) are twins of Look.TreeBark and TreeFoliage. They replace those materials at every site in Terrain (birches, fallen logs, landmark trunks, leafy canopies and pines) and LandmarkProps (fallen logs). TreeFall keeps both the painted and the old materials. ChunkWorker prewarms their ghosts. Showcase `logs` puts today's materials in the back row and the painted ones in front. `Terrain.LeafyCanopy` is exposed for that view. Built clean; pushed cbc746a5.

## Open questions / blockers
- (none)

## Next step
Wait for painted-1 (#554) to land, then retarget this PR to main; Render C before/after.
