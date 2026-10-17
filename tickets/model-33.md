# model-33: Rabbit snare
Lane: Modeling
Owner: Modeling lane (session_01JKX3FNjkrEkAqoTaFJiXAm)
Status: in_review
Branch: claude/model-33-rabbit-snare
PR: #818

## Goal
Animals' animals-34 rabbit snare (their item id 218): a cord noose tied to two small pegged sticks, in the painted item look at real scale (hazel pegs ~20 cm, noose ~12 cm across). In ItemModels:
- `SnareItem` (an `Item?` hook like BowItem): Animals sets it to their item id and the ground, hand and inventory look is the bundled snare (two pegs and the crossbar with the cord wound round, the noose laid flat). `SnareBundled` is that mesh on its own.
- `SnareSet`: the snare pegged across a run: two pegs 16 cm apart, a crossbar lashed across their tops, the noose hanging from its middle with its bottom ~4 cm above the grass, facing along Z. Origin is the ground under the middle of the run (pegs' feet at y = 0).
- `SnareSprung` and `SnareSprungWith(fur)`: the noose drawn out tight to `SnareNeck`, and a node with the RabbitFigure lying on its side there (fur = the rabbit's coat material).
Showcase view `snare` (and `--side`). Renders: renders/model-33-snare.png, model-33-snare-side.png.

## Progress log
- 2026-10-17 14:45: built; Make split into Recipe + Assemble (keepOrigin for the placed snare). Local build 0 errors.

## Open questions / blockers
- (none)

## Next step
Wait for CI, then send the green sha to the PM for Animals.
