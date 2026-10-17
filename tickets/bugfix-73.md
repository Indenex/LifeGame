# bugfix-73: Guiding arrow sat at the far left edge, half cut off, name missing
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-rnwmoa-16
PR: (opening)

## Goal
In Render's still of ui-82 (ui-82-show-the-way-hud.png) the guiding arrow sat at the far left edge, about a tenth of the way down, half cut off, with only "40 m" showing instead of "Mara 40 m" at top centre. Cause: FriendWay.cs set `view.Position = (-Width / 2, 10 + SundialArc.Room)` every frame. A Control's position is in the screen's own coordinates, so with the real viewport size known that put the arrow at x = -100, half off the left edge (SundialArc does the same once in _Ready, while the viewport size is still 0, so the top-centre anchor carries it to the middle afterwards). It is not the angle, and the --screenshot-way preview used the same line, so real play had the same bug. The arrow, the way home (K) and the group-member arrow are all this one node, so all three were wrong. Now the position is set as offsets from the top-centre anchor (-100 to +100 across, 10 + the sundial's room down), so it sits at top centre at any window size, with the name beside the distance.

## Progress log
- 2026-10-17 20:20: fix written; no local Godot here, so CI is the compile check and Render's still decides.

## Open questions / blockers
- UX is making HUD elements draggable and touches this arrow's position; the PM will have them rebase onto this PR.

## Next step
Wait for CI, send the PM the green sha, then Render's confirming still.
