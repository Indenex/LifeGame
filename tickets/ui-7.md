# ui-7: Minigame box and toasts get their own lanes (playtest2)
Lane: UX and UI
Owner: UX and UI thread (session cse_01NAW5B3eCSw3JtCiFrZeSNz)
Status: in_progress
Branch: claude/ux-ui-xlrh2s (local commit; waits on #46, see Open questions)
PR: (none yet)

## Goal
From playtest2: the hand-drill minigame box sat dead centre over the player and the fire, and toasts ("New in your journal") sat on top of the hotbar item label. Done means the box sits low on the screen and never covers the player, toasts have their own lane above the hotbar, with before/after screenshots.

## Progress log
- 2026-10-16 04:30: Hud.cs defines the bottom lanes (NoteLane 150 for toasts, WorkLane 156 for work boxes) and `Hud.PlaceLow(box)`, which anchors a box centre-bottom and shrinks it (down to 70%) so its top stays within the lower third. CraftingUi.cs (Crafting's file) calls `Hud.PlaceLow(card)` instead of centring, and `--screenshot-minigame` opens the drill box without a server. Build 0 errors. Screenshots: /mnt/project-files/screenshots/ui-12-before-drill-box.png, ui-12-after-720p.png, ui-12-after-900p.png.

## Open questions / blockers
- #46 is still open on claude/ux-ui-xlrh2s, the only branch this lane may push. Asked the PM whether to push this onto #46 or wait for #46 to merge and open a small PR then.

## Next step
Push the ui-7 commit per the PM's answer, open the PR (or update #46), send the green sha to the PM.
