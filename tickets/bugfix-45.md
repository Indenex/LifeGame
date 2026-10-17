# bugfix-45: Server refuses trunks, boulders and lean-to sides (continues bugfix-34)
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-45
PR: (not yet)

## Goal
D-182: the server, not just the client, stops a step into a tree trunk, a boulder or a lean-to's sides and back. The check bots now route round obstacles with `tools/common/BotGround.cs` (merged), which is why this can go in now (bugfix-27/34 had to leave it out because the bots walked straight lines). The client's `TerrainCollision` uses the same shared rule (`shared/Obstacles.cs`), so client and server agree.

## Progress log
- 2026-10-17: restored the enforcement from bugfix-34's history (reverted ceffdf60 on the server part): `PlayerCollision.cs` has `ServerGround : ICellGround`, `Obstacles.StepBlocked` and `Structures.StepCrossesLeanTo`. Client `TerrainCollision` uses the shared `Obstacles.At` again. env-21's shore stones moved into `Obstacles.ShoreStones` (the early-return bound is now 0.334) so the client, server and bots use one number. SpawnCheck's trunk/boulder probe restored.
- 2026-10-17: all pass locally against the enforcing server: SpawnCheck (incl. trunk and boulder probes), LoadTest 5 bots 5 s, SocialTest, CraftCheck, BuildTest clay-hut and lean-to,basket,fence.

- 2026-10-17: CI failed `SpawnCheck: can walk on after reconnecting` on 2e02cf69: the test walked 3 m straight east from a random z at x=-80 and the server (rightly) refused a step into a trunk. Fixed: it now slides round obstacles with BotGround and checks it moved >1.5 m. 6 of 6 local runs pass.

- 2026-10-17: CI `Building check (other structures)` failed `a jump hops the fence` on ff8eb598: the test stepped to a mirror point that could be metres away (clamped by the walking allowance) or inside a trunk (now refused). The test now stands 0.9 m from the fence and steps to 0.9 m on the far side, picking a spot free of trunks and boulders. 4 of 4 local fence runs pass.

## Open questions / blockers
- Landmarks (giant trees, outcrops) are not enforced on the server yet.
- Unstuck (bugfix-44) should also avoid trunks: add `Obstacles.At` to its `StandingFree` once this lands.

## Next step
Wait for CI on the PR, then send the green sha to the PM.
