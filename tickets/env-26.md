# env-26: No square grass edges
Lane: Environment
Owner: Environment thread (session_012hVejTxLoxBpZyX3GQmNL9)
Status: in_review
Branch: claude/environment-no-square-edges
PR: (opening)

## Goal
Justin (screenshot renders/justin/2026-10-17-square-grass.webp at f6e6ac5c): "can this be fixed/smoothed so it's not so square ever?" A large patch of short, flat ground with straight, axis-aligned edges against the tall grass. Cause: a clay deposit. World.ClayScore is value noise on a 10 m lattice and grass was cut off hard at ClayScore 0.7; every grass patch also sat square to its 1 m cell, so mask edges drew the cell grid. Cleared ground (buildings, claims, fires, felled trees) and dug cells cut grass the same way. Done means no straight or grid-aligned edge anywhere: grass and flowers thin and shorten over about 2 m toward any bare mask on a noise-ragged line, worn earth round clearings fades out the same way, and grass patches no longer line up. Visuals only: World.IsClay (server) unchanged.

## Progress log
- 2026-10-17: `environment/SoftEdges.cs`: GrassCover (clay with a ragged threshold 0.6-0.76, times Feather), Worn (earth wear round cleared cells), Feather (nearest cleared/dug cell within 2 cells, jittered by hash and 3 m noise). Terrain: tuft chance x smoothstep(cover), tuft height x lerp(0.45, 1, cover), random yaw and +-0.15 m offset per patch; flower chances x cover; TileColor cleared tint x Worn. ChunkWorker snapshot carries cleared cells 2 cells round the chunk (FeatherCells) so feathers cross chunk borders. Builds.

- 2026-10-17: Renders at a clay patch (22,4, zoom -6): the long straight grass edge along the clay is now ragged and the grass shortens toward it (sq2base vs sq2 in the scratchpad). Merged main.

## Open questions / blockers
- (none)

## Next step
Wait for CI; send the green sha to the PM; Render before/after requested.
