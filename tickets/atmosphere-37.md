# atmosphere-37: Clear the warm haze veil (dawn and noon)
Lane: Atmosphere
Owner: World atmosphere lane (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: done
Branch: claude/atmosphere-37-haze
PR: #690

## Goal
Modeling's world check on main (dawn 0.28, noon 0.5, dusk 0.74; renders on claude/painted-16-rock-value, w-0.28/0.5/0.74.png) and Render B's frost dawn wide shot both show a heavy warm haze washing out the view at dawn and noon, with distant trees nearly gone at dawn. A mild morning mist is fine; noon should be clear (PM, 09:51).

Cause:
- **Soft glow (#612)** moved the bloom to the wide glow levels with a honey glow map. The environment's GlowBloom (0.08 to 0.24, capped at 0.1) feeds every pixel into the glow, not just bright light, so the wide levels smeared the whole frame into a warm veil.
- **The cosy grade (#608)** lowered contrast by 0.1 and lifted the darks to plum, which made the veil read milkier.

Fix:
- With soft glow on, GlowBloom is held at 0.015, so only bright light (sun, fire, lamps) glows.
- The widest glow levels are lighter: 0.8 and 0.3, down from 1.0 and 0.6.
- The cosy contrast drop is halved, to 0.05.
- The curve's dark lift is eased.
- The combined fog cap goes from 0.012 to 0.010, so distant trees hold at dawn.

## Progress log
- 2026-10-17 09:58: built; the client compiles with 0 errors.
- 2026-10-17 09:58: CI green on 38ecdf73; merged as #690.

## Open questions / blockers
- Needs Render before/after (main vs this branch) at 0.28, 0.5 and 0.74.

## Next step
None (done). Render B's before/after at 0.28, 0.5 and 0.74 is queued; tune in a new ticket if needed.
