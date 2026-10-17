# env-37: One water level
Lane: Environment
Owner: Environment thread (session_014EL1FHDJi92N6gKixNv6Q5)
Status: in_progress
Branch: local `env-37-local` until #828 merges, then claude/environment-np240l
PR: (none yet)

## Goal
docs/CONTENT.md lists "water level defined in several places" as the landmark family's
next tidy-up (D-206). `World.WaterLevel` (-1.0) becomes the one value: `Trees.WaterLevel`,
`StructureShape.WaterLevel`, `Needs.WaterLevel` and the client's `Look.WaterLevel` now
point at it instead of repeating -1.0. No behaviour or look change. One-line edits in
other lanes' files (Survival's Needs.cs, Building's Structures.cs, Atmosphere's Look.cs),
so the PM tells those lanes.

## Progress log
- 2026-10-17 15:10: edits made locally; no Godot/dotnet in this container, CI builds it.

## Open questions / blockers
- Waiting on #828 to merge before pushing (one branch per session).

## Next step
After #828 merges: reset claude/environment-np240l to origin/main, cherry-pick env-37, update CONTENT.md's "Next" note, push, open the PR.
