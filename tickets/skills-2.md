# skills-2: Skills and journal for clay, building and food
Lane: Journal and skills
Owner: Journal and skills thread
Status: in_review
Branch: claude/journal-skills-iseewg (restarted from main after #7 merged)
PR: #19

## Goal
Every process that has landed since skills-1 trains a skill and appears in the journal: clay crafting (`clay.*` through Crafting's hooks), pit firing and filling pots (Ceramics), building stages `build.frame`, `build.wattle`, `build.daub`, `build.roof` (Builder, capped), picking berries (Foraging), and cooking (`cook.meat`) when it exists. The journal names each of them and counts them in "still to discover".

## Progress log
- 2026-10-15 22:30: `clay.*` crafting, `clay.pit_fire` and `clay.fill` already reach `OnProcessPerformed`, so they train Potter. Added journal names and "to discover" entries for `gather.berries`, `build.frame`, `build.wattle`, `build.daub`, `build.roof`, `clay.pit_fire` and `clay.fill`. Building's #10 already reports the build stages; asked the PM for Survival's `Forage` reducer to report `gather.berries`.

- 2026-10-15 22:31: draft PR #19 opened. Survival's #12 (on main at 06659df) already reports `gather.berries` (`Needs.ForageProcess`), so berry picking trains Foraging with no further change.
- 2026-10-15 22:35: PM said ship #19 as is; marked ready.

## Open questions / blockers
- Which skill cooking (`cook.*`) trains is with Justin (card open via the PM). Implemented default: Firecraft (survival basic). The alternative, a capped Cook skill, is one new line in `shared/Skills.cs`.

## Next step
Wait for CI on PR #19, send the green head sha to the PM (who merges), then work tickets/skills-3.md.
