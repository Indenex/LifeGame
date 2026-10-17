# unity-1: Unity lighting comparison spike
Lane: Unity spike (one worker, comparison only; Godot stays the product)
Owner: Unity spike session (session_017Weex8fujB3VFsrpg84jEP)
Status: in_review
Branch: claude/unity-spike
PR: none (never to main unless the PM asks)

## Goal
A Unity 6 URP project in `unity/` that shows the same spawn meadow and forest as the
Godot client (terrain from `shared/World.cs`, painted trees, chibi player, rocks, water)
at noon and at dusk, so Justin can compare lighting, shadows, fog, sky and post side by
side. Deliverables: paired screenshots `renders/unity-spike-*.png` (Godot vs Unity, same
camera) and a write-up `unity/WRITEUP.md` (better/worse for this art style, performance,
honest cost of a real port).

## Progress log
- 2026-10-17 05:18: started. Unity 6000.3.26f1 (6.3 LTS) Linux editor downloads fine
  from download.unity3d.com (4.5 GB, ~25 MB/s). Godot 4.6.2 mono + render tooling set up.

- 2026-10-17 05:36: Step 0 result. Editor extracts and launches (8.2 GB), but batchmode
  stops at "No valid Unity Editor license found" (no entitlements; headless needs a
  signed-in Personal/Pro seat). Cloud activation would need Justin's Unity credentials,
  which this lane never handles, so Unity renders can't be made here. Route: Justin
  opens `unity/` in Unity Hub on his PC and presses Play; the project builds its own
  scene from Editor scripts so it opens cleanly. Godot side renders fine here (Forward+
  on llvmpipe, offline mode): local ref = main + model-10 + model-6 merged (not pushed).

- 2026-10-17 06:10: Godot exporter works (unity/Tools~/spike_export.gd + export_scene.sh, in
  a tools/render render checkout, offline). The game world is drawn in a SubViewport
  (Main.cs), so the exporter takes CameraRig's viewport, world, lights and environment.
- 2026-10-17 06:12: pushed f2b15169: unity/ URP project (Runtime loader, GroundBuilder from
  shared/, GodotGrade LUT, SpikeSetup editor bootstrap, 5 HLSL shader ports).
- 2026-10-17 06:16: Tools~/compile_check.sh (C# vs URP 17.3 + Unity 6.3 sources, no licence)
  clean; Tools~/hlsl_check.py (DXC vs URP shader library) 104/104; both catch planted errors.
- 2026-10-17 06:20: pushed 88bc27d0: README (what Justin clicks), WRITEUP.md, twilight preset
  (0.76 is after sunset), instancing merged (3,559 Godot multimesh draws -> about 80 batches).
- 2026-10-17 06:24: Tools~/GroundCheck: Unity ground from shared/ vs Godot's exported ground
  mesh: heights exact, colours within 1/255 (2,640 vertices).
- 2026-10-17 06:25: exporter fix: skinned player lost its material (baked mesh copy has
  none); re-exporting noon, plus golden-hour dusk (0.725) and low views (render-cam 9,2.2,0,2.2).

- 2026-10-17 06:45: all six presets exported and committed (f16fc71f): noon, noon-low,
  dusk (0.725, golden hour), dusk-low, twilight (0.76, after sunset), twilight-low (low =
  render-cam 9,2.2,0,2.2). Godot frames in renders/unity-spike-<preset>-godot.png. Instancing
  batches capped at 500 (D3D11 instancing buffer). Compile and HLSL checks clean.
  The spike is done from the cloud side: the Unity half needs Justin's PC (README).

## Open questions / blockers
- Unity can't run here without a license (see log). Unity-side screenshots come from
  Justin's PC; the project has a key to save them at the matching camera presets.

## Next step
Wait for Justin's Unity screenshots (README: Play, keys 1-6, P saves renders/unity-spike-<preset>-<mode>.png),
then pair each with renders/unity-spike-<preset>-godot.png and update unity/WRITEUP.md with what he saw.
