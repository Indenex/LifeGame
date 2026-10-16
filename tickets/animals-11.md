# animals-11: Spindle station and Leather drying rack (D-098)
Lane: Animals
Owner: Animals thread (session session_01SAcVBUUfPFFk1rdXuiQZRU)
Status: in_progress
Branch: claude/animals-wool-cloth-5gkntn (ships inside #90 with animals-8, D-120), on top of Crafting's crafting-stations (#160)
PR: #90 (draft until #160 merges)

## Goal
Per D-098, spinning and weaving happen at a Spindle station, and hide work (scrape, stretch, dry) and later leather work at a Leather drying rack station. Stations can only be built inside a claim and draw inputs from the player's inventory and nearby storage. The Spin minigame (D-070, #90) stays, played at the station. Done when Justin builds both stations in his claim, spins and weaves at one and dries a hide at the other.

## How it works
- Kinds (Building.cs, provisional numbers until House building allocates): `SpindleStation`, `DryingRack`, appended after Crafting's `CraftingTable`; Shapes and StageCosts rows in shared/Structures.cs in the same order; client models in `client/scripts/animals/StationModels.cs`, wired in StructureModel's kind switch (7, 8 for now).
- Spindle station (Q10 a, pending Justin): a distaff stand (forked staff in a ring of stones holding fleece, a drop spindle hanging from the thread), a log stool, and a small weaving frame leaning on it. Built from 1 Drop spindle + 4 Sticks + 2 Cordage + 1 Stone. `cord.spin_wool` (Spin minigame, no tool needed now) and `weave.wool_cloth` need it. `carve.drop_spindle` is made at the crafting table.
- Leather drying rack (Q11 a, pending Justin): two forked posts, guy sticks and a 2.5 m bar at the A-frame's bar height. 6 Sticks + 2 Cordage. `hide.scrape` and `hide.stretch` (now Scraped hide + 1 Cordage, no sticks) need it. Stretching laces the hide onto the first free of 3 pegs, 0.75 m apart along the bar (`HideRack.StationId`, `Peg`, both `[Default]`); a full rack hands everything back with a news line. TakeLeather gives back 2 sticks only for an old A-frame rack (StationId 0). Old racks keep drying where they stand.
- Taking a drying rack down (Building's new `AnimalsStructureRemoved` hook, one line in Building.cs) drops its hides: leather if dry, else the scraped hide.
- Client HideRacks.cs: a hide on a station is drawn without its own A-frame.
- Crafting's framework does the claim-only placement, reach check and drawing from nearby storage and the stash.
- `sew.leather_wraps` (in #149) moves to the drying rack after #149 merges and main is merged in.

## Progress log
- 2026-10-16 20:15: D-098 and D-099 (seasons later; Animals owns seasonal fauna) relayed by the PM. Asked Crafting (session_01Y2sA7pDBFch57bWb1uKMTq) for the framework's shapes and ETA. Asked the PM (Q8) whether #90 merges now as a hand craft (recommended) or waits.

- 2026-10-16 20:45: Crafting's planned shape (crafting-15, branch crafting-stations, pending Justin): `StationKind` enum in shared/Processes.cs (None, Table, Spindle, DryingRack; append new at end), `ProcessDef.Station` (default None) means 'start only within reach of a finished station of that kind in a usable claim'; placement through Building's PlaceStructure with one StructureKind per station; Crafting.cs CountFor/TakeFor draw inventory then storage near the station; over-time work via a per-station slot table and Outcome.Slot (hide_rack may stay at first). Crafting agrees #90 merges now as a hand craft.

- 2026-10-16 22:25: Render requested de48c3f (stations, for Q10/Q11).
- 2026-10-16 22:20: built on crafting-stations 6d6668f; server + client build. Local bot test (scratch StationCheck, TEMP give reducer, not committed): spin away from a station refused ("You need to be at a spindle in your claim"); claim, crafting table, drop spindle at the table, spindle station built (spindle consumed), 2 yarn spun and cloth woven there; drying rack built, 4 hides scraped, 3 laced on pegs 0.75 m apart along the bar, the 4th handed back; damp leather refused; dismantling the rack dropped the 3 hides as scraped hides. Upgrade check against main passed.

## Open questions / blockers
- Q10 (Spindle station look/cost) and Q11 (drying rack holds 3 hides on its bar) with the PM/Justin; built on the recommended (a) options.
- StructureKind numbers from House building (session_01RrKHUxWRH973Ndnxsad9nv); asked.
- Building.cs `AnimalsStructureRemoved` hook: tell House building.

## Next step
When House building gives the kind numbers, fix the enum order / Shapes / StageCosts / StructureModel switch; when #160 merges, merge main into this branch and mark #90 ready.
