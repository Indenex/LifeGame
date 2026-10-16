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
- 2026-10-16 20:55: Justin: fast for testing, much slower later (OSRS/Dofus); added Progression.Pace (1.0) scaling Skills.Gain; log b=12 and curve on rank confirmed.

## Open questions / blockers
- Animals proposes SneakCritChance and SneakCritBonus start/end values (placeholders 5% to 50%, 1x to 3x bonus damage; Justin: crit chance with bonus damage and a visual crit cue, Animals owns the cue).
- Sneak distance 10 -> 3 m and one-strike chance 0 -> 0.9 are proposals; Animals swaps its Husbandry >= 20 stand-in and reports `animal.sneak`.
- Merging with #193 will conflict in shared/Skills.cs (Specializations array, SpecOverrides): keep both sets.

## Next step
Push, send the green sha to the PM, send Animals the effect kinds (SneakDistance, SneakCritChance, SneakCritBonus) and the Things to try line.
