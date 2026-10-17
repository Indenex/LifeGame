# painted-2: Painted bushes
Lane: Modeling
Owner: Modeling session (session_01JKX3FNjkrEkAqoTaFJiXAm), Opus helper
Status: in_review
Branch: claude/painted-2-bushes (stacked on claude/painted-1-rocks, itself on #544 claude/model-10-painted-trees)
PR: (not opened; stacked)

## Goal
The second family in the painted rollout (painted-1 rocks, -2 bushes, -3 ground props, -4 animals, -5 structures, -6 items). The world's bushes read as painted leaves in the Golden Skull / Dofus style: flat colour bands, violet shade and a soft ink edge, in muted olive and grey-green beside the painted trees, with no saturated greens. Justin sees the forest bushes, heather, gorse and the berry bush in the painted look.

## Progress log
- 2026-10-17 05:30: `client/scripts/models/PaintedBushes.cs` builds each bush from clumps. A clump is a dark heart wrapped in pointed leaf lobes (8 triangles each, with a split tip and a raised ridge) that lie down the clump's slope, like the painted broadleaf crown. Normals are mostly the clump's own outward normal, so light falls in a few big painted shapes and the ink runs round the silhouette. Lobes are darker at the root than at the tip. Triangles are wound to face outward; otherwise the two-sided shading flipped half the leaves dark. `PaintedBushes.Bush` has 5 clumps (about 1360 triangles). `PaintedBushes.Clump(leaf)` serves the berry bush (about 320 triangles per clump). `PaintedBushes.Tint` pulls a world bush colour toward the painted olive/grey-green.
- Two materials: `Painted.Bush` is Look.Foliage's painted twin with the shade leaning further violet, more lit dabs and fewer crevice spots. `Painted.Foliage` is `Painted.Of(Look.Foliage)` and keeps sway and the heather season code.
- Hooks: Terrain.cs draws bushes with PaintedBushes.Bush / Painted.Bush, and heather and gorse with Painted.Foliage. ChunkWorker prewarms them. BerryBush.cs uses painted clumps and painted berries and stems (the red stays readable). Berries now sit at the leaf layer (radius 1.0 instead of 0.94).
- Showcase view `bushes`: the back row shows today's bushes, heather, gorse and the old berry lobes. The front row shows the painted ones plus a ripe and a picked berry bush. Build has 0 errors. The noon render went through three passes.

## Open questions / blockers
- (none) Possible follow-up for Justin: gorse and heather keep their own meshes on the painted shader. A later pass could give them leaf lobes too.

## Next step
Send a before/after render request to the Render lane for the PM, then open the PR once painted-1 merges.
