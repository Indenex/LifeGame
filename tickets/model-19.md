# model-19: Fallen giant log with a root plate
Lane: Modeling
Owner: Modeling lane session
Status: in_review
Branch: claude/model-19-giant-log
PR: (see below)

## Goal
For Environment's fallen giant landmark (env-33, PR #718), which draws FallenLog 3 to 4 times bigger. There, FallenLog's five thin root tubes read as antlers or spikes.
- New `PropMeshes.FallenGiantLog(int variant)` with `FallenGiantLogVariants = 6`, about 3 m long at scale 1 like FallenLog, on Painted.Bark with the white `PropMeshes.GiantTint` as its instance colour.
- The butt end is a torn-up root plate: a thick lumpy disc with uneven root lobes and short stubs, thick roots radiating across its face, earth clods and pebbles between, moss along the top edge, and heaped earth at its foot.
- Variant 0 is straight with a splintered crown end; variant 1 is broken in two with a forked crown, as FallenLog 1.
- Showcase view `giantlog` (in `Showcase.ModelingProps.cs`): the old FallenLog 1 at 3.5x next to both giants, with a person for scale.
Render: `renders/model-19-giant-log.png`.

## Progress log
- 2026-10-17: built and iterated over three renders; build has 0 errors.
- 2026-10-17 11:40: PM review (it read as a symmetric wheel). Reworked:
  - The plate is lopsided (taller on one side) with a ragged rim of small flat lumps, some missing.
  - A few uneven roots wander across the face, with ground-coloured soil and stones packed between them and thin root ends dangling off the rim.
  - Moss sits on top. The bark is weathered grey-brown, with moss bands and cushions along the trunk.
  - FallenGiantLogVariants is now 6: even variants are straight, odd ones broken, and each has its own plate roll, size, lobes and roots (D-166).
  - Vertex colours can't exceed the bark tint, so the giant now carries its own colours: it must be drawn with the white `PropMeshes.GiantTint`, not Look.Bark.
- 2026-10-17 12:00: PM's final round: each plate is now a wide, flat disc on edge (a flat-faced loft 0.28 deep along the trunk, about 2.5 trunk widths across), with the trunk out of its middle. It keeps the ragged lopsided rim, and a shallow earth mound sits at its foot.

## Open questions / blockers
- (none) Environment swaps the mesh in its builder and may want to preload it in ChunkWorker as FallenLog is.

## Next step
Wait for CI on the PR, send the green sha to the PM, and tell Environment (session_012hVejTxLoxBpZyX3GQmNL9).
