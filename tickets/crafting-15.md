# crafting-15: Crafting stations (D-098)
Lane: Crafting
Owner: Crafting thread
Status: done
Branch: crafting-stations
PR: #160

## Goal
D-098: only basic items and basic material processing stay in the personal C menu. Everything else is made at a station built inside a claim: a Crafting Table (general and misc), a Spindle (yarn and cloth, with Animals' Spin minigame), and a Leather drying rack (hides and leather). A station draws ingredients from the player's inventory and from storage near it. There are no held items; small hand crafts get a hand animation (Animation lane). A new player still reaches a first fired pot in 1 to 2 hours (M2).

## Proposal (sent to the PM 2026-10-16 18:35; waiting on Justin)
Recipes (main plus open branches):
| Recipe | Where |
|---|---|
| gather.tinder, fire.hand_drill, fire.bow_drill | basic (C) |
| knap.flake, knap.hand_axe, carve.digging_stick, cord.twist | basic (C) |
| lash.spear, haft.knife, lash.stone_axe, lash.bow_drill | basic (C), first tools so you survive outside a claim (Q-D) |
| clay.clean, clay.grit, clay.grog, clay.temper_grit, clay.temper_grog, clay.daub | basic (C), material processing |
| clay.pinch_pot, clay.coil_pot | basic (C), so pots never wait on a claim (Q-A; Ceramics' recommendation, 18:22) |
| clay.brick | basic (C), house material |
| chop.log (at a tree), cook.meat, cook.broth, cook.stew (at a fire) | world actions, unchanged; the fire is their station |
| weave.basket, clay.storage_jar, carve.drop_spindle | Crafting Table |
| sew.wool_tunic | Crafting Table |
| cord.spin_wool, weave.wool_cloth | Spindle |
| hide.scrape, hide.stretch (drying), sew.leather_wraps | Leather drying rack |

First claim and table, without a stall:
- The claim marker already costs only 4 Stick + 2 Fiber, at least 40 m from spawn, with one per player.
- The Crafting Table is a new StructureKind with one stage, costing 4 Stick + 2 Cordage, and can only be placed inside a claim you have rights to.
- Path: gather and light a fire (0-10 min), then the marker and table (10-20 min), then clay to a first pinch pot. That is the same as today, plus about 5 minutes.
- The Spindle and the rack are Animals' StructureKinds, also claim-only.

Framework (Crafting):
- `StationKind` {None, Table, Spindle, DryingRack} and `ProcessDef.Station` in shared/Processes.cs.
- StartCraft needs a finished station of that kind within reach, inside a claim the player has rights to.
- Ingredients come from inventory, then from baskets and jars within 4 m of the station, then the claim stash.
- A client helper, `Stations.NearestOf(kind, pos)`, lets Animation turn the figure to face the station.
- Over-time work (drying) goes in a per-station slot table later; Animals keeps hide_rack for now.

Questions for Justin (recommended option first):
- Q-A: all clay working basic, pinch and coil pots included (rec, Ceramics agrees) / pots at the table or a Potter's bench (stronger potter identity, but no pot before a claim).
- Q-B: table costs 4 sticks + 2 cordage (rec; about 2 min) / also a log (needs an axe, +10 min).
- Q-C: station draws from baskets and jars within 4 m plus the claim stash (rec) / anywhere in the claim (simplest, less physical) / the stash only.
- Q-D: hafted tools (spear, knife, axe, bow drill) basic (rec; survive anywhere) / at the table.

## Decided (D-112, 2026-10-16 18:27)
- Shaping pots (pinch, coil, storage jar) moves to a new Potter's Bench station (Ceramics builds it, in ceramics-8). Clay preparation, bricks, tinder, fires, knapping, the digging stick, cordage and the first hafted tools stay basic.
- The Crafting Table costs 4 sticks + 2 cordage + 1 log and holds the basket, drop spindle and wool tunic. Broth, stew and roasting stay at the fire.
- Stations draw from the inventory, storage within 4 m, then the claim stash.
- Bench cost options went to the PM (rec 6 sticks + 2 stones + 4 fiber).

## Built (this PR)
- shared/Processes.cs: `StationKind` {None, Table, PottersBench, Spindle, DryingRack}, `StationInfo` (StorageReach 4 m, names), `ProcessDef.Station`; weave.basket is made at a Table.
- shared/Structures.cs: `StructureShape.Station`; the Crafting table shape (one stage "Table") and its cost. Building.cs: `StructureKind.CraftingTable` (9: after StorageJar 6, Fence 7, Gate 8) and a `CheckStationSite` call in PlaceStructure.
- server/spacetimedb/Stations.cs: the site check (inside a claim you have rights to), `StationNear` (within WorkReach of the edge, in a claim you have rights to), `CountFor`/`TakeFor` (bag, then baskets and jars within 4 m, then the claim stash), and `BesideStation` (where placed things go).
- Crafting.cs StartCraft: station check before anything is used, and counts and takes through the station.
- Client: `Stations.NearestOf/AtPlayer/StoredNear` (client/scripts/crafting/Stations.cs; Animation uses NearestOf), and the panel shows "at a crafting table" with a "build one inside your claim" hint. CraftingTableModel draws four lashed legs with a split log on top.
- CraftCheck: a basket away from a table is refused, and so is a table outside a claim; then the bot claims land 45 m out, builds a table, stashes fiber, and weaves a basket that takes 4 fiber from the stash.

## Progress log
- 2026-10-16 19:30: framework built. CraftCheck, BuildTest and the upgrade check (main's world carries over, no wipe) pass locally, and server and client build.
- 2026-10-16 19:35: PR #160 opened at 859f25e. Render requested 859f25e (crafting table, built, inside a claim).
- 2026-10-16 19:45: merged main (StorageJar 6, CraftingTable 7); each station kind reports its own build id (build.table, build.bench, build.spindle, build.rack) for Journal; the spindle is named "spindle station" (Animals). Upgrade check against origin/main passes; CraftCheck passes.
- 2026-10-16 20:15: merged main again after Building's fence and gate (#153): StorageJar 6, Fence 7, Gate 8, CraftingTable 9. The upgrade check and CraftCheck pass.
- 2026-10-16 18:45: Ceramics asked for the coil pot to stay basic and the jar at the Table; adopted and sent to the PM.
- 2026-10-16 18:35: proposal written and sent to the PM. Told Animals and Animation the planned shape (StationKind, ProcessDef.Station, Stations.NearestOf).

## Open questions / blockers
- StructureKind numbering with Building (asked via the PM): jar 6, table 7, bench 8, spindle 9, rack 10 is the plan. Whichever of this PR and #105 merges second renumbers.
- Known limits: the panel's greyed-out state refreshes on inventory changes, not when you walk up to a table (pressing the number still checks). The client doesn't check claim rights; the server does.

## Next step
None. Merged to main; kept for history.
