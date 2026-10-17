# env-13: Ground cover to match the art reference (prototype)
Lane: Environment
Owner: Environment lane (session_012hVejTxLoxBpZyX3GQmNL9)
Status: in_progress
Branch: claude/environment-painted-ground
PR: (not yet; prototype until Justin picks, D-154)

## Goal
Match ground cover and terrain to Justin's docs/art-reference (02 forest meadow, 03 forest paths and cliffs): thick blade-shaped yellow-green grass with small flowers, mushrooms, twigs, stumps and rocks scattered through it, darker earth reading as shade between the blades, dirt paths, and (later) bark-like ledge sides. Prototype behind `--ground=painted` only; send the PM 2-3 written options (D-154) with renders, coordinate the palette with Modeling's variant D, and convert nothing until Justin picks.

## Progress log
- 2026-10-17 00:05 UTC: prototype `environment/PaintedGround.cs` (flag `--ground=painted`): blade clumps (3 clumps, 4-6 broad tapering arched blades each per tile, lit/mid/shade tone per blade baked relative to the lit tone, left half lighter than right); ground darkened toward olive earth, bare dirt on trails; mushrooms (more under trees), twigs and the odd stump (PropMeshes.Stump) scattered on grassy cells. Terrain hooks: TileColor(painted), tuft colour from PaintedGround.Grass, tuftMesh. Client builds. Asked Modeling for variant D's palette and whether they want to own mushroom/twig meshes.
- 2026-10-17 00:40 UTC: switched to the PM's steer: Golden Skull foliage sprites packed into client/assets/environment/foliage_atlas.png (tools/env-assets/build_foliage_atlas.py; NOTICE.md) as crossed alpha cards with a prototype card shader (sway, root darkening, fade by 48 m for grass). Ground pulled to the pack's grass-top yellow-green (0.60, 0.66, 0.28), sandy paths (0.70, 0.58, 0.38) on deer trails. Grass clumps: 3 variants of 5 crossed tufts each, turned, nudged and sized per tile; dense cover even under trees. Mushrooms and clover as pack cards; ferns and flowers swapped to cards. Added `--ground=mixed`: today's fine carpet with painted clumps on one tile in four. Renders (xvfb, (0,10), noon, summer): renders/env-13/env-13-1-today.png, -2-painted-cards.png, -3-mixed.png.

## Open questions / blockers
- Modeling: variant D palette values; mushroom/twig ownership.
- From afar (past the 48 m grass fade) the darker earth would show without grass over it; needs a distance blend in the ground shader (Atmosphere) if this is picked.

## Next step
Wait for Justin's pick among the options sent to the PM (A painted cards, B mixed, C procedural blades); then build it properly (shader into Look with Atmosphere, distance colour blend, clump variety) for a PR.
