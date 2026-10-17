# model-7: Hen and its items
Lane: Modeling
Owner: Modeling session (session_01JKX3FNjkrEkAqoTaFJiXAm)
Status: in_progress
Branch: claude/model-7-hen
PR: (opening)

## Goal
Justin picked the hen (Animals, animals-19). A hen figure the herd code can draw in place of the small-sheep stand-in, able to peck, flap, walk and sit, plus item models for Egg (207), Feather (211), raw and cooked chicken, Boiled egg (212) and Burnt egg (213, Crafting's crafting-24). Done when Animals draws hens with `HenFigure` and the items show in hand, on the ground and in the inventory.

## Progress log
- 2026-10-17 03:55: `HenFigure` (client/scripts/models/HenFigure.cs), on the sheep's bone layout so Sheep.cs drives it: body, fleece (tail tuft; keep scale 1), head (pitch down about X to peck), leg0/leg1 the legs, leg2/leg3 the wings (about Z, outward, to flap). Surface 0 feathers tinted by the per-instance material (the sheep fleece tints give white, grey, brown hens), surface 1 comb, wattle, beak, eyes, yellow legs and toes. ~0.42 m tall. Showcase `hen`: three colours, a peck and a flap beside a sheep.
- 2026-10-17 04:10: Item models in ItemModels: Egg (brown, on its side), Feather (quill and vanes), Raw/Roast/Burnt chicken (drumstick on a knuckled bone), Boiled egg 212 (shelled white) and Burnt egg 213 (black, cracked) by number until crafting-24 lands. Showcase `items --ids=207,208,...` shows only those.
- 2026-10-17 04:45: Hen coop for Animals' #475 (D-265) on branch claude/model-7-coop @ ca3185c7, built on #475's head: woven wattle house on four short legs inside the 1.6 x 1.2 m footprint, framed pop-hole at +Z with a stick ramp, ridge pole on crossed gable sticks, gable infill and deep thatch (stage 1). Showcase `coop`. Animals to merge it into #475 or I PR after #475.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the model-7 PR; send the PM the green sha; Animals swaps Sheep.cs's small-sheep stand-in for HenFigure.Create.
