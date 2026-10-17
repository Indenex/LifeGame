# bugfix-36: CraftCheck and ClayCheck walks can't hang
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-36
PR: (to open)

## Goal
The `WalkTo` loops in CraftCheck and ClayCheck were unbounded: a bot that waits for the server to catch up while the server keeps refusing its steps (a solid in the way) spins for ever, and the CI job only ends at its timeout (the Crafting check on #446 ran 18+ min). Flaky-test rule: no unbounded waits, and failures say where everyone was.

## Change
- Both loops stop after 90 s with `stuck walking to (...): bot at, server at, m left, structures within 6 m`.
- When the server has not followed for a second (the bot is 1.5 m or more ahead of it), the bot takes the server's position as truth and carries on from there, instead of waiting for ever.
- Found while testing bugfix-34 (server-side trunk enforcement): bots walk straight lines and get stuck in thickets; that needs real route-finding (bugfix-34).

## Next step
PR, CI, green sha to the PM.
