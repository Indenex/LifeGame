# env-7: Streams that wind (meander fix, rides the env-2 wipe)
Lane: Environment
Owner: Environment session (Opus), session_012hVejTxLoxBpZyX3GQmNL9
Status: in_review
Branch: claude/environment-meander
PR: (once opened)

## Goal
Render shots of env-2 showed straight, parallel stream banks near (-46,-46). The stream line came from two slow warp octaves, so long stretches ran as a straight diagonal at a steady width. Done means streams wind on several scales, with a channel that narrows and widens, while staying wadeable and walkable everywhere. The PM confirmed env-2 is not yet published to Justin's world, so this rides the same wipe (option A): it changes `Land.Height` near streams and needs the `world-wipe` label too.

## Progress log
- 2026-10-16 23:40: `shared/Land.cs`: River adds a 22 m octave (x18 m) and an 11 m octave (x3 m), offset off the noise lattice; channel width factor 0.85 to 1.35 along the stream (30 m noise); MaxRiverSlope 3.0 -> 5.3 for the new octaves' slopes. The early-out is still exact: 0 differing heights over 801x801 m vs no early-out. Longest near-straight run of the nearest stream (5 m samples, 2nd difference <= 1) 135 m -> 40 m; mean |2nd difference| 0.69 -> 1.47; water width 5 to 16 m; water 8.7%; max neighbour step 0.48 m (was 0.38; no cell over 0.5); flood fill: 100% of walkable land within 200 m reachable from spawn. `tools/env-preview/land.py` matched. Server and client build.

## Open questions / blockers
- None. The PM merges with the world-wipe label in the same window as env-2's wipe; Justin keeps his world until the PM says this is in.

## Next step
Run SpawnCheck, CraftCheck and ClayCheck on a fresh local world, open the PR (world-wipe), send the green sha to the PM.
