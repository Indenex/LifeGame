# skills-11: Leatherwork and Weaver specialties
Lane: Journal and skills
Owner: Journal and skills thread (session_013QuyGdXAWemhkois2ictmc)
Status: in_progress
Branch: claude/journal-skills-11 (stacked on #112 and on Crafting's #160)
PR: #193 (draft)

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
- 2026-10-16 19:50: Crafting's #160 (d947764) reports build.table / build.bench / build.spindle / build.rack via StationInfo.BuildProcess. Merged #160 into this branch. Skills.AllProcessIds yields one build id per station kind that exists; journal names; how-to lines ("C beside a crafting table to make it"); Things to try lines for the table, spindle station and drying rack; any recipe with a Station waits for that station to be built. Harness: 47 reachable, crafting table always before the basket; server type-checks.
- 2026-10-16 19:50: #112 merged. Merging main here conflicts in #160's own files (Structures.cs, Building.cs, StructureModel.cs), which are for Crafting to resolve on #160; this branch stays stacked on #160 until it merges.

## Open questions / blockers
- (none)

## Next step
When #160 merges (or Crafting updates it with main), merge main here, re-run the harness, push, and mark #193 ready.
