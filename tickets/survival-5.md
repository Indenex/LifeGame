# survival-5: Getting wet
Lane: Survival
Owner: Survival thread (session_01EzhhyMEiXNWShq9e1gpQ4w)
Status: in_progress
Branch: claude/survival-needs-g7yrhi (work kept on local branch survival-5-local until PR #48 merges, then replayed onto a fresh branch from main)
PR: (not yet)

## Goal
Rain and wading make you wet; wetness builds and fades gradually. Being wet makes cold bite harder, folded into the existing rain +50% (D-034's cold rules) rather than stacked on top. Shelter stops it building, and standing by a fire dries you about twice as fast. Shown subtly on the body (darker skin and leaves, a faint drip), with no new HUD element (D-045). Uses Atmosphere's server rain state.

## Numbers (defaults, PM/Justin can change)
- Heavy rain (1.0) soaks you from dry in 2 minutes; drizzle (0.2) in 10. Shelter's rain cover scales it down.
- Wading soaks you in 10 seconds.
- Dry from soaked in 5 minutes out of the rain, 2.5 minutes by a fire.
- Cold drain multiplier is 1 + 0.5 x wetness (so full soak = the old rain +50%, and you stay chilled after the rain stops until you dry).

## Progress log
- 2026-10-16: `Needs.WetnessRate` in shared/Needs.cs; Vitals gains `Wetness`, `WetnessRate` ([Default(0f)], additive). Chill now uses wetness instead of current rain. Server builds.
- 2026-10-16: client `WetLook.cs`: per-player MaterialOverlay that darkens and adds a soft sheen, plus a faint drip particle when wetness > 0.15. Client builds.

## Open questions / blockers
- Only your own wetness is visible (Vitals is private), so other players don't look wet yet. Would need a public wetness field; asked PM.

## Next step
Render the rain screenshot (local server, weather row set to rain 0.8), send it to the PM, run the CI suite locally.
