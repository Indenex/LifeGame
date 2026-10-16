# env-5: Landmarks and trails
Lane: Environment
Owner: Environment session (Opus), session_012hVejTxLoxBpZyX3GQmNL9
Status: in_progress
Branch: claude/environment-env-5 (stacked on claude/environment-env-4 / PR #213)
PR: (once opened)

## Goal
Give players things to steer by and name (D-081, a world to get lost in): deer trails worn between glades, water and heath; huge lone oaks in glades; rock outcrops on the hilltop heath; stepping stones where a stream is easy to ford. All deterministic (shared seed), visual apart from trails thinning trees through `Forest.Density` (so the server's choppable trees keep paths open). No wipe. Justin sees renders of a giant oak, an outcrop, a ford and a trail through the trees.

## Progress log
- 2026-10-16 22:25: sent the PM env-5 options (A landmarks and trails, recommended; B seeing further; C gatherables by habitat; D footpaths that wear in). Started A as the default while #213 is in CI.
- 2026-10-16 22:45: `shared/Landmarks.cs`: `Trail` (contour of a slow 2-octave noise, about 1.5 m wide, distance by value over gradient with an exact early-out; 3.2% of the land), `GiantOakAt` (first fitting of 6 seeded spots per 64 m square: glade, dry, not heath or wetland, off trails, not on a choppable tree, 20 m+ from spawn; 10 within 150 m, nearest (30,12) and (-34,13)), `OutcropAt` (per 48 m square on heath; 12 within 150 m), `FordStoneAt` (a row every 40 m along each stream). `Forest.Density` x (1 - 0.9 trail). Terrain: giant oak (trunk and canopy scaled 3.2 x 2.6), outcrop clusters of 4 to 6 big boulders, flat stepping stones in the stream, trail ground worn toward Earth with short sparse grass and no undergrowth. Trees per cell 0.071 to 0.054 over 300 m (heath and trails). Client and server build.

## Open questions / blockers
- Waiting on Justin's env-5 pick via the PM (default A).
- Swap in Modeling's Outcrop and RiverStone meshes once on main (model-2-env).

## Next step
Run CraftCheck and SpawnCheck on a fresh local world, request renders (giant oak (30,12), outcrop (36,61), a ford on the stream west of spawn at z = 27 or -13, a trail), and open the PR after #213 merges.
