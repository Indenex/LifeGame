# model-24: Item models keyed by enum name
Lane: Modeling
Owner: Modeling lane session
Status: in_review
Branch: claude/model-24-enum-ids
PR: (see below)

## Goal
Every item that was keyed by number until it reached main is on main now, so ItemModels, PaintedItems and Showcase name them:
- Bow and Arrow (16, 17), Spade (18)
- DropSpindle, Yarn, WoolCloth (197 to 199)
- RawVenison, DeerHide, Antler (202 to 204)
- RawRabbit, CookedRabbit, BurntRabbit, RabbitFur (214 to 217)
There's no change in behaviour: the same ids give the same models.

## Progress log
- 2026-10-17: swapped the numbers for enum names and removed the "by number" comments; build has 0 errors.

## Open questions / blockers
- (none)

## Next step
Wait for CI on the PR, then send the green sha to the PM.
