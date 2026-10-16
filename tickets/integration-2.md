# integration-2: Server-side sneak flag for other players' avatars
Lane: Integration
Owner: Integration session session_01Rf5965nk4Lvd3f2QDteZSq
Status: in_progress
Branch: claude/integration-sneak
PR: (once opened)

## Goal
Other players see you crouch while you sneak (Animation lane, anim-1, PR #100; PM decision "sneak flag through Integration"). Server side only: a public `player_sneaking` table (one row per sneaking player, `PlayerId` primary key), a `SetSneaking(bool)` reducer the client calls only when Shift changes, the row cleared on disconnect, and `SELECT * FROM player_sneaking` in `Subscriptions.Area`. No change to `Move` or `player_pos`; new table only, so it publishes over Justin's world without a wipe. The Animation lane writes the client side in `AvatarWork.cs`.

## Progress log
- 2026-10-16 17:50: `server/spacetimedb/Stance.cs` (table, reducer, `StanceDisconnected`), one hook line in `Reducers.cs` `PlayerDisconnected`, subscription in `client/scripts/net/Subscriptions.cs`, SpawnCheck checks (sneak shows, idempotent, cleared by logging off, set and cleared again). Server, bindings, client and all tools build. Pushed 4c50310.

## Open questions / blockers
- (none)

## Next step
Run SpawnCheck and `tools/upgrade-check.sh origin/main` against a local server, then open the PR and tell the Animation lane (session_019roNRWQBPw4ozNLDhzseig).
