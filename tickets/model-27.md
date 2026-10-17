# model-27: World check of rabbits and hazel bushes
Lane: Modeling
Owner: Modeling lane session
Status: done
Branch: claude/model-27-wildlife
PR: (see below)

## Goal
Check the new rabbit (#723, #727) and hazel bush (#741, #747) at world scale next to a player, a sheep, a hen and a berry bush, and fix what reads wrong.
- Scale is right: the rabbit sits smaller than the hen and well below the sheep, and the hazel clusters read on the bush.
- Found: Sheep.cs gives every animal but the deer a sheep fleece colour on the wool material, so 7 rabbits in 8 come out pale cream, like pet bunnies. Added `RabbitFigure.Coats`, four wild brown-grey agouti coats for `PaintedAnimals.Coat`. Animals switches Sheep.cs to use them for Model == "rabbit" (their file).
- Showcase view `wildlife`: rabbits in today's fleece colours on the left, in RabbitFigure.Coats on the right.
Render: `renders/model-27-wildlife.png`.

## Progress log
- 2026-10-17: rendered; added Coats and the view; build has 0 errors.

## Open questions / blockers
- Animals to use RabbitFigure.Coats in Sheep.cs (asked).

## Next step
Wait for CI on the PR and send the green sha; then start the D-166 variety pass on props near spawn.
