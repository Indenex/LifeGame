# crafting-19: Durability for hand tools and worn equipment (proposal first)
Lane: Crafting
Owner: Crafting thread (handed to a new Sonnet session per D-159, 2026-10-16 20:20)
Status: done
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
- Animals' red deer (#219, animals-14). Animals adds cook.venison themselves (items 205, 206; process copied from cook.meat). Antler (item 204) needs a use. Send the PM as options (D-059): (a, rec) an antler soft hammer that counts as a Hammer for knapping, with +10 on the knap.* quality cap; (b) a pressure flaker for a new fine-flake recipe; (c) a plain material for now. Animals has been told it goes to Justin.
- Bow and arrow (animals-15): Animals says Crafting owns new item ids Item.Bow and Item.Arrow (Crafting range 1-99; take the next free numbers in shared/Items.cs). Modeling (session_01JKX3FNjkrEkAqoTaFJiXAm) has the models ready in PR #235 and needs the two numbers and the branch.
Local setup that worked: dotnet 8 SDK (dotnet-install.sh into ~/.dotnet, with the wasi-experimental workload installed automatically by the first build), and spacetimedb-cli 2.12.0 from GitHub releases into ~/stdb. Then `spacetimedb-cli start`, tools/gen-bindings.sh, publish, and `dotnet run -c Release` in tools/CraftCheck (about 4 min) or tools/ClayCheck (about 10 min). Use a separate database name per check run.

## Progress log
- 2026-10-16 20:20: ticket created at handoff; nothing built yet.
- 2026-10-16 (Sonnet session): merged main into #160 (958cf73, conflict in shared/Processes.cs resolved both-keep: basket = Base(2, 2.5f) + Station Table) and #203 (b1c3f6f); both pushed, CI running. Sent PM the durability options (4 questions, recommended a/a/a/a). Reviewed bugfix-20 CanStartCraft and Ceramics VesselQuality: both approved, sent to those lanes.

- 2026-10-16 20:15: PM relayed Justin's answers: at zero a tool is SPENT (stops working, not destroyed), repair always cheaper than new (~40-60% of materials), new Repair table station (claim only, StructureKind appended after the other table kinds), per-repair quality loss accumulating to a floor of 50% of original, words + worn look, clothing patched and never breaks, uses-per-life well above 40. Sent PM three number packages (A Steady 150/265/400 uses at q35/65/100, repair 40%, loss 20% of remaining gap; B Brisk 100/170/250, 50%, 5% flat; C Long 250/450/700, 40%, 3% flat). Recommended A. Quality today: cap 35 at rank 0 to 100 at rank 100 (shared/Skills.cs QualityCap).

- 2026-10-16 21:00: Justin picked package A. Built on crafting-durability (stacked on #160 crafting-stations): shared/Durability.cs (all numbers and words), server/spacetimedb/Durability.cs (WearTool, NoToolMessage, RepairTool reducer, admin SetToolWearStep + WearConfig table), uint Wear column (low 16 bits = uses, next 8 = original quality) on InventoryItem, WorldItem, BasketItem, ClaimStash; Drop/PickUp/BasketPut/Take/Spill/Stash/Take/Trade move wear with the tool (FindStack picks the most worn first; MoveCarried/CarriedCount for trades). BestTool skips spent tools, prefers the more worn between equals. Wear hooks: StartCraft (NeedsTool), StrikeAnimal, ButcherCarcass, ShearSheep, Dig (digging stick). Durable tools: stone axe, stone knife, spear, digging stick, bow drill (flakes, hand axes, stones never wear). Repair: cordage x1 for axe/knife/spear/bow drill; digging stick only needs a cutting tool. Repair table = StructureKind.RepairTable (10 on this stack; renumber after merge so nothing reorders), StationKind.Repair, cost 4 sticks, 2 cordage, 1 log, 2 stones. Client: inventory and stash labels carry the word (Rough stone axe (worn)), G at a repair table offers "Mend your ..." (client/scripts/crafting/Mending.cs). Local: server and client build, CraftCheck passes with wear, spent, stash round-trip, both repairs and a mended tool chopping again; tools/upgrade-check.sh origin/main passes (no wipe). CraftCheck calls spacetimedb-cli as admin to set the wear step (set_tool_wear_step), so it needs spacetimedb-cli on PATH, as CI has.

- 2026-10-16: Integration's docs/CONTENT.md rule (content is data): wear and repair moved from switches in shared/Durability.cs to data on ItemDef (Wears, RepairCost, RepairNeedsTool), so adding a wearing tool (the bow) is a Def entry. Behaviour unchanged (CraftCheck and the upgrade check pass). Merged main again (PottersBench took its slot); RepairTable re-appended after it. Merge order per PM: #271, then #267, then #285.

## Open questions / blockers
- Package A is built. Waiting on #160 to merge, then merge main into crafting-durability, renumber StructureKind.RepairTable after whatever the other station lanes hold, open the PR, send CI.
- To tell Modeling: the repair table reuses CraftingTableModel for now; worn looks for tools and patched looks for clothing are theirs. To tell Animals: clothing wear is time worn, words 'worn' then 'patched', never breaks, mending loses no quality; use Durability.Pack/Used/Condition-like helpers when they add Garments.
- To tell Journal: the Things to try line for repair, e.g. 'A tool gone soft in the hand is made good again at a table with a length of cord.'

## Next step
None. Merged to main; kept for history.
