# building-14: Bigger buildings and a real doorway (Justin's playtest)
Lane: House building
Owner: House building thread (Sonnet)
Status: in_progress
Branch: claude/house-building-8ueqz2-bigger
PR: (not yet)

## Goal
Justin, looking at a log cabin at its first stage: "the scale of the buildings probably needs to be a bit bigger, and leave an actual doorway opening so the character can walk in, right now he hits his head and his feet on the entrance". He picked about 25% bigger. Every house-type building is 1.25x in footprint and height with the same material costs; each doorway is at least about 2.1 m clear and wider than the player, with nothing lying across the bottom, at every stage.

## Progress log
- 2026-10-16: built as data. `StructureShape.Scale` (default 1; 1.25 on lean-to, clay hut, mud-brick house, log cabin). `Structures.Shapes` is built through `Scaled()`, which multiplies each shape's HalfWidth, HalfDepth, Radius, Clearance, DoorwayZ and SmokeY by its Scale once, so placement, Contains, ground samples, clearance, the server's HasDoorway/Contains checks and the door position all use the real size with no change to their callers. The model stays at its design size and `StructureView` scales itself by `Structures.ScaleOf(kind)` (also the placement ghost; weathering sag multiplies the base scale). The few wall constants that are kind-specific (HutWallRadius, HouseWallX/Z, HouseDoorHalfWidth) now go through `...For(kind)` accessors in StepCrossesWalls, the client's wall collision, and the server's fire-inside-the-room check. Costs unchanged.
  - Doorways: house/cabin door top 1.62 -> 1.78 design units (x1.25 = 2.2 m clear; hung door 2.15 m); the clay hut's hung door 1.45 -> 1.7 (2.125 m) under its 1.7 (2.1 m) wall gap; the 2.4 m/2.2 m wide openings are far wider than the player. The log cabin's front sill log ran across the doorway at foot height: split so it stops either side of the doorway (the sill stage no longer blocks it). Brick footings already left the doorway open; client wall collision opens the doorway gap at every stage it exists.
  - Tests: BuildTest checks each home is 1.25x and that the open doorway lets a step through, a shut door and the side wall do not (shared numbers both sides use). BuildTest default (343 s) passes; log-cabin (376 s) and tools/upgrade-check.sh origin/main pass.
  - Existing buildings in Justin's world take the new size through the shape data (rows hold only kind, position and yaw): no wipe. They may now overlap a neighbour a little; nothing is destroyed.
- Render requested for a walk-through clip of the cabin doorway: see the PM message.

## Open questions / blockers
- Lean-to is also 1.25x (its 1.7 m ridge didn't fit a standing player). Say if not.
- Player height for the 2.1 m clear: I used Justin's own "about 2.1 m"; the doorways are 2.1 to 2.2 m clear.

## Next step
Wait for CI on the building-14 PR, send the PM the green sha, then ask Render for the doorway walk-through. Merge main after #331 / #362 land (Structures.cs conflicts are small).
