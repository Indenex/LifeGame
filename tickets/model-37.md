# model-37: Mushroom patches readable in the grass
Lane: Modeling
Owner: Modeling thread
Status: in_progress
Branch: claude/modeling-i8ox5v
PR: (opening)

## Goal
Survival's mushroom patches (survival-35) sat hidden in tall grass; only the "Gather
mushrooms" prompt showed. Now each patch is 3 to 5 bigger caps (about 1.6x life) in warm
russet and amber, and the grass lies flat in a ring round it (0.4 m, easing back by 0.75 m).
Root patches get the same ring. Berry bushes stand above the grass already.

## Progress log
- 2026-10-17 21:10: GrassClearings (client/scripts/models/GrassClearings.cs) tracks patch
  nodes and feeds the 16 nearest the player to the grass shader as two mat4 globals; hooks:
  Look.cs (globals plus four lines in the grass vertex shader), one Track line each in
  Survival's MushroomPatch.cs and RootPatch.cs. Mushrooms.cs: 3-5 caps, sizes up, warmer caps.

## Open questions / blockers
- (none)

## Next step
Render a half-size in-game shot of a patch, open the PR, send the PM the green sha.
