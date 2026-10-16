# crafting-12: Craft start and end hooks for other players' work sounds
Lane: Crafting
Owner: Crafting thread
Status: in_review
Branch: crafting-work-hooks
PR: #97

## Goal
D-072: other players' craft work is audible, about 60% quieter than your own. `CraftJob` is private, so Atmosphere (atmosphere-12, `server/spacetimedb/WorkSounds.cs`) mirrors jobs into a public table. Crafting gives it two hooks, declared in `Crafting.cs` like the other lane hooks: `OnCraftStarted(ctx, job)` after a job is inserted in `StartCraft`, and `OnCraftEnded(ctx, job)` after it is deleted in `FinishCraft`. Those are the only places that insert or delete `CraftJob`. Unimplemented partial methods compile to nothing, so this merges before or after atmosphere-12. Atmosphere drops its own declarations.

## Progress log
- 2026-10-16 17:30: hooks added on main 7c65c0f; server builds. Told Atmosphere (session_01KvVF5vpkYEHTKNRpHC18WN) to drop its declarations.

## Open questions / blockers
- If atmosphere-12 (WorkSounds.cs 9965c81, carries the same two declarations) merges first, merge main here and delete the declarations from Crafting.cs, keeping the two calls.

## Next step
Wait for CI on the PR, then send the PM the green sha.
