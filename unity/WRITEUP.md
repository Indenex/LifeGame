# Unity vs Godot for LifeGame's look: spike write-up (unity-1)

Justin's question: *"I want to see what their lighting engine etc. looks like vs. Godot."*

## The short answer

- **Our look is our own shaders, and they port almost 1:1.** The painted pines,
  ground, grass, water, sky and chibi all use hand-written light functions in
  `Look.cs`, `Atmosphere.cs` and `ChibiLook.cs`. Neither engine's built-in lighting
  model decides how they look. In Unity URP the same light functions work the same
  way. With the Godot grade on, Unity should give very nearly the same picture.
  Differences will come from the engine *around* the shaders:
  - shadows
  - ambient occlusion and bounce light
  - fog and haze
  - post-processing
- **Where Unity URP is better for us:** steadier, sharper cascaded shadows, a more
  mature post stack (bloom, depth of field, colour grading, TAA and STP upscaling),
  much better profiling tools, and cheap drawing of very many trees and rocks (SRP
  Batcher, GPU Resident Drawer).
- **Where Unity URP is worse for us:** it has **no volumetric fog, no height fog,
  no sun-scatter fog and no screen-space indirect light (SSIL)**. Our dusk,
  twilight and morning-mist moods lean on Godot's volumetric fog (density 0.0078
  at twilight), so URP would need a third-party or hand-written fog to match. HDRP
  has volumetric fog and clouds, but see below.
- **HDRP is not worth it for this art style.** It is physically based (the sun in
  lux, exposure in EV), which fights a painted, hand-lit look. Custom light
  functions like ours are much harder there. It is also heavy on mid-range PCs.
- **The real cost is the port and the pipeline, not the lighting.** 160 of the
  client's 174 C# files (35,700 lines) are written against Godot's API. Every
  cloud lane today renders screenshots and runs the client headless without a
  licence. **Unity cannot do that without paid licence seats in every cloud
  container and CI runner** (see "Pipeline" below).

**Recommendation: stay on Godot.** Spend the effort on the two things Unity would
give us that Godot lacks today: steadier shadow cascades and a profiler pass. Both
are reachable in Godot. Revisit only if a specific Unity feature becomes a must.

I could not see Unity's picture myself. The cloud can't run the Unity editor
without a licence, so the Unity screenshots have to come from Justin's PC (see
`README.md`, about 15 minutes). Everything below about Unity's look is based on
the feature-by-feature port, not on a rendered image. The Godot side is rendered.

## What the spike is

`unity/` is a Unity 6.3 LTS (6000.3.26f1) URP project, built so that the
differences come from the engine and not from different content or numbers:

- **The same world:** the ground is generated in Unity by `shared/World.cs`,
  `Forest.cs` and `Landmarks.cs`. `shared/` compiled for Unity unchanged; the only
  shim needed was for `MathF.Tau`, which Unity's .NET Standard 2.1 lacks.
