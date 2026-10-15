# ui-4: HUD fixes from the Bug fixes playtest
Lane: UX and UI
Owner: UX and UI thread (session_01NAW5B3eCSw3JtCiFrZeSNz)
Status: in_progress
Branch: claude/ux-ui-xlrh2s
PR: (none yet)

## Goal
Four HUD problems the Bug fixes lane found in playtest (screenshots /mnt/project-files/screenshots/bugfix-05-*.png), fixed in one small PR:
(a) the inventory row never runs into the needs rings or the hint corner, however many items you carry (it shrinks to fit);
(b) the G prompt sits below the player, clear of the fire (D-037);
(c) first-time hints never teach something the player already did (S/A/D count as walking, E and right-drag count as turning, lanes can call Hints.Done);
(d) while the journal, claim panel or Esc menu is open, the world HUD (hints, G prompt, inventory) fades away, so nothing shows through or collides with the panel. Claims and Survival read the same `Panels.Open` for the land chip and needs rings.
Also carries the KEYMAP note for Survival's "drink from your pot" (priority 1) and marks ui-3 done.

## Progress log
- 2026-10-15 23:40: started from main 6ad9b24 (ui-3 merged).

## Open questions / blockers
- (none)

## Next step
Build the four fixes, take a screenshot with 10 items and the journal open, open the PR.
