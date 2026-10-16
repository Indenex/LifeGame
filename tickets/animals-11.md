# animals-11: Spindle station and Leather drying rack (D-098)
Lane: Animals
Owner: Animals thread (session session_01SAcVBUUfPFFk1rdXuiQZRU)
Status: blocked
Branch: (not started; ticket kept on claude/animals-clothing-5gkntn until work begins)
PR: (none)

## Goal
Per D-098, spinning and weaving happen at a Spindle station, and hide work (scrape, stretch, dry) and later leather work at a Leather drying rack station. Stations can only be built inside a claim and draw inputs from the player's inventory and nearby storage. The Spin minigame (D-070, #90) stays, played at the station. Done when Justin builds both stations in his claim, spins and weaves at one and dries a hide at the other.

## How it works (plan; shapes come from Crafting's station framework)
- Spindle station: `cord.spin_wool` (Spin minigame) and `weave.wool_cloth` need it. Whether the Drop spindle item stays as a tool or becomes part of the station: ask the PM once the framework is known.
- Leather drying rack: `hide.scrape`, the stretch/dry steps (today `hide.stretch` places a `hide_rack` row in Leather.cs, drying 360 s) and later leather pieces. Existing hide_rack rows in Justin's world must keep working (upgrade without wipe).
- Placement and the inside-a-claim rule: from Crafting's framework (likely Building's PlaceStructure plus Claims).

## Progress log
- 2026-10-16 20:15: D-098 and D-099 (seasons later; Animals owns seasonal fauna) relayed by the PM. Asked Crafting (session_01Y2sA7pDBFch57bWb1uKMTq) for the framework's shapes and ETA. Asked the PM (Q8) whether #90 merges now as a hand craft (recommended) or waits.

- 2026-10-16 20:45: Crafting's planned shape (crafting-15, branch crafting-stations, pending Justin): `StationKind` enum in shared/Processes.cs (None, Table, Spindle, DryingRack; append new at end), `ProcessDef.Station` (default None) means 'start only within reach of a finished station of that kind in a usable claim'; placement through Building's PlaceStructure with one StructureKind per station; Crafting.cs CountFor/TakeFor draw inventory then storage near the station; over-time work via a per-station slot table and Outcome.Slot (hide_rack may stay at first). Crafting agrees #90 merges now as a hand craft.

## Open questions / blockers
- Blocked on Crafting's station framework (branch, process-to-station field, placement, nearby-storage input, timed slots).
- Q8 with the PM: merge #90 now or hold.

## Next step
When Crafting replies with the framework branch, branch from it (or main once merged) and add the two station kinds.
