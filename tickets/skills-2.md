# skills-2: Skills and journal for clay, building and food
Lane: Journal and skills
Owner: Journal and skills thread
Status: done
Branch: claude/journal-skills-iseewg (restarted from main after #7 merged)
PR: #19 (merged 4e6090a), G note reading in #22 (merged 97821e5)

## Goal
Every process that has landed since skills-1 trains a skill and appears in the journal: clay crafting (`clay.*` through Crafting's hooks), pit firing and filling pots (Ceramics), building stages `build.frame`, `build.wattle`, `build.daub`, `build.roof` (Builder, capped), picking berries (Foraging), and cooking (`cook.meat`) when it exists. The journal names each of them and counts them in "still to discover".

## Progress log
- 2026-10-15 22:30: `clay.*` crafting, `clay.pit_fire` and `clay.fill` already reach `OnProcessPerformed`, so they train Potter. Added journal names and "to discover" entries for `gather.berries`, `build.frame`, `build.wattle`, `build.daub`, `build.roof`, `clay.pit_fire` and `clay.fill`. Building's #10 already reports the build stages; asked the PM for Survival's `Forage` reducer to report `gather.berries`.

- 2026-10-15 22:31: draft PR #19 opened. Survival's #12 (on main at 06659df) already reports `gather.berries` (`Needs.ForageProcess`), so berry picking trains Foraging with no further change.
- 2026-10-15 22:35: PM said ship #19 as is; marked ready. CI green on 409a4a8 (run https://github.com/Indenex/LifeGame/actions/runs/38817922812).
- 2026-10-15 22:42: merged main 5adb2c1 (UX Use-key registry #17). Reading a note moved from N to G (Use handler `journal.read_note`, priority 5), offered only for notes that teach something you don't know yet; N binding and the old prompt removed (D-025 done). Client and server build locally.
- 2026-10-15 22:43: #19 had merged at 409a4a8 before 950a7b1 was pushed; merged main 4e6090a and opened a follow-up PR for the G change.

## Open questions / blockers
- Which skill cooking (`cook.*`) trains is with Justin (card open via the PM). Implemented default: Firecraft (survival basic). The alternative, a capped Cook skill, is one new line in `shared/Skills.cs`.

## Next step
Done: merged into main at 97821e5 on 2026-10-15 22:47.