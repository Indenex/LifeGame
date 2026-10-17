# bugfix-27: the server enforces walls (bugfix-24 audit item 2)
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-27 (stacked on claude/admin-1 / #335 for the fly exemption)
PR: (see GitHub; stacked on #335)

## Goal
A hacked client can no longer walk through fences, shut gates, hut or house walls, or round solid things (kilns, baskets, jars, claim markers, stations), because the server's `Move` refuses a step that goes through one (D-182). A jump can hop low fences and gates, but never a fence in a claim the player doesn't belong to, and a flying admin (`/fly`) is exempt.

## What exists and what this adds
- Server already had `StructureBlocksStep` (fences, shut gates, hut and house walls) and `AnimalStepIntoObstacle` (trunks, boulders) for sheep (bugfix-23 / building-8). New `server/spacetimedb/PlayerCollision.cs` reuses the structure rules for players and adds: round solids (step ends inside radius + body and nearer than before), the jump hop (`Jumping.AirHeightWithGrace` from the player's `player_jump` row, with a quarter-second grace either side so a laggy client isn't pushed back) and the claim rule (`Jumping.FenceStops` logic with `ClaimAt` / `HasClaimRights`).
- `Move` calls it after the pace clamp and keeps the player where they were when blocked. The client still slides along walls; the server just refuses, with a smaller body (0.25 against the client's 0.3) so an honest client is never pushed back.
- New shared helper `Structures.StepCrossesLeanTo` (tested, unused for now), `Jumping.AirHeightWithGrace`.

## Not enforced yet (follow-ups)
- Lean-to sides and tree trunks, boulders, landmarks: the check bots in tools/ walk straight lines and wedge on them (tried: BuildTest stalled at the Clay hut gather step with trunks on, and again with lean-to sides on). They need to path around obstacles first. BuildTest got a "step aside when the server keeps me put" detour for walls.
- Rocks: shared `Rocks.LiesAt` is not the rule the client draws (client rocks follow Terrain.AddVegetation), so the server must not use it for players until the two are one rule (Environment).
- Saplings are client-only scenery.

## Progress log
- 2026-10-17: built on a branch stacked on #335 (needs IsFlyingAdmin). Verified locally on a fresh server: BuildTest (all stages) passes in 492 s (it takes longer than before because the bots now walk like players and step around walls), CraftCheck passes (5m40), SpawnCheck and SocialTest pass. New BuildTest check `WallsStopWalkers`: a Move straight through a finished fence is refused (server position stays on its side), and after a `Jump()` the same Move hops it.
  - Bots: BuildTest `Step` steps aside when the server keeps it put; after the hearth stage the bots leave the hut through the doorway (BuildHearth), because walls now bind them.

## Open questions / blockers
- Needs #335 merged first (the branch merges claude/admin-1).

## Next step
Wait for CI on the PR; merge it after #335; then pick the follow-ups above (bots that path around obstacles, then trunks and lean-to sides).

- 2026-10-17: CI on 148898ff failed in BuildTest 'Raised hearth: gathered stones and clay' (bots were still inside the hut, walls and the shut door penned them in). Fix: the bots open the door and walk out through the doorway before gathering. BuildTest passes locally in 472 s.
