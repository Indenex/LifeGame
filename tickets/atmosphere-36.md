# atmosphere-36: Soft glow, light leaks, vignette, rim tint (D-313 picks #8, #10)
Lane: Atmosphere
Owner: World atmosphere lane (session_01KvVF5vpkYEHTKNRpHC18WN)
Status: in_review
Branch: claude/atmosphere-36-glow
PR: #612

## Goal
Justin's cutesy picks from D-313:
- **#8, soft bloom and light leaks:** a warm glow round the sun, fires and lamps, and pastel light leaks at golden hour. Subtle, never washing out; atmosphere-34's fog and bloom cap still applies.
- **#10, gentle vignette:** warmer at dusk, cooler at night, very soft.
Each has a toggle in Options (graphics `soft_glow` / `vignette`; `--soft-glow=0` and `--vignette=0` on the command line). Modeling's character rim light (#1) takes its tint from a new global `rim_tint` (vec4: rgb colour, a strength) that the atmosphere sets by time of day.

What's in `client/scripts/atmosphere/AtmosphereLens.cs`:
- The glow moves to the wider levels and gets a honey glow map.
- A warm halo round the sun in the sky shader (`halo` uniform).
- A CanvasLayer at layer 1, under the HUD, with the vignette and an additive light-leak rect. The leaks bleed in from the screen edge on the sun's side.
- The rim tint.

## Progress log
- 2026-10-17 07:35: built; the client compiles with 0 errors. Shaders aren't compiled in CI, so Render's stills are the first real test.
- 2026-10-17 07:32: rim_tint made relative to the light colour for Modeling's #620 (day = their honey (1,0.86,0.66), a = 1).

## Open questions / blockers
- Modeling's rim (PR #620, Painted.Rim) reads `global uniform vec4 rim_tint;`: colour = LIGHT_COLOR * rim_tint.rgb, strength = RimStrength * rim_tint.a. #612 must merge first.

## Next step
Wait for CI on #612 and Render B's before/after stills (requested 07:37), then tune.
