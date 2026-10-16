# env-4: Corners of the forest (heath, wetland)
Lane: Environment
Owner: Environment session (Opus), session_012hVejTxLoxBpZyX3GQmNL9
Status: in_progress
Branch: claude/environment-env-4 (stacked on claude/environment-env-2 / PR #175, merges env-3 too)
PR: (once opened)

## Goal
Give the forest region distinct moods you can navigate by (D-081, a world to get lost in), on the reshaped land from env-2. Wetland by the streams and ponds: birch and alder, sedge, mossy ground. Open heath on the hilltops: heather, gorse, short grass, the odd broad old oak. Mixed forest in between, as in env-1. Visual only apart from trees thinning on the heath (shared `Forest.Density`, so the server's choppable trees agree); no wipe needed. Justin sees renders of a heath hilltop, a wetland stream bank and the forest between.

## Progress log
- 2026-10-16 21:40: proposed env-4 options to the PM (A corners of the forest, recommended; B gatherables by habitat with Survival/Crafting; C landmarks and trails; D draw distance). Started A as the default.
- 2026-10-16 21:55: `shared/Forest.cs`: `Heath(x, z)` (height above about 0.65 to 0.95 with a 30 m noise ragging the edge; about 16% of the land) and `Wetland(x, z)` (height below about -0.15 to -0.75 with a 14 m noise; about 10%). Density x (1 - 0.85 heath), so the heath has only lone trees. `Terrain.cs`: birch and alder (slim trunks, pale bark, light leaves) on wet ground; broad old oaks for full trees on the heath; heather (flattened bushes) and gorse on the heath; sedge (short reeds) on wet ground; no ferns or wildflowers on the heath; grass short on the heath and thin on wet ground; ground tinted heath-brown and moss-green. No new MultiMeshes (reuses trunk, leafy, bush and reed meshes). Colours are hard-coded in `Undergrowth.cs` until Atmosphere adds them to Look. Client and server build.

## Open questions / blockers
- Justin picked A (D-152).
- Modeling (model-2, claude/model-2) has PropMeshes.Trunk/Rock/FallenLog/Stump; swap them in once on main. Asked them for birch trunk, heather, gorse, cattails, hilltop outcrop, riverbank stones. Done on claude/model-2-env (PR after #197): PropMeshes.Birch (top (0,2.8,0), reuses leafy canopy, tint (0.86,0.84,0.80) with BarkSurface), Heather and Gorse (real colours in vertices, near-white tint, Look.Foliage), Cattails (Grass shader, 1.5 m), Outcrop (2.7 x 1.8 m, RockSurface), RiverStone(0..2) (0.3 m, scale 0.7 to 1.3). Swap in once on main.
- Look.BirchBark/Heather/Gorse/Sedge/HeathGround/Moss are on Atmosphere's #173 with the same values; switch `Undergrowth.cs` to them once #173 merges. If Justin wants heather to bloom in late summer (Atmosphere asks the PM), give heather bushes w = 3.

## Next step
Run CraftCheck on a fresh local world (trees near spawn change on the heath), then request renders of a heath hilltop and a wetland bank from Render, and open the PR once #175 has merged.
