# model-13: Every tree painted; no hole in the crown top
Lane: Modeling
Owner: Modeling session (session_01JKX3FNjkrEkAqoTaFJiXAm)
Status: in_review
Branch: claude/model-13-tree-fixes
PR: (opening)

## Goal
Fix Justin's bug report, "there's trees that didn't get painted" (screenshot: renders/justin/2026-10-17-unpainted-trees.webp at bbb19315). Every tree in the world is drawn with PaintedTrees' painted crowns, and the painted broadleaf shows no dark hole when seen from above.

## Progress log
- 2026-10-17 07:40: Found two paths that skipped PaintedTrees.
  - Birches by the water and the landmark giants (oak crowns and lobes, ancient pines) still drew Terrain's old blob canopy and cone pines. They had painted-3's material but the old geometry, which was the light-green round crowns in the screenshot.
  - Fix: new `PaintedTrees.PineCrown`/`BroadleafCrown` (the foliage surface alone, same frame) in near/far batches like the forest. Birch crowns are lifted and narrowed (`Terrain.BirchCrown`) with a light yellow-green tint (`Terrain.BirchLeaf`). Landmark crowns keep their colours as a tint relative to the plain greens.
  - TreeFall: a crown on its own trunk falls as is, and only whole painted trees swap to their felled mesh.
- The "hole" was on the painted broadleaf, not the pines. The 5-lobe top tuft left the dark heart showing from above. Fix: 9 broad tuft lobes meeting at the middle, plus 3 lying across the top. Showcase `treelod --top` shows it from above.
- Regular forest, heath oaks, near/far LOD, regrowth (instances fade in place) and the chunk worker all already used PaintedTrees.
- Renders: renders/model-13-*.png.

## Open questions / blockers
- (none)

## Next step
Open the PR; send a Render request for a high-angle in-world check by the water (birches) and at a giant tree.
