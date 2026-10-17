# naturekit-1: Blend the Stylized Nature MegaKit into our look
Lane: Modeling
Owner: Modeling lane (session_01Y3HWfLh47B52ViBgWpzWrz)
Status: in_progress
Branch: claude/modeling-i8ox5v
PR: (not yet)

## Goal
Justin (via the PM, 2026-10-17): "this asset pack ... is closer to the style I want. Can we kinda merge these styles together, or use the models/textures where applicable? It should match my design guidelines still." The pack is Quaternius's Stylized Nature MegaKit Standard (CC0): /mnt/project-files/Stylized_Nature_MegaKitStandard.zip.
1. Import the fitting glTFs (trees, rocks, pebbles, bushes, ferns, grass, flowers, mushrooms) to client/assets/naturekit/ with textures cut down (tools/art/naturekit/import.py; License_Standard.txt beside them).
2. Blend: `NatureKit.Mesh(name)` draws a kit model with its own textures under our light (Painted.Light bands, violet shade warming at golden hour, rocks' rim, wind sway on leaves and plants, autumn turning broadleaf leaves). Where the kit looks better, pull our look toward it.
3. Swap one family at a time behind a setting, trees and rocks first; before/after from the game camera at noon and golden hour to the PM; Justin sees every swap before it lands.
4. Compare the kit's mushrooms with model-34 and keep the better.
5. Performance: kit trees are 5k-15k verts (ours far fewer): check draw calls and vertex counts in a forest chunk; LODs or MultiMesh.

## Progress log
- 2026-10-17 15:45: imported 52 models (18 MB with textures). NatureKit.cs loader + blended shader. Showcase `naturekit` (--kit=ours|raw|blend, --time-of-day). Renders: /mnt/project-files/screenshots/naturekit-1-trio-0.5.png and -0.72.png (top ours, middle kit as-is, bottom kit in our light). In our light the kit's ferns and grass lose their black back faces at golden hour. Local build 0 errors.
- 2026-10-17 15:55: #836 merged; branch restarted on main with the naturekit commits. Kit trees in the world behind a setting: Terrain's painted-tree loop draws NatureKit.Tree(pine, v) (CommonTree_1-4, Pine_1-4, scaled to PaintedTrees.Height) when `--naturekit` or graphics/naturekit_trees = 1; ChunkWorker.Prewarm loads them. Not yet: TreeFall (chopped kit trees don't fall), the see-through ghost, a far LOD. Game-camera shots rendering.

## Open questions / blockers
- Look pick for Justin (via the PM): how far to take it.

## Next step
Look at the game shots (offline client, --time-of-day=0.5 and 0.72, with and without --naturekit), send them to the PM; run --view-bench with and without --naturekit for vertex and draw-call cost; then the ghost and TreeFall for kit trees.
