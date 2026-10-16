# building-4: Mud-brick house
Lane: House building
Owner: House building thread (Claude, session_01H6jwMs4KSGQbG6zYjkBKqZ)
Status: in_progress
Branch: local `building-4-local` on top of claude/house-building-8ueqz2 (PR #41). It gets pushed to claude/house-building-8ueqz2, restarted from main, once #41 merges. Until then the work is saved as /mnt/project-files/patches/building-4.patch (apply on #41's branch).
PR: (after #41 merges)

## Goal
A bigger building that comes after the clay hut, now that bricks exist (D-033 deferred it). The mud-brick house is StructureKind 5, a 4.8 x 4.0 m rectangle with its door on +Z. Stages and PM-default costs (the PM is taking them to Justin):
- Footings: 6 stone, 4 sticks
- Brick walls: 16 sun-dried bricks, 6 daub
- Rafters: 12 sticks, 2 cordage
- Thatch: 14 fiber

What's done means:
- Rain cover is 1 and warmth 0.85; the door shut gives 0.9.
- A burning fire anywhere inside the walls makes the whole room fully warm.
- The same door and bar as the hut hang in its doorway.
- It weathers after 7 days untouched instead of 3 (D-046's clock otherwise).
- Players can't walk through its walls, only the doorway.

## Progress log
- 2026-10-16 00:40: started on the PM's go-ahead (default costs).
  - Server: BrickHouse enum, stage costs, FireInside warmth, HasDoorway in HangDoor, and per-kind weathering via Structures.WeatherAfterFor.
  - Shared: shape, DoorwayZ, WeatherAfter.
  - Client: StructureModel.House.cs (footings, hearth ring, brick courses, lintel, rafters, clay gables, thatch); door frame per kind in StructureView; HouseWalls collision; Use and doorway for any kind with a doorway.
  - Screenshots: /mnt/project-files/screenshots/building-08-brick-house.png and building-08b-brick-house-wide.png (`--building-showcase --building-house`).
- 2026-10-16 01:00: BuildTest `brick-house` mode added. It runs alone, outside "both", because bricks take minutes to dry. Running it locally.

## Open questions / blockers
- Justin, through the PM: the cost and the look. 16 bricks is about 64 clay, versus 12 for the hut's daub.

## Next step
Finish the local `dotnet run -- brick-house` BuildTest run. Then, once #41 merges, restart claude/house-building-8ueqz2 from main, apply the patch, and open the PR.
