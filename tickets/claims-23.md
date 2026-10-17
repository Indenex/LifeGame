# claims-23: The claim stash shows who made an item
Lane: Claims
Owner: Claims thread
Status: in_review
Branch: claude/claims-23-stash-maker
PR: (see PR list)

## Goal
The stash grid and the bag grid beside it in the claim panel pass the maker to `BagStack` (6th argument), so the "Made by" tooltip from ui-23 works there as it does in the bag. PM request.

## Progress log
- 2026-10-17: `ClaimPanel.cs`: both rows now pass `Marks.MakerOf(item, wear)` (the stash row has `Wear`, as does `MyInventory`). Client builds against fresh bindings. Not seen in Godot.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR and send the green sha to the PM.
