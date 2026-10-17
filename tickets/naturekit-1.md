# naturekit-1: Blend the Stylized Nature MegaKit into our look
Lane: Modeling
Owner: Modeling lane (session_01Y3HWfLh47B52ViBgWpzWrz)
Status: in_progress
Branch: claude/modeling-i8ox5v (after #836 merges)
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

## Open questions / blockers
- Look pick for Justin (via the PM): how far to take it.

## Next step
Once #836 merges, push this branch; then wire kit trees and rocks into Terrain's placement behind a setting (with Environment), and measure a forest chunk.
