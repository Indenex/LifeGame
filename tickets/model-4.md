# model-4: Painted material look (Justin's art direction, D-154)
Lane: Modeling
Owner: Modeling session session_01JKX3FNjkrEkAqoTaFJiXAm (Opus)
Status: in_progress
Branch: claude/model-4-materials
PR: (prototype only; no PR until Justin picks)

## Goal
Justin dislikes today's fine noisy relief on everything and wants Valheim-like materials: low-poly shapes with small, simple, hand-painted-feeling textures (wood grain, bark, stone, leaves, thatch, wool), with lighting carrying the beauty, cute and warm. Prototype 2 to 3 looks on one test set (log cabin, pines, oaks, boulders, sheep, player) at golden hour and at night by a fire, send descriptions and renders to the PM, and convert nothing in game until he picks.

## Progress log
- 2026-10-17 00:05: PM relayed the direction. Descriptions sent to the PM: A (32 px, 3 to 4 flat tones, pixel-sharp, Valheim-like, recommended), B (64 px smooth, storybook), C (B plus a slight per-object tint for D-166 variety).
- 2026-10-17 00:30: `client/scripts/models/MaterialLook.cs`: procedural painted textures per pattern (Plain, Wood, Bark, Stone, Leaves, Straw, Wool, Skin), triplanar in the object's own space (no UVs, no crawl), colour still from vertex colour and albedo; `Classify` maps today's materials (by their relief settings) to a pattern. Showcase view `materials --look=current|A|B|C --time=day|golden|night [--close]` builds the test set (cabin from StructureModel, Terrain's trees via reflection, PropMeshes rocks, SheepFigure, PlayerModel); night adds a campfire. Lighting here is a stand-in; Atmosphere asked for the real golden-hour and night setups.

## Open questions / blockers
- Justin's pick of A / B / C (or tweaks), via the PM.

## Next step
Wait for Render's images and Justin's pick; then plan the conversion (Look's painted shader gets the chosen textures, by family) with Atmosphere and Environment.
