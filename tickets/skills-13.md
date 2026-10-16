# skills-13: Journal lines for the bow, and a skills panel check
Lane: Journal and skills
Owner: Journal and skills lane (Sonnet)
Status: in_progress
Branch: claude/journal-skills-13
PR: (not yet)

## Goal
After skills-12 (#237 merged at 0821b2d3), keep the journal in step with new content: add Crafting's "Things to try" lines for lash.bow and carve.arrows once those process ids exist on main (#266), with how-to lines, and make the Archery hint depend on lash.bow instead of hunting. Repair needs no line (Crafting: it is a place you stand at).

## Progress log
- 2026-10-16 21:56: created; waiting on #266 for the ids.

- 2026-10-16 22:05: Justin picked: drop the "Speed N%  Best quality" line from learned-step cards (rank title already says it). Done in JournalPanel.EntryRow; Godot client build only in CI.

## Open questions / blockers
- Needs lash.bow and carve.arrows in shared/Processes.cs on main (#266, Crafting).

## Next step
When `git grep lash.bow origin/main -- shared/Processes.cs` finds it: add NextSteps lines "A green stick bent and strung with cord will throw a light shaft a long way." (lash.bow) and "Straight shafts, shaved smooth and feathered with fibre, fly truer than plain sticks." (carve.arrows), prereqs (bow needs cordage and a stick; arrows need the knife), set Skills.Shoot prereq to lash.bow, run the hint harness (see skills-11 Testing notes), push, CI.
