# village-1: Village kit props in our look
Lane: Modeling
Owner: Modeling thread (session_01Y3HWfLh47B52ViBgWpzWrz)
Status: in_progress
Branch: claude/modeling-i8ox5v
PR: (rides with naturekit-1)

## Goal
The props of Quaternius's Medieval Village MegaKit (CC0) that fit our tiers, drawn in our light through NatureKit's blended shader: crates, wooden fences, stone rubble and edging, vines, and the wagon for a later tier. Justin sees them in the showcase (`--view=villagekit`) first; other lanes (House building for fences and storage, Environment for scatter) pick them up from `NatureKit.Mesh("Prop_...")`. No baskets or barrels exist in the kit.

## Progress log
- 2026-10-17 17:00: imported 22 props into client/assets/quaternius/village/ (License_Village_Source.txt beside them) with tools/art/quaternius/import_village.py; textures colour 1024, normal 512, roughness/ORM 64 (unused). NatureKit loads `Prop_*` from there: timber as textured wood muted from the kit's orange, rock trim with its normal map and rim, vines in the kit's two greens. Shot: /mnt/project-files/screenshots/naturekit/village-props-noon.png.

## Open questions / blockers
- Which props Justin wants in the game, and where (via the PM).

## Next step
Offer House building `NatureKit.Mesh("Prop_WoodenFence_*")` and `Prop_Crate` for the fence and storage structures, once Justin has seen the shot.
