# animals-15: Bow and arrows (shooting)
Lane: Animals (shot reducer, hit chance, client targeting) with Crafting (Bow/Arrow items, recipes, power values)
Owner: Animals lane (Sonnet)
Status: todo (waiting for Justin's numbers; stacked on #219 for deer health)
Branch: claude/animals-bow-5gkntn (stacked on claude/animals-deer-5gkntn)
PR: none yet

## Goal
A hunter with a bow and arrows can shoot huntable animals (sheep, deer) at range. The client auto-targets the nearest huntable in a forward cone within range; one server reducer resolves each shot instantly (no projectile simulation; the client animates the arrow). The first crude bow and arrows need 3 to 6 arrow hits plus a chase to bring a deer (HP 100) down.

## Rules (Justin)
- Damage = bowPower x arrowPower x (0.7 + 0.3*bowQ) x (0.7 + 0.3*arrowQ) x rangeFactor x rand(0.85, 1.15). Qualities are 0 to 1 (quality/100).
- Hit chance falls with distance and target speed.

## Proposed numbers (sent to PM as options; recommended first)
- A (rec): MaxRange 40 m, ForwardCone +-25 deg. rangeFactor 1.0 to 15 m, falling linearly to 0.5 at 40 m. Hit chance = clamp(0.95 - 0.012*(d-5), 0.3, 0.95) x clamp(1 - 0.07*speed, 0.35, 1) (a deer at full run 6.5 m/s: x0.55; a standing sheep at 20 m: 0.77). Crude bow power 5, crude arrow power 7 (35 base): quality 50 hits ~21 at 20 m, quality 1 ~15, quality 100 ~30, so 3 to 6 hits.
- B (gentler): same shape but hit chance floor 0.45 and speed penalty 0.05/m/s; power 6 x 7 (about 4 hits).
- C (harder, more chase): MaxRange 30 m, speed penalty 0.09/m/s; power 5 x 6 (about 5 to 7 hits).
- A shot that misses alarms the target (Nervous, like a strike). A hit wounds it and it bolts like a spear wound.

## Split
- Crafting: Item.Bow, Item.Arrow (ids in Crafting's 1-99), their recipes, BowPower/ArrowPower on ItemDef, quiver rules.
- Animals: `ShootAnimal(animalId)` reducer in Hunting.cs, hit/damage maths, client `client/scripts/animals/` targeting cone + arrow flight animation, Audio request for the bowstring.

## Progress log
- 2026-10-16: ticket written, numbers sent to the PM; Crafting asked for items and recipes.

- 2026-10-16: `server/spacetimedb/Bow.cs` ShootAnimal reducer on option A, behind `BowShotHook` (Crafting implements: finds bow and arrow, takes the arrow, fills BowShot). Hunting.cs HurtAnimal split out of StrikeAnimal so spear and arrow share wound/kill. Not built locally; CI is the check. No client yet.

- 2026-10-16: Justin picked option A (via PM). Bow.cs already implements A. Skill effects move to Progression.Effect when skills-12 (claude/journal-skills-12, Stalking skill) merges. Modeling asked for bow and arrow models; Crafting for items/recipes/tag.

- 2026-10-16: Justin: bow must be much harder at level 1 (6-7 hits on a deer), one-shot possible at the top or with crits. Proposed (sent to PM, Journal, Crafting): Archery skill (id archery, trained by animal.shoot) with ArcheryDamage 0.6 to 2.4, ArcheryHitChance 0.8 to 1.0, ArcheryDrawTime 2.0 s to 0.8 s; powers stay bow 60 x arrow 0.65. Rank 0/25/50/100 per-hit damage, crude: 15/35/49/61; excellent: 23/53/74/91. Crafting's fields are on crafting-bow (0ea829f); they write BowShotHook once this branch and #267 are on main.

- 2026-10-16 21:20: Justin: combat skills progress slower; the ceiling must be a single named constant per effect that we raise in later content; skill matters much more than gear. Redone: gear factor now (0.9 + 0.1 q) per item (crude to excellent only ~13% apart, in Bow.cs BowDamage); ArcheryDamage 0.44 at rank 0 to 2.0 at the ceiling (hi is one named constant in Journal's Progression), combat category pace ~0.5x on skill gain. Per-hit damage rank 0/25/50/100: crude 15/38/55/68, excellent 17/43/62/77.

- 2026-10-16 21:30: Justin: Archery progression must be drawn out (7,6,5,4,3,2 hits at ranks 0,20,...,100, crude gear, no crit). Proposed to Journal (skills-12/#237): combat damage multipliers use a harmonic (even-in-hits) shape, m(r) = 1/(1/lo + (1/hi - 1/lo)*r/100), lo 0.44, Ceiling.ArcheryDamage 1.6. Crude per-hit 15/18/21/27/36/55 (hits 7/6/5/4/3/2). With a crit (x1.5-x3): one-shot needs about rank 80 plus a top-Stalking crit. Sent to PM. Bow.cs still has no Archery effect applied: after #237 merges, read ArcheryDamage/HitChance/DrawTime via Progression.Effect(kind, SkillRank(ctx, playerId, Skills.Shoot)).

## Open questions / blockers
- Numbers decided: A.
- Hard kill threshold vs Journal's proposed one-strike chance (0 to 0.9 by rank) for the sneak kill: asked PM.
- Crafting's item ids and power values.

## Next step
When #219 is on main: merge main here, apply Archery effects (damage, hit chance, draw time) from Progression.Effect once Journal's skills-12/#237 has them, then the client targeting cone + arrow flight (ItemModels.ArrowFlight from Modeling's #235) in client/scripts/animals/. Crafting writes BowShotHook.
