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

## Open questions / blockers
- (none)

## Next step
Open a draft PR (stacked on #112 and #160), wait for CI; when #112 and #160 merge, merge main and mark ready.
