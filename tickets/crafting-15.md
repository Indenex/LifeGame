# crafting-15: Crafting stations (D-098)
Lane: Crafting
Owner: Crafting thread
Status: blocked
Branch: crafting-stations
PR: (none yet)

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
| clay.pinch_pot | basic (C), so the first pot needs no building (Q-A) |
| clay.brick | basic (C), house material |
| chop.log (at a tree), cook.meat (at a fire) | world actions, unchanged |
| weave.basket, clay.coil_pot, clay.storage_jar, carve.drop_spindle | Crafting Table |
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
- Q-A: pinch pot basic (rec; first pot needs no building) / pinch pot at the table (stronger push to claim early).
- Q-B: table costs 4 sticks + 2 cordage (rec; about 2 min) / also a log (needs an axe, +10 min).
- Q-C: station draws from baskets and jars within 4 m plus the claim stash (rec) / anywhere in the claim (simplest, less physical) / the stash only.
- Q-D: hafted tools (spear, knife, axe, bow drill) basic (rec; survive anywhere) / at the table.

## Progress log
- 2026-10-16 18:35: proposal written and sent to the PM. Told Animals and Animation the planned shape (StationKind, ProcessDef.Station, Stations.NearestOf).

## Open questions / blockers
- Waiting on Justin's answers to Q-A to Q-D (via the PM). Coordinate with Building (StructureKind and placement), Claims (in-claim check), Animals (spindle, rack) and UX (station panel).

## Next step
When the PM relays the answers, implement the framework on this branch: StationKind, ProcessDef.Station, the StartCraft check, inventory and storage draw, then CraftCheck. Ping Animals and Animation with the sha.
