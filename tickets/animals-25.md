# animals-25: Coat variety per species (D-166)
Lane: Animals
Owner: Animals thread
Status: in_review
Branch: claude/animals-variety-5gkntn
PR: (to open)

## Goal
Hens and rabbits drew with the sheep fleece palette (cream, oatmeal, the odd dark), so rabbits were cream and every hen the same. They now take coats from per-model palettes in `Sheep.cs` (`Palettes`): hens mostly cream and white with some grey, brown and tan; rabbits take Modeling's `RabbitFigure.Coats` (wild agouti browns, #755) painted as coats, the way the deer's are. Sheep and deer are unchanged. Each animal picks its coat by id, so a flock is mixed. Client only.

## Progress log
- 2026-10-17: built; client builds. Not seen in Godot; a Render still of hens and rabbits is the check.

- 2026-10-17: PM and Modeling agreed one source of rabbit colours: `RabbitFigure.Coats` (Modeling's #755, merged into this branch); my interim rabbit palette is gone.

## Open questions / blockers
- The colours are my pick (natural ranges); Justin may want different ones.

## Next step
Wait for CI, send the green sha to the PM.
