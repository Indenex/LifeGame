# bugfix-55: Old-account lane's three fixes (were bugfix-37 to 39 there)
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_progress
Branch: claude/bugfix-rnwmoa-2
PR: (none yet; the old PR #491 was closed unmerged)

## Goal
Land the three fixes from /mnt/project-files/handoff/bugfix-old-lane/ (made against 50495cc), none of which is on main yet:
- ClaimPanel.Close() releases focus on the name and place boxes only when they are in the tree (members and visitors no longer log a Godot "not inside tree" error).
- With the Esc menu open, C, J, B and K no longer open panels behind it (PauseMenu `_ShortcutInput`).
- SocialTest's "A can dig in their own claim" picks the nearest cell not dug to `World.MaxDigDepth`, so it passes on CI's shared db.

## Progress log
- 2026-10-17 15:15: the code commit applies cleanly to main 5064a254 (ticket patch dropped; numbers 37 to 39 are taken here). No local toolchain, so CI is the build check.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR, then send the PM the green sha.
