# model-19: Fallen giant log with a root plate
Lane: Modeling
Owner: Modeling lane session
Status: in_review
Branch: claude/model-19-giant-log
PR: (see below)

## Goal
For Environment's fallen giant landmark (env-33, PR #718), which draws FallenLog 3 to 4 times bigger. There, FallenLog's five thin root tubes read as antlers or spikes.
- New `PropMeshes.FallenGiantLog(int variant)` with `FallenGiantLogVariants = 2`, about 3 m long at scale 1 like FallenLog, on Painted.Bark.
- The butt end is a torn-up root plate: a thick lumpy disc with uneven root lobes and short stubs, thick roots radiating across its face, earth clods and pebbles between, moss along the top edge, and heaped earth at its foot.
- Variant 0 is straight with a splintered crown end; variant 1 is broken in two with a forked crown, as FallenLog 1.
- Showcase view `giantlog` (in `Showcase.ModelingProps.cs`): the old FallenLog 1 at 3.5x next to both giants, with a person for scale.
Render: `renders/model-19-giant-log.png`.

## Progress log
- 2026-10-17: built and iterated over three renders; build has 0 errors.

## Open questions / blockers
- (none) Environment swaps the mesh in its builder and may want to preload it in ChunkWorker as FallenLog is.

## Next step
Wait for CI on the PR, send the green sha to the PM, and tell Environment (session_012hVejTxLoxBpZyX3GQmNL9).
