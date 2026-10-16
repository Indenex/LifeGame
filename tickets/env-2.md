# env-2: Reshape the land (ponds, a stream, gentler hills)
Lane: Environment
Owner: Environment session (Opus), session_012hVejTxLoxBpZyX3GQmNL9
Status: todo
Branch: claude/environment-env-1 (stack on it after #113 merges; a new branch name may be assigned)
PR: (once opened)

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

## Open questions / blockers
- Wipe timing: PM confirms with Justin before anything that wipes is run or merged.

## Next step
Rework the stream in `tools/env-preview/land.py` as a valley carved to a fixed bed depth, then port the chosen height to `shared/Land.cs` (after #113 merges).
