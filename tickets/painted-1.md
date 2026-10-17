# painted-1: Painted prop shader and rocks
Lane: Modeling
Owner: Modeling session (session_01JKX3FNjkrEkAqoTaFJiXAm)
Status: done
Branch: claude/painted-1-rocks (stacked on #544, claude/model-10-painted-trees)
PR: (opening)

## Goal
Justin's top priority: the painted look on everything, family by family (rollout: painted-1 rocks, -2 bushes, -3 ground props, -4 animals, -5 structures, -6 items). This first PR adds the shared painted shader for non-tree surfaces and puts the world's rocks on it.

## Progress log
- 2026-10-17 09:30: `client/scripts/models/Painted.cs`: `Painted.Of(material)` makes a painted twin of any of Look's materials (same uniforms; Look's shader with PAINTED_PROP spliced at anchors that must each match once): flat colour strokes with a few lit dabs on up-facing faces, crevices in one darker tone, a soft ink edge, light in stepped bands with violet shade (LIGHT_COLOR and ambient smooth), base_paint/base_mix to lean a family toward a palette colour. `Painted.GhostOf` gives ghosts to see-through twins (hooked after PaintedTrees.GhostOf). `Painted.Rock`: the Golden Skull stone (pale grey planes, deep blue-violet shade, crevices only where deepest). Terrain and LandmarkProps rocks, outcrops and river stones use it. Showcase `rocks` (today's back row, painted front).

## Open questions / blockers
- (none)

## Next step
PR after #544 merges (stacked); Render before/after; then painted-2 bushes.
