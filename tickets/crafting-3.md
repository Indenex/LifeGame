# crafting-3: Bow drill
Lane: Crafting
Owner: Crafting thread
Status: in_review
Branch: crafting-bow-drill
PR: #30

## Goal
The bow drill from the design doc's tier-2 chain. "String a bow drill" (lash.bow_drill) turns 2 sticks and 1 cordage into a Bow drill, a kept tool (tag FireBow). With it in the inventory, "Bow-drill fire" (fire.bow_drill) lights a fire from the same 2 sticks and tinder as the hand drill, but the drill minigame needs half the strokes (7 instead of 14) and only fails below 0.2 (hand drill: 0.35). Justin sees the new item lying on the ground (bent bow, cord, spindle) and the two new recipes in the tier-2 craft list. The numbers are the lane's defaults, sent to the PM.

## Progress log
- 2026-10-15: plan sent to PM. Item 14 BowDrill, tag FireBow 256, both processes, BowDrillMesh, DrillGame stroke count, CraftCheck step (bow drill lights at score 0.25 and is kept). CraftCheck passes locally; client builds.
- 2026-10-15 23:20: PM approved the default numbers. First CI run failed in SocialTest because the merge ref used an older main. Merged main (1dd55fe); CraftCheck and SocialTest pass locally. Screenshots 10 and 11 sent to the PM.
- 2026-10-15 23:34: CI green on 1dd55fe: https://github.com/Indenex/LifeGame/actions/runs/38823482746

## Open questions / blockers
- (none; PM approved the defaults)

## Next step
Wait for the PM to merge PR #30, then pick crafting-4.
