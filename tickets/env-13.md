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

## Open questions / blockers
- Modeling: variant D palette values; mushroom/twig ownership.
- From afar (past the 48 m grass fade) the darker earth would show without grass over it; needs a distance blend in the ground shader (Atmosphere) if this is picked.

## Next step
Look at the side-by-side screenshots (scratchpad), tune, then send the PM 2-3 options with renders.
