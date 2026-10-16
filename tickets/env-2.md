# env-2: Reshape the land (ponds, a stream, gentler hills)
Lane: Environment
Owner: Environment session (Opus), session_012hVejTxLoxBpZyX3GQmNL9
Status: in_review
Branch: claude/environment-env-2
PR: #175

## Goal
Justin chose to reshape the land after groves and glades (D-084). Done means: a few ponds within an easy walk of spawn, one meandering stream with reedy banks, and softer, rolling hills, all from the shared height function so server and clients agree. The spawn clearing stays dry and fairly flat; clay still favours low wet ground. It wipes the world (items, buildings, dug cells, claims all sit on the old ground), so it lands in ONE PR with an upgrade-check note, and nothing that wipes is run or merged until the PM confirms timing with Justin.

## Plan
1. Prototype a new `BaseHeight` off to the side (`shared/Forest.cs`-style helper, e.g. `shared/Land.cs` owned by Environment), with an ascii/heightmap preview script, before touching `shared/World.cs`:
   - Gentler hills: broad octave at a larger scale (about 48 m instead of 32 m) and lower amplitude; keep the 8 m detail small.
   - Ponds: a sparse low-frequency basin field (about 90 m scale) that dips below `WaterLevel` in a few round hollows; aim for 2 to 4 ponds within 80 m of spawn, none within 24 m.
   - Stream: carve a channel where a 1-D-ish "river" noise crosses its midline (`abs(noise - 0.5) < w`), depth easing to just below water level, banks sloping over 2 to 3 m; sinuous at about 60 m wavelength; keep it out of the spawn clearing.
   - Spawn: blend toward a gentle plateau within 20 m of the origin.
2. Check: water fraction, slope histogram (walkability), clay patch count, tree count, spawn dryness; send the PM 2 to 3 options with preview images (via Render) before committing to one.
3. Hand `World.BaseHeight` change to Integration (owner of `shared/World.cs`) as a one-line call into `Land.Height`; Environment adds water-edge dressing for the stream (reeds, stones, wet mud).
4. Upgrade-check: the module schema does not change, but the world's stored data no longer fits; the PR description carries the wipe note and the PM schedules the publish with `--delete-data` only after Justin's OK.

## Progress log
- 2026-10-16: ticket drafted by Environment after D-084.
- 2026-10-16 19:10: first prototype in `tools/env-preview/land.py` (ascii map, `python3 -I tools/env-preview/land.py`). Gentler hills (48 m broad octave) plus pond basins (36 m field above 0.78) give about 6% water and 3 to 4 ponds within 120 m, spawn kept dry. Lesson: the water plane is flat at -1.0, so a stream cut by subtracting depth leaves dry trenches over hills; next try lowers a broad valley (about 20 m falloff) along the river line and carves the bed to a fixed depth below water.
- 2026-10-16 19:50: prototype v2 in `tools/env-preview/land.py`. The stream now sits in a broad valley (hills sink toward a floor just above water within about 26 m of the river line), with a channel about 3 m wide cut to a fixed bed 0.35 m below water. Distance from the river line uses noise value divided by its gradient; a plain value threshold made 17% of the map stream bed where the noise is flat. Ponds are round hollows (36 m field above 0.86) filled to 0.4 m below water. Spawn is blended to a gentle dry rise within 18 to 32 m. Result: about 6% water, a meandering stream about 66 m east of spawn, a second one further west, a few small ponds. Issues to fix: the river line is the 0.5 contour of 2-D noise, so it also makes closed loops (rings around hills); try clipping loops by keeping only contour segments longer than some length, or use a warped 1-D line instead.
- 2026-10-16 20:00: prototype v3. Loops are fixed: the river line is now `x + 66 + warp(x, z)`, with the warp's slope kept under 1, crossing multiples of 300 m. That gives one stream every 300 m running roughly north to south and wandering, never doubling back. The first stream passes about 66 m west of spawn. Distance uses the offset divided by the gradient. A few small ponds; spawn dry.
- 2026-10-16 20:10: Justin approved building env-2 now (D-129): ONE world wipe, landing once env-2 and the in-flight PRs are in. The PM brings Justin a wipe date and merges this PR at that time. Do NOT merge it and do NOT publish to Justin's world. Coordinate with Integration (`shared/World.cs`, upgrade check) and with Crafting, Ceramics and Animals (trees, clay, sheep). Before and after renders go through Render first.
- 2026-10-16 20:30: `shared/Land.cs` (C# port of the preview, checked equal to `tools/env-preview/land.py` at sample points); `World.BaseHeight` now returns `Land.Height` (one-line hook in an Integration-owned file). Stats over 301x301 m: water 8.7% (old 1.3%), clay 38.6% (old 37.3%), trees 0.071 per cell, max neighbour step 0.38 m (old 0.21), no cell steeper than 0.5. One stream wanders north to south about 40 to 60 m west of spawn; about 6 ponds within 90 m; spawn is a dry rise within 18 m. 1M heights take 0.4 s. Pond banks are a wide dip (field 0.36 to 0.74 toward -0.6) before the pond proper, and the stream channel slopes over 1 to 6 m, which keeps slopes gentle. Pond noise is offset off the lattice, because the shared hash is weak on the axes and ponds lined up in rows.
- 2026-10-16 20:45: on a fresh local world, LoadTest 5x5, BuildTest, SpawnCheck, CraftCheck and SocialTest all pass. ClayCheck once failed with "pot set down out of the fire's reach", but that run reused a database with CraftCheck's old fire nearby. On a fresh world the fire lands at (2.1, 2.1), same as main, and firing succeeds. A full ClayCheck run (water now at (-35,-35)) is in progress.
- 2026-10-16 20:55: Justin pre-approved the wipe (D-143); renders don't block (D-142). The PM adds the `world-wipe` label (D-130) and merges. Merged main (177 commits, clean), server and client build. Opened PR #175 at 608c712. Told PM and Integration (Integration runs the world tools and reviews the World.cs hook). Atmosphere seasons landed on claude/atmosphere-16-seasons keyed on w, with no TreeLeaves material.

## Open questions / blockers
- Wipe timing: PM confirms with Justin before anything that wipes is run or merged.

## Next step
Wait for CI on PR #175 (upgrade check red until `world-wipe`), send the green sha to the PM; confirm the background ClayCheck passed.
