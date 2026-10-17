# unity-1: Unity lighting comparison spike
Lane: Unity spike (one worker, comparison only; Godot stays the product)
Owner: Unity spike session (session_017Weex8fujB3VFsrpg84jEP)
Status: in_progress
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

## Open questions / blockers
- Unity can't run here without a license (see log). Unity-side screenshots come from
  Justin's PC; the project has a key to save them at the matching camera presets.

## Next step
Write the Godot export autoload (glTF of the built scene + camera/sun/fog JSON), then the
Unity project skeleton (shared DLL, Editor bootstrap, painted URP shader).
