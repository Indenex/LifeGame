# skills-15: Journal lines for new content (bow, arrows, cooking)
Lane: Journal and skills
Owner: Journal and skills lane (Sonnet)
Status: todo
Branch: claude/journal-skills-15
PR: (not yet)

## Goal
Keep the journal's "Things to try" hints (client/scripts/journal/NextSteps.cs, D-105) and how-to lines (Recipes.cs) in step with new processes, one sentence each naming the goal without the recipe. Check with the hint harness (see tickets/skills-11.md Testing) that every `Skills.AllProcessIds()` id is reached.

## Progress log
- 2026-10-16 22:30: main has 59 ids, 57 reached. Missing hints: cook.broth and cook.stew (asked Crafting for lines). sew.wool_tunic already has a line ("Cloth cut and sewn to fit keeps the night's chill off a little."); Animals' wording was "Cloth and yarn from the wool of your sheep can be sewn into something you wear against the cold." (swap if wanted).

- 2026-10-16 22:35: added cook.broth and cook.stew lines (broth says berries, not meat, since the recipe is water + 3 berries). Harness 59/59 both picks.

## Open questions / blockers
- The bow and arrow recipes (lash.bow, carve.arrows) are not in shared/Processes.cs yet. When they land: "A green stick bent and strung with cord will throw a light shaft a long way." (lash.bow), "Straight shafts, shaved smooth and feathered with fibre, fly truer than plain sticks." (carve.arrows); then set the Skills.Shoot prerequisite in NextSteps.After to lash.bow instead of Skills.Kill.
- Tool repair needs no line (Crafting: it is a place you stand at).

## Next step
`git fetch origin && git grep -n 'Id = "lash.bow"\|Id = "carve.arrows"\|Id = "cook.stew"' origin/main -- shared/Processes.cs`; for each id that exists, add its line to NextSteps.Lines (and Recipes.cs how-to if the key is a bare action), run the harness (all ids reached), push, open a small PR.
