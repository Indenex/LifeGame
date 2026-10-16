# bugfix-19: a drying pot's G line hides Drink, Fill and Pick berries
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-19
PR: (opening)

## Goal
While a freshly shaped pot or brick dries (4 to 8 minutes), G near it still does what it would do without it: Drink or Fill pots at the water's edge, Pick berries, pick up a fired pot. The "Drying, about N min left" line shows only when G has nothing real to do. Its floating moisture label steps aside only when the G prompt really shows that line, so drying info is never hidden while the prompt shows something else.

Cause: client/scripts/ceramics/CeramicsView.cs offered the drying line at priority 12 with a Target. Shaping sets the pot 1.2 m in front of you, so you face it, and a faced offer beats Drink and Fill (no Target) and outranks Forage and pick-up (priority 10). Clay is found mostly near water. Separately, `dryingOfferFrame` hid the label whenever the drying offer was built, even when another lane's offer won the prompt.

Fix: the wet-piece line moves to its own handler `ceramics.drying` at priority 0 with no Target, below every real action (Drink from your pot is 1). `Use.Shown` (new, in client/scripts/ui/Use.cs, set by UseKey) is the offer the prompt shows; the label hides only when that is one of the drying lines. Dry pick-up and the "G twice fires it wet" offer beside a fire are unchanged.

## Progress log
- 2026-10-16: found in the clay-path review (see bugfix-17). Fixed; the client builds.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR, record the run link here, then send the PM the green sha.