- **The same objects:** the painted pines (#544), the chibi player (#466), rocks,
  grass and water are exported from the running Godot client. That's 344 meshes and
  25,641 instances within 140 m of spawn.
- **The same light:** the export carries Godot's sun, ambient, sky, fog, glow,
  tonemap and grade at that moment, and Unity rebuilds its lights from them.
- **The same final curve:** Godot 4.6's ACES tonemapper, contrast, saturation and
  the atmosphere grade's colour gradient are baked into a URP colour LUT. With
  **G**, Unity's own ACES grade can be compared instead.
- **Two lighting modes:** **L** switches between the game's hand-written painted
  light model (ported) and URP's stock PBR lighting on the same colours. The
  second shows what Unity's own lighting engine does with our art.

Checks that run without a licence:

- All C# compiles against the real Unity 6.3 and URP 17.3 APIs
  (`Tools~/compile_check.sh`).
- All shaders compile with DXC against URP's shader library in 104 variant
  combinations (`Tools~/hlsl_check.py`).
- Both checks catch planted errors.

## Feature by feature, for this art style

| | Godot 4.6 (what we use) | Unity 6.3 URP | Notes for LifeGame |
|---|---|---|---|
| Custom light functions | `light()` in any spatial shader | Hand-written HLSL pass (as in this spike) or Shader Graph custom function | Equal. Ported 1:1 |
| Sun shadows | 4 splits, 60 m, soft (PCSS-like by angular size 0.8°) | 4 cascades, 60 m, 4096 px, soft PCF; Unity 6 adds better filtering | URP is crisper and steadier, Godot softer at a distance. A matter of taste; Godot's softness suits the painted look |
| Ambient occlusion | SSAO, intensity 3 | SSAO renderer feature | Similar |
| Indirect bounce | SSIL (on) | None in URP | URP loses Godot's soft colour bleed under trees |
| Global illumination | SDFGI or VoxelGI available (off) | Baked lightmaps, Adaptive Probe Volumes (need baking) | For a procedurally generated world neither engine bakes; both rely on sky ambient |
| Distance fog | Exponential, aerial perspective, fog on the sky | Linear, Exp, Exp² only | URP: no aerial perspective, no height fog |
| Volumetric fog | Yes, lit by the sun, used at dusk and twilight | **No** (HDRP only, or the Asset Store) | The biggest look gap for our evening moods |
| Sky | Custom sky shader | Custom skybox shader | Equal. Ported. Unity needs the ambient probe filled by hand for a runtime sky, as the spike does |
| Glow and bloom | Glow (screen blend, HDR threshold 0.85) | Bloom (additive) | Similar amount, slightly different falloff |
| Tonemap and grade | ACES, brightness/contrast/saturation, 1D gradient | ACES or Neutral, colour adjustments, LUTs, curves | URP's grading tools are richer; Godot's exact curve is reproducible in URP (done) |
| Depth of field | Far blur at 32 m | Gaussian or Bokeh | Similar |
| Anti-aliasing | MSAA 4×, FXAA (we use both), TAA | MSAA, FXAA, SMAA, TAA, STP upscaler | Unity has more choice |
| Many trees and rocks | MultiMesh per chunk (3,559 draws here) | GPU instancing (merged to 78 batches here), GPU Resident Drawer | Unity scales better as the forest grows |
| Particles | GPU and CPU particles | VFX Graph, Shuriken | Both fine. Fireflies were not ported in the spike |

## Performance notes

- I couldn't measure Unity here (no licence in the cloud). Structurally, the spike
  draws the same 140 m of world as Godot: about 78 instanced batches, plus 289
  ground chunks and a few hundred single objects. That is light work for URP on
  any recent GPU.
- The Godot export of the same area holds 3,851 draws, because Godot builds one
  multimesh per chunk per plant kind. Godot handles this fine today. As view
  distance and forest density grow, Unity's SRP Batcher and GPU Resident Drawer
  scale better without extra work. In Godot, merging multimeshes across chunks
  would give much of the same win.
- URP's forward path costs about the same per pixel as Godot's Forward+ for these
  shaders (one directional light, a few texture reads). Godot's volumetric fog and
  SSIL are its most expensive effects. URP's frame would be cheaper mainly because
  it lacks them.

## What a real port would cost

| Part | Size | Port |
|---|---|---|
| `server/` (SpacetimeDB module) | 8,800 lines | **Unchanged.** SpacetimeDB has a Unity C# SDK, and the bindings generate for it |
| `shared/` | 4,700 lines | **Unchanged** (shown in this spike) |
| Client game code | 35,700 lines in 174 files, 160 of them on Godot's API | **Rewrite** against Unity's API |
| … UI (`ui/`, `Hud.cs`; 259 uses of `Control`) | about 6,000 lines | Rewrite in UI Toolkit; every window rebuilt and re-styled |
| … Procedural models (`models/`; 86 `ArrayMesh`, 43 `SurfaceTool`) | about 4,900 lines | Mostly mechanical: Godot mesh calls become Unity's `Mesh` API |
| … Atmosphere, weather, sky (`atmosphere/`) | about 6,700 lines | Rewrite; the volumetric fog needs a replacement |
| … Feature lanes (crafting, building, ceramics, social, animals, survival, journal) | about 13,000 lines | Rewrite of node, input and scene code; the logic carries over |
| … Animation (custom `Skeleton3D` rig) | about 1,500 lines | Rewrite on Unity's Animator or a custom rig |
| Shaders | 22 Godot shaders in C# strings | Port to HLSL. This spike did 5 of the main ones (about 700 lines) in a day, including the painted foliage |
| Tools | Render lane, gameplay checks, load test, launcher, CI exports | Rebuild. See "Pipeline" |

Estimate: **2 to 3 months of the whole team** (every lane porting its own code in
parallel), with a feature freeze. Then a long tail of matching the look and feel
again (camera, movement, feel of input, every UI window). Nothing new for players
in that time.

### Pipeline: the hidden cost

- Every cloud session (the Render lane, any lane checking its work) runs Godot
  headless without an account. Unity needs a signed-in licence for every editor
  run, batchmode included. This spike hit exactly that wall.
- Cloud renders and CI builds would need either Justin's credentials stored as
  environment secrets (a Personal licence also limits how many machines can be
  active), or paid **Unity Pro / Build Server** seats.
- Without that, no lane could see its own work, and screenshots for Justin would
  stop.
- Godot's licence is MIT: no seats, no revenue terms.

## What was left out of the port, and why

- Seasons, snow, puddles, wading wetness, felled-tree animation, see-through tree
  ghosts, rain, fireflies, stars and the moon: all inactive at noon in summer, and
  each is a straight port when needed.
- Volumetric fog: URP has none. The spike adds half of Godot's volumetric density to
  Unity's exponential fog as a stand-in. Expect dusk and twilight to look flatter
  in Unity. That is the honest comparison, not a bug in the spike.

## Files

- `README.md`: what Justin clicks to run it.
- `Tools~/spike_export.gd`, `Tools~/export_scene.sh`: the Godot exporter (runs in a
  render checkout, offline).
- `Assets/LifeGame/Runtime/`: loader, ground builder, Godot grade LUT, scene setup.
- `Assets/LifeGame/Shaders/`: the HLSL ports.
- `Assets/LifeGame/Editor/SpikeSetup.cs`: creates the URP settings and scene on
  first open.
- `../renders/unity-spike-*-godot.png`: the Godot reference frames (the HUD hidden,
  1280 × 720, same cameras as the Unity presets).
