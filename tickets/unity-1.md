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

## Open questions / blockers
- Unity license activation in batchmode (Personal needs Justin's Unity account; never
  handle credentials here). Checking now.

## Next step
Extract the Unity editor to ~/unity-editor and run `Unity -batchmode -quit -nographics`
to see whether it starts without a license.
