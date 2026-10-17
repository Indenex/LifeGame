# audio-12: Water sounds (wading, splash, float)
Lane: Audio
Owner: Audio thread
Status: in_review
Branch: claude/audio-12-water
PR: (see below)

## Goal
Now that ponds are wadeable and deep water floats (#414): wading steps that get deeper and heavier with depth, a splash on entering the water, drips on leaving, and a gentle paddle stroke / lapping while afloat. All as CueDef data (`CueDefs.WadeShallow/WadeKnee/WadeDeep/SplashIn/Drip/Paddle/Lap`).

## Progress log
- 2026-10-17: built on top of audio-11 (#424). `Footsteps()` now reads `Wading.Depth(feet y)`: under 0.3 m shallow, 0.3 to 0.65 knee, over 0.65 deep, afloat (depth >= Wading.FloatDepth) paddle strokes per stride and a quiet lap every 2.6 s when still. Entering water plays `Sfx.Splash(pos, strength)` (strength from speed); leaving plays the drip. Levels (loudest 50 ms, dBFS, with the morning bed): shallow -27.9, knee -27.9, deep -28.4, splash small -29.2 / big -22.2, paddle -34.1, lap -35.6, drip -33.6. Edges and fuzz soak clean, client builds. `water:17` SoundPreview scene plays all of them.
- Hook for other lanes: `Sfx.Splash(Vector3 where, float strength01)` plays the splash for anything hitting water (yours at full volume, others positional and quieter). The player's own entry is already handled in Atmosphere; do not add a second one.

## Open questions / blockers
- Other players wading are silent (only the local player's strides are sensed). Needs a per-player foot event; ask PM if wanted.
- Not heard in the running game (no Godot here); Render can film it.

## Next step
Wait for #424 to merge, retarget this PR to main, send the green sha to the PM.
