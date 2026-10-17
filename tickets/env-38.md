# env-38: Water polish, painted Ghibli look
Lane: Environment
Owner: Environment thread (session_014EL1FHDJi92N6gKixNv6Q5)
Status: in_progress
Branch: claude/environment-np240l
PR: (none yet)

## Goal
Justin, from a playtest: "water is a step in the right direction, but it needs to be
cuter and match the style more. Right now it's kinda buggy." A polish pass on the water
(WaterLook's shader, AtmosphereWater, AtmosphereWaterSurface; the PM handed these
Atmosphere files to this lane): soft flat colour bands, gentle stylised ripples and
shoreline foam, no realistic glare, and the bugs fixed. Before/after stills through Render.

## Bugs found (code reading; no Godot in this container)
1. Depth was measured along the view ray, not straight down: the shore foam band and the
   colour bands grew and shrank as the camera turned, ponds read darker at a glance, and
   anything standing in the water (player legs, sheep) got a thick foam/dark halo.
   Fixed: vertical depth from the depth buffer.
2. Stream flow map jittered mid-channel: its direction came from the slope of
   StreamDistance, which flips from bank to bank and is ~0 on the centre line, where the
   flow is strongest; ripples, leaves and foam streaks slid every which way there.
   Fixed: Land.StreamAlong, from the stream line's own smooth field.
3. Screen reflections took anything within 2 x step + 0.4 m behind the ray as a hit, so
   the bank's grass smeared across the water. Fixed: tighter hit test (1.2 x step + 0.15).
4. Splash droplets were unshaded near-white: they glowed at night. Fixed: lit material.
5. The milky white water (Render's env35-pond-47bafb1-*.png, and likely Justin's "buggy"):
   WaterRipples' two buffers are never drawn before the first step reads them, so they
   hold black, which reads as the surface pushed 0.5 down everywhere in the 32 m square
   round the player. The shader painted it as crest foam until the waves damped out
   (seconds at 60 fps; much longer at a low frame rate, as in the software renders).
   Fixed: the first two steps write a calm surface. Crest foam also needs a higher
   crest now (5 to 9 cm, was 3.5 to 7).

## Look changes
- Flat painted bands by vertical depth: pale aqua shelf, teal middle (streams), soft
  blue-teal deep (pond middles), with short soft blends and a gentle noise wobble.
- Bed shows through only on the shelf.
- Reflections light and blurred (mip 3, at most 80%), fresnel in two soft steps (max
  ~38%) instead of a mirror ramp up to 80%; sky in two soft bands.
- No sun glint or halo: a soft warm sheen (6%) on ripples facing the sun.
- Pale painted ripple lines where the wind's ripples crest, fading by 30 m.
- Shore foam: a soft band at the bank plus a thin line lapping out from it and fading
  (stays shallower than stream and pond beds, so it never washes over a bed).
- Clay troughs (Ceramics' ClayLook): depth_scale 4 and no lapping line, so a hand-deep
  trough still reads as deep water.

## Progress log
- 2026-10-17 15:50: PM: dragonflies nearly invisible in Render's pond shots. Bigger (0.45),
  stronger jewel bodies with a faint glow, a glint flashing on the wings, lower (0.15 to
  0.6 m), over open water only (bed 0.18 m down). Rides this PR as an env-35 follow-up.
- 2026-10-17 15:40: built the pass locally (not compiled here: the network policy blocks
  the Godot and .NET downloads); before/after renders requested through the PM.

- 2026-10-17 15:55: CI on 4d1f443e green (14/14), https://github.com/Indenex/LifeGame/actions/runs/39045459794; green sha sent to the PM.

- 2026-10-17 16:30: Render's pond frame at 7107848a: water teal, not milky, no shader errors. Fixed the two flaws it showed: stair-stepped foam streaks from the wave grid (sim now sampled with four smoothing taps, wider minimum splash, softer crest foam) and dragonflies reading as crossed sticks (oval wings with a dark rim and tip spot, a head, thorax and banded tapering tail).

- 2026-10-17 17:05: paused by Justin. Stair-stepped white blocks still show in the pond foreground at 198e5ed0 and after the sim-normal fix in 973518b2. With --water-reflections=0 they remain, so they aren't the screen reflections. A debug build (local branch dbg-water, fe42b526, saved as /mnt/project-files/patches/env-38/0001-dbg.patch: `--wdbg=1..5` shows normal, fresnel, foam, ripple lines, depth) was about to show which term causes them.

## Open questions / blockers
- Godot shader errors only show at runtime, so the Render lane's "after" shots are the
  first real check of the shader.

## Next step
Find the stair-stepped term: render the pond (`--offline --debug-position=-44,-20 --render-cam=7,2.2,0,0 --time-of-day=0.5`) with a debug view of each of normal, fresnel, foam, ripple lines and depth (17:05 log line), fix it, then send the PM the green sha.
