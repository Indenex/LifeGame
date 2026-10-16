# survival-9: Endurance, a skill trained by running
Lane: Survival
Owner: Survival thread (session_014pHdaxJ7uESzsvWekpjvsH)
Status: in_progress
Branch: claude/survival-arrival (with survival-8; stacked on #119)
PR: (with survival-8, after #119 merges)

## Goal
D-123: stamina (D-111) grows with a new basic skill, Endurance, trained by running. Run 8 s / refill 12 s at rank 0, up to 16 s / 7 s at Master.

## Design
- Client counts seconds actually run; every Stamina.RunPerUse (20 s) it calls the `Ran` reducer. Server ignores reports closer than 18 s apart (private RunReport table), then calls OnProcessPerformed("move.run"), which journals it and trains the skill whose prefix matches.
- Master after ~800 uses = ~4.4 h of running (Skills.Gain curve). RunPerUse is a default; tell the PM if it should change.
- Client rank: SurvivalClient.StaminaRank() reads my_skills for spec "endurance" (0 until it exists).
- Journal and skills (session_013QuyGdXAWemhkois2ictmc) adds the Specialization ("endurance", "Endurance", basic, prefix "move"), a name for "move.run", and a "Things to try" line.

## Progress log
- 2026-10-16: built Ran reducer, RunReport table, client reporting and rank lookup. Asked Journal and skills for the Specialization line. Not compiled locally (no dotnet).

- 2026-10-16: Journal and skills added Endurance in PR #112 (claude/journal-skills-10): spec "endurance" prefix "move", Skills.Run named Running, how-to and Things to try lines; onlookers don't learn move.* (Skills.Watchable). No change needed here.

## Open questions / blockers
- Endurance trains only once #112 is on main; if this PR lands first, running is journaled but trains nothing until then (harmless). If needed, copy #112's Specializations line verbatim.

## Next step
After #119 merges: merge main, open the survival-8/9 PR, wait for CI, send the green sha to the PM.
