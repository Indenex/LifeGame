# skills-10: Things to try (M2, the first evening)
Lane: Journal and skills
Owner: Journal and skills thread (session_013QuyGdXAWemhkois2ictmc)
Status: in_progress
Branch: claude/journal-skills-10 (stacked on claude/journal-skills-iseewg, PR #43)
PR: (once opened)

## Goal
D-081 M2: a new player always has a next goal. Under the journal's discoveries, a short "Things to try" list shows up to three gentle hints for things the player hasn't discovered yet but could reach from what they already know (for example, after fire: "Wet clay hardens in a fire. Clay hides in low, damp ground."). Hints name the goal without spelling out the recipe (D-063, light on spoilers), lowest tier first, and update as you learn.

Also: `chop.log` (main) trained no skill and its journal line said "C to make it"; chopping is the Use key at a tree with an axe.

## How
- `client/scripts/journal/NextSteps.cs`: one hint line per process; "reachable" = every ingredient is lying around (no process makes it) or made by something you know; tools likewise; non-recipe steps (pit firing, kiln, building stages, animals) list what they follow from.
- `JournalPanel`: a "Things to try" block under the discoveries.
- `Recipes.HowTo` checks its hand-written sentences before the recipe text, so chopping reads right.

## Progress log
- 2026-10-16 17:50: PM was offered 4 options (hints / visible gains / road-ahead checklist / hints then gains); started option 1 as the stated default. Branch made from #43's head plus main (merged cleanly).

## Open questions / blockers
- Which option for skills-10 (sent to the PM 17:44): default option 1, "Things to try".
- `chop.log` skill: default Foraging (felling and gathering wood); sent to the PM as a choice.

## Next step
Write client/scripts/journal/NextSteps.cs and the panel block; push; ask Render for a journal shot.
