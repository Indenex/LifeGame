# model-7: Hen and its items
Lane: Modeling
Owner: Modeling session (session_01JKX3FNjkrEkAqoTaFJiXAm)
Status: in_progress
Branch: claude/model-7-hen
PR: (not yet)

## Goal
Justin picked the hen (Animals, animals-19). A hen figure the herd code can draw in place of the small-sheep stand-in, able to peck, flap, walk and sit, plus item models for Egg (207), Feather (211), raw and cooked chicken, Boiled egg (212) and Burnt egg (213, Crafting's crafting-24). Done when Animals draws hens with `HenFigure` and the items show in hand, on the ground and in the inventory.

## Progress log
- 2026-10-17 03:55: `HenFigure` (client/scripts/models/HenFigure.cs), on the sheep's bone layout so Sheep.cs drives it: body, fleece (tail tuft; keep scale 1), head (pitch down about X to peck), leg0/leg1 the legs, leg2/leg3 the wings (about Z, outward, to flap). Surface 0 feathers tinted by the per-instance material (the sheep fleece tints give white, grey, brown hens), surface 1 comb, wattle, beak, eyes, yellow legs and toes. ~0.42 m tall. Showcase `hen`: three colours, a peck and a flap beside a sheep.

## Open questions / blockers
- (none)

## Next step
Item models: egg, feather, raw and cooked chicken, boiled and burnt egg (find how ItemModels registers item meshes; follow docs/CONTENT.md); then open the PR and send Animals the green sha.
