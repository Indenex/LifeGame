# bugfix-14: "Lay out a updraft kiln"
Lane: Bug fixes
Owner: Bug fixes thread
Status: done
Branch: claude/bugfix-lyle6o
PR: #68

## Goal
The build-mode prompt reads "Lay out an updraft kiln", not "Lay out a updraft kiln". Every other structure name keeps "a".

Fix in client/scripts/building/BuildingSystem.cs (UpdatePrompt): pick "a" or "an" from the name's first letter.

## Progress log
- 2026-10-16: seen in the clay-path playtest (bugfix-14-before-a-updraft-kiln.png); fixed locally; prompt now reads "Lay out an updraft kiln" (bugfix-14-after-an-updraft-kiln.png).

## Open questions / blockers
- (none)

## Next step
None; merged.
