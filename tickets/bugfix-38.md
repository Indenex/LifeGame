# bugfix-38: Check bots route round trunks, boulders and buildings
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-38
PR: (to open)

## Goal
The check bots walk straight lines and get stuck on anything solid in the way: round solids (claim markers, tables, kilns), fences, hut and lean-to walls, trunks and boulders in a thicket. That is the root of the on-and-off gather failures lanes see in BuildTest, CraftCheck and SocialTest. Flaky-test rule: bots must walk like players.

## Change (tools only; server and client behaviour unchanged)
- `shared/Obstacles.cs`: the trunk/boulder rule as shared code (also used by bugfix-34 for the server later).
- `tools/common/BotGround.cs` (linked into BuildTest, CraftCheck, ClayCheck, SocialTest, SpawnCheck): grid A* `Route` over trunks, boulders and the server's own building rules (fences, shut gates, hut/house walls, lean-to sides, round solids), `Slide` (push out of obstacles, hold at walls as the server would), `Stand` (move a target out of a solid), `Describe` for failure messages.
- Tools follow routes: CraftCheck/ClayCheck `WalkTo`, BuildTest `WalkTo` and gather steps, SocialTest `Walk` (by legs, with a 60 s limit and a message saying where the bot is and what stands near), SpawnCheck.
- Local: SpawnCheck, CraftCheck, ClayCheck, SocialTest and BuildTest (both parts) pass.

## Next
bugfix-34 (server refuses trunks/boulders/lean-to sides; the bots are ready for it).
