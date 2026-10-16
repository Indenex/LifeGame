# skills-10: Things to try (M2, the first evening)
Lane: Journal and skills
Owner: Journal and skills thread (session_013QuyGdXAWemhkois2ictmc)
Status: in_review
Branch: claude/journal-skills-10 (stacked on claude/journal-skills-iseewg, PR #43)
PR: #112 (draft, stacked on #43)

## Goal
D-081 M2: a new player always has a next goal. Under the journal's discoveries, a short "Things to try" list shows up to three gentle hints for things the player hasn't discovered yet but could reach from what they already know (for example, after fire: "Wet clay hardens in a fire. Clay hides in low, damp ground."). Hints name the goal without spelling out the recipe (D-063, light on spoilers), lowest tier first, and update as you learn.

Also: `chop.log` (main) trained no skill and its journal line said "C to make it"; chopping is the Use key at a tree with an axe.

## How
- `client/scripts/journal/NextSteps.cs`: one hint line per process; "reachable" = every ingredient is lying around (no process makes it) or made by something you know; tools likewise; non-recipe steps (pit firing, kiln, building stages, animals) list what they follow from.
- `JournalPanel`: a "Things to try" block under the discoveries.
- `Recipes.HowTo` checks its hand-written sentences before the recipe text, so chopping reads right.

## Progress log
- 2026-10-16 17:50: PM was offered 4 options (hints / visible gains / road-ahead checklist / hints then gains); started option 1 as the stated default. Branch made from #43's head plus main (merged cleanly).
- 2026-10-16 18:21: Justin (via PM): D-104 chopping trains Foraging; D-105 skills-10 is option 1, Things to try. Ask each lane for one hint line per new process. Green shas go to Integration (D-095).
- 2026-10-16 18:19: CI green on 131e27d (run https://github.com/Indenex/LifeGame/actions/runs/38942933136; Godot client builds Linux and Windows); PR marked ready, sha sent to the PM.
- 2026-10-16 18:08: draft PR #112 opened; render requested.
- 2026-10-16 18:05: NextSteps.cs + panel block + chop fixes written. Hints follow D-081's road (fire, food, shelter, fired pot): road steps past the furthest one reached first, skipped ones after, one slot kept for something off the road. Logic checked outside Godot with a .NET harness against shared/: every one of the 42 processes has a hint, and following the hints (first or third each time) reaches all 42. Godot client not built locally (no Godot/bindings here); CI builds it. `chop` mapped to Foraging (default).

## Open questions / blockers
- (none)

## Next step
Wait for the PM to merge #43 then #112 (green on 131e27d) ; then start the next ticket the PM gives.
