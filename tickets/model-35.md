# model-35: Wild roots (for Survival's survival-36)
Lane: Modeling
Owner: Modeling thread (session_01Y3HWfLh47B52ViBgWpzWrz)
Status: in_review
Branch: claude/modeling-i8ox5v
PR: #853 (rides with naturekit-1)

## Goal
A painted wild-root patch for Survival's root patches (#856): wild carrot with a rosette of feathery leaves and one to three stalks under flat white umbels, each with a dark floret at its heart; the dug spot (a hole, its earth heaped beside, a broken stalk and torn leaves); and the dug roots as an item (ground, hand, icon). Same blended-plant look as the other painted plants.

## Progress log
- 2026-10-17 18:20: client/scripts/models/WildRoots.cs: Variants=4, Patch(v), Dug(v), Pile, PileInto(MeshKit), One(k, crown, dir, size); WildRoots.Material sways in the wind. ItemModels.WildRootItem (Survival sets it to Item.WildRoots). Showcase view `wildroots` (`--near`). Shots: /mnt/project-files/screenshots/naturekit/wild-roots{,-near}.png.

## Open questions / blockers
- (none)

## Next step
Survival wires RootPatch to WildRoots.Patch/Dug(id % Variants) with WildRoots.Material, and sets ItemModels.WildRootItem = Item.WildRoots, in their own files on #856.
