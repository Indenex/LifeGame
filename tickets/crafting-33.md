# crafting-33: Animal offers follow the hand
Lane: Crafting
Owner: Crafting thread
Status: done
Branch: crafting-hints2
PR: (see below)

## Goal
Since crafting-29 the server only lets the equipped tool strike, shear or butcher, and a bow shoots only from the hand. The Animals client (`client/scripts/animals/Herd.cs`) still offered those actions to anyone merely carrying a blade, spear or bow, so G or the strike key was offered and then refused ("Take it in your hand first"). The offers now follow the hand (`Equipped.Has`, `Equipped.Item`), and `Use.RegisterHint("animals.equip_blade")` says "Take your blade in hand: press its number key" at a shearable sheep or a carcass when a blade is carried but not in hand. Client only.

## Progress log
- 2026-10-17: change made, client builds.

## Next step
None: merged (#672). Later sections below are history.
