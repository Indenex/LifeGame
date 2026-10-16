# crafting-19: Durability for hand tools and worn equipment (proposal first)
Lane: Crafting
Owner: Crafting thread (handed to a new Sonnet session per D-159, 2026-10-16 20:20)
Status: todo
Branch: crafting-durability
PR: (none yet)

## Goal
Justin, via the PM (2026-10-16 19:48, D-148 follow-up): open fires keep needing feeding. Building tools, stations and structures (tables, benches, kilns, hearths) never wear. Hand tools (knife, axe, bow drill, etc.) and worn equipment get durability. Keep it non-chore (D-148), light (D-081) and start-low (D-075). Values stay hidden: show a worn look or a word, not numbers. FIRST send the PM 2 to 4 options, recommended first, covering:
- the wear rate;
- what happens at zero (it breaks and drops materials, or stops working until repaired);
- repair (by hand with the same materials, and whether quality is lost);
- whether quality affects durability;
- how wear is shown.
Coordinate with Animals (clothing: Garments, worn_item) and Modeling (worn looks). Sequence it after #160, #192 and #194.

Suggested recommended option to start from:
- Each use of a tool for a process (BestTool in StartCraft) wears it a little. A crude tool lasts about 40 uses and an excellent one about 150 (quality raises durability).
- At zero it breaks and drops its head (the flake or hand axe), so re-hafting is one quick craft.
- No repair minigame. Shown as words in the tooltip: "worn", "nearly spent".
Note: InventoryItem stacks by (item, quality) today; per-item wear needs a new column or table, so check the upgrade (no-wipe) rule with tools/upgrade-check.sh.

## Crafting lane state at handoff (2026-10-16 20:20)
Open PRs (all mine; the PM merges on green shas; send them to the PM, session_01CvoSQGRe3vDvDXWQu2qruU):
- #160 crafting-15, stations + Crafting Table (branch crafting-stations, head fb11e89, CI running). StructureKind order: StorageJar 6, Fence 7, Gate 8, CraftingTable 9. When main moves, merge main and append new kinds AFTER main's; never reorder; run tools/upgrade-check.sh origin/main. Stacked on it: Ceramics' bench (claude/clay-ceramics-8-kfr4kr, PottersBench goes after CraftingTable), Animals #90 (Spindle, DryingRack), UX #165 (station panel; also hides station recipes from C), Journal #193 (station hints), Animation (claude/anim-1-stations).
- #192 crafting-16, D-113 craft times (branch crafting-craft-times): GREEN at 7f3e3ee, sent to the PM.
- #194 crafting-17, FireTopUpHook for D-149 hearths (branch crafting-hearth-topup): GREEN at f72c327, sent to the PM. Tell House building (session_01RrKHUxWRH973Ndnxsad9nv) when it merges.
- #203 crafting-18, CraftCheck fire-ground flake fix for Integration (branch crafting-check-fire-spot, head d644116, CI running). Tell Integration (session_01Rf5965nk4Lvd3f2QDteZSq) the green sha.
#160, #192 and #203 all touch tools/CraftCheck/Program.cs; after one merges, merge main into the others (it has always been both-keep).
Merged today: #70 chop wood, #47 fire time left, #92 cold hearths, #97 craft hooks, #122 fires (D-090, D-121, D-109, D-132 hooks), #125 tool speed (D-089).
Waiting on others:
- Render lane (session_013qe8zPzzBHx9rFPEYoKMr5): renders of the cold hearth (11ca4f8) and the crafting table (859f25e) were requested; nothing back yet.
- Bug fixes (session_01HddD17VpF2CmiCxHoPH84Y): adds a `CanStartCraft` refusal hook in Crafting.cs in bugfix-20 (approved; review it).
- Ceramics adds `CraftJob.VesselQuality` (approved; review it; it needs the upgrade check).
- Modeling #197 swaps in HearthModel for FireView (approved), and will swap CraftingTableModel for StationMeshes.CraftingTable after #160.
Local setup that worked: dotnet 8 SDK (dotnet-install.sh into ~/.dotnet, with the wasi-experimental workload installed automatically by the first build), and spacetimedb-cli 2.12.0 from GitHub releases into ~/stdb. Then `spacetimedb-cli start`, tools/gen-bindings.sh, publish, and `dotnet run -c Release` in tools/CraftCheck (about 4 min) or tools/ClayCheck (about 10 min). Use a separate database name per check run.

## Progress log
- 2026-10-16 20:20: ticket created at handoff; nothing built yet.

## Open questions / blockers
- (none yet; the options go to the PM first)

## Next step
Send the PM the green shas of #160 and #203 when CI finishes (merging main first if they conflict), then draft the durability options above and send them to the PM.
