# ceramics-14: Slip decoration (banded rim, free colour)
Lane: Clay and ceramics
Owner: Clay and ceramics lane (Sonnet session_01Sc3N1RCsurSszfX5YwGzMU)
Status: in_progress
Branch: claude/clay-ceramics-18-kfr4kr
PR: (none yet)

## Goal
Pots can be decorated with coloured slip at the Potter's bench: an optional step after shaping, using slip made from a little clean clay plus a pigment. Look only; no change to pot quality; nothing to tend (D-148). Justin sees a decorated pot on the ground, in the hand and in the inventory.

## Progress log
- 2026-10-17 07:20: Justin picked (via PM): look = banded rim, one or two bands; colours = a free colour picker like the maker's mark, reusing the mark panel's colour square and range (D-272). Built on this branch: shared/Slip.cs (slip rides in the pot's Wear number above the maker's bits: bit 17 slipped, bit 18 two bands, then hue 5 bits, saturation 3, value 3 on a grid inside the mark colour range; stacks with different slip never merge, and it follows the pot through drying, firing, pick-up, filling and drinking like the maker does); server `SlipPiece(pieceId, color, bands)` in server/spacetimedb/Slip.cs (only the maker, damp clay at or above leather-hard, at their Potter's bench, pot or jar only, 1 or 2 bands, costs 1 clean clay, server clamps and snaps the colour); shader bands in ClayLook (`slip_*` uniforms, wobbling brush edge, streaks; also fixed `local_scale` so baked ground and bag meshes put the mark and bands at the right height); ground/bag meshes via ClayItemLook.Decoration; SlipPicker window (client/scripts/ceramics/SlipPicker.cs, UX's HueStrip and ShadeSquare); Use prompt "Decorate with slip" (priority 3, only beside the bench with clean clay and a damp pot of yours); tooltip line ("a band of slip"); demos `--ceramics-demo=slip` and `slip-panel`; ClayCheck --pot slips both coil pots and checks it survives fire, pick-up and filling.
- 2026-10-17 06:50: PM asked for the written looks again for Justin (A and B wait on crafting-25). Sent: Q1 look (A banded rim recommended, B dotted ring, C finger-swipe waves, D half-dipped) and Q2 colours (A white clay + ochre + charcoal recommended, B white clay + charcoal, C free picker). Nothing built.
- 2026-10-17 00:20: PARKED. Justin picked clay pipes and drain tiles for water for ceramics-13 instead. The four looks are kept below for later.
- 2026-10-17 00:15: PM offered no other ticket; I proposed slip decoration (recommended), potter's wheel, clay pipes. Sent the PM four written looks (banded rim, zigzag or dotted ring, finger-swipe waves, marker's mark only) because the decoration look is a look-and-feel choice (D-154): build only a rough prototype until Justin picks.

## Open questions / blockers
- Cooking in a slipped pot loses the slip, same gap as the maker's mark (needs crafting-25's Maker column work); structures that a pot becomes (set-down jars) do not show slip yet either (ceramics A).
- Cost: 1 clean clay per decoration (a number; PM can change it). Re-slipping a damp pot replaces the old slip and costs again.
- Waiting on Justin's picks (Q1 look, Q2 colours; sent to the PM 2026-10-17 06:50).
- Pigment source: ochre or charcoal from the world. Ask Environment where ochre appears (the PM was asked whether to ask now or hold until the pick).

## Next step
Open the PR when ClayCheck --pot passes, send the PM the green sha, send Render B a stills request (`--ceramics-demo=slip`, `--ceramics-demo=slip-panel`).
