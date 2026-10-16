# animals-10: Sheep stop at fences and gates
Lane: Animals
Owner: Animals thread (session session_01SAcVBUUfPFFk1rdXuiQZRU)
Status: done
Branch: claude/animals-fence-5gkntn
PR: #115

## Goal
Sheep can't walk through a finished stick fence, a shut gate, or a hut/house wall (doorways stay open), so players can pen a flock (D-087, building-8). Done when a sheep walking at a fence turns away in a build.

## How it works
- `server/spacetimedb/AnimalFires.cs`: `AnimalStepBlocked` asks a new hook `StructureStepHook(ctx, fromX, fromZ, toX, toZ, ref blocked)` first. House building implements it in its own files (building-8). A partial hook (rather than a direct call) lets either PR merge first: unimplemented, it compiles away. Running sheep swerve and walking sheep re-pick a spot, the same as at the shore; Hunting's startle leap uses the same check.
- No schema change.

## Progress log
- 2026-10-16 21:25: CI green on 8441538 (run https://github.com/Indenex/LifeGame/actions/runs/38947192848); merged as #115. House building told building-8 can follow.
- 2026-10-16 20:40: live test with building-8 (claude/house-building-8ueqz2-fence 70f5850) merged locally: TEMP reducer laid a finished 6 m square fence pen round a flock; a bot charged the flock from changing sides for 90 s. Without the pen, sheep went 6.86 m from the centre; with it, the furthest was 2.70 m (fence line 3 m minus 0.3 m body). No server errors. Order: #115 must merge before building-8.
- 2026-10-16 20:05: hook added on a branch from main cc75409; proposed to House building (session_01RrKHUxWRH973Ndnxsad9nv) instead of a direct call.

## Open questions / blockers
- (none)

## Next step
Done: merged in #115.
