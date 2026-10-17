# env-22: Felled trees fall, leave a stump, and regrow in stages
Lane: Environment
Owner: Environment thread (session_012hVejTxLoxBpZyX3GQmNL9)
Status: done
Branch: claude/environment-tree-fall
PR: #537 (merged)

## Goal
Part of Justin's "interact with the world more" push (PM). A felled tree no longer vanishes: it tips over away from the chopper (so "Stand clear when it falls" reads), falls with weight over 1.6 s, bounces once, lies as a log 4 s, then sinks away; a stump stays. Over Trees.RegrowSeconds (2 h) it regrows in stages: stump, sapling beside it, young tree, nearly full; the server's row delete brings the full tree. Pines, broadleaf and birches. Client only, from ChoppedTree (Felled, LastChop); no server change.

## Progress log
- 2026-10-17: `environment/TreeFall.cs`: TreeFall node (Main hook: create + Attach(conn)); `Terrain.StandingTree(x, z)` builds one cell's tree through the normal chunk code (Snapshot.Keep / Only), so every species and variant matches; `Terrain.NoEase` makes the chunk drop felled cells at once instead of its old shrink-topple. Events `TreeFall.Falling` and `TreeFall.Landed` (1.6 s after Felled) for Audio's crash and Atmosphere's #486 dust. `--debug-fell=x,z[,s]` fells the nearest tree with no server, for renders. `--view-hash` at (-4,46) identical to main (F40A5E9D7558D092). Builds.

- 2026-10-17: Renders with `--debug-fell=-10,0,4 --debug-fall-pose=t` (t holds the fall at t s) at -10,0 zoom -4: a pine tips away from the player, butt slides off the stump, lies beside it. Fixed: the falling tree's see-through copy stays on (it was fading out near the camera); pivot at the stump top. `--debug-stages=x,z` shows the four regrowth stages on the nearest trees: stump rots away (14-28% of regrow time), sapling from 28%, young, nearly full.

## Open questions / blockers
- Root flare on the lying log: fixed in env-23 with Modeling's cut trunks.

## Next step
Done; merged as #537.