# bugfix-62: TownPanel.Close releases focus outside the tree
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-rnwmoa-4
PR: (see PM)

## Goal
Pressing F8 with the town panel set up logged Godot "!is_inside_tree()" errors from TownPanel.Close() (lines 83-84, via Panels.Opening from BugReportPanel.Open). The name and say boxes now release focus only when they are in the tree, as ClaimPanel does (bugfix-55).

## Progress log
- 2026-10-17 15:40: written; no local build (toolchain downloads blocked). CI is the build check; render before/after requested through the PM.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR, then send the PM the green sha.
