# building-10: Log cabin
Lane: House building
Owner: House building thread (session_01RrKHUxWRH973Ndnxsad9nv)
Status: in_progress
Branch: local building-10 (worktree /home/user/lg-cabin), backed up on claude/house-building-8ueqz2-cabin; stacked on building-9
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

## Open questions / blockers
- (none)

## Next step
Run `dotnet run -- log-cabin` in tools/BuildTest against a fresh local server. Once building-9's PR has merged, merge main and open the building-10 PR, then request a render.
