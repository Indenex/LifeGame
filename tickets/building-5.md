# building-5: Sleeping mat respawn point
Lane: House building
Owner: House building thread
Status: done
Branch: claude/house-building-8ueqz2
PR: #61

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
- 2026-10-16: BuildTest weaves, lays, rolls up and re-lays a mat in the clay hut; merged main
  (bugfix-8 loops: mat Use finder and Carried are plain loops too). Locally green: BuildTest,
  LoadTest 5x5, SpawnCheck, CraftCheck, SocialTest, upgrade-check over origin/main.

- 2026-10-16 (resumed session_01RrKHUxWRH973Ndnxsad9nv): Justin decided D-061, keep the mat. Merged main into the branch (clean); locally green: BuildTest, LoadTest 5x5, SpawnCheck, CraftCheck, SocialTest, upgrade check over origin/main.

## Open questions / blockers
- (none) Survival approved the `RespawnPointHook` call in `Survival.cs` (2026-10-16); their survival-5 adds `Wetness = 0,` a few lines below, which should merge cleanly.

## Next step
Done (merged as #61, D-061).
