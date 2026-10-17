# env-37: One water level
Lane: Environment
Owner: Environment thread (session_014EL1FHDJi92N6gKixNv6Q5)
Status: done
Branch: claude/environment-np240l
PR: #833

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
- (none)

## Next step
None, merged as #833.
