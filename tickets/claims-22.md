# claims-22: Tie-break order between Judge and Elder
Lane: Claims
Owner: Claims thread
Status: in_review
Branch: claude/claims-22-power-tests (stacked on claims-21, PR #562)
PR: (none yet; open after #562 merges)

## Goal
Both the Judge and the Elder hold `TownPower.BreakTies`. When both vote in a tied election, whose vote counts must not depend on row order.

## Progress log
- 2026-10-17: `HoldersWith` now orders by office id, so the Judge's vote is looked at first and the Elder's only when the Judge has not voted. Server module builds.
- Not done: tests for the Gatekeeper's gate barring and the Elder alone breaking a tie. Both need a finished gate (resources and crafting steps) or a vacant Judge seat, which SocialTest cannot set up cheaply. They belong in the building harness (BuildTest) and need Integration's help; proposed to the PM.

## Open questions / blockers
- (none)

## Next step
Wait for #555 and #562 to merge, then open the PR for this branch.
