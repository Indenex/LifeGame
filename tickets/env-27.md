# env-27: Painted ground, two takes for Justin (A painted shader, B+ Golden Skull tiles)
Lane: Environment
Owner: Environment thread (session_012hVejTxLoxBpZyX3GQmNL9)
Status: in_progress
Branch: claude/environment-painted-terrain
PR: (none yet; rough prototypes for Justin's side-by-side pick, D-154)

## Goal
Justin's top priority: the painted look on everything, starting with the ground (he called out the flat brushed-noise grass and dirt). Two rough prototypes, same spots, for his pick:
- A (`--ground=painted`): the ground in the painted trees' and props' language (Modeling's Painted.Declarations/Light): broad warm and cool colour patches, soft value bands with brush-broken edges, directional strokes, stepped light with violet shade.
- B+ (`--ground=tiles`): the Golden Skull iso-starter tile paintings with anti-tiling: two grids (second offset half a cell), each cell turned and shifted at random and blended across cell edges; two variants per type by region; grass/dirt/sand blended by a brush-broken height from the ground's own colour; the ground's palette laid over the tiles' detail; the same stepped light. 2 samples per layer plus 1 for its average.
Both: grass blades keep shape and number (D-256) and only take the stepped light (`painted_light`); a bolder shoreline (wider sand bank, darker wet band, taller reed clumps of 2-4), per PM after Justin couldn't see the env-21 water edge. Without `--ground=` the game draws exactly as before.

## Golden Skull licence
`client/assets/environment/golden-skull/ground_atlas.png`: eight tile top faces cut from the 2D Isometric Starter Pack (2021), unwarped from the iso diamond to 128 px squares. Justin bought the pack (commercial licence, docs/art-packs/golden-skull/LICENSE.txt): use in the game is allowed; the pack must not be redistributed, or altered and redistributed, without Golden Skull's written consent; Justin says hosting in this repo is fine for now. The pack has only isometric block sprites (no seamless ground textures), so B+ is softer and lower-resolution than A by nature.

## Progress log
- 2026-10-17: first A as Painted.Of(Look.Ground) was too subtle; rebuilt both as one shader from Look.PaintedCode with Painted.Splice/Declarations/Light (#577). PaintedGroundLevel 0.66 keeps ground and grass at the old brightness. Local renders at the stream bank (-37,10) and spawn meadow (0,10), zoom -6 (scratchpad g3-cmp, m2-cmp). Builds; no shader errors.

## Open questions / blockers
- Justin's pick between A and B+ (PM is bringing the paired stills).

## Next step
Render paired stills (requested): spawn meadow, a dirt/path edge, the stream bank, each close and Far, noon; then build out the picked one.
