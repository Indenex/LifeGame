# skills-16: Journal coverage after the big merges
Lane: Journal and skills
Owner: Journal and skills lane (Sonnet)
Status: in_review
Branch: claude/journal-skills-16
PR: (see below)

## Goal
PM check-in 2026-10-17: journal 'Things to try' hints (D-105) for what merged since skills-15, and a check that every new process trains a skill through the one shared formula (D-175).

## Progress log
- 2026-10-17 01:55: audit on main 2040436: 64 process ids, hint harness 62/64. Missing: cook.venison and hide.scrape_deer. Lines added; harness 64/64 both picks. Every real process id maps to a skill (cook -> Firecraft, hide -> Leatherwork) so gains go through Skills.Gain; only claim.mark has no skill (known: it is a first-evening step).
- Placing crafted things (#412): placement is the end of existing recipes (pots, benches), no new process id, nothing to add.
- Build menu (#334) and wading/floating (#414): no process ids; UI and movement only.
- Durability/repair (#267): building the repair table reports build.table (shared with the crafting table, already hinted). Repairing a tool (Durability.RepairTool) reports no process, so it trains no skill.

## Open questions / blockers
- No skill home: tool repair, wading/floating (see PM message).
- Not on main yet: bow recipes (#389), clay trough (#417). Their lines are queued in tickets/skills-15.md (lash.bow, carve.arrows) and need the trough id when it lands.

## Next step
When #389 / #417 land on main: `git grep -n 'Id = "lash.bow"\|Id = "carve.arrows"\|trough' origin/main -- shared/Processes.cs`, add the hint lines from tickets/skills-15.md plus one for the trough, set Skills.Shoot's prerequisite in NextSteps.After to lash.bow, run the hint harness (tickets/skills-11.md Testing), push, PR.
