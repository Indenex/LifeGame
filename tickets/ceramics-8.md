# ceramics-8: Potter's bench
Lane: Clay and ceramics
Owner: Clay and ceramics lane (handed off 2026-10-16 ~20:00 from Opus session_01Diab34HTvew38VmUaqhj5Q to a fresh Sonnet session, D-159)
Status: in_progress
Branch: claude/clay-ceramics-8-kfr4kr (stacked on crafting-stations #160; contains ceramics-7). One PR for ceramics-7 + -8 after #160 merges (PM).
PR: (opening 2026-10-16 20:45, see progress log)

## Goal
D-112 (Justin): shaping pots needs a Potter's bench inside a claim; clay preparation (clean, grit, grog, temper, brick, daub) stays a hand craft. Done when:
1. B menu has "Potter's bench" (StructureKind.PottersBench, number from Building: likely 8), one stage "Bench": 6 sticks + 2 stones + 4 fiber (Crafting agreed; PM taking it to Justin). Its Shape has `Station = StationKind.PottersBench`; Crafting's framework makes it claim-only to place.
2. clay.pinch_pot, clay.coil_pot and clay.storage_jar carry `Station = StationKind.PottersBench`. Shipped in the same PR as the bench, so pots are never unreachable.
3. Wet pieces are set down beside the bench (Crafting sets job X/Z 0.9 m from the bench toward the player); Ceramics' OnPlaced keeps working.
4. Client: a bench model (client/scripts/ceramics/BenchModel.cs + one StructureModel hook), ClayCheck runs claim, bench, pinch pot.
First fired pot still reachable in about 1 to 2 hours of play.

## Handoff (read first)
- State: code complete and checked for ceramics-7 (cooking pot) + ceramics-8 (Potter's bench); NOT yet a PR. The PM decided one PR for both, opened only after Crafting's PR #160 (branch `crafting-stations`) merges to main. #105 (storage jar, ceramics-6) is merged.
- Branch layout: `claude/clay-ceramics-8-kfr4kr` = crafting-stations + ceramics-7 + ceramics-8 (open the PR from here). `claude/clay-ceramics-7-kfr4kr` is an older subset; ignore it. `claude/clay-ceramics-9-kfr4kr` (lamp) is stacked on -8 (see tickets/ceramics-9.md).
- StructureKind order everywhere (enum, Structures.Shapes, Structures.StageCosts): StorageJar 6, CraftingTable 7, PottersBench 8. House building's #153 (Fence, Gate) and anything else append after whatever is on main when they merge; never renumber a kind already on main.
- Setup in a fresh container: install .NET 8, spacetimedb-cli 2.12 and Godot 4.6.2 mono as in .github/workflows/build.yml. The sandbox may refuse tools/gen-bindings.sh (its rm -rf); if client/module_bindings doesn't exist yet, run `cd server && spacetimedb-cli generate --lang csharp --out-dir ../client/module_bindings --module-path ./spacetimedb`. Local server: `spacetimedb-cli start --listen-addr 127.0.0.1:3000`, publish to a scratch db with `cd server && spacetimedb-cli publish <db> --server local --yes`.
- Checks: `cd tools/ClayCheck && dotnet run -c Release -- http://127.0.0.1:3000 <db> [--pot | --kiln --jar | --lamp]` (each ~13 min; a fresh db each run). `tools/upgrade-check.sh origin/main` for the no-wipe check.
- Awaited from other lanes: Crafting (session_01Y2sA7pDBFch57bWb1uKMTq) #160 merge; crafting-16 (D-113 craft times) will lengthen clay times, so re-run ClayCheck after it lands. Modeling (session_01JKX3FNjkrEkAqoTaFJiXAm) #197 bench model + CookingMeshes (see open questions). Render (session_013qe8zPzzBHx9rFPEYoKMr5) has requests at a432ab6 (bench + cooking pot) and cd30d6f (lamp demo). PM: session_01CvoSQGRe3vDvDXWQu2qruU.

## Progress log
- 2026-10-16 19:35: ClayCheck adapted (claim, stone axe, log, bench before shaping; 5965db1). PASSED: --pot (c8bench1), default pit run (c8bench2), --kiln --jar (c8bench3); each also checks shaping is refused with no bench. Merged main aa3e443 (#105, #119) and crafting-stations 07fd6d0 (a432ab6): table and bench models by station flag; broth and stew are GoodMeals (D-111). Server, client, ClayCheck, CraftCheck build. Upgrade check vs origin/main (with #105) PASSED.
- 2026-10-16 19:10: 08c862d bench (StructureKind 8 after jar 6 and table 7; D-127 cost 1 log + 2 stones; BenchModel; Station on pinch/coil/jar). 3b1f192: model by station flag, `build.bench` for the journal, CraftJob.VesselQuality (Crafting approved) so pots keep their quality. Upgrade check vs origin/main PASSED (no wipe). #105 merged first as agreed. ClayCheck adaptation (claim, axe, log, bench) in progress by a subagent; WIP 2fe3a89.
- 2026-10-16 18:37: Crafting agreed option 1 (cost) and described the framework (crafting-15, branch crafting-stations, sha in a couple of hours): StationKind enum in Processes.cs, ProcessDef.Station, StructureShape.Station, generic claim check in PlaceStructure, ingredients from inventory, nearby stores, then the claim stash.

- 2026-10-16 20:20: new Sonnet session. #160 still open (conflicts with main). Merged origin/main (env-2, #153 fences) into this branch: StructureKind now ...StorageJar 6, Fence 7, Gate 8, CraftingTable 9, PottersBench 10 (enum, Shapes, StageCosts, StructureModel). Server and ClayCheck build. Told Crafting to resolve #160 the same way; asked Animals for a mutton hook. ClayCheck --pot on fresh db c8env2 running.

- 2026-10-16 20:45: #160 and #192 (craft times) on main; merged main (5781182): bench = kind 10 after CraftingTable 9; pinch/coil/jar keep Base() times plus Station. Server, ClayCheck build. ClayCheck --pot PASSED on fresh db (c8final); earlier run on pre-#160 merge also passed. Upgrade check vs origin/main PASSED. Opening the one combined PR now. Mutton for --lamp: Animals says use spear x2 + ButcherCarcass (DebugGiveMeat comes later). Bug fixes #254 adds RequireServer(ctx) to DryClay/FinishFiring/FinishKiln: merge main when it lands.

## Open questions / blockers
- Modeling's `CookingMeshes` (client/scripts/models/CookingMeshes.cs, claude/model-2-env, after #197): once on main, in SimmerView.Show add `new MeshInstance3D { Mesh = CookingMeshes.Under }` as a sibling of the pot body (unscaled) and `CookingMeshes.Floaters(dish, PotMesh.WaterLine(CoilPot).X * PotView.Size)` at the liquid disc's height + 0.002 (PotOfBroth->Broth, PotOfStew->Stew, Tallow->Tallow). Materials are baked in.
- Modeling PR #197 has `StationMeshes.PottersBench()`, a fuller bench model (all stage 0, pegs -1, +Z edge kept clear). Whichever of #197 and this PR merges second switches the StructureModel `PottersBench` case from `BenchModel.Pieces()` to `StationMeshes.PottersBench()`; if it's us, delete client/scripts/ceramics/BenchModel.cs.
- StructureKind order: jar is 6 (#105); Building's #153 appends Fence, Gate; whichever merges second goes after the other, keeping StructureKind, Structures.Shapes and Structures.StageCosts in the same order. The bench takes the next free number at merge time (and ClayRules has KilnStructureKind/JarStructureKind ints that must match).
- Bench cost decided: 1 log + 2 stones (D-127).

## Next step
When #160 is on main: `git merge origin/main` into claude/clay-ceramics-8-kfr4kr, build server/client/ClayCheck, run ClayCheck --pot on a fresh db and tools/upgrade-check.sh origin/main, push, open ONE PR (title \"ceramics-7 + ceramics-8: cooking pot and Potter's bench\"; body: what players get from both tickets, cross-lane hooks incl. CraftJob.VesselQuality (Crafting-approved, no wipe), checks), subscribe to it, and send the PM the green sha.