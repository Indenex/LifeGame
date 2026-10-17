# atmosphere-26: Wading and floating, seen
Lane: World atmosphere (visual)
Owner: Atmosphere thread (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_review
Branch: claude/atmosphere-26-wading
PR: #429

## Goal
The PM's pick (2026-10-17), after bugfix-32 (#414) let players wade into ponds and float in deep water. The water answers: soft painted ripple rings around a wading or floating player, a small splash on entering, and wet, darker legs up to the waterline that dry off after leaving. Stylised to match docs/art-reference. Audio is asked for the sounds. Done: Render shots of wading, floating and just out of the water, and a PR green.

## Progress log
- 2026-10-17 01:55: first build (client 0 errors).
  - New client/scripts/atmosphere/AtmosphereWater.cs, local player.
  - Ripples: a 2.6 m plane at the water surface following the player, with three painted rings widening and fading. They come faster and stronger with speed. Tint is the air light lerped to white; fades in and out over about 0.25 s.
  - Splash: a one-shot burst of 28 droplets from a ring at the surface when the player goes from dry to in the water (wading in or landing from a jump).
  - Wet legs: globals `wet_line` (world height) and `wet_amount` (0 to 1, drying over 30 s), set from how high the water reached on the player (up to 1.4 m). The painted shader (Look.cs, which the player figure uses through Look.Solid) darkens albedo to 62% and raises wetness (gloss) below the line, within 0.65 m of player_position, with a soft 4 cm edge.
  - In water means depth over 3 cm with the feet below the surface (Wading.Depth from terrain.HeightAt), so a hop over a puddle doesn't count.

## Open questions / blockers
- Audio: wading and splash sounds (requested via the PM).
- Other players don't get ripples or wet legs yet; that needs per-player state. A later follow-up if wanted.

## Next step
Wait for #429 CI and send the PM the green sha. Render request sent (wading, floating, splash, just out). Audio request sent via the PM.
