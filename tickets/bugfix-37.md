# bugfix-37: Claim panel logs an error when a member closes it
Lane: Bug fixes
Owner: Bug fixes thread (cse_01Eo43mKAGzgoCFVpXAGCUqW)
Status: in_review
Branch: claude/bugfix-lyle6o
PR: #491

## Goal
`ClaimPanel.Close` releases focus on the "Add someone by name" and "Name this place" boxes, but both are only put in the panel for the claim's owner. For a member or visitor the boxes are outside the tree, and Godot logs an error on every close. Done: Close only releases a box that is in the panel, so closing it as a non-owner is silent.

## Progress log
- 2026-10-16 06:30: found in the two-player claim playtest (old ticket number bugfix-18 on this branch, before another session used 17-36).
- 2026-10-17 04:00: ported to main 50495cc; main added the place box, guarded both. Client builds. Not re-run in a two-player client on current main (same one-line guard as tested before).

## Open questions / blockers
- (none)

## Next step
Wait for the PR's CI, then send the PM the green sha.
