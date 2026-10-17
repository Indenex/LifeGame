# bugfix-61: Gaps between the log cabin's wall logs
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-rnwmoa-4
PR: (see PM)

## Goal
The cabin's walls alternated courses between the side walls and the front/back, so each wall had a log only every other course and you could see inside (Justin's F8 report). Every wall now gets a log every course, the side walls half a log higher so the ends cross at the corners, with radii a touch over half a course so stacked logs overlap. Low side sill logs close the gap under the side walls; a front log runs across the doorway only once its underside clears the door head (StructureModel.Cabin.cs).

## Progress log
- 2026-10-17 15:40: written; no local build (toolchain downloads blocked). CI is the build check; render before/after requested through the PM.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR, then send the PM the green sha.
