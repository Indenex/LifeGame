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

- 2026-10-17 16:10: switched to the Source/Godot pack (8d28456e), now in client/assets/quaternius/nature with License_Nature_Source.txt (CC0); import script tools/art/quaternius/import.py. Ported the kit's Godot shaders into NatureKit's blended shader (a8687198): flat leaf greens + blue-green low colour, card spread, sun-driven leaf glow, bark foot tint + normal maps, grass gradients and wind lines. Shots: /mnt/project-files/screenshots/naturekit/kit-shaders-blend-{noon,golden}.png. Build clean locally (after tools/gen-bindings.sh).

- 2026-10-17 16:30: --view-bench (headless, all chunks per radius, before culling and before Godot's mesh LOD). Ours vs kit trees: radius 6: 16.66 M vs 14.09 M triangles, 4758 vs 3540 draw calls (+3724 vs +2506 shadow); radius 8: 27.64 M vs 23.45 M, 8043 vs 6007 (+6273 vs +4237); radius 12: 52.84 M vs 44.86 M, 16170 vs 12192. Kit trees are cheaper (one mesh per tree instead of near+far painted parts). Fixed: NatureKit.Tree prewarms itself on the main thread when nothing has yet. Birch and twisted leaves default to green until autumn (PM default while Justin picks).

- 2026-10-17 16:45: boulders from the kit behind graphics/naturekit_rocks (or --naturekit): Terrain's boulder line calls NatureKit.Rock(variant); each kit rock is scaled to our variant's footprint (collision unchanged), sunk 12%, and takes half the world stone hue plus each one's darkening. Bench radius 8 with kit trees+rocks: 23.96 M triangles, 6017 draws (rocks add ~2%). River stones stay ours.

## Open questions / blockers
- Look pick for Justin (via the PM): how far to take it.

## Next step
Wait for the Render lane's in-game shots (ours vs --naturekit) and Justin's pick; meanwhile start village props (baskets, barrels, crates, fences) from /mnt/project-files/assets-incoming/villagekit-godot.zip into client/assets/quaternius/village/.