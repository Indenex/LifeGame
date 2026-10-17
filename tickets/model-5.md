# model-5: Convert the game to the painted look (variant D), once Justin picks
Lane: Modeling (with Atmosphere, Environment, House building, Animals)
Owner: Modeling session session_01JKX3FNjkrEkAqoTaFJiXAm (Opus)
Status: todo (waits on Justin's pick, D-154; D is the front-runner)
Branch: (new branches off main per step)
PR: (one per step)

## Goal
Everything a player sees uses the look Justin picks from model-4, matched to docs/art-reference: light in 2 to 3 crisp bands with blue-violet shade, a soft dark rim on shapes, painted 3-tone strokes per material instead of fine noisy relief, the reference palette, and stylised tree shapes. Rain, snow, puddles, autumn leaves, heather bloom, tree fade and the far-land fog keep working. Done when the playtest build looks like the model-4 D renders.

## Plan (each step its own small PR; owners in brackets)
1. **Look's painted shader takes the painted textures** [Modeling; Look.cs is shared, so tell Integration and Atmosphere first]. Nearly every surface already goes through `Look.Make(...)` families (Ground, BarkSurface, RockSurface, Foliage, Solid), so this one change converts most of the world:
   - add a `pattern` texture uniform per family (from `MaterialLook`'s 3-tone generator) and sample it in object space for props and world space for terrain, replacing `relief()`'s noise; keep `relief()` only where weather needs a height (snow in hollows, puddles);
   - add the banded `light()` (3 bands, violet shade) and the dark rim, behind a global `painted_look` switch so it can be flipped back in one place;
   - Thatch and fibre (Foliage tinted straw in StructureModel) get the Straw pattern through an explicit `pattern` parameter instead of being guessed from colour.
2. **Palette** [Modeling with Environment and Atmosphere]: Look's base colours shift to the reference palette (stone grey-violet, foliage olive, grass yellow-green, earth warm brown). Values from model-4: stone x (0.92, 0.90, 1.08), leaves x (1.0, 1.0, 0.78), ground grass (0.50, 0.62, 0.27).
3. **Trees** [Environment owns Terrain]: `PropMeshes.ScallopPine` and `PropMeshes.LeafClumps` replace Terrain's pine and leafy canopy meshes (same trunk, same heights); landmark trees and variety (D-166) follow.
4. **Ground cover** [Environment, already prototyping `--ground=painted`]: broad 3-tone grass blades, mushrooms (`PropMeshes.Mushroom`), twigs (`PropMeshes.Twig`), stumps, paths.
5. **Grade and lighting** [Atmosphere]: atmosphere-25 grade (faded warm, violet shadows, bloom, god rays) on by default.
6. **Characters and animals** [Modeling, Animals]: player and sheep keep vertex colours; skin gets the Skin pattern at low strength, wool the Wool pattern; check the banded light reads on faces at the game camera distance.
7. **Items, stations, held tools** [Modeling]: they use Look.Solid with relief settings, so step 1 covers them; check icons (ItemIcon renders) still read.
8. Renders of each step through the Render lane; Justin sees the in-game result before the switch defaults on.

## Progress log
- 2026-10-17 01:30: plan written at the PM's request while the model-4 renders are made. Nothing converted.

## Open questions / blockers
- Justin's pick (model-4 renders with --grade=1, via Render and the PM).

## Next step
When Justin picks D: start step 1 on a branch off main, after telling Integration and Atmosphere that Look.cs's painted shader is changing.
