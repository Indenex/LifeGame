# bugfix-34: Server enforces trunks, boulders and lean-to sides
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_progress
Branch: claude/bugfix-34
PR: (to open)

## Goal
bugfix-27 (#394) made the server refuse steps through fences, gates and hut/house walls. Left out: tree trunks, boulders and lean-to sides, because the check bots walk straight lines and wedge on them, and because the rock rule differs between client (Terrain.AddVegetation) and server (`Rocks.LiesAt`). Done = a hacked client cannot walk through those either, honest clients are never pushed back, and the check bots still pass.

## Plan
1. `shared/Obstacles.cs`: the trunk and boulder rule moved out of `client/scripts/environment/TerrainCollision.cs` (`ObstacleAt`) into shared code, parameterised by what each side knows (tile height, dug depth, cleared cells). One rule for client, server and bots. Giant trees and outcrop stones (Features, LandmarkProps) stay client-only for now.
2. The client calls the shared rule (no behaviour change). A shared push-out (`Obstacles.PushOut`) is used by the client, the bots and the server.
3. Server `PlayerStepBlocked` refuses a step that ends inside an obstacle's body (radius + 0.25) that it was not already inside; honest clients, which slide along the edge, are never refused. Felled trees (ChoppedTree), cleared cells (under structures) and dug cells follow the client rules.
4. Check bots apply the same push-out as the client, so they slide round trunks instead of wedging.
5. Lean-to sides via `Structures.StepCrossesLeanTo` (already in shared).
6. Check: a Move straight into a trunk and a boulder is refused; a Move that slides along is accepted.

## Progress log
- 2026-10-17: ticket written; started with the shared rule.

## Next step
Write shared/Obstacles.cs and switch the client to it.
