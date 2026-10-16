# skills-10: Things to try (M2, the first evening)
Lane: Journal and skills
Owner: Journal and skills thread (session_013QuyGdXAWemhkois2ictmc)
Status: in_review
Branch: claude/journal-skills-10 (was stacked on #43, merged 18:26)
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
- 2026-10-16 19:20: No CI had run since 6911bc0: #112 conflicted with main (GitHub 'dirty'). Merged main (c09f9b8), no manual conflicts. Main brought weave.mat (sleeping mat) and clay.storage_jar (kiln-only, D-076): hint lines added; the jar waits for coil pot and kiln. Harness: all 45 reachable; server type-checks. D-143 wipe window: nothing to fold in (no schema change).
- 2026-10-16 19:05: Crafting asked (D-127) for hints along the new first-pot path: stone axe, log, claim marker, potter's bench, pinch pot. Road reordered to fire, claim, food/shelter, stone axe, log, bench, clay, pot. Claiming and the bench report no process ids of their own yet (a claim marker counts as build.frame), so NextSteps uses placeholder ids `claim.mark` and `build.bench`: quiet until they appear in Skills.AllProcessIds, and a requirement on a missing id is ignored. Harness checked both ways (ids absent: 43 reached; ids present: 45 reached, pot only after the bench).
- 2026-10-16 18:45: Survival asked (D-123) for Endurance: new basic skill, prefix `move`, `move.run` named "Running" (fed every 20 s of running from Survival's stamina, claude/survival-arrival). Added to shared/Skills.cs, how-to line and Things to try line (key read from the `run` action). Running can't be learned by watching: Skills.Watchable, checked in Knowledge.TeachOnlookers. Server module type-checks locally (dotnet build without the wasi target); harness green.
- 2026-10-16 18:28: Animals' hint lines in: hides, shearing, hunting, butchering replaced with theirs; drop spindle, spinning, wool cloth and sewing added (quiet until animals-9 lands). Harness still reaches all 42.
- 2026-10-16 18:21: Justin (via PM): D-104 chopping trains Foraging; D-105 skills-10 is option 1, Things to try. Ask each lane for one hint line per new process. Green shas go to Integration (D-095).
- 2026-10-16 18:19: CI green on 131e27d (run https://github.com/Indenex/LifeGame/actions/runs/38942933136; Godot client builds Linux and Windows); PR marked ready, sha sent to the PM.
- 2026-10-16 18:08: draft PR #112 opened; render requested.
- 2026-10-16 18:05: NextSteps.cs + panel block + chop fixes written. Hints follow D-081's road (fire, food, shelter, fired pot): road steps past the furthest one reached first, skipped ones after, one slot kept for something off the road. Logic checked outside Godot with a .NET harness against shared/: every one of the 42 processes has a hint, and following the hints (first or third each time) reaches all 42. Godot client not built locally (no Godot/bindings here); CI builds it. `chop` mapped to Foraging (default).

## Open questions / blockers
- Claims and Ceramics to report `claim.mark` (marker finished) and `build.bench` (bench finished) via OnProcessPerformed; then add both to Skills.AllProcessIds. Asked via Crafting and the PM, 19:05.

## Next step
Wait for CI on #112's head (runners backed up), then send the PM the green sha; the PM merges #112 (green on 131e27d) ; then start the next ticket the PM gives.
