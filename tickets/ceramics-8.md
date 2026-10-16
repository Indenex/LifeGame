# ceramics-8: Potter's bench
Lane: Clay and ceramics
Owner: Clay and ceramics thread (session_01Diab34HTvew38VmUaqhj5Q)
Status: in_progress
Branch: claude/clay-ceramics-8-kfr4kr (stacked on crafting-stations #160; contains ceramics-7). One PR for ceramics-7 + -8 after #160 merges (PM).
PR: (none yet)

## Goal
D-112 (Justin): shaping pots needs a Potter's bench inside a claim; clay preparation (clean, grit, grog, temper, brick, daub) stays a hand craft. Done when:
1. B menu has "Potter's bench" (StructureKind.PottersBench, number from Building: likely 8), one stage "Bench": 6 sticks + 2 stones + 4 fiber (Crafting agreed; PM taking it to Justin). Its Shape has `Station = StationKind.PottersBench`; Crafting's framework makes it claim-only to place.
2. clay.pinch_pot, clay.coil_pot and clay.storage_jar carry `Station = StationKind.PottersBench`. Shipped in the same PR as the bench, so pots are never unreachable.
3. Wet pieces are set down beside the bench (Crafting sets job X/Z 0.9 m from the bench toward the player); Ceramics' OnPlaced keeps working.
4. Client: a bench model (client/scripts/ceramics/BenchModel.cs + one StructureModel hook), ClayCheck runs claim, bench, pinch pot.
First fired pot still reachable in about 1 to 2 hours of play.

## Progress log
- 2026-10-16 19:35: ClayCheck adapted (claim, stone axe, log, bench before shaping; 5965db1). PASSED: --pot (c8bench1), default pit run (c8bench2), --kiln --jar (c8bench3); each also checks shaping is refused with no bench. Merged main aa3e443 (#105, #119) and crafting-stations 07fd6d0 (a432ab6): table and bench models by station flag; broth and stew are GoodMeals (D-111). Server, client, ClayCheck, CraftCheck build. Upgrade check vs origin/main (with #105) PASSED.
- 2026-10-16 19:10: 08c862d bench (StructureKind 8 after jar 6 and table 7; D-127 cost 1 log + 2 stones; BenchModel; Station on pinch/coil/jar). 3b1f192: model by station flag, `build.bench` for the journal, CraftJob.VesselQuality (Crafting approved) so pots keep their quality. Upgrade check vs origin/main PASSED (no wipe). #105 merged first as agreed. ClayCheck adaptation (claim, axe, log, bench) in progress by a subagent; WIP 2fe3a89.
- 2026-10-16 18:37: Crafting agreed option 1 (cost) and described the framework (crafting-15, branch crafting-stations, sha in a couple of hours): StationKind enum in Processes.cs, ProcessDef.Station, StructureShape.Station, generic claim check in PlaceStructure, ingredients from inventory, nearby stores, then the claim stash.

## Open questions / blockers
- StructureKind order: jar is 6 (#105); Building's #153 appends Fence, Gate; whichever merges second goes after the other, keeping StructureKind, Structures.Shapes and Structures.StageCosts in the same order. The bench takes the next free number at merge time (and ClayRules has KilnStructureKind/JarStructureKind ints that must match).
- Bench cost decided: 1 log + 2 stones (D-127).

## Next step
When Crafting's #160 merges: merge main into claude/clay-ceramics-8-kfr4kr, build, open one PR (ceramics-7 + ceramics-8), send the PM the green sha. Render request sent at a432ab6.