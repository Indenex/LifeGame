# building-1: Blueprint building: lean-to, then clay hut
Lane: House building
Owner: House building thread
Status: in_review
Branch: claude/house-building-8ueqz2
PR: #6

## Goal
Placing a house puts down a visible see-through blueprint of the finished building; any player can add materials and each stage (frame, wattle, daub, roof) turns solid. Only the placer or the claim owners can cancel or remove it. Starts with a stick lean-to, then a clay hut once clay exists. Structures persist and shelter players. Done when two players finish a lean-to together, stand inside sheltered, and still see it after reconnecting.

## Progress log
- 2026-10-15: lane started (ticket created by the PM from the lane brief)
- 2026-10-15 21:13: pushed e14eb27, opened draft PR #6. Lean-to (frame, lashing, ribs, thatch) and wattle-and-daub clay hut (posts, wattle, daub, roof). B to place with a ghost preview, hold G to add materials, anyone can help. `Module.ShelterAt/ShelterFor` for Survival. `tools/BuildTest` (two bots build both, stand inside, reconnect) passes locally and in CI.
- 2026-10-15 21:17: CI run https://github.com/Indenex/LifeGame/actions/runs/38808275263 green (server + BuildTest, Linux, Windows, macOS clients).
- 2026-10-15 21:25: merged main; placed sites now show the whole finished building as a see-through blueprint, and pieces turn solid as materials arrive (Justin's requirement). Screenshot: project files `screenshots/building-04-blueprint.png`.

- 2026-10-15 21:30: added the CheckClaimAccess partial hook for Claims; BuildTest lean-to passes locally.
- 2026-10-15 21:40: Justin approved merging PR #6 and kept the proposed costs and limits. Merged main (Survival is in): implemented ShelterAtHook, so finished buildings cut the cold drain. Moved building keys off Survival's: H adds materials (G is forage), T turns the blueprint (R is drink). BuildTest passes locally.

## Open questions / blockers
- Waiting on crafting-core reaching main before switching to `LifeGame.Shared.Item` ids, the `OnProcessPerformed`/`QualityCapFor` hooks (ids build.frame, build.wattle, build.daub, build.roof), and Brick/Daub (109/110) for the clay hut. Until then costs use Stick, Fiber, Stone and dig-clay Clay.
- Dismantle is placer-only; claim owners get it once Claims exists.
- Claims access: Building.cs declares `static partial void CheckClaimAccess(ctx, x, z)`, called when placing and dismantling (never when adding materials). Claims implements it by calling its `RequireClaimAccess`.

## Next step
Once CI is green on the latest commit, merge PR #6 into main (Justin approved) and tell the PM the merge commit.
