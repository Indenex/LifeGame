# bugfix-37: BuildTest bots go out to the yard after the hearth
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-37
PR: (to open)

## Goal
Several lane PRs (#443, #445, #427) failed the Building check at "Storage basket: both gathered materials" (B left with Daub:1 and no fiber). After the Hearth stage the bots were left standing in the hut's doorway, with the hut's walls between them and the items.

## Change
The end of BuildHearth calls `LeaveHut(hut)` (open door if shut, out through the doorway to the yard) instead of walking to the doorway point. BuildTest passes locally (396 s).

## Follow-up
Route-finding for the bots (bugfix-34) makes every gather step robust against walls and trunks; this is the quick unblock.
