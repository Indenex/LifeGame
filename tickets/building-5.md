# building-5: Sleeping mat respawn point
Lane: House building
Owner: House building thread
Status: in_progress
Branch: claude/house-building-8ueqz2
PR: (not yet)

## Goal
A player can weave a sleeping mat (8 fiber, `weave.mat`), stand inside a finished hut or
mud-brick house they have a right to (they placed it, helped build it, or hold rights in the
claim it stands in), and press G to lay the mat out. After dying they wake on the mat. One mat
per player: laying a new one rolls up the old. G next to your own mat rolls it up. If the
building is taken down or weathers away, the mat drops to the ground and respawn falls back to
Survival's wilderness edge. Justin sees a woven grass mat on the floor by the hearth.

## Progress log
- 2026-10-16: server `BuildingBed.cs` (SleepingMat table, LayMat, RollUpMat, DropMats,
  RespawnPointHook), one-line hook call in `Survival.cs` (asked Survival to approve), client
  mat model and G handler, showcase `--building-mat`. Screenshot:
  /mnt/project-files/screenshots/building-09-sleeping-mat.png

## Open questions / blockers
- Survival to confirm the `RespawnPointHook` call in `Survival.cs`.

## Next step
Add a mat check to tools/BuildTest (craft, lay in a finished hut, see the row, roll up), run the tests, open the PR.
