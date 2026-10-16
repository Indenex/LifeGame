# building-4: Mud-brick house
Lane: House building
Owner: House building thread (Claude, session_01H6jwMs4KSGQbG6zYjkBKqZ)
Status: done
Branch: claude/house-building-8ueqz2 (restarted from main 88994fa after #41 merged)
PR: #53

## Goal
A bigger building that comes after the clay hut, now that bricks exist (D-033 deferred it). The mud-brick house is StructureKind 5, a 4.8 x 4.0 m rectangle with its door on +Z. Stages and costs (Justin confirmed them, D-057):
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
- 2026-10-16 01:15: local `dotnet run -- brick-house` passed in 814 s. The bricks took about 9 minutes to dry in the open. The house was built by two players, it shelters you, it survives a reconnect, and the door and bar work.
- 2026-10-16 01:20: stores are now per kind (StructureShape.StoreStacks, Structures.IsStore), so Ceramics' storage jar can reuse the basket's store by appending a kind with StoreStacks set. BuildTest basket passes.
- 2026-10-16 02:53: merged as #53.

## Open questions / blockers
- (none; Justin kept 16 bricks and the other default costs, D-057)

## Next step
Done. The next building ticket is waiting on the PM.