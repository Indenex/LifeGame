# painted-11: No wash-out at dusk
Lane: Modeling
Owner: Modeling session (session_01JKX3FNjkrEkAqoTaFJiXAm)
Status: in_review
Branch: claude/painted-11-dusk (stacked on painted-12, #642)
PR: (opening)

## Goal
Front-lit painted props washed out to a pale peach-white at dusk. The cause was the painted light's low-sun lift, which raised every band on top of Atmosphere's bright golden light. The PM approved the fix after the faces.

## Progress log
- 2026-10-17 08:30: In `Painted.Light`, the low-sun lift now raises only the shade (`* (1 - mid)`), and the mid and lit bands step down at low sun (×0.8 and ×0.5). Sunlit faces keep their facets and colour at dusk; noon is unchanged.
- Render renders/painted-11-dusk.png. Top: main at 0.74. Middle: this branch at 0.74. Bottom: this branch at noon.

## Open questions / blockers
- (none)

## Next step
Merge after #642; send the green sha to the PM.
