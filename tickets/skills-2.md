# skills-2: Skills and journal for clay, building and food
Lane: Journal and skills
Owner: Journal and skills thread
Status: in_progress
Branch: claude/journal-skills-iseewg (restarted from main after #7 merged; local branch `skills-2` until then)
PR: (none yet)

## Goal
Every process that has landed since skills-1 trains a skill and appears in the journal: clay crafting (`clay.*` through Crafting's hooks), pit firing and filling pots (Ceramics), building stages `build.frame`, `build.wattle`, `build.daub`, `build.roof` (Builder, capped), picking berries (Foraging), and cooking (`cook.meat`) when it exists. The journal names each of them and counts them in "still to discover".

## Progress log
- 2026-10-15 22:30: `clay.*` crafting, `clay.pit_fire` and `clay.fill` already reach `OnProcessPerformed`, so they train Potter. Added journal names and "to discover" entries for `gather.berries`, `build.frame`, `build.wattle`, `build.daub`, `build.roof`, `clay.pit_fire` and `clay.fill`. Building's #10 already reports the build stages; asked the PM for Survival's `Forage` reducer to report `gather.berries`.

## Open questions / blockers
- Waiting on Survival (Survival.cs `Forage`) to call `OnProcessPerformed(..., "gather.berries", ...)`, requested through the PM 2026-10-15 22:30.
- `cook.meat` doesn't exist yet. Its prefix needs a skill: default is Foraging (survival basic) unless Justin wants a Cook skill.

## Next step
Once #7 merges, restart the branch from main, cherry-pick the local `skills-2` commits, push and open the skills-2 PR.
