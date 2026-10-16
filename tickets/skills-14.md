# skills-14: Skills reference process ids, not name prefixes (CONTENT.md Skill row)
Lane: Journal and skills
Owner: Journal and skills lane (Sonnet)
Status: in_review
Branch: claude/journal-skills-14
PR: (not yet)

## Goal
Behaviour-preserving cleanup (PM approved option 1, nothing visible to players): a skill names what trains it (exact process ids first, else a recipe family), `SpecOverrides` and the `animal.`/`stalk`/`shoot` prefix workarounds are gone, and the built-in actions (dig, forage, hunt, shoot, build stages...) live in one table with their journal names and order instead of ExtraNames + AllProcessIds + BuildStages.

## Progress log
- 2026-10-16 22:20: done. Proved identical: a probe printing id -> skill -> journal name for all 62 ids (every AllProcessIds entry plus animal.*, sew.*, hide.*, bogus ids) is byte-identical before and after. Server type-check OK, tools/ProgressionCheck OK. Hint harness: 56/58 reached both before and after; cook.broth and cook.stew have no hint line on main (not from this change).

## Open questions / blockers
- cook.broth and cook.stew (Crafting) need "Things to try" lines (D-105); asked Crafting.
- Last conversion step (ProcessDef carries its skill) touches Crafting's Processes.cs; left for later.

## Next step
Wait for CI on the PR, send the green sha to the PM.
