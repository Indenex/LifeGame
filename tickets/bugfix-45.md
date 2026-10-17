# bugfix-45: Server refuses trunks, boulders and lean-to sides (continues bugfix-34)
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_progress
Branch: claude/bugfix-45
PR: (not yet)

## Goal
D-182: the server, not just the client, stops a step into a tree trunk, a boulder or a lean-to's sides and back. The check bots now route round obstacles with `tools/common/BotGround.cs` (merged), which is why this can go in now (bugfix-27/34 had to leave it out because the bots walked straight lines). The client's `TerrainCollision` uses the same shared rule (`shared/Obstacles.cs`), so client and server agree.

## Progress log
- 2026-10-17: restored the enforcement from bugfix-34's history (reverted ceffdf60 on the server part): `PlayerCollision.cs` has `ServerGround : ICellGround`, `Obstacles.StepBlocked` and `Structures.StepCrossesLeanTo`. Client `TerrainCollision` uses the shared `Obstacles.At` again. env-21's shore stones moved into `Obstacles.ShoreStones` (the early-return bound is now 0.334) so the client, server and bots use one number. SpawnCheck's trunk/boulder probe restored.
- 2026-10-17: running all five tools locally (SpawnCheck, LoadTest, SocialTest, CraftCheck, BuildTest x2); results below once done.

## Open questions / blockers
- Landmarks (giant trees, outcrops) are not enforced on the server yet.
- Unstuck (bugfix-44) should also avoid trunks: add `Obstacles.At` to its `StandingFree` once this lands.

## Next step
Read the local tool results, push, open the PR, send the green sha to the PM.
