# building-25: Playtest fixes for building with friends
Lane: House building
Owner: House building thread (session_01DzgG4copWqifKWUcydbjBZ)
Status: in_progress
Branch: claude/building-ht3tnl
PR: (not yet; same PR as building-24)

## Goal
"Home and friends" focus (PM, 2026-10-17). Land the playtest-4 and playtest-5 building fixes that never reached main (/mnt/project-files/patches/building-8/0002 and 0003, numbered building-9 there), rewritten for today's code (E is the Use key, the picture build menu, see-through walls):
- Shift+E at your own unfinished site takes it down (the server's existing `Dismantle`); what went in drops on the ground. The site line shows "Shift+E take down" on your own sites. Others can't (server refuses).
- A click lays the footprint exactly where the cursor is, not where the gliding ghost has got to; the outline is clamped to a spot you can build on without walking (footprint half-size + WorkReach - 0.4 m).
- The door and gate offer reaches 1.8 m from the doorway, not 1.2 m (hard to find in playtest 4).
- A lean-to shelters where its roof is (shared `Structures.Shelters`, used by server and client): out to the open front, 0.3 m short of the low back.
- No footprint over a fire (shared `Structures.FireProblem`, checked on the server too, D-182). A home may go up round its hearth fire well inside its walls; a hearth goes under one. The outline steps back off a fire, and a lean-to outline opens toward the nearest fire within 8 m until you turn it with T.
Dropped from those patches: the old roof fade (replaced by see-through, D-168), "B opens on the first kind" (the picture menu replaced it), the smoke colour (Atmosphere owns smoke now).

## Progress log
- 2026-10-17 16:10: written. Server, client and BuildTest build. BuildTest lean-to gains `LeanToCoverAndFires` (shelter and fire rules) and `TakeDownSite` (B is refused, A takes down their own site); BuildTest's `FindSite` skips spots on a fire. KEYMAP.md Shift+E row mentions the takedown (tell UX through the PM).

## Open questions / blockers
- (none)

## Next step
Run `dotnet run -- lean-to` in tools/BuildTest against a fresh local server, then push and open the PR with building-24.
