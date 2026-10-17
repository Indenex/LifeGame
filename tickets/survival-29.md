# survival-29: Waking after a death is checked
Lane: Survival
Owner: Survival thread
Status: in_review
Branch: claude/survival-die-check
PR: (see STATUS)

## Goal
The one gap left in the waking-place work (survival-23): nothing checked that you really wake on your chosen mat after dying, or that a rolled-up mat is forgotten. `AdminDie` (admin only, like `AdminGive`) ends an admin's own character's life so tests can reach it. BuildTest's clay-hut part ends with: A is made an admin through the CLI as the database owner, dies and wakes on the mat (0.00 m from the chosen point, `WakeKind` at-rest-spot), rolls the mat up, dies again and wakes at the default place (`WakeKind` bed-gone, the waking place forgotten). Last in the run, since dying throws A out to the edge of the wilderness.

## Progress log
- 2026-10-17 11:20: done; clay-hut part passes locally (221 s).

## Open questions / blockers
- None.

## Next step
Wait for CI, then send the green sha to the PM.
