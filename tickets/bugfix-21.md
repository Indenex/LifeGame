# bugfix-21: loose sticks, stones and fiber spawn under water
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-21
PR: (opening)

## Goal
Ground items seeded at world start and topped up by RespawnItems land only on dry ground. None sit in a pond or on a stream bed, where a player can't reach them and where they still counted toward the chunk's 12-item target, which kept nearby dry ground from being topped up.

Cause: server/spacetimedb/Reducers.cs SpawnGroundItem picked any random point in the chunk. Fix: up to 8 random points, keeping the first that Survival's IsDry accepts (base height more than 0.3 m above water, as forage bushes use). A chunk that is all water gets nothing that round. No schema change; existing items in Justin's world stay where they are.

Found in the PM-requested playtest on env-2 (claude/environment-env-2, 608c712), which has about 8% water (main has about 1.3%).

## Progress log
- 2026-10-16: on a fresh env-2 world, 45 of 768 ground items were under water. With this fix applied to env-2: 0 of 766. Server builds on main.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR, record the run link here, then send the PM the green sha.
