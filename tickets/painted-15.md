# painted-15: Stations and batch devices on the painted look
Lane: Modeling
Owner: Modeling lane session
Status: in_review
Branch: claude/painted-15-held
PR: (see below)

## Goal
Make StationMeshes' materials painted twins by family:
- wood uses Wood, stone uses Stone, cord and weave use Thatch, and vertex-colour parts use Plain.
This covers the potter's bench, distaff, drying rack and repair table, plus the settling tub, crushing block and tempering trough batch devices. They come out smooth and clean (D-319), like the structures around them, wherever they're drawn.
- Before this, StructureModel painted them in game only with the Plain family, so there was no wood or stone tuning.
- The batch devices, not yet hooked into the world, weren't painted at all.
Comparison: `renders/painted-15-stations-before-after.png` (top before, bottom after; stations left, devices right).

## Progress log
- 2026-10-17: changed StationMeshes.EnsureMaterials only. Local build has 0 errors; rendered the stations and devices before and after.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR, then send the green sha to the PM.
