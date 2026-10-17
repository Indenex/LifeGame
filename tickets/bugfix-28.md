# bugfix-28: BuildTest "B: Out of reach ... FAIL Clay hut finished (stage 1)"
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-28
PR: (see GitHub)

## Goal
BuildTest stops failing in CI with a 180 s loop of `B: Out of reach` and `FAIL Clay hut finished (stage 1)` (seen on PRs #360, #337, #365, #383 and others since the Move speed budget, bugfix-24).

## Cause (from Integration's diagnostic in #374 and a CI log, run 38986223683)
`B: bot (-29.1, -8.0), server (-29.0, -7.9), 4.2 m from the Clay hut's middle; reach is 2.2 m past a 1.9 m radius (4.1 m)`. The bot and the server agree where B is: B simply stopped 0.1 m too far away. `Bot.Step` returned the distance left as `dist - 5 * dt` even on calls where it did not walk because the pace gate (waiting for the server to catch up, bugfix-24) held it back. `WalkStraight` stops when that figure is within `within`, so a gated bot (B carries 12 clay, 14 fiber and 11 sticks and walks at about half pace) ended up to 0.8 m short of its target on every gated call, and `WalkTo`'s detour (integration-1, #374) then sidestepped up to six times without getting closer, leaving B at a detour point. A, carrying little, is never gated, which is why only B fails.

## Fix (tool only; the server rule is untouched, as the PM asked)
- `Step` reports real progress (`moved`), so waiting for the server no longer counts as walking.
- `WalkStraight` caps its loop (900 steps, 90 s) so a bot held in place by a wall or the pace can't wait for ever.

## Progress log
- 2026-10-17: found from the CI log above. Local: main's BuildTest failed one of the runs; the fixed build passed (352 s). More runs recorded below.

## Open questions / blockers
- None.

## Next step
Wait for CI on the PR, then send the PM the green sha.
