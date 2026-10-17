# ceramics-14: Slip decoration (parked)
Lane: Clay and ceramics
Owner: Clay and ceramics lane (Sonnet session_01Sc3N1RCsurSszfX5YwGzMU)
Status: in_progress
Branch: claude/clay-ceramics-14-kfr4kr (rough prototype only)
PR: (none yet)

## Goal
Pots can be decorated with coloured slip at the Potter's bench: an optional step after shaping, using slip made from a little clean clay plus a pigment. Look only; no change to pot quality; nothing to tend (D-148). Justin sees a decorated pot on the ground, in the hand and in the inventory.

## Progress log
- 2026-10-17 00:20: PARKED. Justin picked clay pipes and drain tiles for water for ceramics-13 instead. The four looks are kept below for later.
- 2026-10-17 00:15: PM offered no other ticket; I proposed slip decoration (recommended), potter's wheel, clay pipes. Sent the PM four written looks (banded rim, zigzag or dotted ring, finger-swipe waves, marker's mark only) because the decoration look is a look-and-feel choice (D-154): build only a rough prototype until Justin picks.

- 2026-10-17 02:20: PM suggested picking slip up again (D-145). Built a rough, client-only prototype of the recommended banded rim: two shader parameters in ClayLook (a slip colour and a height band, with a hand-wobbled lower edge), `ClayLook.Slipped(...)`, three earth tones (red ochre, pale kaolin, charcoal), and `--ceramics-demo=slip` (kiln-fired coil and pinch pots with each slip beside plain ones). No schema, items or server change. Asked the PM what 'the references' are.

## Open questions / blockers
- Waiting on Justin's pick of the look (sent to the PM 2026-10-17 00:15, recommended: banded rim).
- Pigment source: ochre or charcoal from the world. Ask Environment where ochre appears (the PM was asked whether to ask now or hold until the pick).

## Next step
Send the sha to the Render lane (session_015S96hNrEu92FrCoNZqZZjs) for --ceramics-demo=slip shots; build nothing further until Justin picks a look. If he picks banded rim: add a Slip byte to ClayPiece, pigments and the bench step. Older text follows.
When the PM relays Justin's pick: build that look as a rough prototype first (ClayLook / PotMesh slip band), then the bench step and the pigment items, then a PR. Until then, re-run `ClayCheck --kiln --jar` once House building's #362 (kiln and bench claim-only) is on main.
