# building-10: Log cabin
Lane: House building
Owner: House building thread (session_01RrKHUxWRH973Ndnxsad9nv)
Status: in_review
Branch: claude/house-building-8ueqz2 (restarted from main after #202 merged; old backup claude/house-building-8ueqz2-cabin)
PR: (not yet)

## Goal
D-087, third of three; Justin picked option (a) as D-151. The log cabin is the forest's way to the same home as the mud-brick house. Same 4.8 × 4.0 m room and doorway, the same shelter (rain cover 1.0, warmth 0.5, 0.55 with the door shut, fully warm with a fire inside), the same door and bar, and it weathers after 7 days, like the house. It is built of logs from felled trees (D-060):
- Sill: 4 stone + 4 logs
- Log walls: 16 logs
- Rafters: 12 sticks + 2 cordage
- Thatch: 14 fiber

Justin sees a cabin of round logs with crossed corners, a log lintel over the door, log gable ends and the house's thatched roof.

## Progress log
- 2026-10-16 19:50: built.
  - Shared: a `Room` shape flag for the brick house and the cabin; StepCrossesWalls and StageProcess use it. LogCabin shape and costs, appended after Hearth.
  - Server: StructureKind.LogCabin. ShelterAt's fire-warms-the-room, sheep wall-blocking and the client's walls, door frame and collision all work by the Room flag now, not by BrickHouse.
  - Client: StructureModel.Cabin.cs. The house's rafters and thatch moved into a shared `HouseRoof(list, t, gable)`; the cabin uses log gables.
  - BuildTest `log-cabin` mode, run on its own: each bot makes a stone axe and chops 10 logs, both build the cabin, then the door test.
  - Server, client and BuildTest build.

- 2026-10-16 20:05: `dotnet run -- log-cabin` passed locally in 437 s. Each bot made a stone axe and chopped 10 logs, both built the cabin, standing inside is sheltered, it survived a reconnect, and the door hung, opened, barred and unbarred.
- 2026-10-16 20:05: HANDOFF to a Sonnet session (D-159). Not yet merged with the latest main; stacked on building-9 (#202).
- 2026-10-16 21:25 (Sonnet session): #202 merged. Fast-forwarded the branch to main and merged the cabin branch. Conflicts resolved: Structures.cs (Station and Room flags both kept), StructureModel switch (Hearth, then IsRoom => LogCabin), BuildTest Craft (score parameter kept with crafting-16's 1.2x wait). LogCabin is the last kind (after Hearth). Walls: the cabin has Room = true, so bugfix-23's Collide already runs it through HouseWalls; no extra collision code. Local: server builds, bindings regen, `BuildTest log-cabin` passed (412 s), upgrade-check vs origin/main passed, LoadTest and SpawnCheck passed.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the building-10 PR, then send the PM the green sha. Request a render (log cabin in a clearing, door open; plus a half-built one with log walls going up). If another PR takes the next kind first, merge main and re-append LogCabin after the last kind, then rerun tools/upgrade-check.sh origin/main.
