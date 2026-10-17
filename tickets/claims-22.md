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
- BuildTest (fence part): a claim member bars and unbars the claim's gate. Passes locally on a fresh database.
- Not done: a roster signer who is no member failing to bar a gate, and the Gatekeeper succeeding. Those need a finished gate plus a second claim and a town in one run (B would have to lay out and sign its own claim). Elder alone breaking a tie needs a vacant Judge seat. Both are heavy to set up; asked House building how they would do the gate setup.

## Open questions / blockers
- (none)

## Next step
Wait for #555 and #562 to merge, then open the PR for this branch.
