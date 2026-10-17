# painted-17: Warm the painted shade at golden hour
Lane: Modeling
Owner: Modeling lane (session_01JKX3FNjkrEkAqoTaFJiXAm)
Status: in_review
Branch: claude/painted-17-warm-shade
PR: (opening)

## Goal
Pale rocks and props read pink-lilac at golden hour (PM, Atmosphere; Render B's golden2-trio-072). Atmosphere's #767 made the low sun soft gold; what's left is the props' own violet shade_tint in Painted.Light. At low sun the shade tone now warms to an amber grey of the same depth (0.85 * low_sun toward luminance * (1.12, 1.0, 0.86)); by day it is the violet shade as before. Painted.Light is also spliced into painted foliage (PaintedTrees) and ClayLook, so they warm at dusk too. The second boulder forms (#760) were checked: painted white like the first forms, same WorldRock tint, no lilac base colour.

## Progress log
- 2026-10-17 13:10: shader change written (uncompiled-safe: shader string only); before renders of main at noon and 18.5 running.

- 2026-10-17 13:55: rendered the world at --time-of-day=0.72 (golden hour; the flag runs 0 to 1, so 18.5 wraps to noon) on main and on this branch: renders/painted-17-golden-before-after.png (main left) and painted-17-golden-rock.png. The big rock's shaded side goes from lilac to a warm taupe; noon is unchanged. Local build 0 errors.

## Open questions / blockers
- (none)

## Next step
Wait for CI and Atmosphere's review, then send the green sha to the PM.
