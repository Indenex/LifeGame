# bugfix-7: Make a client/server mismatch obvious and graceful
Lane: Bug fixes
Owner: Bug fixes thread (session cse_01Eo43mKAGzgoCFVpXAGCUqW)
Status: in_progress
Branch: claude/bugfix-lyle6o (built on local branch bugfix-7-local; pushed once #55 merges)
PR: (not opened yet)

## Goal
Justin ran a new client against an older server module: he saw a raw "Subscription error: no such table: claim", chat said "no such reducer", and WASD did nothing. Done means a mismatched server shows one calm line, "Your server is older than this game. Publish the new server, then restart.", and the player can still walk.

## Progress log
- 2026-10-16 02:00: cause of dead WASD: Main spawned the player only when the area subscription applied; one missing table fails the whole subscription, so `spawned` stayed false and Walk() never ran. Fix: the area subscription's OnError spawns too.
- 2026-10-16 02:00: server stamps its git commit into a public `server_info` table (server/spacetimedb/ServerInfo.cs, written on Init and on every connect so no-wipe upgrades get it). `shared/BuildStamp.targets` (imported by the client and server csproj) writes BuildStamp.Commit at build time, "dev" outside git. Client ServerCheck: "no such table"/"no such reducer" in a subscription or reducer error shows the calm line once (Hud.Warn, stays on screen) and logs the raw text; other errors show as before. A different build sha with matching tables is only logged, since CI clients and local servers rarely share a commit.
- 2026-10-16 02:10: tested: this client against the 04a9b12 module shows the line and walks (screenshots/bugfix-07-older-server-line.png, bugfix-07-still-walks.png); against its own module, no warning and server_info reads af355d3.

## Open questions / blockers
- None.

## Next step
After #55 merges: reset claude/bugfix-lyle6o to origin/main, cherry-pick bugfix-7-local's commits, gen bindings, build, push, open the PR.
