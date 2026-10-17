# bugfix-57: Hidden values, family 4 (StructureProgress stage internals)
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_progress
Branch: local commit on main 5064a254; patch in /mnt/project-files/bugfix/bugfix-57/. Goes on claude/bugfix-rnwmoa after #830 and bugfix-56, or a branch the PM names.
PR: (none yet)

## Goal
Part of audit item 7 (tickets/bugfix-53.md). Other clients no longer receive a stage-in-progress's summed material quality, item count or helper list. `StructureProgress.Quality` stays public: the client draws store stacks and trough litres from it (BuildingSystem.Use.cs, CeramicsView.Troughs.cs, ClayCheck).

- New private `StructureStage` (StructureId, StageQualitySum, StageItems, StageHelpers) in Building.cs. `SaveProgress` writes the public row with those columns zeroed and the real values to the private row; `ProgressOf` overlays them (a stage begun before this reads its public columns); `RemoveStructure` deletes both.
- No client or tool reads the stage columns.

## Progress log
- 2026-10-17 15:50: written; not built locally (no toolchain). CI (BuildTest) and the Upgrade check are the checks.

## Open questions / blockers
- (none)

## Next step
Push to a branch, open the PR (list Building.cs for House building), wait for CI.
