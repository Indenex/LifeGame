# model-10: Painted trees in the world
Lane: Modeling
Owner: Modeling session (session_01JKX3FNjkrEkAqoTaFJiXAm)
Status: in_progress
Branch: claude/model-10-painted-trees
PR: (opening)

## Goal
Justin wants the painted look rolled out: the painted pines (D-282, D-285; prototype claude/model-4-materials @ 3609e3d3) and broadleaf v2 (d11ab9d6) replace the old tree meshes in the real world, keeping season colours, the see-through fade, falling when cut, snow and the far fog rim, with LOD so Far and Very far view distances stay fast. Done when the PR is merged and Render's in-game before/after shots at spawn are with the PM. Environment (session_012hVejTxLoxBpZyX3GQmNL9) owns Terrain.cs.

## Progress log
- 2026-10-17 07:40: `client/scripts/models/PaintedTrees.cs`: pine and broadleaf meshes (4 seeded variants each in the world), detailed and coarse versions, built at the old trees' height (4.4 m at scale 1) so Terrain's placement, fade capsule and sway fit; shader = Look's PaintedCode with PAINTED_FOLIAGE (painted bough colour, stepped crown-wide light, low-sun lift, translucency) spliced at anchors that must each match once, so Look keeps doing seasons, snow, wetness, falling, fog rim and the ghost. Look.cs hooks: PaintedCode, MakeShader and Ghost made internal; GhostOf falls through to PaintedTrees.GhostOf. Terrain: regular pines, broadleaf and heath oaks use the painted meshes (birches and landmark trees unchanged); each chunk draws the detailed version within 40 m of the camera and the coarse one beyond (visibility ranges, 4 m margins). ChunkWorker: batches carry a range; PaintedTrees.Prewarm. Builds; first offline in-game render running.
- 2026-10-17 08:30: Perf, offline client, llvmpipe software Vulkan, view radius 8 (Far), 289 chunks loaded, noon at spawn. main e6490e7: 584 draw calls, 1.57 M primitives, loaded in 182 s. Painted, 4 variants per kind per chunk: 900 draw calls, 1.51 M, 198 s. Painted with 2 variants per kind per region (`Terrain.TreeVariant`): 732 draw calls, 1.57 M, 206 s (the load times are noisy on llvmpipe). Felled meshes for TreeFall: `PaintedTrees.Felled(pine, v)`, `PropMeshes.CutTrunk`. Broadleaf top lobes shortened. Showcase `treelod` (detailed front, far behind, on the real material).

## Open questions / blockers
- (none)

## Next step
Wait for CI and Environment's review on the PR; after #537 lands, merge main and fix TreeFall.cs per Environment (near batch only, painted materials in the filter); send the PM the green sha.
