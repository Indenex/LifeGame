# integration-2: Server-side sneak flag for other players' avatars
Lane: Integration
Owner: Integration session session_01Rf5965nk4Lvd3f2QDteZSq
Status: done
Branch: claude/integration-sneak
PR: #121

## Goal
Other players see you crouch while you sneak (Animation lane, anim-1, PR #100; PM decision "sneak flag through Integration"). Server side only: a public `player_sneaking` table (one row per sneaking player, `PlayerId` primary key), a `SetSneaking(bool)` reducer the client calls only when Shift changes, the row cleared on disconnect, and `SELECT * FROM player_sneaking` in `Subscriptions.Area`. No change to `Move` or `player_pos`; new table only, so it publishes over Justin's world without a wipe. The Animation lane writes the client side in `AvatarWork.cs`.

## Progress log
- 2026-10-16 17:50: `server/spacetimedb/Stance.cs` (table, reducer, `StanceDisconnected`), one hook line in `Reducers.cs` `PlayerDisconnected`, subscription in `client/scripts/net/Subscriptions.cs`, SpawnCheck checks (sneak shows, idempotent, cleared by logging off, set and cleared again). Server, bindings, client and all tools build. Pushed 4c50310.
- 2026-10-16 18:15: merged main. Local server: SpawnCheck passes (all sneak checks); `tools/upgrade-check.sh origin/main` passes (publishes over main's world, no wipe). Animation's #100 is stacked on this branch, so this merges first.
- 2026-10-16 18:15 (integration-1 note): PM delegated merges (D-095); merged #107 and #97, then this session's permission check blocked merge work as "merge without review", so merges went back to the PM until Justin allows it.

- 2026-10-16 18:30: CI green on 9272510 (https://github.com/Indenex/LifeGame/actions/runs/38945043618), clean vs main 031141c; sha sent to the PM.

- 2026-10-16 18:42: merged to main (#121).

## Open questions / blockers
- (none)

## Next step
Done: merged to main as #121. Client side is anim-1 (#100).
