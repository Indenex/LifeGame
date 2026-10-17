# model-34: Woodland mushrooms
Lane: Modeling
Owner: Modeling lane (session_01Y3HWfLh47B52ViBgWpzWrz)
Status: in_review
Branch: claude/modeling-i8ox5v (rides PR #836 with painted-19)
PR: #836

## Goal
Survival's survival-35 (#837) mushroom patches, in the painted look (`client/scripts/models/Mushrooms.cs`):
- `Mushrooms.Patch(variant)`: a ripe cluster of 4 to 7 penny-bun mushrooms (stout pale stems, broad warm-brown domed caps with a paler rim and cream gills), the biggest near the middle, the outer ones leaning out, with a moss cushion and two fallen leaves. No ground disc of its own, so it sits on any terrain.
- `Mushrooms.Picked(variant)`: the same spot picked: the cut stubs of the stems taken and two tiny buttons just breaking the soil.
- 4 variants (D-166), caps from chestnut to pale hazel. About 1.3x life (biggest cap ~11 cm across) so they read from a few metres. Origin is the ground under the middle. Material: `Mushrooms.Material` (painted, vertex colours).
- `ItemModels.MushroomItem`: an `Item?` hook (like SnareItem). Survival sets it to `Item.Mushrooms` and gets three mushrooms lying together for the ground, hand and inventory icon (`Mushrooms.Pile`).
Showcase view `mushrooms` (and `--near`). Renders: /mnt/project-files/screenshots/model-34-mushrooms.png and model-34-mushrooms-near.png.

## Progress log
- 2026-10-17 15:40: built and rendered; local build 0 errors. On this branch with painted-19 because the harness gives this lane one branch.

## Open questions / blockers
- Survival wires it in their files on #837: in MushroomPatch, one MeshInstance3D with `Mushrooms.Patch(v)` and one with `Mushrooms.Picked(v)` (v from the patch id), Ripe toggles which shows; set `ItemModels.MushroomItem = Item.Mushrooms` once at start and drop the stand-in cap in SurvivalItemLook.

## Next step
Wait for CI on #836, send the green sha to the PM; answer Survival's feedback on scale or colour.
