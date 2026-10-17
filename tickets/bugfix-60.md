# bugfix-60: Flat glowing card in a lit home's doorway
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-rnwmoa-4
PR: (see PM)

## Goal
At night a home with a fire inside showed a flat orange additive quad in its doorway (AtmosphereHomes.cs doorGlows), which read as a lit panel instead of an opening (Justin's F8 report 2026-10-17-101208). The quad is gone; the doorway OmniLight still spills warm light onto the ground and the fire inside shows through the opening.

## Progress log
- 2026-10-17 15:40: written; no local build (toolchain downloads blocked). CI is the build check; render before/after requested through the PM.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR, then send the PM the green sha.
