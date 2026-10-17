# model-33: Rabbit snare
Lane: Modeling
Owner: Modeling lane (session_01JKX3FNjkrEkAqoTaFJiXAm)
Status: done
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

- 2026-10-17 14:35: 8b2ec1b4 green and mergeable, CI https://github.com/Indenex/LifeGame/actions/runs/39041104853 (the red "Server module" on 77b5c627 is a cancelled superseded run). Green sha sent to the PM for Animals; still for Justin is renders/model-33-snare.png.

## Open questions / blockers
- Waiting on the PM to merge. Animals then sets `ItemModels.SnareItem` to their item 218 and places SnareSet / SnareSprungWith (their files).

## Next step
Watch PR #818: if main moves and it conflicts, merge main into claude/model-33-rabbit-snare, build (0 errors), push, and send the new green sha to the PM. Answer any feedback from Animals on the snare's scale or the sprung pose.
