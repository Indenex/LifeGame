# ceramics-8: Potter's bench
Lane: Clay and ceramics
Owner: Clay and ceramics thread (session_01Diab34HTvew38VmUaqhj5Q)
Status: blocked
Branch: (to be created, stacked on Crafting's `crafting-stations` / crafting-15)
PR: (none yet)

## Goal
D-112 (Justin): shaping pots needs a Potter's bench inside a claim; clay preparation (clean, grit, grog, temper, brick, daub) stays a hand craft. Done when:
1. B menu has "Potter's bench" (StructureKind.PottersBench, number from Building: likely 8), one stage "Bench": 6 sticks + 2 stones + 4 fiber (Crafting agreed; PM taking it to Justin). Its Shape has `Station = StationKind.PottersBench`; Crafting's framework makes it claim-only to place.
2. clay.pinch_pot, clay.coil_pot and clay.storage_jar carry `Station = StationKind.PottersBench`. Shipped in the same PR as the bench, so pots are never unreachable.
3. Wet pieces are set down beside the bench (Crafting sets job X/Z 0.9 m from the bench toward the player); Ceramics' OnPlaced keeps working.
4. Client: a bench model (client/scripts/ceramics/BenchModel.cs + one StructureModel hook), ClayCheck runs claim, bench, pinch pot.
First fired pot still reachable in about 1 to 2 hours of play.

## Progress log
- 2026-10-16 18:37: Crafting agreed option 1 (cost) and described the framework (crafting-15, branch crafting-stations, sha in a couple of hours): StationKind enum in Processes.cs, ProcessDef.Station, StructureShape.Station, generic claim check in PlaceStructure, ingredients from inventory, nearby stores, then the claim stash.

## Open questions / blockers
- StructureKind order: jar is 6 (#105); Building's #153 appends Fence, Gate; whichever merges second goes after the other, keeping StructureKind, Structures.Shapes and Structures.StageCosts in the same order. The bench takes the next free number at merge time (and ClayRules has KilnStructureKind/JarStructureKind ints that must match).
- Blocked on Crafting's crafting-stations sha (they ping). StructureKind number from Building (Crafting asked them: jar 6, Table 7, Bench 8, ...).
- PM/Justin: bench cost (option 1 recommended, sent by Crafting).

## Next step
When Crafting sends the crafting-stations sha: branch from it (merging in claude/clay-ceramics-7-kfr4kr if not on main yet), add the bench kind/shape/cost/model, set Station on the three shaping processes, extend ClayCheck with a claim + bench.
