# survival-26: The server checks a reported run happened (D-182)
Lane: Survival
Owner: Survival thread
Status: in_review
Branch: claude/survival-run-check
PR: (see STATUS)

## Goal
`Ran` (Endurance training, D-123) trusted the client: a player could stand still and call it every 18 seconds to train Endurance. Skill matters more than gear (D-197), so that is the kind of gap the hardening pass closes. Now the server refuses a report unless the player moved within the last `Stamina.RunReportMoveWindow` (3 s) and is at least `Stamina.RunReportMinMeters` (25 m) from where they last reported (a real 20 s run covers about 110 m). Nothing changes for players who run.

## Progress log
- 2026-10-17 08:20: done. `RunReport` gains X and Z (`[Default(0f)]`, so no wipe); upgrade check against origin/main passes. SurvivalCheck gained four checks (standing still trains nothing, real movement trains, a second report from nearly the same place trains nothing, moving on trains again) and its bot name is letters only (same one-line fix as PR #589).

## Open questions / blockers
- Pacing a straight line 25 m back and forth still trains it; it takes real walking, so it costs about what running does. Not worth more code.

## Next step
Wait for CI, then send the PM the green sha.
