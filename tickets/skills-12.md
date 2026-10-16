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
- SneakCritChance 3% -> 40% and SneakCritBonus +0.5x -> +2.0x damage are Animals' values (Justin: crit chance with bonus damage and a visual crit cue; Animals owns the cue and reports animal.sneak from its strike code).
- Sneak distance 10 -> 3 m and one-strike chance 0 -> 0.9 are proposals; Animals swaps its Husbandry >= 20 stand-in and reports `animal.sneak`.
- Merging with #193 will conflict in shared/Skills.cs (Specializations array, SpecOverrides): keep both sets.

- 2026-10-16 21:20: Archery skill (non-basic, capped) trained by animal.shoot (Animals reports it; a miss reports failed=true so a hit teaches double); effects ArcheryDamage 0.44->2.0, ArcheryHitChance 0.8->1.0, ArcheryDrawTime 2.0->0.8 s per Animals. Journal hint prereq is hunting until lash.bow exists on main.

- 2026-10-16 21:30: Justin: combat skills slower, ceiling easy to raise, skill matters more than item base. Added Progression.CombatPaceFactor (0.5) via Skills.Gain(points, failed, specId) (server passes spec.Id), IsCombat (stalking, archery), and Progression.Ceiling named constants for every effect top value.

## Queued for a follow-up PR (after #237 merges, so CI is not restarted)
- Things to try lines from Crafting (D-105): lash.bow "A green stick bent and strung with cord will throw a light shaft a long way."; carve.arrows "Straight shafts, shaved smooth and feathered with fibre, fly truer than plain sticks."; tool repair (no process id; Repair table, StructureKind.RepairTable, G at the table) "A tool gone soft in the hand is made good again at a table with a length of cord." Add to client/scripts/journal/NextSteps.cs once the ids exist on main (#266); repair needs an id from Crafting first.

## Next step
Push, send the green sha to the PM, send Animals the effect kinds (SneakDistance, SneakCritChance, SneakCritBonus) and the Things to try line.
