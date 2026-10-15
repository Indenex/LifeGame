# crafting-3: Bow drill
Lane: Crafting
Owner: Crafting thread
Status: in_progress
Branch: crafting-bow-drill
PR: (not yet)

## Goal
The bow drill from the design doc's tier-2 chain. "String a bow drill" (lash.bow_drill) turns 2 sticks and 1 cordage into a Bow drill, a kept tool (tag FireBow). With it in the inventory, "Bow-drill fire" (fire.bow_drill) lights a fire from the same 2 sticks and tinder as the hand drill, but the drill minigame needs half the strokes (7 instead of 14) and only fails below 0.2 (hand drill: 0.35). Justin sees the new item lying on the ground (bent bow, cord, spindle) and the two new recipes in the tier-2 craft list. The numbers are the lane's defaults, sent to the PM.

## Progress log
- 2026-10-15: plan sent to PM. Item 14 BowDrill, tag FireBow 256, both processes, BowDrillMesh, DrillGame stroke count, CraftCheck step (bow drill lights at score 0.25 and is kept). CraftCheck passes locally; client builds.

## Open questions / blockers
- PM/Justin: are 7 strokes and fail-below 0.2 right? (default: yes)

## Next step
Take the screenshot (fire preview with the bow drill beside it, tier-2 craft panel), push, open the PR.
