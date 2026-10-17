# painted-19: Painted look on the camp stragglers
Lane: Modeling
Owner: Modeling lane (session_01Y3HWfLh47B52ViBgWpzWrz, from 2026-10-17 15:00 on this account)
Status: in_progress
Branch: claude/modeling-i8ox5v
PR: (not yet)

## Goal
D-292 rollout sweep: the last world props still drawn on plain Look or StandardMaterial3D, now on the painted shader like the rest:
- Hide racks (Animals' HideRacks.cs): the hide is a painted Hide twin per rack (it still darkens from wet pink to tan; wet hide catches a few more lit dabs instead of the old PBR sheen), the old A-frame is painted structure Wood.
- Roasting meat on its stick (Crafting's FireView.RoastView): painted Food per roast (still browns, then chars), the stick painted Wood.
- The cooking pot's three stones and what floats in the pot (Modeling's CookingMeshes): structure Stone and item Food.
Small hooks only in other lanes' files (material swaps; RoastView and Rack made internal for the Showcase). Showcase view `camp` shows all three. Renders: screenshots/painted-19-camp-before.png and painted-19-camp-after.png.

## Progress log
- 2026-10-17 15:20: caught up after the account switch (model-33 #818 and painted-17 #802 are on main; painted-18 #807 waits on Justin's rock-shade pick). Found the stragglers by grepping mesh code that never reaches Painted. Local build 0 errors.

## Open questions / blockers
- (none)

## Next step
Render `--view=camp` on this branch and with the three material files reverted (before), push, open the PR, send renders and the green sha to the PM.
