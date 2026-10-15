# bugfix-1: Stuck at spawn when saved far from the origin
Lane: Bug fixes
Owner: Bug fixes thread (session cse_01Eo43mKAGzgoCFVpXAGCUqW)
Status: in_progress
Branch: claude/bugfix-lyle6o
PR: #18

## Goal
A player whose saved position is 48 m or more from the origin (for example woken on the wilderness rim after dying, which is 58 m out) logs back in and appears where they left, with the world around them loaded, instead of standing at spawn forever. The client used to subscribe to chunks around (0,0) first and wait for its own `player_pos` row there, which never came. Now it subscribes to its own `player` and `player_pos` rows by identity first (kept for the session, so a far respawn also arrives), then to the area around the real position, and only shows the player once that area has loaded so nothing pops in. `tools/SpawnCheck` proves it in CI.

## Progress log
- 2026-10-15 22:40: reproduced locally (SpawnCheck: origin-only area misses a player at x=-80). Fix in `client/scripts/Main.cs` (SubscribeSelf, spawn after the first area applies); queries moved to `client/scripts/net/Subscriptions.cs` so the bot runs exactly the client's queries. SpawnCheck passes locally; client builds.

- 2026-10-15 22:50: PR #18 opened (draft). Checked in the real client (Godot 4.6.2, llvmpipe): a player saved at x=-56 now wakes there with bushes and stones loaded (screenshots/bugfix-01-after-far-spawn.png); main's client leaves them at the origin unable to walk (screenshots/bugfix-01-before-stuck-at-spawn.png).

## Open questions / blockers
- (none)

## Next step
Wait for CI on PR #18; when green, mark it ready and send the PM the head sha.
