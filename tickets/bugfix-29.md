# bugfix-29: Stale "Online" players
Lane: Bug fixes
Owner: Bug fixes thread
Status: done
Branch: claude/bugfix-29
PR: #406 (merged)

## Goal
A player whose client crashed, was killed or lost power never stays "Online" for ever. `ClientDisconnected` already clears it on a clean drop; for the rest the client sends `Heartbeat` every 20 s and a sweep in the 30 s `RespawnItems` tick marks anyone silent for 75 s offline (same cleanup as a disconnect). A heartbeat from a swept-but-still-connected client brings them back online.

## Progress log
- 2026-10-17: server `Presence.cs` (private `PlayerSeen`, `Heartbeat`, `SweepPresence`), hooks in connect and `RespawnItems`, client heartbeat in Main.cs. Builds. Local test: bots that never heartbeat (LoadTest) are swept ~75-105 s after joining.

## Open questions / blockers
- (none)

## Next step
Done: merged in #406. Nothing left on this ticket.

## Follow-up (bugfix-33, 2026-10-17)
The sweep marked check bots offline (they never heartbeat and sit idle for long stretches), so SocialTest failed intermittently with "Nobody is there" / no "struck down" line. Now only connections that have sent a Heartbeat are ever swept (the connect hooks clear the seen row, Heartbeat creates it), and disconnect clears it. The real client heartbeats at once on joining.
