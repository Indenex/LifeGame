# atmosphere-5: No pops when ground is cleared; canopy fade under Compatibility
Lane: World atmosphere and sound
Owner: Atmosphere thread (session_01PQgMLoZLFAprUyZzmQuSUX)
Status: in_progress
Branch: claude/atmosphere-sound-hlqfke (after #32 merges)
PR: (none yet)

## Goal
From the Bug fixes playtest (via PM): placing a blueprint made the grass vanish instantly, a pop against D-036; and the canopy fade looked like a dark blob under the Compatibility renderer. Done means grass on newly cleared ground lies down and sinks over about 1.6 s while the ground color eases to bare earth (and the reverse when a blueprint is removed), and the tree fade looks the same in Compatibility as in Forward+.

## Progress log
- 2026-10-15 23:45: Terrain notes which cells' clearing changed on a chunk rebuild; tufts on those cells carry (start time, flatten/regrow) as MultiMesh custom data, and ground vertices carry their old color plus start time in CUSTOM0. Look.Clock / global `world_seconds` drives the blend. Debug arg --debug-clear=seconds bares a 3 m circle around the player for screenshots. Compatibility render of the woods in progress.
- 2026-10-16 00:30: Compatibility fade checked: it works and looks close to Forward+; the grey distant trees are Compatibility's missing volumetric fog, not the fade. Fade radius tightened (0.9/1.9). Clearing renders verified: ground eases to bare earth and grass thins over ~1.6 s (screenshots/atmosphere-16-clearing-midway.png, -done.png). Branch kept local as atmosphere-5-wip until #32 merges; a backup copy was pushed by mistake to claude/atmosphere-5-hlqfke and can be deleted.

## Open questions / blockers
- Trees and rocks on a newly cleared cell still disappear at once.

## Next step
After #32 merges: merge main into atmosphere-5-wip, push it to claude/atmosphere-sound-hlqfke, open the PR.
