# bugfix-51: Heavy-reducer pacing, loose-item cap, and the bucket for reducers that skipped it
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-51
PR: (not yet opened)

## Goal
Audit items from bugfix-24, after the call bucket (bugfix-50, #776). Honest play never notices any of it; a hacked client is slowed and refused with a friendly message.
- Per-reducer pacing (`Caps.cs`, `PaceMark` table, one row per player and reducer): PlaceStructure 0.4 s, Dismantle 0.3 s, SetName 1 s, SetLook/SetRaceLook 0.5 s. Refusals read "One moment, ...".
- Loose-item cap: at most `MaxItemsPerChunk` (400) loose items per chunk, checked in `Drop` and in placing an item. Per chunk, not per player, so no schema change and no item is ever lost (the refusal rolls back). Message: "There's too much lying about here. Pick some up or put it away first".
- Bucket coverage: about 40 player-facing reducers (doors, locks, baskets, character menu, claims, towns, trades, troughs, groups, knowledge) never called RequirePlayer, so they bypassed the bucket. They now call `MeterCall(ctx)` (RateLimit.cs), which spends a call for the caller's player row if there is one. Dismantle also lacked RequirePlayer; it has it now. Scheduled and admin reducers are not metered.

## Progress log
- 2026-10-17: Caps.cs, pacing and cap wired in, MeterCall added to unmetered reducers; server module builds, ctx.Sender check passes.
- SpawnCheck gained a SetName pacing test. Local run of LoadTest (5 bots), SpawnCheck, SocialTest, CraftCheck, BuildTest all pass with no "Slow down" or "One moment" outside the deliberate tests.

## Open questions / blockers
- Chunk cap of 400 cannot be reached by CI bots; checked locally by temporarily lowering the constant.
- Integration should re-check that no tool path trips "Slow down" or "One moment".

## Next step
Wait for PR CI, then send the PM the green sha.
