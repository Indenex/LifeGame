# atmosphere-13: Night sky
Lane: World atmosphere and sound
Owner: Atmosphere thread
Status: in_progress
Branch: claude/atmosphere-sound-hlqfke after #66 merges (local atmosphere-13-wip, stacked on atmosphere-12-wip)
PR: (after #66 merges)

## Goal
From the PM, 2026-10-16: stars that fade in at dusk and out at dawn, a moon that rises and sets with soft moonlight on clear nights, phases over a few in-game days, clouds hiding both. Subtle, in my files. Done: screenshots to the PM, merged.

## Progress log
- 2026-10-16 05:35: built locally (Atmosphere.cs, Look.cs water and globals).
  - Moon: phase cycles over MoonCycleDays = 8 in-game days, full on day zero (Atmosphere.MoonPhase; --moon-phase=0..1 pins it, 0 new, 0.5 full). It runs the sun's path lagging by phase * one day, so a full moon rises at sunset and a new moon travels with the sun; pale day moon at quarter phases. The disc is lit on the sun side (sky uniforms moon_phase, moon_light), with faint earthshine; halo scales with phase.
  - Moonlight: the directional light at night comes from the moon's azimuth, never lower than 16 degrees; energy 0.55 * (0.65 + 0.35 * phase light * moon above horizon), shadows fainter with the moon down or thin. A full moon matches the old night; a new-moon night is about 35% dimmer but still readable (atmosphere-21-night-new-moon-pond.png).
  - Stars: show as (1 - daylight)^2, so only once dusk is gone; fewer under a bright moon; per-star warm/cool tint and twinkle (stronger near the horizon); faint galaxy band. Clouds draw over them and the moon; night clouds dimmer and silvered near the moon.
  - The game camera (CameraRig pitch -34) barely shows the sky, so ponds now mirror the night sky: reflected stars and a soft moon path (globals moon_dir, moon_light, night_sky), gone in rain or under cloud.
  - --sky-view=degrees tilts the camera up, for sky screenshots only.
  - Screenshots /mnt/project-files/screenshots/atmosphere-21-{night-full-moon-pond,night-new-moon-pond,sky-full-moon,sky-gibbous-moon,sky-quarter-moon,sky-moon-through-cloud}.png.

## Open questions / blockers
- Justin: the sky is almost never in view with the current camera pitch. A camera that tilts up when zoomed in is UX's file (CameraRig); ask Justin whether he wants that.
- Justin: new-moon nights are a little darker than today's nights. Keep, or keep every night as bright as now?

## Next step
Wait for the PM's answer on the two questions; push once #66 merges.
