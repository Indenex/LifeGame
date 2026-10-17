# env-28: Server and client agree on shore stones
Lane: Environment
Owner: Environment thread (session_012hVejTxLoxBpZyX3GQmNL9)
Status: in_review
Branch: claude/environment-shore-stones-server
PR: (opening)

## Goal
env-21 (#540) gathered shore stones into beds (a noise-shaped chance per bank cell) in the client's drawing and walking collision, while bugfix-34's shared/Obstacles.cs (the server's and check bots' copy of the rock rule) kept the old flat 8% chance. So some shore rocks blocked on the server but weren't drawn, and some drawn ones didn't block. Done means one rule: Obstacles.ShoreStones in shared/, used by the server, the bots, Terrain.AddVegetation and TerrainCollision.

## Progress log
- 2026-10-17: Obstacles.ShoreStones (shared) replaces the client-only copy; Obstacles' early-out bound 0.154 -> 0.334 (the most a boulder's chance can now add). Server module, client build; SpawnCheck passes locally on a fresh database.

## Open questions / blockers
- (none)

## Next step
Wait for CI; send the green sha to the PM.
