# skills-17: Journal lines for the bow recipes
Lane: Journal and skills
Owner: Journal and skills lane (Sonnet)
Status: in_review
Branch: claude/journal-skills-17
PR: (see below)

## Goal
Crafting's bow recipes (#389) are on main: add their 'Things to try' hints (D-105) and make the Archery hint depend on having a bow and arrows instead of hunting.

## Progress log
- 2026-10-17 02:30: lash.bow and carve.arrows lines added (Crafting's wording); Skills.Shoot now needs both. Hint harness 66/66 on both pick paths. Both recipes train Toolmaking through their lash and carve families (shared formula, D-175); animal.shoot trains Archery.

## Open questions / blockers
- Clay trough (#417) not on main yet: when its process id lands (`git grep trough origin/main -- shared/Processes.cs`), add one line.
- PM's pick on tool repair / wading skill homes (options A, A+B, C) still open.

## Next step
Wait for CI on the PR, send the green sha to the PM; then the trough line when #417 merges.
