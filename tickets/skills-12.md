# skills-12: One progression formula, and Stalking
Lane: Journal and skills
Owner: Journal and skills lane (Sonnet)
Status: in_progress
Branch: claude/journal-skills-12 (from main 4915aee; independent of #193)
PR: (not yet)

## Goal
Justin: one formula handles all skill gains and effects, front-loaded (first ~15% of ranks give ~40% of the benefit, D-075). `shared/Progression.cs` holds the curve and a typed effect table; every bespoke rank formula uses it. Add a Stalking skill (trained by `animal.sneak` and `animal.kill`) with Sneak distance and one-strike kill effects.

## Progress log
- 2026-10-16 20:22: PM assigned; before/after table sent to PM, building with log b=12 and the curve on rank unless told otherwise.
- 2026-10-16 20:50: built Progression.cs; migrated QualityCap, DurationScale, MinigameSteps, DugClayQuality, SecondLumpChance, Fires.MaxSecondsFor, Stamina; Stalking skill + Sneak process + hints; tools/ProgressionCheck passes; server type-checks.

## Open questions / blockers
- PM answers on curve family (log recommended) and whether to also slow Skills.Gain.
- Sneak distance 10 -> 3 m and one-strike chance 0 -> 0.9 are proposals; Animals swaps its Husbandry >= 20 stand-in and reports `animal.sneak`.
- Merging with #193 will conflict in shared/Skills.cs (Specializations array, SpecOverrides): keep both sets.

## Next step
Push, send the green sha to the PM, send Animals the effect kinds (SneakDistance, OneStrikeChance) and the Things to try line.
