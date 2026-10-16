# survival-5: Getting wet
Lane: Survival
Owner: Survival thread (session_01EzhhyMEiXNWShq9e1gpQ4w)
Status: in_review
Branch: claude/survival-needs-g7yrhi (restarted from main cab7fe4 after #48 merged)
PR: #75

## Goal
Rain and wading make you wet; wetness builds and fades gradually. Being wet makes cold bite harder, folded into the existing rain +50% (D-034's cold rules) rather than stacked on top. Shelter stops it building, and standing by a fire dries you about twice as fast. Shown subtly on the body (darker skin and leaves, a faint drip), with no new HUD element (D-045). Uses Atmosphere's server rain state.

## Numbers (decided: D-062, Justin "as proposed")
- Heavy rain (1.0) soaks you from dry in 2 minutes; drizzle (0.2) in 10. Shelter's rain cover scales it down.
- Wading soaks you in 10 seconds.
- Dry from soaked in 5 minutes out of the rain, 2.5 minutes by a fire.
- Cold drain multiplier is 1 + 0.5 x wetness (so full soak = the old rain +50%, and you stay chilled after the rain stops until you dry).

## Progress log
- 2026-10-16: `Needs.WetnessRate` in shared/Needs.cs; Vitals gains `Wetness`, `WetnessRate` ([Default(0f)], additive). Chill now uses wetness instead of current rain. Server builds.
- 2026-10-16: fixed wetness still changing while logged off (disconnect now pauses it).
- 2026-10-16: client `WetLook.cs`: per-player MaterialOverlay that darkens and adds a soft sheen, plus a faint drip particle when wetness > 0.15. Client builds.

- 2026-10-16: merged main (b3c6414). Locally: LoadTest 5x5, BuildTest, SpawnCheck, CraftCheck, SocialTest pass; `tools/upgrade-check.sh origin/main` passes (no wipe). Screenshots sent to PM: screenshots/13-survival-wet-dry-vs-soaked.png, 14-survival-wet-in-rain.png. Soaked skin reads about 19% darker with a soft sheen and a few drops.
- 2026-10-16: PM: don't add to #48; keep a backup patch at /mnt/project-files/survival/survival-5.patch (git diff origin/main HEAD) and open its own PR after #48 merges. Others should look wet too (D-036): public `Drenched` table (PlayerId, Level 0-10, row only while wet, removed on disconnect), updated by SurvivalTick; client puts WetLook on remote avatars via `SurvivalClient.AvatarOf` (one-line hook in Main). Waking after death resets wetness. Tested with LoadTest bots in rain: Drenched rows appear.- 2026-10-16: PM perf asks (Bug fixes profiling): Carrying, PotOfWater, NearestRipeBush and UpdateLoad use plain loops instead of LINQ; NeedGauge queues a redraw only when its arc or color changes, or while it glows or breathes.
- 2026-10-16: #48 merged (main cab7fe4). Replayed the survival-5 commits onto claude/survival-needs-g7yrhi from main.

- 2026-10-16: D-062: Justin approved the wetness numbers as proposed; no code change. PR #75 CI green on ccff739 (run https://github.com/Indenex/LifeGame/actions/runs/38855013374). Merged main 7c65c0f (ceramics/kiln/wiki; no file overlap with this PR; clean). No dotnet in this container, so CI is the build check.

## Open questions / blockers
- Sent to PM 2026-10-16: next Survival milestone, options A) unsafe raw water, boil in a fired pot (recommended) B) tiredness and sleep C) food spoils D) first-20-minutes polish pass. Default until answered: A.
- Sent to PM: should wet clothing insulate less (animals-9 clothing hook)? Default: no, clothing ignores wetness.
- 2026-10-16: reviewed animals-9 hook plan (ClothingWarmthHook, cold x (1 - insulation)); told Animals (session_01SAcVBUUfPFFk1rdXuiQZRU) where it goes.

## Next step
Wait for CI on the newest head of #75, then send the green sha to the PM (session_01CvoSQGRe3vDvDXWQu2qruU).
