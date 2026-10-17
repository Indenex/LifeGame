# building-24: Finished buildings drawn as one mesh per material
Lane: House building
Owner: House building thread (session_01DzgG4copWqifKWUcydbjBZ)
Status: in_progress
Branch: claude/building-ht3tnl
PR: (not yet)

## Goal
"Home and friends" focus (PM, 2026-10-17). A camp full of friends' houses should not cost the frame rate. Port the unmerged perf patch (/mnt/project-files/patches/building-perf-merge/, made on the older main before see-through walls) onto today's main: a finished building is drawn as one mesh with one surface per material, shared by every finished building of its kind, instead of one mesh per stick, rib and brick course. Old measurement: a 20-building camp went from 1071 to 481 draw calls. Nothing looks different. A building being cut away for the player (see-through, D-168) goes back to its pieces while faded and merges again once solid. Jars and troughs (maker's stamp) and batch devices (pieces change as they fill) stay piece by piece. `--building-showcase --building-camp` lays out the camp; `--building-no-merge` turns merging off; `--render-stats=1` prints draw calls.

## Progress log
- 2026-10-17 15:40: reviewed both patch folders. building-8's smoke and doorway firelight already shipped as Atmosphere's AtmosphereHomes.cs (atmosphere-19), so it is dropped. The perf merge and the playtest-4/5 fixes (building-25) are not on main.
- 2026-10-17 15:50: StructureView.Merge.cs and the hooks in StructureView.cs (Show, SetDevice, UpdateSeeThrough, SetWeathered) written for main's see-through; BuildingSystem.Camp.cs from the patch. Client builds.

## Open questions / blockers
- (none)

## Next step
Measure draw calls of `--building-showcase --building-camp --render-stats=1` with and without `--building-no-merge`, then open the PR.
