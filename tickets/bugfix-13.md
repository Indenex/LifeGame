# bugfix-13: facing a fire hides "Fire the pot"
Lane: Bug fixes
Owner: Bug fixes thread
Status: in_review
Branch: claude/bugfix-lyle6o
PR: #68

## Goal
With a dry pot beside a burning fire, G says "Fire the pot" whichever way you face. With an unfired pot in your bag at a fire, G says "Set pots in the fire". Kiln and pick-up prompts follow what you face, as KEYMAP.md asks.

Cause: none of the Ceramics Use offers passed a `Target`, so under the facing rule (KEYMAP.md: a Target you face beats one with none) Crafting's "Feed the fire" always won while you looked at the fire. You had to turn your back on the pot to fire it. Fix in client/scripts/ceramics/CeramicsView.cs: every Ceramics offer passes its pot, fire or kiln as Target, and firing a dry pot by a fire gets its own handler at priority 22 (the same as "Set pots in the fire"). Without the priority, a pot sitting on the fire (a fire lit where you just shaped a pot lands on it) could never be fired. Picking up a dry pot or brick stays at 12. KEYMAP.md's priority list is updated.

## Progress log
- 2026-10-16: reproduced on a fresh db: fire lit on the pinch pot's spot; facing the fire showed "Feed the fire", facing away "Fire the pot" (bugfix-13-before-facing-fire-hides-fire-the-pot.png). After the fix all four facings show "Fire the pot" (bugfix-13-after-fire-the-pot-every-facing.png) and G fired it.

## Open questions / blockers
- (none)

## Next step
Wait for CI on #68, then send the PM the green sha.
