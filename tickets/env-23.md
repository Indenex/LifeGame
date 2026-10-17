# env-23: The falling tree is sawn off, no roots
Lane: Environment
Owner: Environment thread (session_012hVejTxLoxBpZyX3GQmNL9)
Status: in_review
Branch: claude/environment-cut-trunk
PR: (opening)

## Goal
The felled tree's log no longer shows the trunk's root flare at its butt (PM option 1 after env-22). The falling tree is drawn with Modeling's cut meshes: PaintedTrees.Felled(pine, TreeVariant(x, z, 31/30)) for painted pines and broadleaf (the same shape as the standing tree), PropMeshes.CutTrunk for birch trunks. The regrowing tree keeps its roots. Also marks env-21 and env-22 done, with the env-21 water-edge follow-up noted.

## Progress log
- 2026-10-17: Terrain.StandingTree(x, z, felled) swaps the meshes; TreeFall uses felled: true for the fall. Builds. Render at -10,0 pose 5: the lying log ends in a clean saw cut beside the stump, no root prongs.

## Open questions / blockers
- (none)

## Next step
Check a fall pose render, then CI; send the green sha to the PM.
