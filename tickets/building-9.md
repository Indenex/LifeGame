# building-9: Raised hearth
Lane: House building
Owner: House building thread (session_01RrKHUxWRH973Ndnxsad9nv)
Status: in_progress
Branch: local building-9, stacked on claude/house-building-8ueqz2 (#153); moves to that branch once #153 merges
PR: (not yet)

## Goal
D-087, second of three (fence, hearth, cabin). Working on option (a), sent to the PM 2026-10-16. A raised hearth is built inside a finished clay hut or mud-brick house: a ring of stones (4 stone), then a clay surround (3 daub). A fire lit on it burns 1.5× as long per stick and isn't put out by rain, and it makes a hut fully warm, as a fire already does in the house. Smoke rises through the roof. Justin sees a stone-and-clay hearth by the wall with the fire on it.

## Progress log
- 2026-10-16 18:45: ticket opened. Design question sent to the PM: (a) recommended; (b) looks only; (c) also cooks faster.

## Open questions / blockers
- PM/Justin: hearth design (a/b/c). Working on (a).
- Crafting: needs a hook to read the hearth when a fire is lit and fuelled (to ask once (a) is confirmed).

## Next step
Read how Crafting lights and fuels fires (Crafting.cs FireSpot, fuel), then design the HearthAt hook and a StructureKind Hearth placeable only inside finished huts and houses.
