# bugfix-15: claim members' door prompt says "Barred"
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-lyle6o
PR: (opening)

## Goal
A member of a claim standing at a barred door in that claim sees "Unbar and open    Shift unbar", and at an unbarred one "Open the door    Shift bar", as whoever hung the door does. The server already lets claim members bar and unbar (BuildingStorage.cs CanLock), so only the prompt was wrong. Outsiders still see "Barred".

Cause: client/scripts/building/BuildingSystem.Use.cs only counted the door's hanger as able to bar. Fix: BuildingSystem gets a `MayBar(x, z)` hook, Main wires it to the new `SocialLayer.HasRightsAt` (you own or belong to the claim there), and the prompt treats either as able to bar.

## Progress log
- 2026-10-16: found in the two-client playtest. Owner hung and barred a door; the member saw "Barred", and plain G opened and unbarred it anyway (bugfix-15-before-member-sees-barred.png). Fixed locally; the member now sees the Shift unbar hint and Shift+G unbarred it (bugfix-15-after-member-can-unbar.png).

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR, then send the PM the green sha.
