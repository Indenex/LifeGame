# animals-22: Wild rabbit
Lane: Animals
Owner: Animals thread
Status: in_review
Branch: claude/animals-rabbit-5gkntn
PR: (to open)

## Goal
The PM's default next animal (D-323; Justin asleep, so confirm with him in the morning): a wild rabbit as a data entry (D-206). Three warrens at the meadow edges ((18,14) 4 rabbits, (-22,-12) 3, (50,-8) 4), small, quick and skittish (run 7.5 m/s, notice 18 m, wary within 6 m). A light blow brings one down (`AnimalDef.DamageTaken` 4, so a hit counts 4x). It gives raw rabbit (item 214) and a scrap of fur (217); roast rabbit 215 feeds 0.2 and burnt 216 0.05 (D-075: a small meal, below chicken 0.3 and mutton 0.4); `cook.rabbit` comes from the RoastDef. Drawn with Modeling's RabbitFigure (`Model == "rabbit"`, #727): one hop per gait cycle (`RabbitFigure.Hop`), a 0.6 m hop.

## Progress log
- 2026-10-17: built; server and client build; a fresh world spawns 11 rabbits; upgrade check passes (new species flocks appear on an existing world).

- 2026-10-17: Modeling's rabbit (#727) merged; wired into Sheep.cs (mesh, head and hips, hop gait, tail bone full size); the temporary Def.Size knob removed.

## Open questions / blockers
- Needs Modeling's rabbit model (Pebble's approved look; hop and run), Audio (squeak when hurt, alarm thump), a Journal line (added: cook.rabbit). Crafting: fur has no use yet; a snare trap recipe was proposed.
- Numbers for the PM: food 0.2 / 0.05; run 7.5; the three warrens; DamageTaken 4.

## Next step
Wait for CI, send the green sha to the PM; ask Modeling for the rabbit and Audio for its calls.
