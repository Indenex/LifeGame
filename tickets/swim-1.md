# swim-1: Wading and floating train Swimming
Lane: Bug fixes (water movement; Journal and skills defines the skill)
Owner: Bug fixes thread
Status: in_review
Branch: claude/swim-1
PR: (to open)

## Goal
Justin: wading and floating should train a new Swimming skill. The server reports it from where players actually are (like a server-side move.run), so a client cannot fake it.

## Design
- Process ids `swim.wade` (feet on the bed, moving in water) and `swim.float` (afloat past 0.9 m, moving): shared constants `Wading.WadeProcess` / `Wading.FloatProcess`. Not `move.*`: that prefix is Endurance's and would train Endurance.
- Every `Wading.WaterPerUse` (20 s) of moving through water is one report, counted per survival tick (2 s) in `server/spacetimedb/Swimming.cs` (private `WaterTime` table), only while the player moved within the last 3 s.
- One-line hook in `SurvivalTick` (Survival.cs): `NoteSwimming(ctx, player, pos)`.
- JOURNAL AND SKILLS must still add: the Specialization "swimming" ("Swimming", basic, prefix `swim`), names for the two ids, Things-to-try lines, and mark them not Watchable. Until then the reports only journal the process; no skill trains.

## Progress log
- 2026-10-17: implemented; SpawnCheck wades back and forth until the journal shows swim.wade/swim.float (passes locally, 56 s total).

## Next step
Open PR; send the ids to Journal (via the PM); send the green sha to the PM.
