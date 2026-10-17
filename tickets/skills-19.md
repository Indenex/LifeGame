# skills-19: Journal lines for new content as it lands (eggs, fletched arrows, waterskin, trough)
Lane: Journal and skills
Owner: Journal and skills lane (Sonnet)
Status: todo
Branch: claude/journal-skills-19
PR: (not yet)

## Goal
Add 'Things to try' hints (D-105, one sentence, goal not recipe) to client/scripts/journal/NextSteps.cs (and Recipes.cs how-to where the key is a bare action) for processes that other lanes have announced but that are not on main yet. Each line goes in only once its id exists in shared/Processes.cs on main, so the hint harness (tickets/skills-11.md Testing) stays at "every id reached".

## Queued lines (wait for the id on main)
- cook.egg (Crafting, PR crafting-eggs after Animals' #454): "An egg set in the embers comes out firm and good to eat. Left too long it blackens."
- carve.fletched_arrows (Crafting): "Feathers on an arrow's tail keep it flying true." (plain carve.arrows still works; Archery needs a bow and arrows.)
- hide.waterskin (Survival, PR #484, survival-20): "A hide can be sewn shut to carry a few mouthfuls of water." Unlock after the player has a sheep hide (the harness handles this through the recipe's Consumes); tier 2 or 3 (hand craft, sheep hide + cordage with a cutting tool). Trains Leatherwork through the "hide" family.
- carve.spade (Crafting, PR from crafting-shovel; a log, a stone and cordage with a cutting tool, Toolmaking by the carve family): "A flat of wood with a stone edge moves earth faster than a pointed stick."
- clay trough (#417, Ceramics): one line once its process id exists.

## Progress log
- 2026-10-17 03:30: ticket created; none of the four ids is on main yet.

- 2026-10-17 03:55: #484 merged: added hide.waterskin (Survival wording). Also the harness found animal.eggs (Animals, Eggs.cs; built-in action already in Skills.cs, Husbandry) without a hint: added a line + how-to. Harness 72/72. Still waiting: cook.egg, carve.fletched_arrows, carve.spade, clay trough.

- 2026-10-17 04:00: #470 merged: added cook.egg and carve.fletched_arrows (Crafting wording). Harness 74/74. Waiting only on carve.spade and the clay trough.

## Open questions / blockers
- Waiting on #454/crafting-eggs, #484, #417 to merge.

## Next step
`git fetch origin && git grep -n 'Id = "cook.egg"\|Id = "carve.fletched_arrows"\|Id = "hide.waterskin"\|trough' origin/main -- shared/Processes.cs`; for each id present add its line, run the hint harness, push, open one small PR, send the green sha to the PM.
