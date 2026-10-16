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

## Open questions / blockers
- Justin picks the numbers (A/B/C above).
- Crafting's item ids and power values.

## Next step
Client targeting cone + arrow animation in client/scripts/animals/; implement BowShotHook when Crafting's Bow/Arrow items land (or ask Crafting to).
