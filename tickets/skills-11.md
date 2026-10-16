# skills-11: Leatherwork and Weaver specialties
Lane: Journal and skills
Owner: Journal and skills thread (session_013QuyGdXAWemhkois2ictmc)
Status: in_progress
Branch: claude/journal-skills-11 (stacked on claude/journal-skills-10, PR #112)
PR: (once opened)

## Goal
D-065 and D-071 promised capped specialties for hide work and wool work once stations made them real (#160 stations, #90 wool and clothing). Two new capped skills, sharing the 300-rank cap with Potter and Builder:
- Leatherwork: scraping, stretching and drying hides (`hide.*`), and sewing leather wraps.
- Weaver: spinning wool (`cord.spin_wool`), weaving wool cloth (`weave.wool_cloth`) and sewing clothes (`sew.*`).
Husbandry keeps shearing, hunting and butchering; Toolmaking keeps cordage, baskets, mats and the drop spindle (making a tool). "Things to try" also learns the stations: building a crafting table, spindle, drying rack and potter's bench, and recipes made at a station wait for it.

## How
- `shared/Skills.cs`: two Specializations (Basic = false); `SpecOverrides` for processes whose prefix belongs to another skill (`cord.spin_wool`, `weave.wool_cloth` -> Weaver; `sew.leather_wraps` -> Leatherwork). Every caller goes through `SpecFor`, so server gains, quality caps and speed all follow.
- Points already earned stay where they were (Husbandry, Toolmaking); new work trains the new skills. No schema change, no wipe.
- Station hints: need #160 merged (ProcessDef.Station) and each station's build to report its own process id (today StageProcess reports build.frame).

## Progress log
- 2026-10-16 19:35: PM assigned skills-11 (D-145 never idle). Specialties and overrides done; harness shows the mapping; server type-checks.
- 2026-10-16 19:37: D-153 (Justin): foot wraps train Leatherwork, drop spindle stays Toolmaking, as built.

## Open questions / blockers
- Station build ids: asked Crafting (owner of #160) to have finishing a station report `build.<station>` (build.table, build.bench, build.spindle, build.rack).

## Next step
When #160 merges: merge main, add station requirements and station hints to NextSteps; open the PR (stacked on #112 until it merges).
