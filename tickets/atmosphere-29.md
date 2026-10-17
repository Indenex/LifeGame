# atmosphere-29: Painterly look, lighting side
Lane: World atmosphere (visual)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_progress
Branch: claude/atmosphere-29-painterly
PR: (none yet; prototype behind a flag until Justin picks, D-154)

## Goal
A joint job with Modeling from Justin, on the pines: "I still want more of the painted texture look. Maybe we can achieve some of that with custom shaders/lighting?" Modeling leads the painted foliage material (stepped light bands, brush-noise band edges, Golden Skull brush textures, ink rims, coloured shade). Atmosphere owns how it fits the lighting and grade: the stepped ramp must work with sun, moon, fog and god rays. Atmosphere also prototypes an optional screen-wide painterly pass, measures its cost, and proposes it to Justin through the PM with Render shots. Done: Justin has picked, and the picked option is merged.

## Progress log
- 2026-10-17 03:20: `client/scripts/atmosphere/Painterly.cs` (new) is a screen-wide Kuwahara pass: a quad in front of the camera after the 3D scene, with each pixel taking the calmest of four patches, which are turned along a slow brush-noise field and stretched into strokes. The HUD is untouched. Flags: `--painterly=1` (soft, radius 2, 36 taps), `--painterly=2` (strong, radius 3, 64 taps), `--painterly-mix=0..1`, and `--gpu-bench`, which logs the 3D view's average GPU ms every 5 s. Hook: `AddChild(new Painterly())` in `Atmosphere.cs`. The client builds with 0 errors. Not run yet.

- 2026-10-17 03:30: reviewed Modeling's painted pine ramp (claude/model-4-materials @ 0940e3c9, MaterialLook.CardPine light()). Sent them three issues: (1) a bug where omni lights add a flat 32% within their range, because the band floor has no ATTENUATION (fix: a `reach` term); (2) the 0.32 floor makes shadows too weak next to Look's ground (suggest about 0.1); (3) no cloud shadows (use cloud_shadow via Look.MakeShader). Asked Render for pinetest under dawn, noon, moonlight, fog and storm, plus a night shot with a fire.
- 2026-10-17 09:25: re-aimed at D-313 (cutesy, relaxing, a hint of realism), PM OK at 09:17; still behind a flag until Justin picks (D-154). Merged main. Painterly.cs now has 4 options for `--painterly=`:
  - 1: soft paint dabs.
  - 2: strong paint dabs.
  - 3, new, cosy wash: light dabs (mix 0.55) plus soft toon light bands (5 steps, half strength).
  - 4, new, soft bands only: 4 steps, 70%, no dab pass, so the cheapest.
  The bands work on compressed brightness, keep hue, and never crush shade to black. `--painterly-bands=N` overrides the step count. The client builds with 0 errors.

## Open questions / blockers
- Waiting on Render: comparison shots and the GPU cost.
- Waiting on Modeling: their stepped-ramp material, so I can check it under sun, moon, fog and rays.

## Next step
Render B: noon, dusk and night stills of off / 1 / 3 / 4 at the same view with the chibi, plus `--gpu-bench` numbers, then send the PM 3 to 4 options with costs for Justin.
