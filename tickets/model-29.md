# model-29: More fallen-log shapes (D-166)
Lane: Modeling
Owner: Modeling lane (session_01JKX3FNjkrEkAqoTaFJiXAm)
Status: in_review
Branch: claude/model-29-fallen-log-variety
PR: (opening)

## Goal
Fallen trunks on the forest floor (Terrain.cs, LandmarkProps) came in only two shapes, so the same log showed up again and again. `PropMeshes.FallenLogVariants` goes from 2 to 6, with four new shapes in `BuildFallenLogB`: a short hollow butt thick with moss, a long thin bent pole with snapped branch stubs, a trunk with its biggest limb lying out sideways, and two smaller logs fallen across each other. Same frame as before (lying along x, about 3 m, bottom on the ground), the same Painted.Bark material and tint, so every caller picks them up with no change. Showcase view `fallenlogs` shows all six.

## Progress log
- 2026-10-17 12:10: four new variants built and rendered (renders/model-29-fallenlogs.png); a bark-stripped grey log was dropped because vertex colours can't lighten past the bark tint Terrain uses. Local build 0 errors.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR, then send the green sha to the PM.
