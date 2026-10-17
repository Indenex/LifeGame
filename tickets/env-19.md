# env-19: Forest-edge undergrowth band
Lane: Environment
Owner: Environment thread (session_012hVejTxLoxBpZyX3GQmNL9)
Status: in_review
Branch: claude/environment-edge-undergrowth
PR: (opening)

## Goal
Where the meadow meets the wood, a loose, patchy band of the existing ferns and low shrubs softens the line between grass and leaf litter, after the art references. Uses existing meshes; grass unchanged. PM approved (options a, b, c from the meadow-edge self-review; this is b).

## Progress log
- 2026-10-17: `rim` term in Terrain.AddVegetation (forest density 0.08-0.45, patchy by noise salt 98, none on heath) adds ferns (0.22 x rim) and shrubs (0.08 x rim). View bench at 0,10: about +2% instances within radius 3. Builds; local renders at -10,0.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR, send the green sha to the PM, ask Render for a before/after; then env-20 (bush colour toward muted olive).
